using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Queue
{
    public interface ICrawlingQueueProxy
    {
        Task EnqueueAsync(CrawlingQueueItem crawlingQueueItem, CancellationTokenSource cts);
        Task<IList<CrawlingQueueItem>> FetchAsync(int portionSize, CancellationTokenSource cts);
    }
}
