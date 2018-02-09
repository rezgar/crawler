using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.Queue;
using Rezgar.Crawler.Queue.QueueProxies;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler
{
    public class CrawlingConfiguration
    {
        public int MaxSimmultaneousQueueItemsProcessed { get; } = 10;
        public TimeSpan MaxTimeToProcessOneQueueItem { get; } = TimeSpan.FromMinutes(5);
        
        public readonly IDictionary<string, WebsiteConfig> WebsiteConfigs = new Dictionary<string, WebsiteConfig>();

        public IEnumerable<QueueProxy> GenerateCrawlingQueueProxies()
        {
            foreach (var config in WebsiteConfigs.Values)
            {
                var configQueueProxy = new CollectionCrawlingQueueProxy(config.InitializationDocumentLink, config.EntryLinks);
                yield return configQueueProxy;

                // All jobs depend on config. First, config InitializationLink and site-wide entry links are downloaded
                // Only then, Jobs' links download starts
                var jobSequenceQueueProxies = new List<CollectionCrawlingQueueProxy>
                {
                    configQueueProxy
                };

                var jobsProcessedInParallel = 0;

                foreach(var job in config.Jobs)
                {
                    jobsProcessedInParallel++;

                    CollectionCrawlingQueueProxy jobQueueProxy;
                    if (jobsProcessedInParallel > config.JobsProcessedInParallel)
                    {
                        jobQueueProxy = new CollectionCrawlingQueueProxy(job.InitializationDocumentLink, job.EntryLinks, jobSequenceQueueProxies.ToArray());
                    }
                    else
                    {
                        jobQueueProxy = new CollectionCrawlingQueueProxy(job.InitializationDocumentLink, job.EntryLinks, configQueueProxy);
                    }

                    jobSequenceQueueProxies.Add(
                        jobQueueProxy
                    );

                    yield return jobQueueProxy;
                }
            }
        }
    }
}
