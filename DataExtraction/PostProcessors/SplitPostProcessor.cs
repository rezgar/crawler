using Rezgar.Crawler.DataExtraction.Dependencies;
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

        public override IEnumerable<string> Execute(string value, DependencyDataSource dependencyDataSource)
        {
            return value.Split(dependencyDataSource.ResolveAll(Separator).ToArray(), IgnoreEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return Separator;
        }
    }
}
