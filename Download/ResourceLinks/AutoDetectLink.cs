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
        public readonly CollectionDictionary<string, string> LinkExtractedItems;

        public readonly DocumentLink ReferrerDocumentLink;

        public AutoDetectLink(string url, WebsiteConfig config, ExtractionLink extractionLink, CollectionDictionary<string, string> linkExtractedItems, DocumentLink referrerDocumentLink = null)
            : base(url, extractionLink.HttpMethod, extractionLink.Parameters, config)
        {
            ExtractionLink = extractionLink;
            LinkExtractedItems = linkExtractedItems;
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
                    Config,
                    ExtractionLink.ExtractLinks,
                    ExtractionLink.ExtractData,
                    LinkExtractedItems,
                    ReferrerDocumentLink
                );

                return pageLink.ProcessWebResponseAsync(webResponse);
            }
            else
            {
                var fileLink = new FileLink(
                    Url,
                    ExtractionLink.Parameters,
                    Config,
                    ReferrerDocumentLink
                );

                return fileLink.ProcessWebResponseAsync(webResponse);
            }
        }
    }
}
