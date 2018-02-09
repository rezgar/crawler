using Rezgar.Crawler.Download;
using Rezgar.Crawler.Download.ResourceLinks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Configuration.WebsiteConfigSections
{
    public class WebsiteJob : CrawlingBase
    {
        #region Variables & Properties

        public string Name = "default";

        public WebsiteConfig Config;

        #endregion

        internal WebsiteJob(WebsiteConfig config)
        {
            Config = config;
        }

        internal WebsiteJob(WebsiteJob template)
            : this(template.Config)
        {
            PredefinedValues = new CrawlingPredefinedValues(template.PredefinedValues);

            foreach (var link in template.EntryLinks)
            {
                var entryLinkCopy = link.Copy();
                entryLinkCopy.Job = this;
                EntryLinks.Add(entryLinkCopy);
            }

            InitializationDocumentLink = template.InitializationDocumentLink?.Copy() as InitializationLink; // is stateless, so we don't have to create a new object
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
