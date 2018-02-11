using Nito.AsyncEx;
using Rezgar.Crawler.Download;
using Rezgar.Crawler.Download.ResourceLinks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Queue.QueueProxies
{
    public class CollectionCrawlingQueueProxy : QueueProxy
    {
        // NOTE: Can not be changed, added/removed externally.
        // That means that there will never be new items added and the status flow is static

        protected InitializationLink InitializationLink { get; private set; } = null;
        protected IList<CrawlingQueueItem> QueueItems { get; private set; } = new List<CrawlingQueueItem>();
        protected IEnumerable<CrawlingQueueItem> QueueItemsAvailable => QueueItems.Where(pred => pred.Status == CrawlingQueueItem.CrawlingStatuses.NotLinked);
        
        public CollectionCrawlingQueueProxy(InitializationLink initializationLink, ICollection<ResourceLink> resourceLinks, params CollectionCrawlingQueueProxy[] dependencies)
            : base(dependencies)
        {
            if (initializationLink != null)
                InitializationLink = initializationLink;

            foreach (var resourceLink in resourceLinks)
                QueueItems.Add(new CrawlingQueueItem(resourceLink));
        }

        public override async Task<IList<CrawlingQueueItem>> FetchAsync(int portionSize, CancellationTokenSource cts)
        {
            ChangeStatus(Statuses.Fetching);
            
            // On first fetch returns only InitializationQueueItem.
            if (InitializationLink != null)
            {
                try
                {
                    await CrawlingEngine.CrawlAsync(InitializationLink);
                }
                catch(Exception ex)
                {
                    Trace.TraceError($"{GetType().Name}.FetchAsync: Initialization failed for link {InitializationLink.Url} (Config: {InitializationLink.Config.Name}, Job: {InitializationLink.Job?.Name}) with exception [{ex}]");
                    ChangeStatus(Statuses.Error);
                    return new CrawlingQueueItem[] { };
                }
            }

            var queueItems = QueueItemsAvailable.ToArray();

            if (queueItems.Length > 0)
            {
                var queueItemsCountdown = new AsyncCountdownEvent(queueItems.Length);

                foreach (var queueItem in queueItems)
                {
                    queueItem.ProcessingCompleted += () =>
                    {
                        queueItemsCountdown.Signal();
                    };
                }

                queueItemsCountdown
                    .WaitAsync()
                    .ContinueWith(allQueuedItemsCompletedTask =>
                    {
                        ChangeStatus(Statuses.Depleted);
                    });
            }
            else
                ChangeStatus(Statuses.Depleted);

            // TODO: Add predefined values validation for config/job after initialization and before crawling EntryLinks.
            return queueItems;
        }
    }
}
