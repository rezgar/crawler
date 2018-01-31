using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;
using System.Globalization;
using Rezgar.Crawler.DataExtraction.PostProcessors.ParsePostProcessors;
using Rezgar.Utils.Parsing.Parsers;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{

    public class AddPostProcessor : ParsePostProcessor<decimal?>
    {
        public readonly StringWithDependencies AddendumString;
        public readonly decimal AddendumDecimal;

        public AddPostProcessor(StringWithDependencies addendum)
            : base(new DecimalParser("F"))
        {
            AddendumString = addendum;
            AddendumDecimal = decimal.Parse(AddendumString, CultureInfo.InvariantCulture);
        }

        public override IEnumerable<string> Execute(decimal? value)
        {
            yield return ToString(value ?? 0 + AddendumDecimal);
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return AddendumString;
        }
    }
}
