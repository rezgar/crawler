using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rezgar.Crawler.Download;
using Rezgar.Crawler.Queue;
using System.Threading;
using System.Collections.Concurrent;
using System.Net;
using Rezgar.Crawler.DataExtraction;
using Rezgar.Crawler.Download.ResourceContentUnits;
using Rezgar.Crawler.Download.ResourceLinks;
using System.Diagnostics;
using Rezgar.Crawler.Engine;
using Rezgar.Utils.Http;

namespace Rezgar.Crawler
{
    public class CrawlingEngine : IDisposable
    {
        protected CrawlingParameters _crawlingParameters;
        protected readonly CrawlingEventInterceptorManager _crawlingEventInterceptorManager;

        public CrawlingEngine(CrawlingParameters crawlingParameters, CrawlingEventInterceptorManager crawlingEventInterceptorManager)
        {
            _crawlingParameters = crawlingParameters;
            _crawlingEventInterceptorManager = crawlingEventInterceptorManager;
        }

        public async Task ProcessCrawlingQueueAsync(CrawlingQueue crawlingQueue)
        {
            if (!crawlingQueue.CrawlingConfiguration.Validate())
                throw new ArgumentException("Crawling configuration invalid. Crawling not started.");
            
            _crawlingParameters.CancellationTokenSource.Token.Register(() => 
                crawlingQueue.QueueCancellationTokenSource.Cancel()
            );

            var tasksLock = new System.Threading.ReaderWriterLockSlim();
            var tasks = new HashSet<Task>();
            
            var semaphore = new Semaphore(crawlingQueue.CrawlingConfiguration.MaxSimmultaneousQueueItemsProcessed / 2, crawlingQueue.CrawlingConfiguration.MaxSimmultaneousQueueItemsProcessed);
            while(semaphore.WaitOne(crawlingQueue.CrawlingConfiguration.MaxTimeToProcessOneQueueItem))
            {
                if (crawlingQueue.QueueCancellationTokenSource.IsCancellationRequested)
                {
                    await Task.WhenAll(tasks.ToArray());

                    // TODO: Move remaining items from local queue to the distributed queue
                    // TODO: Figure out how to filter out duplicates from the queue? Or should we?
                    //       We will probably have to resort to known urls-based duplicates check
                    //       Because otherwise we will drown in failing sql queries on multiplie machines

                    Trace.TraceWarning("ProcessCrawlingQueueAsync: Queue cancellation requested. Preventing dequeing of new elements. Processing will be shut down after currently executing items are complete.");
                    break;
                }

                var queueItem = crawlingQueue.DequeueAsync();
                if (queueItem == null) // Both Local and Proxy queues are depleted
                {
                    await Task.WhenAll(tasks.ToArray());
                    if (crawlingQueue.LocalQueue.Count == 0) // NOTE: If Queue is depleted, we must wait until all running tasks are executed, because they might add new items to queue
                        break;
                    else
                        continue;
                }

                if (!await _crawlingEventInterceptorManager.OnAfterDequeueAsync(queueItem))
                {
                    // If interceptor returns false, means it's an instruction to ignore this item;
                    continue;
                }

                tasksLock.EnterWriteLock();

                tasks.Add(TaskExtensions.Unwrap(
                    CrawlAsync(queueItem.ResourceLink)
                        .ContinueWith(async task =>
                        {
                            tasksLock.EnterWriteLock();
                            tasks.Remove(task); // to avoid infinite bloating of the collection
                            tasksLock.ExitWriteLock();

                            try
                            {
                                if (task.Status == TaskStatus.RanToCompletion)
                                {
                                    var resourceContentUnits = task.Result;

                                    var httpResultUnit = resourceContentUnits.OfType<HttpResultUnit>().Single();

                                    // Process resource content units extracted from Response
                                    foreach (var resourceContentUnit in resourceContentUnits)
                                    {
                                        switch (resourceContentUnit)
                                        {
                                            case ExtractedLinksUnit extractedLinksUnit:
                                                foreach (var extractedLink in extractedLinksUnit.ExtractedLinks)
                                                {
                                                    var crawlingQueueItem = new CrawlingQueueItem(extractedLink);

                                                    // Do not enqueue item if prevented by any interceptor
                                                    if (!await _crawlingEventInterceptorManager.OnBeforeEnqueueAsync(crawlingQueueItem))
                                                    {
                                                        continue;
                                                    }

                                                    crawlingQueue.EnqueueAsync(crawlingQueueItem);
                                                }
                                                break;

                                            case ExtractedDataUnit extractedDataUnit:
                                                if (!await _crawlingEventInterceptorManager.OnDataDocumentDownloadedAsync(
                                                        queueItem.ResourceLink as DocumentLink,
                                                        extractedDataUnit,
                                                        httpResultUnit
                                                    ))
                                                {
                                                    // If any of interceptors failed to process the download result,
                                                    // AND failed to store download result for later processing
                                                    // we must re-enqueue the item, in order to ensure the results are not lost for good


                                                    // We ignore the item and log the error. Chances are we couldn't process the item for a reason. And repeating would just make it stuck infinitely (re-downloading and re-processing)
                                                    // (WAS) we must re-enqueue the item, in order to ensure the results are not lost for good

                                                    //crawlingQueue.EnqueueAsync(queueItem);
                                                }
                                                break;

                                            case DownloadedFilesUnit downloadedFileUnit:
                                                if (!await _crawlingEventInterceptorManager.OnFileDownloadedAsync(
                                                        queueItem.ResourceLink as FileLink,
                                                        downloadedFileUnit,
                                                        httpResultUnit
                                                    ))
                                                {
                                                    // If any of interceptors failed to process the download result,
                                                    // AND failed to store download result for later processing....

                                                    // We ignore the item and log the error. Chances are we couldn't process the item for a reason. And repeating would just make it stuck infinitely (re-downloading and re-processing)
                                                    // (WAS) we must re-enqueue the item, in order to ensure the results are not lost for good

                                                    //crawlingQueue.EnqueueAsync(queueItem);
                                                }
                                                break;

                                            case HttpResultUnit httpResultUnitStub:
                                                // TODO: Determine what we should do if HTTP download failed. Either re-enqueue or ignore, or alert/do something else
                                                switch (httpResultUnitStub.HttpStatus)
                                                {
                                                    //case HttpStatusCode.InternalServerError: // it's likely to repeat within the same run
                                                    case HttpStatusCode.GatewayTimeout:
                                                    case HttpStatusCode.RequestTimeout:
                                                        crawlingQueue.EnqueueAsync(queueItem); // Trying to recrawl item if it failed for some intermitent reason
                                                        break;
                                                }
                                                break;

                                            default:
                                                throw new NotSupportedException();
                                        }
                                    }
                                }
                                else
                                    Trace.TraceError("CrawlAsync: Failed for queue item {0} with exception [{1}]", queueItem.ResourceLink, task.Exception);
                            }
                            finally
                            {

                                semaphore.Release();
                            }
                        })
                    )
                );

                tasksLock.ExitWriteLock();
            }

            await Task.WhenAll(tasks.ToArray());
        }

        #region Static methods

        public static async Task<IList<ResourceContentUnit>> CrawlAsync(ResourceLink resourceLink, bool processResponse = true)
        {
            var webRequest = WebRequest.Create(resourceLink.Uri) as HttpWebRequest;

            // TODO: Proxy

            resourceLink.Job.Config.CrawlingSettings
                .SetUpWebRequest(webRequest, resourceLink);

            return await
                await webRequest.GetResponseAsync()
                    .ContinueWith(async webResponseTask =>
                    {
                        var result = new List<ResourceContentUnit>();
                        
                        var httpResultUnit = new HttpResultUnit
                        {
                            RequestUrl = resourceLink.Url,
                            Exception = webResponseTask.Exception
                        };
                        
                        if (webResponseTask.Status == TaskStatus.RanToCompletion)
                        {
                            using (var webResponse = webResponseTask.Result as HttpWebResponse)
                            {
                                webRequest.FixCookies(webResponse);

                                httpResultUnit.ResponseUrl = webResponse.ResponseUri.ToString();
                                httpResultUnit.ContentEncoding = webResponse.ContentEncoding;
                                httpResultUnit.ContentLength = webResponse.ContentLength;
                                httpResultUnit.ContentType = webResponse.ContentType;
                                httpResultUnit.Cookies = webResponse.Cookies;
                                httpResultUnit.Headers = webResponse.Headers;
                                httpResultUnit.HttpStatus = webResponse.StatusCode;
                                httpResultUnit.HttpStatusDescription = webResponse.StatusDescription;
                                
                                if (processResponse)
                                {
                                    result.AddRange(await resourceLink.ProcessWebResponseAsync(webResponse));
                                }
                                else
                                {
                                    result.Add(await resourceLink.ReadResponseStringAsync(webResponse));
                                }
                            }
                        }
                        else
                            Trace.TraceError("CrawlAsync.GetWebResponse: Failed for queue item {0} with exception {1}", resourceLink, webResponseTask.Exception);

                        result.Add(httpResultUnit);

                        return result;
                    });
        }

        #endregion

        public void Dispose()
        {
            _crawlingEventInterceptorManager.Dispose();
        }
    }
}
