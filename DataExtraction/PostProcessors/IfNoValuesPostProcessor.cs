using Rezgar.Crawler.DataExtraction.Dependencies;
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

        public override IEnumerable<string> Execute(IEnumerable<string> values, DependencyDataSource dependencyDataSource)
        {
            if (!values.Any())
                yield return dependencyDataSource.Resolve(ReplacementValue);
        }

        public override IEnumerable<string> Execute(string value, DependencyDataSource dependencyDataSource)
        {
            throw new NotSupportedException();
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return ReplacementValue;
        }
    }
}
