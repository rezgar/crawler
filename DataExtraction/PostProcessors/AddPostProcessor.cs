using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;
using System.Globalization;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    using ParsePostProcessors;

    public class AddPostProcessor : ParseDecimalPostProcessor
    {
        public readonly StringWithDependencies AddendumString;
        public readonly decimal AddendumDecimal;

        public AddPostProcessor(StringWithDependencies addendum)
        {
            AddendumString = addendum;
            AddendumDecimal = decimal.Parse(AddendumString, CultureInfo.InvariantCulture);
        }

        public override IEnumerable<string> Execute(decimal value)
        {
            yield return (value + AddendumDecimal).ToString("F", CultureInfo.InvariantCulture);
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return AddendumString;
        }
    }
}
