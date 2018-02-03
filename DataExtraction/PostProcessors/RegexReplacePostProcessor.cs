using Rezgar.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class RegexReplacePostProcessor : PostProcessor
    {
        public readonly string RegexPattern;
        public readonly bool CaseSensitive = false;
        public readonly StringWithDependencies ReplaceValue = null;

        public RegexReplacePostProcessor(string regexPattern, StringWithDependencies replaceValue, bool caseSensitive = false)
        {
            RegexPattern = regexPattern;
            ReplaceValue = replaceValue;
            CaseSensitive = caseSensitive;
        }

        public override IEnumerable<string> Execute(string value)
        {
            var options = RegexOptions.Compiled;

            if (!CaseSensitive)
                options |= RegexOptions.IgnoreCase;

            var regex = new Regex(RegexPattern, options);

            yield return regex.Replace(value, ReplaceValue);
        }

        #region Idependent

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            //foreach (var dependencyName in RegexPattern.GetDependencyNames())
            //    yield return dependencyName;

            if (ReplaceValue != null)
                yield return ReplaceValue;
        }

        #endregion
    }
}