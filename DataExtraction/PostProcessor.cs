using Rezgar.Crawler.DataExtraction.Dependencies;
using Rezgar.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction
{
    public abstract class PostProcessor: IDependent
    {
        public virtual IEnumerable<string> Execute(IEnumerable<string> values, DependencyDataSource dependencyDataSource)
        {
            return values.SelectMany(value => Execute(value, dependencyDataSource));
        }

        public abstract IEnumerable<string> Execute(string value, DependencyDataSource dependencyDataSource);

        public virtual IEnumerable<StringWithDependencies> GetStringsWithDependencies() { yield break; }
    }
}
