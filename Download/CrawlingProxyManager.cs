using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Download
{
    public class CrawlingProxyManager
    {
        public readonly IList<CrawlingProxy> Proxies = new List<CrawlingProxy>();
    }
}
