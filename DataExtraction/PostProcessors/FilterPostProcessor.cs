using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class FilterPostProcessor : PostProcessor
    {
        public readonly StringWithDependencies EqualTo;
        public readonly StringWithDependencies NotEqualTo;

        public FilterPostProcessor(StringWithDependencies equalTo, StringWithDependencies notEqualTo)
        {
            EqualTo = equalTo;
            NotEqualTo = notEqualTo;
        }

        public override IEnumerable<string> Execute(string value)
        {
            if (
                (EqualTo == null || value == EqualTo) &&
                (NotEqualTo == null || value != NotEqualTo)
            )
                yield return value;
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            if (EqualTo != null)
                yield return EqualTo;
            if (NotEqualTo != null)
                yield return NotEqualTo;
        }
    }
}
