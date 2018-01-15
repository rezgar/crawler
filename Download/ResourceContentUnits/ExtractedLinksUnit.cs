using Rezgar.Crawler.Download.ResourceLinks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Download.ResourceContentUnits
{
    public class ExtractedLinksUnit : ResourceContentUnit
    {
        public IList<ResourceLink> ExtractedLinks;

        public ExtractedLinksUnit(IList<ResourceLink> extractedLinks)
        {
            ExtractedLinks = extractedLinks;
        }
    }
}
