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
        public virtual IEnumerable<string> Execute(IEnumerable<string> values)
        {
            return values.SelectMany(Execute);
        }

        public abstract IEnumerable<string> Execute(string value);

        public virtual IEnumerable<StringWithDependencies> GetStringsWithDependencies() { yield break; }
    }
}
