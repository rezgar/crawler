using Rezgar.Crawler.DataExtraction.Dependencies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class TakePostProcessor : PostProcessor
    {
        public readonly StringWithDependencies NumberString;

        public TakePostProcessor(StringWithDependencies number)
        {
            NumberString = number;
        }

        public override IEnumerable<string> Execute(IEnumerable<string> values, DependencyDataSource dependencyDataSource)
        {
            return values.Take(int.Parse(dependencyDataSource.Resolve(NumberString)));
        }

        public override IEnumerable<string> Execute(string value, DependencyDataSource dependencyDataSource)
        {
            throw new NotSupportedException();
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return NumberString;
        }
    }
}
