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

        public override IEnumerable<string> Execute(IEnumerable<string> values)
        {
            return values.Take(int.Parse(NumberString));
        }

        public override IEnumerable<string> Execute(string value)
        {
            throw new NotSupportedException();
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return NumberString;
        }
    }
}
