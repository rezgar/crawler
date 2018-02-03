using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Configuration
{
    public class CrawlingConfiguration
    {
        public int MaxSimmultaneousQueueItemsProcessed { get; } = 10;
        public TimeSpan MaxTimeToProcessOneQueueItem { get; } = TimeSpan.FromMinutes(5);
        
        public readonly IDictionary<string, WebsiteConfig> WebsiteConfigs = new Dictionary<string, WebsiteConfig>();

        public IEnumerable<CrawlingQueueItem> GenerateCrawlingQueueItems()
        {
            foreach (var config in WebsiteConfigs.Values)
                foreach (var job in config.Jobs)
                    foreach (var link in job.EntryLinks)
                        yield return new CrawlingQueueItem(link);
        }

        public bool Validate()
        {   
            var result = true;
            foreach(var websiteConfig in WebsiteConfigs.Values)
            {
                if (!websiteConfig.PredefinedValues.Validate())
                {
                    result = false;
                }
            }

            Trace.TraceError("Crawling configuration validation failed for one or more websites");
            return result;
        }
    }
}
