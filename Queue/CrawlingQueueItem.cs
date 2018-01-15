using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.Download;
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

        public CrawlingQueueItem(ResourceLink resourceLink)
        {
            ResourceLink = resourceLink;
        }
    }
}
