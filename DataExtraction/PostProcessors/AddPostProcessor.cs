using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;
using System.Globalization;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class AddPostProcessor : PostProcessor
    {
        public readonly StringWithDependencies AddendumString;
        public AddPostProcessor(StringWithDependencies addendum)
        {
            AddendumString = addendum;
        }

        public override IEnumerable<string> Execute(string value)
        {
            var addendumDecimal = decimal.Parse(AddendumString, CultureInfo.InvariantCulture);
            var valueDecimal = decimal.Parse(value, CultureInfo.InvariantCulture);

            yield return (valueDecimal + addendumDecimal).ToString("F", CultureInfo.InvariantCulture);
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return AddendumString;
        }
    }
}
