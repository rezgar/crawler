using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;
using System.Net;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class UrlDecodePostProcessor : PostProcessor
    {
        public override IEnumerable<string> Execute(string value)
        {
            yield return WebUtility.UrlDecode(value);
        }
    }
}
