using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction.ExtractionItems;
using Rezgar.Crawler.Download.ResourceLinks;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rezgar.Crawler.Configuration
{
    public class WebsiteConfig
    {
        #region Constants

        #endregion

        #region Fields and properties

        public string Name;
        public WebsiteCrawlingSettings CrawlingSettings;

        // NOTE: Decided to keep the JOBS section, can be useful in parallel website processing
        public IDictionary<string, WebsiteJob> JobsByName = new Dictionary<string, WebsiteJob>();
        public ICollection<WebsiteJob> Jobs
        {
            get => JobsByName.Values;
        }

        internal WebsitePredefinedValues PredefinedValues = new WebsitePredefinedValues();

        /// <summary>
        /// Link (with items) extracted once per Config execution. 
        /// Used to extract initialization data for PredefinedValues, set session info, login user etc.
        /// </summary>
        public DocumentLink InitializationDocumentLink;

        public IDictionary<string, ExtractionItem> ExtractionItems = new Dictionary<string, ExtractionItem>();

        #endregion

        #region Public methods

        public void PredefineValue(string name, params string[] value)
        {
            PredefinedValues.Dictionary[name] = value;
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
