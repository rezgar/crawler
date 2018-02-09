using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction.ExtractionItems;
using Rezgar.Crawler.Download;
using Rezgar.Crawler.Download.ResourceLinks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rezgar.Crawler.Configuration
{
    public class WebsiteConfig : CrawlingBase
    {
        #region Constants

        #endregion

        #region Fields and properties

        public string Name;
        public WebsiteCrawlingSettings CrawlingSettings;

        internal int? JobsProcessedInParallel;
        internal IDictionary<string, WebsiteJob> JobsByName = new Dictionary<string, WebsiteJob>();
        internal ICollection<WebsiteJob> Jobs
        {
            get => JobsByName.Values;
        }

        internal IDictionary<string, WebsiteJob> JobTemplatesByName = new Dictionary<string, WebsiteJob>();
        internal ICollection<WebsiteJob> JobTemplates
        {
            get => JobTemplatesByName.Values;
        }
        internal WebsiteJob JobTemplate
        {
            get => JobTemplates.SingleOrDefault();
        }

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

        /// <summary>
        /// TODO: Can probably expose EntryLinks. Required to use job templates on external job creation.
        /// </summary>
        public WebsiteJob RegisterJob(string name, string templateName = null)
        {
            if (templateName == null || !JobTemplatesByName.TryGetValue(templateName, out var template))
            {
                template = JobTemplate;
            }

            // will throw if no name provided and there are multiple templates registered

            var result = new WebsiteJob(template);
            result.Name = name;

            JobsByName.Add(name, result);
            return result;
        }

        #endregion
    }
}
