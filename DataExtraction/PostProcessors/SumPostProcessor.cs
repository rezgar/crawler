using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;
using System.Globalization;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class SumPostProcessor : PostProcessorBase
    {
        public override IEnumerable<string> Execute(IEnumerable<string> values)
        {
            yield return 
                values.Select(value => !string.IsNullOrEmpty(value) ? decimal.Parse(value, CultureInfo.InvariantCulture) : 0)
                .Sum()
                .ToString("F");
        }

        public override IEnumerable<string> Execute(string value)
        {
            throw new NotSupportedException();
        }
    }
}
