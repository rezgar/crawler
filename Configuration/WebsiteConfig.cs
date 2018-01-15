using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Configuration
{
    public class WebsiteConfig
    {
        public string Name;
        public WebsiteCrawlingSettings CrawlingSettings;
        public IDictionary<string, WebsiteJob> JobsByName = new Dictionary<string, WebsiteJob>();
        public ICollection<WebsiteJob> Jobs
        {
            get => JobsByName.Values;
        }

        // TODO: Move out of the class
        //public HashSet<string> KnownUrlUniqueParts = new HashSet<string>();

        public IDictionary<string, ExtractionLink> ExtractionLinks = new Dictionary<string, ExtractionLink>();
        public IDictionary<string, ExtractionItem> ExtractionItems = new Dictionary<string, ExtractionItem>();

        //public IDictionary<string, CrawlingConditional> CrawlingConditionals = new Dictionary<string, CrawlingConditional>();
        //public IDictionary<string, CrawlingCustomAction> CrawlingCustomActions = new Dictionary<string, CrawlingCustomAction>();
        //public IDictionary<string, CrawlDataItemExtractionRule>
    }
}
