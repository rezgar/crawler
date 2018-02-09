using Rezgar.Crawler.Download;
using Rezgar.Crawler.Download.ResourceLinks;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rezgar.Crawler.Configuration
{
    public abstract class CrawlingBase
    {
        internal CrawlingPredefinedValues PredefinedValues = new CrawlingPredefinedValues();
        internal IList<ResourceLink> EntryLinks = new List<ResourceLink>();

        /// <summary>
        /// Link (with items) extracted once per Config execution. 
        /// Used to extract initialization data for PredefinedValues, set session info, login user etc.
        /// </summary>
        internal InitializationLink InitializationDocumentLink = null;

        /// <summary>
        /// Crawling state, persisted across crawling items (cookies, etc.)
        /// If Job is not null, Job-based CrawlingSettings are used instead.
        /// TODO: Inherit both Config and Job from an abstract "CrawlingUnit" class or something, since they have a lot in common
        /// </summary>
        internal Download.CrawlingState CrawlingState = new CrawlingState();
    }
}
