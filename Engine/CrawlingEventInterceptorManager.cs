using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Crawler.Queue;
using System.Diagnostics;
using Rezgar.Crawler.Download.ResourceContentUnits;
using Rezgar.Crawler.Download.ResourceLinks;
using Rezgar.Utils.Collections;
using Rezgar.Crawler.Download;

namespace Rezgar.Crawler.Engine
{
    public class CrawlingEventInterceptorManager : CrawlingEventInterceptorBase
    {
        private readonly CollectionDictionary<int, CrawlingEventInterceptorBase> CrawlingEventInterceptors = new CollectionDictionary<int, CrawlingEventInterceptorBase>();

        public CrawlingEventInterceptorManager()
        {
        }

        #region Events

        public override Task<bool> OnAfterDequeueAsync(CrawlingQueueItem crawlingQueueItem)
        {
            return ExecuteOnAllInterceptorsAsync(eventInterceptor => 
                eventInterceptor.OnAfterDequeueAsync(crawlingQueueItem)
            );
        }

        public override Task<bool> OnBeforeEnqueueAsync(CrawlingQueueItem crawlingQueueItem)
        {
            return ExecuteOnAllInterceptorsAsync(eventInterceptor => 
                eventInterceptor.OnBeforeEnqueueAsync(crawlingQueueItem)
            );
        }

        public override Task<bool> OnDataDocumentDownloadedAsync(ResourceLink resourceLink, ExtractedDataUnit extractedDataUnit, HttpResultUnit httpResultUnit)
        {
            return ExecuteOnAllInterceptorsAsync(eventInterceptor =>
                eventInterceptor.OnDataDocumentDownloadedAsync(resourceLink, extractedDataUnit, httpResultUnit)
            );
        }

        public override Task<bool> OnFileDownloadedAsync(FileLink fileResourceLink, DownloadedFilesUnit downloadedFileUnit, HttpResultUnit httpResultUnit)
        {
            return ExecuteOnAllInterceptorsAsync(eventInterceptor =>
                eventInterceptor.OnFileDownloadedAsync(fileResourceLink, downloadedFileUnit, httpResultUnit)
            );
        }

        #endregion

        #region Public Methods

        public void RegisterInterceptor(CrawlingEventInterceptorBase interceptor, int order = 10)
        {
            CrawlingEventInterceptors.AddValue(order, interceptor);
        }

        #endregion

        #region Private Methods

        private async Task<bool> ExecuteOnAllInterceptorsAsync(Func<CrawlingEventInterceptorBase, Task<bool>> eventInterceptorExecutionFunc)
        {
            foreach(var kvp in CrawlingEventInterceptors.OrderBy(pred => pred.Key))
            {
                var tasks = kvp.Value.Select(eventInterceptor =>
                    ExecuteEventInterceptorMethod(eventInterceptor, eventInterceptorExecutionFunc)
                ).ToArray();

                // NOTE: Without ToArray we enumerate the tasks twice => means we launch tasks twice
                // So not only do we get duplicate execution of code, but the task.Result in tasks.All(...) would throw an exception because it might not be done executing yet (2nd run)
                await Task.WhenAll(tasks);

                if (!tasks.All(task => task.Result))
                    return false;
            }

            return true;
        }

        private async Task<bool> ExecuteEventInterceptorMethod(CrawlingEventInterceptorBase eventInterceptor, Func<CrawlingEventInterceptorBase, Task<bool>> executeMethodFunc)
        {
            return await executeMethodFunc(eventInterceptor)
                .ContinueWith(task =>
                {
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        if (!task.Result)
                        {
                            Trace.TraceWarning("{0}: returned FALSE", eventInterceptor.GetType());
                        }

                        return task.Result;
                    } 
                    else
                    {
                        Trace.TraceError("{0}: Failed with exception {1}", eventInterceptor.GetType(), task.Exception);
                        return false;
                    }
                });
        }

        #endregion

        public override void Dispose()
        {
            foreach (var interceptor in CrawlingEventInterceptors.Values.SelectMany(pred => pred))
                interceptor.Dispose();

            base.Dispose();
        }
    }
}
