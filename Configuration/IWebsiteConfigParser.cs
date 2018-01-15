using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Configuration
{
    public interface IWebsiteConfigParser
    {
        WebsiteConfig Parse(string configText);
    }
}
