using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction;
using Rezgar.Crawler.DataExtraction.Dependencies;
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
        public CollectionDictionary<string, string> PreExtractedItems { get; protected set; }
        public CollectionDictionary<string, StringWithDependencies> PreExtractedItemsWithDependencies { get; protected set; }
        public IDictionary<string, ExtractionItem> ExtractionItemsOverride;

        public DocumentLink
        (
            string url,
            string httpMethod,
            IDictionary<string, string> parameters,
            IDictionary<string, string> headers,
            bool extractLinks,
            bool extractData,
            WebsiteConfig config,
            WebsiteJob job,
            CollectionDictionary<string, string> preExtractedItems,
            DocumentLink referrerDocumentLink = null
        ) 
            : base(url, httpMethod, parameters, headers, config, job, referrerDocumentLink)
        {
            ExtractLinks = extractLinks;
            ExtractData = extractData;
            PreExtractedItems = preExtractedItems;
        }

        public DocumentLink(
            StringWithDependencies urlWithDependencies,
            string httpMethod,
            IDictionary<string, StringWithDependencies> parametersWithDependencies,
            IDictionary<string, StringWithDependencies> headersWithDependencies,
            bool extractLinks,
            bool extractData,
            WebsiteConfig config,
            WebsiteJob job,
            CollectionDictionary<string, StringWithDependencies> preExtractedItemsWithDependencies,
            DocumentLink referrerDocumentLink = null
        )
            : base(urlWithDependencies, httpMethod, parametersWithDependencies, headersWithDependencies, config, job, referrerDocumentLink)
        {
            ExtractLinks = extractLinks;
            ExtractData = extractData;
            PreExtractedItemsWithDependencies = preExtractedItemsWithDependencies;
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
        
        public override void Resolve(DependencyDataSource dependencyDataSource)
        {
            base.Resolve(dependencyDataSource);
            PreExtractedItems = PreExtractedItemsWithDependencies.ToCollectionDictionary(
                pred => pred.Key,
                pred => pred.Value.Select(value => dependencyDataSource.Resolve(value))
            );
        }

        public override ResourceLink Copy()
        {
            var result = new DocumentLink(
                null,
                HttpMethod,
                null,
                null,
                ExtractLinks,
                ExtractData,
                Config,
                Job,
                null,
                ReferrerResourceLink?.Copy() as DocumentLink
            );

            result.CopyBaseData(this);

            if (PreExtractedItems != null)
                result.PreExtractedItems = new CollectionDictionary<string, string>(PreExtractedItems);
            if (PreExtractedItemsWithDependencies != null)
                result.PreExtractedItemsWithDependencies = new CollectionDictionary<string, StringWithDependencies>(PreExtractedItemsWithDependencies);

            result.ExtractionItemsOverride = ExtractionItemsOverride.ToDictionary(pred => pred.Key, pred => pred.Value);

            return result;
        }
    }
}
