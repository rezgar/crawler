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
using Rezgar.Crawler.Download.ResourceContentUnits;
using Rezgar.Crawler.Download.ResourceLinks;
using System.Diagnostics;
using Rezgar.Crawler.Engine;
using Rezgar.Utils.Http;
using Nito.AsyncEx;

namespace Rezgar.Crawler
{
    public class CrawlingEngine : IDisposable
    {
        static CrawlingEngine()
        {
            // If not set, defaults to 2. With 2 active conections to a domain, the next one is cancelled and WebResponse gets randomly disposed with no descriptive exception
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
        }

        protected CrawlingParameters _crawlingParameters;
        protected readonly CrawlingEventInterceptorManager _crawlingEventInterceptorManager;

        public CrawlingEngine(CrawlingParameters crawlingParameters, CrawlingEventInterceptorManager crawlingEventInterceptorManager)
        {
            _crawlingParameters = crawlingParameters;
            _crawlingEventInterceptorManager = crawlingEventInterceptorManager;
        }

        public async Task ProcessCrawlingQueueAsync(CrawlingQueue crawlingQueue)
        {
            _crawlingParameters.CancellationTokenSource.Token.Register(() => 
                crawlingQueue.QueueCancellationTokenSource.Cancel()
            );

            var tasksLock = new System.Threading.ReaderWriterLockSlim();
            var tasks = new HashSet<Task>();
            
            var queueItemsProcessingSemaphore = new SemaphoreSlim(crawlingQueue.CrawlingConfiguration.MaxSimmultaneousQueueItemsProcessed / 2, crawlingQueue.CrawlingConfiguration.MaxSimmultaneousQueueItemsProcessed);
            while(await queueItemsProcessingSemaphore.WaitAsync(crawlingQueue.CrawlingConfiguration.MaxTimeToProcessOneQueueItem))
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

                var queueItem = await crawlingQueue.DequeueAsync();
                if (queueItem == null) // Both Local and Proxy queues are depleted
                {
                    // NOTE: If Queue is depleted, we must wait until all running tasks are executed, because they might add new items to queue
                    await Task.WhenAll(tasks.ToArray());

                    // wait for all queue proxies to complete fetching items
                    // TODO: consider locking (multithreading scenario)
                    var queueProxiesPending = crawlingQueue.QueueProxies.Where(queueProxy => queueProxy.IsPending()).ToArray();
                    if (queueProxiesPending.Length > 0)
                        continue;

                    if (crawlingQueue.LocalQueue.Count > 0) 
                        continue;

                    break;
                }

                if (!await _crawlingEventInterceptorManager.OnAfterDequeueAsync(queueItem))
                {
                    // If interceptor returns false, means it's an instruction to ignore this item;
                    continue;
                }

                tasksLock.EnterWriteLock();

                queueItem.ChangeStatus(CrawlingQueueItem.CrawlingStatuses.Downloading);

                tasks.Add(System.Threading.Tasks.TaskExtensions.Unwrap(
                    CrawlAsync(queueItem.ResourceLink)
                        .ContinueWith(async task =>
                        {
                            tasksLock.EnterWriteLock();
                            tasks.Remove(task); // to avoid infinite bloating of the collection
                            tasksLock.ExitWriteLock();

                            try
                            {
                                queueItem.ChangeStatus(CrawlingQueueItem.CrawlingStatuses.Downloaded);

                                if (task.Status == TaskStatus.RanToCompletion)
                                {
                                    var resourceContentUnits = task.Result;
                                    var httpResultUnit = resourceContentUnits.OfType<HttpResultUnit>().Single();

                                    queueItem.ChangeStatus(CrawlingQueueItem.CrawlingStatuses.Processing);

                                    var resourceContentUnitsProcessingCountdown = new AsyncCountdownEvent(resourceContentUnits.Count);
                                    
                                    // Process resource content units extracted from Response
                                    foreach (var resourceContentUnit in resourceContentUnits)
                                    {
                                        switch (resourceContentUnit)
                                        {
                                            case ExtractedLinksUnit extractedLinksUnit:
                                                if (extractedLinksUnit.ExtractedLinks.Count > 0)
                                                {
                                                    var linksProcessingCountdown = new AsyncCountdownEvent(extractedLinksUnit.ExtractedLinks.Count);

                                                    foreach (var extractedLink in extractedLinksUnit.ExtractedLinks)
                                                    {
                                                        var crawlingQueueItem = new CrawlingQueueItem(extractedLink);

                                                        // Do not enqueue item if prevented by any interceptor
                                                        if (!await _crawlingEventInterceptorManager.OnBeforeEnqueueAsync(crawlingQueueItem))
                                                        {
                                                            continue;
                                                        }

                                                        crawlingQueueItem.ProcessingCompleted += () =>
                                                            linksProcessingCountdown.AddCount(1)
                                                        ;
                                                        crawlingQueue.Enqueue(crawlingQueueItem);
                                                    }

                                                    // Wait while all links are processed before releasing the content units semaphore and set Status = Processed for parent
                                                    linksProcessingCountdown.WaitAsync()
                                                        .ContinueWith(linksProcessingTask =>
                                                            resourceContentUnitsProcessingCountdown.AddCount(1)
                                                        );
                                                }
                                                else
                                                    resourceContentUnitsProcessingCountdown.AddCount(1);

                                                // Set Processed status when all extracted links are processed

                                                break;

                                            case ExtractedDataUnit extractedDataUnit:
                                                if (!await _crawlingEventInterceptorManager.OnDataDocumentDownloadedAsync(
                                                        queueItem.ResourceLink, // May be a DocumentLink, or a FrameLink. Not quite intuitive and probably requires redesign.
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
                                                resourceContentUnitsProcessingCountdown.Signal();
                                                break;

                                            case DownloadedFilesUnit downloadedFileUnit:
                                                // If download file is a result of redirection,
                                                // we must either explicitly declare that we're expecting a file, or throw a processing exception
                                                
                                                var fileLink = queueItem.ResourceLink as FileLink;
                                                if (fileLink == null)
                                                {
                                                    Trace.TraceError($"ProcessCrawlingQueueAsync: Downloaded file unit. Resource link is of type {queueItem.ResourceLink.GetType().Name}, expecting FileLink. Preventing processing.");
                                                    break;
                                                }

                                                if (!await _crawlingEventInterceptorManager.OnFileDownloadedAsync(
                                                        fileLink,
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

                                                resourceContentUnitsProcessingCountdown.Signal();
                                                break;

                                            case HttpResultUnit httpResultUnitStub:
                                                // TODO: Determine what we should do if HTTP download failed. Either re-enqueue or ignore, or alert/do something else
                                                switch (httpResultUnitStub.HttpStatus)
                                                {
                                                    //case HttpStatusCode.InternalServerError: // it's likely to repeat within the same run
                                                    case HttpStatusCode.GatewayTimeout:
                                                    case HttpStatusCode.RequestTimeout:
                                                        queueItem.ChangeStatus(CrawlingQueueItem.CrawlingStatuses.NotLinked);
                                                        crawlingQueue.Enqueue(queueItem); // Trying to recrawl item if it failed for some intermitent reason
                                                        break;
                                                    default:
                                                        // We need to invoke ProcessingCompleted only after Data and Links extracted are really processed.
                                                        //queueItem.ChangeStatus(CrawlingQueueItem.CrawlingStatuses.ProcessingCompleted);
                                                        break;
                                                }
                                                resourceContentUnitsProcessingCountdown.Signal();
                                                break;

                                            default:
                                                throw new NotSupportedException();
                                        }
                                    }

                                    // Do not actually wait for related resources processing completion.
                                    // Those might be extracted links or files. No need to hold queue resources while linked units are downloaded
                                    // Set Processed status after all content units were registered and interceptors triggered
                                    await resourceContentUnitsProcessingCountdown.WaitAsync()
                                        .ContinueWith(resourceContentUnitsProcessingTask => 
                                            queueItem.ChangeStatus(CrawlingQueueItem.CrawlingStatuses.Processed)
                                        );
                                }
                                else
                                    Trace.TraceError("CrawlAsync: Failed for queue item {0} with exception [{1}]", queueItem.ResourceLink, task.Exception);
                            }
                            finally
                            {
                                queueItemsProcessingSemaphore.Release();
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

            resourceLink.SetUpWebRequest(webRequest);

            return await System.Threading.Tasks.TaskExtensions.Unwrap(
                    webRequest.GetResponseAsync()
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

                                // Use job-based crawling state, if crawling is based off a job. Otherwise, use config-based.
                                var crawlingState = resourceLink.Job?.CrawlingState ?? resourceLink.Config.CrawlingState;
                                crawlingState.Cookies = webResponse.Cookies;

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
                    }));
        }

        #endregion

        #region Private

        #endregion

        public void Dispose()
        {
            _crawlingEventInterceptorManager.Dispose();
        }
    }
}
