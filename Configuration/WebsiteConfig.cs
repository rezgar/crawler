using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction.ExtractionItems;
using Rezgar.Crawler.Download;
using Rezgar.Crawler.Download.ResourceLinks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

        internal CrawlingPredefinedValues PredefinedValues = new CrawlingPredefinedValues();
        internal IList<ResourceLink> EntryLinks = new List<ResourceLink>();
        /// <summary>
        /// Link (with items) extracted once per Config execution. 
        /// Used to extract initialization data for PredefinedValues, set session info, login user etc.
        /// </summary>
        internal DocumentLink InitializationDocumentLink;
        internal IDictionary<string, ExtractionItem> ExtractionItems = new Dictionary<string, ExtractionItem>();

        #endregion

        #region Public methods

        public WebsiteConfig PreDefine(string name, params string[] values)
        {
            PredefinedValues.Dictionary[name] = values.ToList();
            return this;
        }

        public string GetPredefined(string name)
        {
            return GetPredefinedCollection(name).SingleOrDefault();
        }
        public IList<string> GetPredefinedCollection(string name)
        {
            return PredefinedValues.Dictionary.GetValues(name);
        }

        #endregion

        #region Declarations

        #endregion
    }
}
