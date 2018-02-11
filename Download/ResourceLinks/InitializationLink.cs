using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction.Dependencies;
using Rezgar.Crawler.Download.ResourceContentUnits;
using Rezgar.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Download.ResourceLinks
{
    public class InitializationLink : DocumentLink
    {
        private CrawlingBase _crawlingBase => Job as CrawlingBase ?? Config as CrawlingBase;

        public InitializationLink(
            string url,
            string httpMethod,
            IDictionary<string, string> parameters,
            IDictionary<string, string> headers,
            bool extractLinks,
            bool extractData,
            WebsiteConfig config,
            WebsiteJob job,
            CollectionDictionary<string, string> preExtractedItems = null
        )
            : base(url, httpMethod, parameters, headers, extractLinks, extractData, config, job, preExtractedItems, null)
        {

        }

        public InitializationLink(
            StringWithDependencies urlWithDependencies,
            string httpMethod,
            IDictionary<string, StringWithDependencies> parametersWithDependencies,
            IDictionary<string, StringWithDependencies> headersWithDependencies,
            bool extractLinks,
            bool extractData,
            WebsiteConfig config,
            WebsiteJob job,
            CollectionDictionary<string, StringWithDependencies> preExtractedItemsWithDependencies = null
        )
            : base(urlWithDependencies, httpMethod, parametersWithDependencies, headersWithDependencies, extractLinks, extractData, config, job, preExtractedItemsWithDependencies, null)
        {

        }

        public override async Task<IList<ResourceContentUnit>> ProcessWebResponseAsync(WebResponse webResponse)
        {
            var resourceContentUnits = base.ProcessWebResponseAsync(webResponse);

            foreach (var extractedUnit in await resourceContentUnits)
            {
                switch (extractedUnit)
                {
                    case ExtractedDataUnit extractedDataUnit:
                        foreach (var record in extractedDataUnit.ExtractedData)
                            _crawlingBase.PredefinedValues.Dictionary[record.Key] = record.Value;
                        break;
                    case ExtractedLinksUnit extractedLinkUnit:
                        await Task.WhenAll(
                            extractedLinkUnit.ExtractedLinks
                            .Select(link => CrawlingEngine.CrawlAsync(link, false))
                            .ToArray()
                        );
                        // In case of initialization link, crawl related items immediately (omitting the regular crawling queue)
                        extractedLinkUnit.ExtractedLinks.Clear();
                        break;
                }
            }

            return await resourceContentUnits;
        }

        public override ResourceLink Copy()
        {
            var result = new InitializationLink(
                null,
                HttpMethod,
                null,
                null,
                ExtractLinks,
                ExtractData,
                Config,
                Job,
                null
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
