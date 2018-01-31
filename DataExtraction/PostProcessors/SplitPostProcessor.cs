using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class SplitPostProcessor : PostProcessor
    {
        public readonly StringWithDependencies Separator;
        public readonly bool IgnoreEmptyEntries;

        public SplitPostProcessor(StringWithDependencies separator, bool ignoreEmptyEntries)
        {
            Separator = separator;
            IgnoreEmptyEntries = ignoreEmptyEntries;
        }

        public override IEnumerable<string> Execute(string value)
        {
            return value.Split(new string[] { Separator }, IgnoreEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return Separator;
        }
    }
}
