using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction;
using Rezgar.Crawler.DataExtraction.ExtractionItems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        internal WebsitePredefinedValues PredefinedValues = new WebsitePredefinedValues();

        public IDictionary<string, ExtractionItem> ExtractionItems = new Dictionary<string, ExtractionItem>();

        #endregion

        #region Public methods

        public void PredefineValue(string name, string value)
        {
            PredefinedValues[name] = value;
        }

        public bool Validate()
        {
            var result = true;
            if (!PredefinedValues.Validate())
            {
                Trace.TraceError($"Predefined Values validation failed for WebsiteConfig {Name}");
                result = false;
            }
            
            return result;
        }

        #endregion

        #region Declarations
        
        #endregion
    }
}
