using Rezgar.Crawler.DataExtraction.Dependencies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;
using System.Globalization;
using Rezgar.Utils.Parsing.Parsers;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{

    public class AddPostProcessor : ParsePostProcessor<decimal?>
    {
        public readonly StringWithDependencies AddendumString;

        public AddPostProcessor(StringWithDependencies addendum)
            : base(new DecimalParser("F"))
        {
            AddendumString = addendum;
        }

        public override IEnumerable<string> Execute(decimal? value, DependencyDataSource dependencyDataSource)
        {
            var addendumDecimal = decimal.Parse(dependencyDataSource.Resolve(AddendumString), CultureInfo.InvariantCulture);
            yield return ToString(value ?? 0 + addendumDecimal);
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return AddendumString;
        }
    }
}
