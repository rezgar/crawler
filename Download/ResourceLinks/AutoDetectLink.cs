using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction.Dependencies;
using Rezgar.Crawler.DataExtraction.ExtractionItems;
using Rezgar.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Download.ResourceLinks
{
    public class AutoDetectLink : ResourceLink
    {
        public CollectionDictionary<string, string> PreExtractedItems { get; protected set; }
        public CollectionDictionary<string, StringWithDependencies> PreExtractedItemsWithDependencies { get; protected set; }
        public readonly bool ExtractLinks;
        public readonly bool ExtractData;

        public new DocumentLink ReferrerResourceLink
        {
            get => base.ReferrerResourceLink as DocumentLink;
            set => base.ReferrerResourceLink = value;
        }

        public AutoDetectLink(
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
        
        public AutoDetectLink(
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

        public override Task<IList<ResourceContentUnit>> ProcessWebResponseAsync(WebResponse webResponse)
        {
            var contentType = webResponse.Headers[HttpRequestHeader.ContentType];

            if (contentType.Contains("text/html"))
            {
                var pageLink = new DocumentLink(
                    Url,
                    HttpMethod,
                    Parameters,
                    Headers,
                    ExtractLinks,
                    ExtractData,
                    Config,
                    Job,
                    PreExtractedItems,
                    ReferrerResourceLink
                );

                return pageLink.ProcessWebResponseAsync(webResponse);
            }
            else
            {
                var fileLink = new FileLink(
                    Url,
                    Parameters,
                    Headers,
                    Config,
                    Job,
                    ReferrerResourceLink
                );

                return fileLink.ProcessWebResponseAsync(webResponse);
            }
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
            var result = new AutoDetectLink(
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

            return result;
        }
    }
}
