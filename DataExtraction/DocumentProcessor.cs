using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.DataExtraction.Dependencies;
using Rezgar.Crawler.DataExtraction.ExtractionItems;
using Rezgar.Crawler.Download;
using Rezgar.Crawler.Download.ResourceContentUnits;
using Rezgar.Crawler.Download.ResourceLinks;
using Rezgar.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Rezgar.Crawler.Configuration.WebsiteConfigSections.WebsiteCrawlingSettings;

namespace Rezgar.Crawler.DataExtraction
{
    using DocumentProcessors;

    public abstract class DocumentProcessor
    {
        protected DocumentLink _documentLink;
        protected DocumentTypes _documentType;

        public readonly DependencyDataSource DependencyDataSource;
        public CollectionDictionary<string, ResourceLink> ExtractedLinks = new CollectionDictionary<string, ResourceLink>();
        public CollectionDictionary<string, ResourceLink> ExtractedFrames = new CollectionDictionary<string, ResourceLink>();
        public CollectionDictionary<string, string> ExtractedItems = new CollectionDictionary<string, string>();

        private CollectionDictionary<string, DocumentProcessor> ContextItemDocumentParsers = new CollectionDictionary<string, DocumentProcessor>();

        protected DocumentProcessor(DocumentLink documentLink, DocumentTypes documentType)
        {
            _documentLink = documentLink;
            _documentType = documentType;

            if (documentLink.PreExtractedItems != null)
                foreach (var kvp in documentLink.PreExtractedItems)
                    ExtractedItems.AddValues(kvp.Key, kvp.Value);

            DependencyDataSource = new DependencyDataSource(
                ExtractedItems,
                documentLink.Config.PredefinedValues,
                documentLink.Job?.PredefinedValues
            );
        }

        #region Public 

        public void Parse()
        {
            ExtractItems();
        }

        #endregion

        #region Protected

        protected abstract IEnumerable<(string ExtractedValue, ResponseParserPositionPointer ExtractedValuePosition)> 
            ExtractItemValuesFromLocation(
                ExtractionLocation location, 
                ResponseParserPositionPointer? relativeLocationBase = null
            );
        
        protected void ExtractItems()
        {
            var extractionItems = _documentLink.ExtractionItemsOverride ?? _documentLink.Config.ExtractionItems;

            foreach (var extractionItem in extractionItems.Values)
            {
                var extract = false;
                switch (extractionItem)
                {
                    case ExtractionFrame extractionFrame:
                        extract = _documentLink.ExtractData; // Only extract frames when we have to extract data. We either need frame as content, or as a source for dependent items extraction. 
                        break;
                    case ExtractionLink extractionLink:
                        extract = _documentLink.ExtractLinks;
                        break;
                    default:
                        extract = _documentLink.ExtractData;
                        break;
                }

                if (extract)
                {
                    ExtractAutoDetect(extractionItem, extractionItems, ExtractedItems);
                }
            }
        }

        protected void ExtractAutoDetect(
            ExtractionItem extractionItem,
            IDictionary<string, ExtractionItem> extractionItems,
            CollectionDictionary<string, string> extractedItems,
            ResponseParserPositionPointer? relativeLocationBase = null)
        {
            if (extractedItems.ContainsKey(extractionItem.Name))
                return;

            // First, extract item as a normal item
            ExtractItem(extractionItem, extractionItems, extractedItems, relativeLocationBase);

            // Then, add specific functionality, like link-scoped items and registering a ExtractedLink entity
            // If item is a link, extract it's link aspect
            ExtractionLink extractionLink;
            if ((extractionLink = extractionItem as ExtractionLink) != null)
            {
                ExtractLink(extractionLink);
            }

            // If item is a frame (which is a link as well), then it's link data has already been extracted an we only need to perform Frame-specific actions, like download and replace values in ExtractedItems
            ExtractionFrame extractionFrame;
            if ((extractionFrame = extractionItem as ExtractionFrame) != null)
            {
                // Frames are stored separated from links, to avoid queuing and download by the crawler
                ExtractedFrames[extractionFrame.Name] = ExtractedLinks[extractionFrame.Name];
                ExtractedLinks.Remove(extractionFrame.Name);

                // TODO: Download frames inline and store them in ExtractedItems (override initially extracted values)
                var frameResourceLinks = ExtractedFrames.GetValues(extractionFrame.Name);
                var frameDownloadTasks = frameResourceLinks
                    .Select(frame => CrawlingEngine.CrawlAsync(frame, false))
                    .ToArray();

                Task.WaitAll(frameDownloadTasks); // We're not in async context, so we'll have to hold this thread until we download all the inline downloads required

                // Replace previously extracted data for the frame with it's downloaded content
                ExtractedItems[extractionFrame.Name] =
                    frameDownloadTasks
                        .SelectMany(frameDownloadTask => frameDownloadTask.Result)
                        .OfType<ResponseStringUnit>()
                        .Select(frameResponse =>
                        {
                            IEnumerable<string> result = new [] { frameResponse.Content };
                            if (extractionFrame.PostProcessOnDownload)
                            {
                                result = PostProcess(result, extractionItem.PostProcessors, DependencyDataSource);
                            }

                            return result.ToArray();
                        })
                        .SelectMany(pred => pred)
                        .ToArray();
            }
        }

        protected void ExtractItem(
            ExtractionItem extractionItem, 
            IDictionary<string, ExtractionItem> extractionItems, 
            CollectionDictionary<string, string> extractedItems,
            ResponseParserPositionPointer? relativeLocationBase = null
        )
        {
            if (extractedItems.ContainsKey(extractionItem.Name))
                return; // already extracted as someone's dependency

            ExtractDependencies(extractionItem, extractionItems, extractedItems, relativeLocationBase);

            var extractedValues = new List<string>();

            // constant value, if specified in config
            if (extractionItem.Value != null)
                extractedValues.Add(DependencyDataSource.Resolve(extractionItem.Value));

            // values, extracted from page using selector
            if (extractionItem.Location != null)
            {
                var documentParsers = new List<DocumentProcessor>();
                // If Context is not specified for item, then item is extracted from base document, without any complexities
                if (extractionItem.Context == null)
                {
                    documentParsers.Add(this);
                }
                else
                {
                    // If Context is specified, we're creating a separate DocumentParser for every context item 
                    // Create once per Context item, not parse the document 10 times for each element extracted
                    var contextItemName = DependencyDataSource.Resolve(extractionItem.Context.ContextItemName);

                    if (!ContextItemDocumentParsers.TryGetValue(contextItemName, out var contextDocumentParsers))
                    {
                        // Generate context parsers for this ContextItem if they have not been generated before
                        ContextItemDocumentParsers[contextItemName]
                            = contextDocumentParsers
                            = new List<DocumentProcessor>();

                        var contextResourceFrames = ExtractedFrames[contextItemName];
                        var contextDocumentLinks = contextResourceFrames.OfType<DocumentLink>().ToArray();

                        Debug.Assert(contextResourceFrames.Count == contextDocumentLinks.Length);

                        var contextDocumentStrings = ExtractedItems.GetValues(contextItemName);

                        Debug.Assert(contextDocumentLinks.Length == contextDocumentStrings.Count);

                        for (var i = 0; i < contextDocumentLinks.Length; i++)
                        {
                            // TODO: Documents and Downloaded strings order is not the same. Refactor.
                            var documentString = contextDocumentStrings[i];
                            var documentLink = contextDocumentLinks[i];

                            var contextDocumentParser =
                                _documentLink.Config.CreateDocumentStringParser(
                                    documentString,
                                    documentLink,
                                    extractionItem.Context.ContextDocumentType
                                );

                            // Reusing document parsers globally inside the config. Otherwise we get infinite recursive
                            contextDocumentParser.ContextItemDocumentParsers = ContextItemDocumentParsers;
                            contextDocumentParser.ExtractedItems = ExtractedItems;
                            contextDocumentParser.ExtractedLinks = ExtractedLinks;
                            contextDocumentParser.ExtractedFrames = ExtractedFrames;

                            contextDocumentParsers.Add(contextDocumentParser);
                            contextDocumentParser.Parse();
                        }
                    }

                    documentParsers.AddRange(contextDocumentParsers);
                }

                foreach (var documentParser in documentParsers)
                    extractedValues.AddRange(
                        documentParser.ExtractItemValuesFromLocation(extractionItem.Location, relativeLocationBase)
                        .Select(pred => pred.ExtractedValue)
                    );
            }

            // TODO: Reconsider architecture
            // Links are not post-processed at all and frames are post-processed on download only
            
            // apply post-processing, if specified
            extractedItems.AddValues(
                extractionItem.Name, 
                extractionItem.PostProcessOnExtraction
                    ? PostProcess(extractedValues, extractionItem.PostProcessors, DependencyDataSource)
                    : extractedValues
            );
        }
        
        protected void ExtractLink(ExtractionLink extractionLink)
        {
            ExtractedLinks.AddValues(extractionLink.Name, ExtractResourceLinks(extractionLink));
        }
        protected IEnumerable<ResourceLink> ExtractResourceLinks(ExtractionLink extractionLink)
        {
            if (ExtractedItems.ContainsKey(extractionLink.Name))
            {
                for (var i = 0; i < ExtractedItems[extractionLink.Name].Count; i++)
                {
                    var linkValue = ExtractedItems[extractionLink.Name][i];
                    var linkInDocumentPositionPointer = new ResponseParserPositionPointer(extractionLink.Location, i);

                    var linkScopedExtractedItems = new CollectionDictionary<string, string>();
                    foreach (var extractionItem in extractionLink.PredefinedExtractionItems.Values)
                    {
                        ExtractItem(
                            extractionItem,
                            extractionLink.PredefinedExtractionItems,
                            linkScopedExtractedItems,

                            extractionLink.IsPredefinedExtractionItemsLocationRelativeToLink
                                ? linkInDocumentPositionPointer
                                : (ResponseParserPositionPointer?)null
                        );
                    }

                    var url = linkValue;

                    ResourceLink resourceLink;
                    switch (extractionLink.Type)
                    {
                        case ExtractionLink.LinkTypes.Document:
                            resourceLink = new DocumentLink(
                                url, 
                                extractionLink.HttpMethod, 
                                DependencyDataSource.Resolve(extractionLink.Parameters),
                                DependencyDataSource.Resolve(extractionLink.Headers),
                                extractionLink.ExtractLinks, 
                                extractionLink.ExtractData, 
                                _documentLink.Config, 
                                _documentLink.Job, 
                                linkScopedExtractedItems, 
                                _documentLink
                            );
                            break;
                        case ExtractionLink.LinkTypes.File:
                            resourceLink = new FileLink(
                                url,
                                DependencyDataSource.Resolve(extractionLink.Parameters),
                                DependencyDataSource.Resolve(extractionLink.Headers), 
                                _documentLink.Config, 
                                _documentLink.Job, 
                                _documentLink
                            );
                            break;
                        case ExtractionLink.LinkTypes.Auto:
                            resourceLink = new AutoDetectLink(
                                linkValue,
                                extractionLink.HttpMethod,
                                DependencyDataSource.Resolve(extractionLink.Parameters),
                                DependencyDataSource.Resolve(extractionLink.Headers),
                                extractionLink.ExtractLinks,
                                extractionLink.ExtractData,
                                _documentLink.Config,
                                _documentLink.Job,
                                linkScopedExtractedItems,
                                _documentLink
                            );
                            break;
                        default:
                            throw new NotSupportedException();
                    }

                    yield return resourceLink;
                }
            }
        }

        #endregion

        #region Utility

        private IEnumerable<string> PostProcess(IEnumerable<string> values, IList<PostProcessor> postProcessors, DependencyDataSource dependencyDataSource)
        {
            // apply post-processing, if specified
            foreach (var value in values)
            {
                var valuesBeingProcessed = new[] {value};

                foreach (var postProcessor in postProcessors)
                {
                    valuesBeingProcessed = postProcessor.Execute(valuesBeingProcessed, dependencyDataSource).ToArray();
                }

                foreach(var valueProcessed in valuesBeingProcessed)
                    yield return valueProcessed;
            }
        }

        private void ExtractDependencies(
            ExtractionItem extractionItem,
            IDictionary<string, ExtractionItem> extractionItems,
            CollectionDictionary<string, string> extractedItems,
            ResponseParserPositionPointer? relativeLocationBase = null
        )
        {
            // TODO:
            // A MAJOR BUG!!!
            // StringWithDependency objects are shared between threads and jobs
            // Therefore, current strategy of "Resolving" it on extraction:
            // 1) Changes instance value for everyone that might be using it
            // 2) "Resolve" stores resolved data inside the object, 
            //    so next resolve with new data does nothing since the string is already resolved

            // Links are extracted as dependencies as well (like normal extraction items)
            // ensure dependencies are extracted
            var stringsWithDependencies = extractionItem.GetStringsWithDependencies();
            foreach (var stringWithDependencies in stringsWithDependencies)
            {
                foreach (var dependencyName in stringWithDependencies.DependencyNames)
                {
                    // dependency may be of PredefinedValues and doesn't require to be extracted from any of page items
                    if (extractionItems.ContainsKey(dependencyName))
                    {
                        ExtractAutoDetect(
                            extractionItems[dependencyName],
                            extractionItems,
                            extractedItems,

                            relativeLocationBase
                        );
                    }
                    else
                    {
                        Debug.Assert(
                            _documentLink.Config.PredefinedValues.Dictionary.ContainsKey(dependencyName) ||
                            (_documentLink.Job?.PredefinedValues.Dictionary.ContainsKey(dependencyName) ?? false)
                        );
                    }
                }
            }
        }

        #endregion

        #region Declarations

        public struct ResponseParserPositionPointer
        {
            public ExtractionLocation Location;
            public int ElementIndex;

            public ResponseParserPositionPointer(ExtractionLocation location, int elementIndex)
            {
                Location = location;
                ElementIndex = elementIndex;
            }
        }

        #endregion
    }

    public static class DocumentParserExtensions
    {
        public static DocumentProcessor CreateDocumentStringParser(this WebsiteConfig websiteConfig, string documentString, DocumentLink documentLink, DocumentTypes documentType)
        {
            switch (documentType)
            {
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.HtmlCSS:
                    return new HtmlLocationCssDocumentProcessor(documentString, documentLink);
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.HtmlXPath:
                    return new HtmlLocationXPathDocumentProcessor(documentString, documentLink);
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.Xml:
                    throw new NotImplementedException();
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.Json:
                    throw new NotImplementedException();

                default:
                    throw new NotSupportedException();
            }
        }
        public static DocumentProcessor CreateWebResponseParser(this WebsiteConfig websiteConfig, WebResponse webResponse, DocumentLink documentLink)
        {
            switch (websiteConfig.CrawlingSettings.DocumentType)
            {
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.HtmlCSS:
                    return new HtmlLocationCssDocumentProcessor(webResponse, documentLink);
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.HtmlXPath:
                    return new HtmlLocationXPathDocumentProcessor(webResponse, documentLink);
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.Xml:
                    throw new NotImplementedException();
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.Json:
                    throw new NotImplementedException();

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
