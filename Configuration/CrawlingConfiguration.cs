﻿using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.Queue;
using System;
using System.Collections.Generic;
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
                    foreach (var entryItem in job.EntryCrawlingQueueItems)
                        yield return entryItem;
        }
    }
}
