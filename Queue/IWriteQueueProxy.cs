using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Queue
{
    public interface IWriteQueueProxy
    {
        Task EnqueueAsync(CrawlingQueueItem crawlingQueueItem, CancellationTokenSource cts);
    }
}
