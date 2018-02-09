using Rezgar.Utils.Collections;
using Rezgar.Utils.MessageQueue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Crawler.Queue;

namespace Rezgar.Crawler.Engine.CrawlingEventInterceptors
{
    public class KnownUrlsCrawlingEventInterceptor : KnownDataCrawlingEventInterceptorBase
    {
        public KnownUrlsCrawlingEventInterceptor(IList<string> knownData, IMessageQueue dataSyncMessageQueue) : base(knownData, dataSyncMessageQueue)
        {
        }

        public override async Task<bool> OnBeforeEnqueueAsync(CrawlingQueueItem crawlingQueueItem)
        {
            return AddKnownData(crawlingQueueItem.ResourceLink.Uri.ToString());
        }
    }
}
