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
        #region Constants

        #endregion

        #region Fields and properties

        public string Name;
        public WebsiteCrawlingSettings CrawlingSettings;
        public IDictionary<string, WebsiteJob> JobsByName = new Dictionary<string, WebsiteJob>();
        public ICollection<WebsiteJob> Jobs
        {
            get => JobsByName.Values;
        }

        public IDictionary<string, string> GlobalItems = new Dictionary<string, string>();

        public IDictionary<string, ExtractionItem> ExtractionItems = new Dictionary<string, ExtractionItem>();
        public IDictionary<string, ExtractionLink> ExtractionLinks = new Dictionary<string, ExtractionLink>();

        #endregion

        #region Public methods

        #endregion

        #region Declarations

        public static class DefaultItems
        {
            public const string USER_NAME = "user_name";
            public const string PASSWORD = "password";
            public const string REQUEST_VERIFICATION_TOKEN = "request_verification_token";
        }

        #endregion
    }
}
