using Rezgar.Crawler.Configuration;
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

        public ConcurrentQueue<CrawlingQueueItem> LocalQueue = new ConcurrentQueue<CrawlingQueueItem>();
        public ICrawlingQueueProxy CrawlingQueueProxy;

        public CrawlingConfiguration CrawlingConfiguration;
        public CrawlingProxyManager CrawlingProxyManager;

        public readonly CancellationTokenSource QueueCancellationTokenSource = new CancellationTokenSource();

        public CrawlingQueue(CrawlingConfiguration crawlingConfiguration, CrawlingProxyManager crawlingProxyManager, ICrawlingQueueProxy crawlingQueueProxy = null)
        {
            CrawlingConfiguration = crawlingConfiguration;
            CrawlingProxyManager = crawlingProxyManager;
            CrawlingQueueProxy = crawlingQueueProxy;
        }

        public void EnqueueAsync(CrawlingQueueItem queueItem)
        {
            // if too many in queue already and proxy present, add to proxy (global azure queue), else add to local queue
            if (CrawlingQueueProxy != null && LocalQueue.Count >= MaxLocalQueueSize)
            {
                CrawlingQueueProxy.EnqueueAsync(queueItem, QueueCancellationTokenSource) // No need to wait for the operation to complete successfully
                    .ContinueWith(pred =>
                    {
                        if (pred.Status != TaskStatus.RanToCompletion)
                        {
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
                LocalQueue.Enqueue(queueItem);
            }
        }

        public CrawlingQueueItem DequeueAsync()
        {
            // if too few elements in local queue, request an async load of additional portion from global queue
            // check if 0 elements in queue. if yes, wait a bit for async load to finish and quit if still 0
            
            if (CrawlingQueueProxy != null && LocalQueue.Count <= MinLocalQueueSizeBeforeFetch)
            {
                _queueLock.EnterWriteLock();

                if (LocalQueue.Count <= MinLocalQueueSizeBeforeFetch) // Double check because threads might be stuck in attempt to enterwritelock, while one thread is fetching data
                {
                    CrawlingQueueProxy.FetchAsync(MaxLocalQueueSize - LocalQueue.Count, QueueCancellationTokenSource)
                        .ContinueWith(task =>
                        {
                            if (task.Status == TaskStatus.RanToCompletion)
                            {
                                foreach (var record in task.Result)
                                {
                                    LocalQueue.Enqueue(record);
                                }

                                _queueLock.ExitWriteLock();
                            }
                            else
                                Trace.TraceError("CrawlingQueue.DequeueAsync.FetchAsync: Fetch from remote queue failed with exception {0}", task.Exception);
                        });
                }
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
