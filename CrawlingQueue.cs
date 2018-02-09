using Rezgar.Crawler.Download;
using Rezgar.Crawler.Queue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rezgar.Crawler
{
    public class CrawlingQueue
    {
        private const int MaxLocalQueueSize = 500;
        private const int MinLocalQueueSizeBeforeFetch = 20;

        private System.Threading.ReaderWriterLockSlim _queueLock = new System.Threading.ReaderWriterLockSlim(System.Threading.LockRecursionPolicy.SupportsRecursion);

        public readonly ConcurrentQueue<CrawlingQueueItem> LocalQueue = new ConcurrentQueue<CrawlingQueueItem>();

        public readonly IList<QueueProxy> QueueProxies;
        public IEnumerable<IWriteQueueProxy> QueueProxiesWrite => QueueProxies.OfType<IWriteQueueProxy>();
        public IEnumerable<QueueProxy> QueueProxiesAvailable => QueueProxies.Where(pred => pred.Status == QueueProxy.Statuses.Inactive);

        public readonly CrawlingConfiguration CrawlingConfiguration;
        public readonly CrawlingProxyServerManager ProxyServerManager;

        public readonly CancellationTokenSource QueueCancellationTokenSource = new CancellationTokenSource();

        public CrawlingQueue(
            CrawlingConfiguration crawlingConfiguration, 
            CrawlingProxyServerManager proxyServerManager, 
            ICollection<QueueProxy> queueProxies
        )
        {
            CrawlingConfiguration = crawlingConfiguration;
            ProxyServerManager = proxyServerManager;
            QueueProxies = new List<QueueProxy>(queueProxies);
        }

        public void EnqueueAsync(CrawlingQueueItem queueItem)
        {
            var writeQueueProxy = QueueProxiesWrite.FirstOrDefault();

            // if too many in queue already and proxy present, add to proxy (global azure queue), else add to local queue
            if (writeQueueProxy != null && LocalQueue.Count >= MaxLocalQueueSize)
            {
                writeQueueProxy.EnqueueAsync(queueItem, QueueCancellationTokenSource) // No need to wait for the operation to complete successfully
                    .ContinueWith(pred =>
                    {
                        if (pred.Status != TaskStatus.RanToCompletion)
                        {
                            queueItem.ChangeStatus(CrawlingQueueItem.CrawlingStatuses.InLocalQueue);
                            LocalQueue.Enqueue(queueItem);
                        }
                    })
                    .ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                            Trace.TraceError("CrawlingQueue.[CrawlingQueueProxy].EnqueueAsync: Exception while trying to enqueue {0} [{1}]", queueItem.ResourceLink, task.Exception);
                    });
            }
            else
            {
                queueItem.ChangeStatus(CrawlingQueueItem.CrawlingStatuses.InLocalQueue);
                LocalQueue.Enqueue(queueItem);
            }
        }

        public CrawlingQueueItem DequeueAsync()
        {
            // if too few elements in local queue, request an async load of additional portion from global queue
            // check if 0 elements in queue. if yes, wait a bit for async load to finish and quit if still 0

            if (QueueProxiesAvailable.Any() && LocalQueue.Count <= MinLocalQueueSizeBeforeFetch)
            {
                _queueLock.EnterWriteLock();

                var recordsFetched = 0;
                // Wait while at least one of QueueProxy tasks fetches some links to crawl
                var fetchTasks = QueueProxiesAvailable.Select(async queueProxy =>
                {
                    try
                    {
                        var records = await queueProxy.FetchAsync(MaxLocalQueueSize - LocalQueue.Count, QueueCancellationTokenSource);
                        foreach (var record in records)
                        {
                            record.ChangeStatus(CrawlingQueueItem.CrawlingStatuses.InLocalQueue);
                            LocalQueue.Enqueue(record);
                        }
                        recordsFetched += records.Count;
                        return records.Count > 0;
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("CrawlingQueue.DequeueAsync.FetchAsync: Fetch from remote queue failed with exception {0}", ex);
                    }

                    return false;
                })
                .ToArray();

                while(!
                    (fetchTasks.All(fetchTask => fetchTask.Status == TaskStatus.RanToCompletion)
                    || recordsFetched > 0)
                )
                {
                    Task.WaitAny(fetchTasks);
                }
                
                _queueLock.ExitWriteLock();
            }

            CrawlingQueueItem result;

            try
            {
                _queueLock.EnterReadLock();

                if (LocalQueue.TryDequeue(out result))
                    return result;
            }
            finally
            {
                _queueLock.ExitReadLock();
            }

            return null;
        }
    }
}
