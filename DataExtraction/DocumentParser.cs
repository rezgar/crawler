using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction.DocumentParsers;
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
    public abstract class DocumentParser
    {
        protected WebsiteConfig _websiteConfig;
        protected DocumentLink _documentLink;
        protected DocumentTypes _documentType;

        public CollectionDictionary<string, ResourceLink> ExtractedLinks = new CollectionDictionary<string, ResourceLink>();
        public CollectionDictionary<string, ResourceLink> ExtractedFrames = new CollectionDictionary<string, ResourceLink>();
        public CollectionDictionary<string, string> ExtractedItems = new CollectionDictionary<string, string>();

        private CollectionDictionary<string, DocumentParser> ContextItemDocumentParsers = new CollectionDictionary<string, DocumentParser>();

        protected DocumentParser(WebsiteConfig websiteConfig, DocumentLink documentLink, DocumentTypes documentType)
        {
            _websiteConfig = websiteConfig;
            _documentLink = documentLink;
            _documentType = documentType;

            if (documentLink.PreExtractedItems != null)
                foreach (var kvp in documentLink.PreExtractedItems)
                    ExtractedItems.AddValues(kvp.Key, kvp.Value);
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
            var extractionItems = _documentLink.ExtractionItemsOverride ?? _websiteConfig.ExtractionItems;
            foreach (var extractionItem in extractionItems.Values)
            {
                var extract = false;
                switch (extractionItem)
                {
                    case ExtractionFrame extractionFrame:
                        extract = false; // Don't extract specifically. Only if somebody references it explicitly
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
                var frameDownloadTasks = frameResourceLinks.Select(frame => CrawlingEngine.CrawlAsync(frame, false)).ToArray();
                Task.WhenAll(frameDownloadTasks)
                    .Wait(); // We're not in async context, so we'll have to hold this thread until we download all the inline downloads required

                // Replace previously extracted data for the frame with it's downloaded content
                ExtractedItems[extractionFrame.Name] =
                    frameDownloadTasks
                        .SelectMany(frameDownloadTask => frameDownloadTask.Result)
                        .OfType<ResponseStringUnit>()
                        .Select(frameResponse => frameResponse.Content)
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
                extractedValues.Add(extractionItem.Value);

            // values, extracted from page using selector
            if (extractionItem.Location != null)
            {
                var documentParsers = new List<DocumentParser>();
                // If Context is not specified for item, then item is extracted from base document, without any complexities
                if (extractionItem.Context == null)
                {
                    documentParsers.Add(this);
                }
                else
                {
                    // If Context is specified, we're creating a separate DocumentParser for every context item 
                    // Create once per Context item, not parse the document 10 times for each element extracted
                    if (!ContextItemDocumentParsers.TryGetValue(extractionItem.Context.ContextItemName, out var contextDocumentParsers))
                    {
                        // Generate context parsers for this ContextItem if they have not been generated before
                        ContextItemDocumentParsers[extractionItem.Context.ContextItemName]
                            = contextDocumentParsers
                            = new List<DocumentParser>();

                        var contextResourceFrames = ExtractedFrames[extractionItem.Context.ContextItemName];
                        var contextDocumentLinks = contextResourceFrames.OfType<DocumentLink>().ToArray();

                        Debug.Assert(contextResourceFrames.Count == contextDocumentLinks.Length);

                        var contextDocumentStrings = ExtractedItems.GetValues(extractionItem.Context.ContextItemName);

                        Debug.Assert(contextDocumentLinks.Length == contextDocumentStrings.Count);

                        for (var i = 0; i < contextDocumentLinks.Length; i++)
                        {
                            // TODO: Documents and Downloaded strings order is not the same. Refactor.
                            var documentString = contextDocumentStrings[i];
                            var documentLink = contextDocumentLinks[i];

                            var contextDocumentParser =
                                _websiteConfig.CreateDocumentStringParser(
                                    documentString,
                                    documentLink,
                                    extractionItem.Context.ContextDocumentType
                                );

                            // Reusing document parsers globally inside the config. Otherwise we get infinite recursive
                            contextDocumentParser.ContextItemDocumentParsers = ContextItemDocumentParsers;
                            contextDocumentParser.ExtractedItems = ExtractedItems;
                            contextDocumentParser.ExtractedLinks = ExtractedLinks;
                            contextDocumentParser.ExtractedFrames = ExtractedFrames;

                            contextDocumentParser.Parse();
                            contextDocumentParsers.Add(contextDocumentParser);
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

            // apply post-processing, if specified
            foreach (var value in extractedValues)
            {
                var valuesBeingProcessed = new[] { value };

                foreach (var postProcessor in extractionItem.PostProcessors)
                {
                    valuesBeingProcessed = postProcessor.Execute(valuesBeingProcessed).ToArray();
                }

                extractedItems.AddValues(extractionItem.Name, valuesBeingProcessed);
            }
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
                    var config = _documentLink.Config;

                    ResourceLink resourceLink;
                    switch (extractionLink.Type)
                    {
                        case ExtractionLink.LinkTypes.Document:
                            resourceLink = new DocumentLink(url, extractionLink.HttpMethod, extractionLink.Parameters, config, extractionLink.ExtractLinks, extractionLink.ExtractData, linkScopedExtractedItems, _documentLink);
                            break;
                        case ExtractionLink.LinkTypes.File:
                            resourceLink = new FileLink(url, extractionLink.Parameters, config, _documentLink);
                            break;
                        case ExtractionLink.LinkTypes.Auto:
                            resourceLink = new AutoDetectLink(
                                linkValue,
                                config,
                                extractionLink,
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

        private void ExtractDependencies(
            ExtractionItem extractionItem,
            IDictionary<string, ExtractionItem> extractionItems,
            CollectionDictionary<string, string> extractedItems,
            ResponseParserPositionPointer? relativeLocationBase = null
        )
        {
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
                        Debug.Assert(_websiteConfig.PredefinedValues.Dictionary.ContainsKey(dependencyName));
                    }
                }

                if (!stringWithDependencies.HasBeenResolved)
                    if (!stringWithDependencies.Resolve(extractedItems, _websiteConfig.PredefinedValues))
                    {
                        Trace.TraceError("ExtractSingleItem: Could not resolve item {0} with dependencies ({1}) based on extracted items {2}",
                            extractionItem.Name,
                            string.Join(",", stringsWithDependencies.Select(pred => pred.FormatString)),
                            string.Join(",", extractedItems.Select(pred => string.Format("[{0}: {1}]", pred.Key, string.Join(",", pred.Value))))
                        );
                        return;
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
        public static DocumentParser CreateDocumentStringParser(this WebsiteConfig websiteConfig, string documentString, DocumentLink documentLink, DocumentTypes documentType)
        {
            switch (documentType)
            {
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.HtmlCSS:
                    return new HtmlLocationCSSDocumentParser(websiteConfig, documentString, documentLink);
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.HtmlXPath:
                    return new HtmlLocationXPathDocumentParser(websiteConfig, documentString, documentLink);
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.Xml:
                    throw new NotImplementedException();
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.Json:
                    throw new NotImplementedException();

                default:
                    throw new NotSupportedException();
            }
        }
        public static DocumentParser CreateWebResponseParser(this WebsiteConfig websiteConfig, WebResponse webResponse, DocumentLink documentLink)
        {
            switch (websiteConfig.CrawlingSettings.DocumentType)
            {
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.HtmlCSS:
                    return new HtmlLocationCSSDocumentParser(websiteConfig, webResponse, documentLink);
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.HtmlXPath:
                    return new HtmlLocationXPathDocumentParser(websiteConfig, webResponse, documentLink);
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
