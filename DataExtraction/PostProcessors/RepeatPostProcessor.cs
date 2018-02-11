using Rezgar.Crawler.DataExtraction.Dependencies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class RepeatPostProcessor : PostProcessor
    {
        public readonly StringWithDependencies NumberString;
        
        public RepeatPostProcessor(StringWithDependencies number)
        {
            NumberString = number;
        }

        public override IEnumerable<string> Execute(string value, DependencyDataSource dependencyDataSource)
        {
            return Enumerable.Repeat(value, int.Parse(dependencyDataSource.Resolve(NumberString)));
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return NumberString;
        }
    }
}
