using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.DataExtraction.ResponseParsers;
using Rezgar.Crawler.Download;
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

namespace Rezgar.Crawler.DataExtraction
{
    public abstract class ResponseParserBase
    {
        protected WebResponse _webResponse;
        protected WebsiteConfig _websiteConfig;
        protected DocumentLink _extractableDocumentLink;

        public CollectionDictionary<string, ResourceLink> ExtractedLinks = new CollectionDictionary<string, ResourceLink>();
        public CollectionDictionary<string, string> ExtractedItems = new CollectionDictionary<string, string>();

        protected ResponseParserBase(WebsiteConfig websiteConfig, WebResponse webResponse, DocumentLink extractableDocumentLink)
        {
            _websiteConfig = websiteConfig;
            _webResponse = webResponse;
            _extractableDocumentLink = extractableDocumentLink;

            foreach (var kvp in extractableDocumentLink.ExtractedItems)
                ExtractedItems.AddValues(kvp.Key, kvp.Value);
        }

        protected abstract IEnumerable<(string ExtractedValue, ResponseParserPositionPointer ExtractedValuePosition)> 
            ExtractItemValuesFromLocation(
                ExtractionLocation location, 
                ResponseParserPositionPointer? relativeLocationBase = null
            );
        
        protected void ExtractItems()
        {
            if (_extractableDocumentLink.ExtractLinks)
            {
                foreach(var extractionLink in _websiteConfig.ExtractionLinks.Values)
                {
                    ExtractLink(extractionLink);
                }
            }

            if (_extractableDocumentLink.ExtractData)
            {
                foreach (var extractionItem in _websiteConfig.ExtractionItems.Values)
                {
                    ExtractItem(
                        extractionItem, 
                        _websiteConfig.ExtractionItems, 
                        ExtractedItems
                    );
                }
            }
        }

        protected void ExtractLink(
            ExtractionLink extractionLink
        )
        {
            var extractedLinkValuesWithPosition = new List<(string ExtractedValue, ResponseParserPositionPointer? ExtractedValuePosition)>();

            if (extractionLink.Value != null)
                extractedLinkValuesWithPosition.Add((extractionLink.Value, null));

            if (extractionLink.Location != null)
            {
                foreach(var extractedLinkValueWithPostition in ExtractItemValuesFromLocation(extractionLink.Location))
                    extractedLinkValuesWithPosition.Add(extractedLinkValueWithPostition);
            }

            var extractedLinkUrlIndex = 0;
            foreach (var extractedLinkValueWithPosition in extractedLinkValuesWithPosition)
            {
                var linkExtractedItems = new CollectionDictionary<string, string>();
                foreach (var extractionItem in extractionLink.ExtractionItems.Values)
                {
                    ExtractItem(
                        extractionItem, 
                        extractionLink.ExtractionItems, 
                        linkExtractedItems,

                        extractedLinkValueWithPosition.ExtractedValuePosition
                    );
                }

                var url = extractedLinkValueWithPosition.ExtractedValue;
                var job = _extractableDocumentLink.Job;

                ResourceLink link;
                switch(extractionLink.Type)
                {
                    case ExtractionLink.LinkTypes.Document:
                        link = new DocumentLink(url, extractionLink.HttpMethod, job, extractionLink.ExtractLinks, extractionLink.ExtractData, linkExtractedItems, _extractableDocumentLink);
                        break;
                    case ExtractionLink.LinkTypes.File:
                        link = new FileLink(url, job, _extractableDocumentLink);
                        break;
                    case ExtractionLink.LinkTypes.Auto:
                        link = new AutoDetectLink(
                            extractedLinkValueWithPosition.ExtractedValue,
                            _extractableDocumentLink.Job,
                            extractionLink,
                            linkExtractedItems,
                            _extractableDocumentLink
                        );
                        break;
                    default:
                        throw new NotSupportedException();
                }

                ExtractedLinks.AddValue(
                    extractionLink.Name,
                    link
                );

                extractedLinkUrlIndex++;
            }
        }

        protected void ExtractItem(
            ExtractionItem extractionItem, 
            IDictionary<string, ExtractionItem> extractionItems, 
            CollectionDictionary<string, string> extractedItems,
            ResponseParserPositionPointer? relativeLocationBase = null
        )
        {
            // ensure dependencies are extracted
            var stringsWithDependencies = extractionItem.GetStringsWithDependencies();
            foreach (var stringWithDependencies in stringsWithDependencies)
            {
                foreach (var dependencyName in stringWithDependencies.DependencyNames)
                {
                    ExtractItem(
                        extractionItems[dependencyName], 
                        extractionItems, 
                        extractedItems,
                        
                        relativeLocationBase
                    );
                }
                
                if (!stringWithDependencies.HasBeenResolved)
                    if (!stringWithDependencies.Resolve(extractedItems, _websiteConfig.GlobalItems))
                    {
                        Trace.TraceError("ExtractSingleItem: Could not resolve item {0} with dependencies ({1}) based on extracted items {2}",
                            extractionItem.Name,
                            string.Join(",", stringsWithDependencies.Select(pred => pred.FormatString)),
                            string.Join(",", extractedItems.Select(pred => string.Format("[{0}: {1}]", pred.Key, string.Join(",", pred.Value))))
                        );
                        return;
                    }
            }

            if (extractedItems.ContainsKey(extractionItem.Name))
                return; // already extracted as someone's dependency

            var extractedValues = new List<string>();

            if (extractionItem.Value != null)
                extractedValues.Add(extractionItem.Value);

            if (extractionItem.Location != null)
                extractedValues.AddRange(
                    ExtractItemValuesFromLocation(extractionItem.Location, relativeLocationBase)
                    .Select(pred => pred.ExtractedValue)
                );

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

    public static class ResponseParserExtensions
    {
        public static ResponseParserBase ParseExtractableDocumentWebResponse(this WebsiteConfig websiteConfig, WebResponse webResponse, DocumentLink extractableDocumentLink)
        {
            switch (websiteConfig.CrawlingSettings.DocumentType)
            {
                case Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.Html:
                    return new HtmlResponseParser(websiteConfig, webResponse, extractableDocumentLink);
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
