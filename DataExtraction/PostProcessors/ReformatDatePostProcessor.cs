using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class ReformatDatePostProcessor : PostProcessor
    {
        public string FormatOriginal;
        public string FormatTarget;

        public ReformatDatePostProcessor(string formatOriginal, string formatTarget)
        {
            FormatOriginal = formatOriginal;
            FormatTarget = formatTarget;
        }

        public override IEnumerable<string> Execute(string value)
        {
            DateTime result;
            
            if ((FormatOriginal != null && DateTime.TryParseExact(value, FormatOriginal, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                || (FormatOriginal == null && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)))
            {
                yield return result.ToString(FormatTarget, CultureInfo.InvariantCulture);
            }
        }
    }
}
