using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class GetUrlParameterPostProcessor : PostProcessor
    {
        public readonly StringWithDependencies ParameterName;

        public GetUrlParameterPostProcessor(StringWithDependencies parameterName)
        {
            ParameterName = parameterName;
        }

        public override IEnumerable<string> Execute(string value)
        {
            if (value != null)
            {
                var uri = new Uri(value);
                yield return Utils.Uri.QueryString.GetParameter(uri.Query, ParameterName);
            }
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return ParameterName;
        }
    }
}
