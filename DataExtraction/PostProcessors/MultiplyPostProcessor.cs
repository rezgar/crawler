using Rezgar.Crawler.DataExtraction.Dependencies;
using Rezgar.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    using ParsePostProcessors;
    using Utils.Parsing.Parsers;

    public class MultiplyPostProcessor : ParsePostProcessor<decimal?>
    {
        public readonly StringWithDependencies MultiplierString;
        public readonly decimal MultiplierDecimal;

        public MultiplyPostProcessor(StringWithDependencies multiplier)
            : base(new DecimalParser())
        {
            MultiplierString = multiplier;
        }

        public override IEnumerable<string> Execute(decimal? value, DependencyDataSource dependencyDataSource)
        {
            var multiplierDecimal = decimal.Parse(dependencyDataSource.Resolve(MultiplierString), CultureInfo.InvariantCulture);
            yield return ToString(value * MultiplierDecimal);
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return MultiplierString;
        }
    }
}
