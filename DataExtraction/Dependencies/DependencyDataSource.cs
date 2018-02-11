using Rezgar.Crawler.Configuration;
using Rezgar.Utils.Collections;
using SmartFormat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Rezgar.Crawler.DataExtraction.Dependencies
{
    public class DependencyDataSource
    {
        public readonly CollectionDictionary<string, string> ExtractedItems;
        public readonly CrawlingPredefinedValues ConfigPredefinedValues;
        public readonly CrawlingPredefinedValues JobPredefinedValues;

        public DependencyDataSource(CollectionDictionary<string, string> extractedItems, CrawlingPredefinedValues configPredefinedValues, CrawlingPredefinedValues jobPredefinedValues)
        {
            ExtractedItems = extractedItems;
            ConfigPredefinedValues = configPredefinedValues;
            JobPredefinedValues = jobPredefinedValues;
        }

        public string Resolve(StringWithDependencies stringWithDependencies)
        {
            return ResolveAll(stringWithDependencies).FirstOrDefault();
        }
        public CollectionDictionary<string, string> Resolve(CollectionDictionary<string, StringWithDependencies> dictionaryWithDependencies)
        {
            if (dictionaryWithDependencies == null)
                return null;

            return dictionaryWithDependencies.ToCollectionDictionary(
                pred => pred.Key,
                pred => pred.Value.Select(value => Resolve(value))
            );
        }
        public IDictionary<string, string> Resolve(IDictionary<string, StringWithDependencies> dictionaryWithDependencies)
        {
            if (dictionaryWithDependencies == null)
                return null;

            return dictionaryWithDependencies.ToDictionary(pred => pred.Key, pred => Resolve(pred.Value));
        }
        public IEnumerable<string> ResolveAll(StringWithDependencies stringWithDependencies)
        {
            if (stringWithDependencies == null)
                yield return null;

            if (!stringWithDependencies.RequiresResolve)
                yield return stringWithDependencies.FormatString;

            var resolvedValues = new CollectionDictionary<string>();

            foreach (var dependencyName in stringWithDependencies.DependencyNames)
            {
                if (ExtractedItems.ContainsKey(dependencyName))
                {
                    var dependencyValues = ExtractedItems[dependencyName];
                    foreach (var dependencyValue in dependencyValues)
                        resolvedValues.AddValue(dependencyName, dependencyValue);
                }
                else if (ConfigPredefinedValues.Dictionary.ContainsKey(dependencyName))
                {
                    var dependencyValue = ConfigPredefinedValues.Dictionary[dependencyName];
                    resolvedValues.AddValues(dependencyName, dependencyValue);
                }
                else if (JobPredefinedValues != null && JobPredefinedValues.Dictionary.ContainsKey(dependencyName))
                {
                    var dependencyValue = JobPredefinedValues.Dictionary[dependencyName];
                    resolvedValues.AddValues(dependencyName, dependencyValue);
                }
                else
                {
                    resolvedValues.AddValue(dependencyName, string.Empty);
                    Trace.TraceError($"{GetType().Name}.ResolveAll: Could not resolve item {dependencyName} with dependencies [{string.Join(",", stringWithDependencies.DependencyNames)}] based on extracted items {ExtractedItems.Select(pred => string.Format("[{0}: {1}]", pred.Key, string.Join(",", pred.Value)))}");
                }
            }

            foreach (var combination in FormatAllDependencyCombinations(stringWithDependencies, resolvedValues))
                yield return combination;
        }

        private IEnumerable<string> FormatAllDependencyCombinations(StringWithDependencies stringWithDependencies, CollectionDictionary<string> collectionDictionary)
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

                    yield return Smart.Format(stringWithDependencies.FormatString, formatDictionary);
                }
            }
        }

    }
}
