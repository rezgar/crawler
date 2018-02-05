using Rezgar.Crawler.Download;
using Rezgar.Crawler.Download.ResourceLinks;
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

        public string Name = "default";

        public WebsiteConfig Config;

        //public Dictionary<string, CrawlingConditional> Conditionals;
        internal CrawlingPredefinedValues PredefinedValues = new CrawlingPredefinedValues();
        internal IList<ResourceLink> EntryLinks = new List<ResourceLink>();
        internal DocumentLink InitializationDocumentLink;

        internal System.Net.CookieContainer CookieContainer;

        #endregion

        internal WebsiteJob(WebsiteConfig config)
        {
            Config = config;
        }

        internal WebsiteJob(WebsiteJob template)
            : this(template.Config)
        {
            PredefinedValues = new CrawlingPredefinedValues(template.PredefinedValues);
            EntryLinks = template.EntryLinks.ToList();
            foreach (var link in EntryLinks)
                link.Job = this;

            InitializationDocumentLink = template.InitializationDocumentLink; // is stateless, so we don't have to create a new object
            if (InitializationDocumentLink != null)
                InitializationDocumentLink.Job = this;
        }

        #region Public methods

        public WebsiteJob PreDefine(string name, params string[] values)
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
    }
}
