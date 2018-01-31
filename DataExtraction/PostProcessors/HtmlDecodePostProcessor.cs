using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class HtmlDecodePostProcessor : PostProcessor
    {
        public override IEnumerable<string> Execute(string value)
        {
            yield return WebUtility.HtmlDecode(value);
        }
    }
}
