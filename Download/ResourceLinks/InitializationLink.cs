using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction;
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
            StringWithDependencies url, 
            string httpMethod, 
            IDictionary<string, StringWithDependencies> parameters, 
            IDictionary<string, StringWithDependencies> headers, 
            
            WebsiteConfig config, 
            WebsiteJob job, 
            bool extractLinks, 
            bool extractData, 
            CollectionDictionary<string, string> preExtractedItems = null
        ) 
            : base(url, httpMethod, parameters, headers, config, job, extractLinks, extractData, preExtractedItems, null)
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
                Url,
                HttpMethod,
                Parameters?.ToDictionary(pred => pred.Key, pred => pred.Value),
                Headers?.ToDictionary(pred => pred.Key, pred => pred.Value),
                Config,
                Job,
                ExtractLinks,
                ExtractData,
                PreExtractedItems != null ? new CollectionDictionary<string, string>(PreExtractedItems) : null
            );

            result.ExtractionItemsOverride = ExtractionItemsOverride.ToDictionary(pred => pred.Key, pred => pred.Value);

            return result;
        }
    }
}
