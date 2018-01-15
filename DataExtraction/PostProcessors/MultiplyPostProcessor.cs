using Rezgar.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class MultiplyPostProcessor : PostProcessorBase
    {
        public readonly StringWithDependencies MultiplierString;
        public MultiplyPostProcessor(StringWithDependencies multiplier)
        {
            MultiplierString = multiplier;
        }

        public override IEnumerable<string> Execute(string value)
        {
            var multiplierDecimal = decimal.Parse(MultiplierString, CultureInfo.InvariantCulture);
            var valueDecimal = decimal.Parse(value, CultureInfo.InvariantCulture);

            yield return (valueDecimal * multiplierDecimal).ToString("N", CultureInfo.InvariantCulture);
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return MultiplierString;
        }
    }
}
