using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction;
using Rezgar.Crawler.DataExtraction.ExtractionItems;
using Rezgar.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Download.ResourceLinks
{
    public class AutoDetectLink : ResourceLink
    {
        public readonly ExtractionLink ExtractionLink;
        public readonly CollectionDictionary<string, string> PreExtractedItems;

        public readonly DocumentLink ReferrerDocumentLink;

        public AutoDetectLink(string url, WebsiteConfig config, ExtractionLink extractionLink, CollectionDictionary<string, string> preExtractedItems, DocumentLink referrerDocumentLink = null)
            : base(url, extractionLink.HttpMethod, extractionLink.Parameters, extractionLink.Headers, config)
        {
            ExtractionLink = extractionLink;
            PreExtractedItems = preExtractedItems;
            ReferrerDocumentLink = referrerDocumentLink;
        }

        public override Task<IList<ResourceContentUnit>> ProcessWebResponseAsync(WebResponse webResponse)
        {
            var contentType = webResponse.Headers[HttpRequestHeader.ContentType];

            if (contentType.Contains("text/html"))
            {
                var pageLink = new DocumentLink(
                    Url,
                    ExtractionLink.HttpMethod,
                    ExtractionLink.Parameters,
                    ExtractionLink.Headers,
                    Config,
                    ExtractionLink.ExtractLinks,
                    ExtractionLink.ExtractData,
                    PreExtractedItems,
                    ReferrerDocumentLink
                );

                return pageLink.ProcessWebResponseAsync(webResponse);
            }
            else
            {
                var fileLink = new FileLink(
                    Url,
                    ExtractionLink.Parameters,
                    ExtractionLink.Headers,
                    Config,
                    ReferrerDocumentLink
                );

                return fileLink.ProcessWebResponseAsync(webResponse);
            }
        }
    }
}
