using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction;
using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.Download.ResourceContentUnits;
using Rezgar.Utils.Collections;
using Rezgar.Crawler.DataExtraction.ExtractionItems;

namespace Rezgar.Crawler.Download.ResourceLinks
{
    public class DocumentLink : ResourceLink
    {
        public readonly bool ExtractLinks;
        public readonly bool ExtractData;

        /// <summary>
        /// Values, extracted pre Link download
        /// </summary>
        public readonly CollectionDictionary<string, string> PreExtractedItems;
        public IDictionary<string, ExtractionItem> ExtractionItemsOverride;

        public DocumentLink
        (
            StringWithDependencies url,
            string httpMethod,
            IDictionary<string, StringWithDependencies> parameters,
            IDictionary<string, StringWithDependencies> headers,
            WebsiteConfig config,
            WebsiteJob job,
            bool extractLinks,
            bool extractData,

            CollectionDictionary<string, string> preExtractedItems = null,
            DocumentLink referrerDocumentLink = null
        ) 
            : base(url, httpMethod, parameters, headers, config, job, referrerDocumentLink)
        {
            ExtractLinks = extractLinks;
            ExtractData = extractData;
            PreExtractedItems = preExtractedItems;
        }
        
        public override async Task<IList<ResourceContentUnit>> ProcessWebResponseAsync(WebResponse webResponse)
        {
            var webResponseParser = Config
                .CreateWebResponseParser(webResponse, this);

            webResponseParser.Parse();

            var result = new List<ResourceContentUnit>();

            var links = webResponseParser.ExtractedLinks
                .Values
                .SelectMany(pred => pred)
                .ToList();

            if (links.Count > 0)
                result.Add(new ExtractedLinksUnit(links));

            var data = webResponseParser.ExtractedItems;
            if (data.Count > 0)
                result.Add(new ExtractedDataUnit(data));

            return result;
        }
    }
}
