using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class ConcatPostProcessor : PostProcessorBase
    {
        public readonly StringWithDependencies Separator;

        public ConcatPostProcessor(StringWithDependencies separator)
        {
            Separator = separator;
        }

        public override IEnumerable<string> Execute(IEnumerable<string> values)
        {
            yield return string.Join(Separator, values);
        }

        public override IEnumerable<string> Execute(string value)
        {
            throw new NotSupportedException();
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            if (Separator != null)
                yield return Separator;
        }
    }
}
