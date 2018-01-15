﻿using Rezgar.Utils.Serialization;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Rezgar.Crawler.Queue
{
    public class AzureCrawlingQueueProxy : ICrawlingQueueProxy
    {
        protected CloudStorageAccount _azureStorageAccount;
        protected CloudQueueClient _azureQueueClient;
        protected CloudQueue _azureCrawlingQueue;

        public const string AzureCrawlingQueueName = "SharedCrawlingQueue";

        public AzureCrawlingQueueProxy(string azureStorageConnectionString)
        {
            _azureStorageAccount = CloudStorageAccount.Parse(azureStorageConnectionString);
            _azureQueueClient = _azureStorageAccount.CreateCloudQueueClient();

            _azureCrawlingQueue = _azureQueueClient.GetQueueReference(AzureCrawlingQueueName);
        }

        public async Task EnqueueAsync(CrawlingQueueItem crawlingQueueItem, CancellationTokenSource cts)
        {
            // 1. Asign an AsyncState object when creating a task, so that if remote action fails, we still have the object to insert into local queue
            // 2. Use a timeout value for remote operation

            await _azureCrawlingQueue.AddMessageAsync(
                    CloudQueueMessage.CreateCloudQueueMessageFromByteArray(crawlingQueueItem.ToByteArray()), //new CloudQueueMessage(crawlingQueueItem.ToByteArray()),
                    null,
                    null,
                    new QueueRequestOptions
                    {
                    
                    },
                    new OperationContext
                    {
                    
                    },
                    cts.Token
                );
        }

        public Task<IList<CrawlingQueueItem>> FetchAsync(int portionSize, CancellationTokenSource cts)
        {
            throw new NotImplementedException();
        }
    }
}
