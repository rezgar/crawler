using Rezgar.Crawler.Configuration;
using Rezgar.Utils.Collections;
using SmartFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction
{
    public class StringWithDependencies
    {
        public StringWithDependencies(
            /// Can contain string value and/or another CrawlDataItem reference
            string valueFormatString
        )
        {
            FormatString = valueFormatString;

            int dependencyReferenceSearchLastCharacterIndex = 0;
            do
            {
                var dependencyReferenceStart = valueFormatString.IndexOf('{', dependencyReferenceSearchLastCharacterIndex);
                if (dependencyReferenceStart > -1)
                {
                    var dependencyReferenceEnd = valueFormatString.IndexOf('}', dependencyReferenceStart);
                    if (dependencyReferenceEnd < 0)
                        throw new FormatException();

                    var dependencyReference = valueFormatString.Substring(dependencyReferenceStart + 1, dependencyReferenceEnd - dependencyReferenceStart - 1);
                    DependencyNames.Add(dependencyReference);
                    dependencyReferenceSearchLastCharacterIndex = dependencyReferenceEnd;
                }
                else
                    break;
            }
            while (true);

            if (DependencyNames.Count == 0)
            {
                HasBeenResolved = true;
                StringsResolved.Add(FormatString);
            }
        }

        public readonly string FormatString;
        public IList<string> StringsResolved = new List<string>();
        public readonly HashSet<string> DependencyNames = new HashSet<string>();

        public bool HasBeenResolved { get; private set; } = false;

        #region Methods

        public bool Resolve(CollectionDictionary<string, string> extractedItems, CrawlingPredefinedValues predefinedValues, bool replaceWithEmptyValuesWhenNotFound)
        {
            var result = true;

            var resolvedValues = new CollectionDictionary<string>();
            foreach(var dependencyName in DependencyNames)
            {
                if (extractedItems.ContainsKey(dependencyName))
                {
                    var dependencyValues = extractedItems[dependencyName];
                    foreach (var dependencyValue in dependencyValues)
                        resolvedValues.AddValue(dependencyName, dependencyValue);
                }
                else if (predefinedValues.Dictionary.ContainsKey(dependencyName))
                {
                    var dependencyValue = predefinedValues.Dictionary[dependencyName];
                    resolvedValues.AddValues(dependencyName, dependencyValue);
                }
                else
                {
                    //result = false;
                    if (replaceWithEmptyValuesWhenNotFound)
                    {
                        resolvedValues.AddValue(dependencyName, string.Empty);
                    }
                    else
                        result = false;
                }
            }

            HasBeenResolved = result;
            StringsResolved = FormatAllDependencyCombinations(resolvedValues).ToArray();
            return true;
        }
        
        private IEnumerable<string> FormatAllDependencyCombinations(CollectionDictionary<string> collectionDictionary)
        {
            foreach (var i in collectionDictionary.Keys)
            {
                var indexes = new Dictionary<string, int>();
                foreach (var kvp in collectionDictionary)
                {
                    indexes.Add(kvp.Key, 0);
                }

                for (var j = 0; j < collectionDictionary[i].Count; j++)
                {
                    var formatDictionary = new Dictionary<string, string>();
                    foreach (var key in collectionDictionary.Keys)
                    {
                        formatDictionary[key] = collectionDictionary[key][indexes[key]++];
                    }
                    formatDictionary[i] = collectionDictionary[i][j];

                    yield return Smart.Format(FormatString, formatDictionary);
                }
            }
        }

        #endregion

        public static implicit operator StringWithDependencies(string valueFormatString)
        {
            if (valueFormatString == null)
                return null;

            return new StringWithDependencies(valueFormatString);
        }

        public static implicit operator string(StringWithDependencies extractionValue)
        {
            return extractionValue?.ToString();
        }

        public override string ToString()
        {
            if (!HasBeenResolved)
                throw new InvalidOperationException();

            return StringsResolved.FirstOrDefault() ?? string.Empty;
        }
    }
}
