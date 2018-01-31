using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class IfNoValuesPostProcessor : PostProcessor
    {
        public readonly StringWithDependencies ReplacementValue;

        public IfNoValuesPostProcessor(StringWithDependencies replacementValue)
        {
            ReplacementValue = replacementValue;
        }

        public override IEnumerable<string> Execute(IEnumerable<string> values)
        {
            if (!values.Any())
                yield return ReplacementValue;
        }

        public override IEnumerable<string> Execute(string value)
        {
            throw new NotSupportedException();
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return ReplacementValue;
        }
    }
}
