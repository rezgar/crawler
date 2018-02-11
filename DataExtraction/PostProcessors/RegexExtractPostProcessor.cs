using Rezgar.Crawler.DataExtraction.Dependencies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class RegexExtractPostProcessor : PostProcessor
    {
        public readonly string RegexPattern;
        public readonly bool CaseSensitive = false;
        public readonly bool MultiLine = false;
        public readonly string DefaultValue = null;
        public readonly MatchProcessingModes MatchProcessingMode = MatchProcessingModes.FirstResult;
        public readonly MatchGroupProcessingModes MatchGroupProcessingMode = MatchGroupProcessingModes.FirstResult;

        public enum MatchProcessingModes
        {
            FirstResult,
            Enumerate
        }

        public enum MatchGroupProcessingModes
        {
            FirstResult,
            Enumerate
        }

        public RegexExtractPostProcessor(string regexPattern, bool caseSensitive = false, bool multiLine = false, string defaultValue = null, MatchProcessingModes matchProcessingMode = MatchProcessingModes.FirstResult)
        {
            RegexPattern = regexPattern;
            CaseSensitive = caseSensitive;
            MultiLine = multiLine;
            DefaultValue = defaultValue;
            MatchProcessingMode = matchProcessingMode;
        }

        public override IEnumerable<string> Execute(string value, DependencyDataSource dependencyDataSource)
        {
            var options = RegexOptions.Compiled;

            if (!CaseSensitive)
                options |= RegexOptions.IgnoreCase;

            if (MultiLine)
                options |= RegexOptions.Multiline;

            var regex = new Regex(RegexPattern, options);

            var matches = regex.Matches(value);
            var matchesToProcess = matches.Cast<Match>();
            switch (MatchProcessingMode)
            {
                case MatchProcessingModes.FirstResult:
                    matchesToProcess = matchesToProcess.Take(1);
                    break;
            }

            IList<string> extractedValues = new List<string>();
            foreach (var match in matchesToProcess)
            {
                var matchGroups = match.Groups
                            .Cast<Group>()
                            .Skip(1) //skip complete match itself
                            .Select(group => group.Value.Trim())
                            .Where(group => !string.IsNullOrEmpty(group));

                IEnumerable<string> extractedFromMatch;
                switch (MatchGroupProcessingMode)
                {
                    case MatchGroupProcessingModes.FirstResult:
                        extractedFromMatch = matchGroups.Take(1);
                        break;

                    case MatchGroupProcessingModes.Enumerate:
                        extractedFromMatch = matchGroups;
                        break;

                    //case MatchProcessingModes.Concat:
                    //    break;
                    default:
                        throw new NotSupportedException();
                }

                foreach (var extractedValue in extractedFromMatch)
                    extractedValues.Add(extractedValue);
            }

            if (extractedValues.Count > 0)
                return extractedValues;

            if (DefaultValue != null)
                return Enumerable.Repeat(DefaultValue, 1);

            return Enumerable.Empty<string>();
        }

        #region Idependent

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            //return RegexPattern.GetDependencyNames();
            yield break;
        }

        #endregion
    }
}
