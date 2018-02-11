using Rezgar.Crawler.DataExtraction.Dependencies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class ConcatPostProcessor : PostProcessor
    {
        public readonly StringWithDependencies Separator;

        public ConcatPostProcessor(StringWithDependencies separator)
        {
            Separator = separator;
        }

        public override IEnumerable<string> Execute(IEnumerable<string> values, DependencyDataSource dependencyDataSource)
        {
            yield return string.Join(dependencyDataSource.Resolve(Separator), values);
        }

        public override IEnumerable<string> Execute(string value, DependencyDataSource dependencyDataSource)
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
