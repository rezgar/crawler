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

namespace Rezgar.Crawler.Download.ResourceLinks
{
    public class DocumentLink : ResourceLink
    {
        public readonly bool ExtractLinks;
        public readonly bool ExtractData;

        /// <summary>
        /// Values, extracted pre Link download
        /// </summary>
        public readonly CollectionDictionary<string, string> ExtractedItems;

        public DocumentLink
        (
            StringWithDependencies url,
            string httpMethod,
            WebsiteJob job,
            bool extractLinks,
            bool extractData,
            CollectionDictionary<string, string> extractedItems,

            DocumentLink referrerDocumentLink = null
        ) 
            : base(url, httpMethod, job)
        {
            ExtractLinks = extractLinks;
            ExtractData = extractData;
            ExtractedItems = extractedItems;

            ReferrerResourceLink = referrerDocumentLink;
        }
        
        public override async Task<IList<ResourceContentUnit>> ProcessWebResponseAsync(WebResponse webResponse)
        {
            var parsedExtractableDocument = Job.Config
                .ParseExtractableDocumentWebResponse(webResponse, this);

            var result = new List<ResourceContentUnit>();

            var links = parsedExtractableDocument.ExtractedLinks.Values.SelectMany(pred => pred).ToList();
            if (links.Count > 0)
                result.Add(new ExtractedLinksUnit(links));

            var data = parsedExtractableDocument.ExtractedItems;
            if (data.Count > 0)
                result.Add(new ExtractedDataUnit(data));

            return result;
        }
    }
}
