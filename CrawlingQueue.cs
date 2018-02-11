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

            // start dependency monitoring for each proxy. 
            // the ones, that don't have dependencies, will immediately exit
            // the others will monitor dependencies and change own status when dependencies are resolved
            foreach (var queueProxy in QueueProxies)
                queueProxy.MonitorDependenciesAsync();
        }

        public void Enqueue(CrawlingQueueItem queueItem)
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

                            lock(LocalQueue)
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

                lock (LocalQueue)
                    LocalQueue.Enqueue(queueItem);
            }
        }

        public async Task<CrawlingQueueItem> DequeueAsync()
        {
            // if too few elements in local queue, request an async load of additional portion from global queue
            // check if 0 elements in queue. if yes, wait a bit for async load to finish and quit if still 0

            if (QueueProxiesAvailable.Any() && LocalQueue.Count <= MinLocalQueueSizeBeforeFetch)
            {
                // NOTE: If there's an AWAIT inside a locking scope, lock gets reset (if we use lock(syncRoot), it's illegal to use await inside)
                //_queueLock.EnterWriteLock();

                foreach(var queueProxy in QueueProxiesAvailable)
                {
                    try
                    {
                        var records = await queueProxy.FetchAsync(MaxLocalQueueSize - LocalQueue.Count, QueueCancellationTokenSource);
                        foreach (var record in records)
                        {
                            record.ChangeStatus(CrawlingQueueItem.CrawlingStatuses.InLocalQueue);
                            lock (LocalQueue)
                                LocalQueue.Enqueue(record);
                        }

                        // Don't fetch all the queues immediately. If queue items are retrieved from one of the queues, proceed to download
                        lock (LocalQueue)
                            if (LocalQueue.Count > MinLocalQueueSizeBeforeFetch)
                                break;
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("CrawlingQueue.DequeueAsync.FetchAsync: Fetch from remote queue failed with exception {0}", ex);
                    }
                }

                //_queueLock.ExitWriteLock();
            }

            CrawlingQueueItem result;

            try
            {
                //_queueLock.EnterReadLock();
                lock (LocalQueue)
                    if (LocalQueue.TryDequeue(out result))
                        return result;
            }
            finally
            {
                //_queueLock.ExitReadLock();
            }

            return null;
        }
    }
}
