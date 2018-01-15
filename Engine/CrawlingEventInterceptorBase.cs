using Rezgar.Crawler.Download.ResourceContentUnits;
using Rezgar.Crawler.Download.ResourceLinks;
using Rezgar.Crawler.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Engine
{
    public abstract class CrawlingEventInterceptorBase: IDisposable // makes sense for some of the implementations, therefore must be implemented in base class to be correctly disposed
    {
        /// <returns>True - continue enqueuing, False - prevent enqueuing</returns>
        public async virtual Task<bool> OnBeforeEnqueueAsync(CrawlingQueueItem crawlingQueueItem) { return true; }
        /// <returns>True - continue item execution, False - ignore this item</returns>
        public async virtual Task<bool> OnAfterDequeueAsync(CrawlingQueueItem crawlingQueueItem) { return true; }
        
        /// <returns>
        ///     True - if download result was successfully processed
        ///     False - download result processing failed (should either reenqueue, trace and continue or use some kind of error queue)
        ///     
        ///     NOTE: If there is a sequence of failed processing results - it's a sign that we should stop crawling this job (either duplicates, or site is down, or we got blocked)
        ///</returns>
        public async virtual Task<bool> OnDataDocumentDownloadedAsync(DocumentLink extractableDocumentLink, ExtractedDataUnit extractedDataUnit, HttpResultUnit httpResultUnit) { return true; }

        /// <returns>True - if file download result was successfully processed, False - download result processing failed (should either reenqueue, trace and continue or do something else)</returns>
        public async virtual Task<bool> OnFileDownloadedAsync(FileLink fileResourceLink, DownloadedFilesUnit downloadedFileUnit, HttpResultUnit httpResultUnit) { return true; }

        public virtual void Dispose()
        {
        }
    }
}
