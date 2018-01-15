using Rezgar.Crawler.Download;
using Rezgar.Crawler.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Configuration.WebsiteConfigSections
{
    public class WebsiteJob
    {
        #region Variables & Properties

        public string Name;

        public WebsiteConfig Config;
        //public Dictionary<string, CrawlingConditional> Conditionals;
        public IList<CrawlingQueueItem> EntryCrawlingQueueItems = new List<CrawlingQueueItem>();
        
        public System.Net.CookieContainer CookieContainer;

        #endregion

        public WebsiteJob(WebsiteConfig config)
        {
            Config = config;
        }
    }
}
