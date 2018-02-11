using Rezgar.Crawler.DataExtraction.Dependencies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;

using Rezgar.Utils.Collections;
using Rezgar.Utils.Uri;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class SetUrlParameterPostProcessor : PostProcessor
    {
        public readonly StringWithDependencies ParameterName;
        public readonly StringWithDependencies Value;

        public SetUrlParameterPostProcessor(StringWithDependencies parameterName, StringWithDependencies value)
        {
            ParameterName = parameterName;
            Value = value;
        }

        public override IEnumerable<string> Execute(string value, DependencyDataSource dependencyDataSource)
        {
            yield return Utils.Uri.QueryString.SetParameter(
                value, 
                dependencyDataSource.Resolve(ParameterName),
                dependencyDataSource.Resolve(Value)
            );
        }

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            yield return ParameterName;
            yield return Value;
        }
    }
}
