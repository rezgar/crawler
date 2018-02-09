﻿using Rezgar.Crawler.Download;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Queue
{
    public class CrawlingQueueItem
    {
        public ResourceLink ResourceLink;
        public CrawlingStatuses Status { get; private set; } = CrawlingStatuses.NotLinked;

        public event Action QueuedInProxyQueue;
        public event Action QueuedInLocalQueue;
        public event Action ProcessingStarted;
        public event Action ProcessingCompleted;

        public CrawlingQueueItem(ResourceLink resourceLink)
        {
            ResourceLink = resourceLink;
        }

        public CrawlingQueueItem ChangeStatus(CrawlingStatuses status)
        {
            Status = status;
            switch(status)
            {
                case CrawlingStatuses.InProxyQueue:
                    QueuedInProxyQueue?.Invoke();
                    break;
                case CrawlingStatuses.InLocalQueue:
                    QueuedInLocalQueue?.Invoke();
                    break;
                case CrawlingStatuses.Processing:
                    ProcessingStarted?.Invoke();
                    break;
                case CrawlingStatuses.ProcessingCompleted:
                    ProcessingCompleted?.Invoke();
                    break;
                default:
                    throw new NotSupportedException();
            }
            return this;
        }

        public enum CrawlingStatuses
        {
            NotLinked,
            InProxyQueue,
            InLocalQueue,
            Processing,
            ProcessingCompleted
        }
    }
}
