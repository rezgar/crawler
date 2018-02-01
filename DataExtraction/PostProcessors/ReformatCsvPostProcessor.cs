using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using CsvHelper;
using Rezgar.Crawler.DataExtraction.PostProcessors.ParsePostProcessors;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    using CsvHelper.Configuration;
    using Rezgar.Utils.Collections;

    public class ReformatCsvPostProcessor : ParseCsvPostProcessor
    {
        private readonly string _outputDelimiter = ",";
        private readonly CollectionDictionary<string, CsvColumnTransition> _columnTransitions;
        private readonly IList<string> _columnsOrder;

        public ReformatCsvPostProcessor(
            string outputDelimiter,
            IList<CsvColumnTransition> columnTransitions
        )
        {
            _outputDelimiter = outputDelimiter;
            _columnTransitions = columnTransitions.ToCollectionDictionary(pred => pred.Name, pred => pred);
            _columnsOrder = columnTransitions.Select(pred => pred.Name).Distinct().ToArray();
        }

        #region Overrides of PostProcessor

        public override IEnumerable<string> Execute(CsvDocument value)
        {
            var resultBuilder = new StringBuilder();

            using (var stringWriter = new StringWriter(resultBuilder))
            {
                using (var writer = new CsvWriter(stringWriter, new Configuration
                {
                    Delimiter = _outputDelimiter
                }))
                {
                    // Names could repeat to support alternative column calculation methods
                    foreach (var column in _columnsOrder)
                        writer.WriteField(column);

                    foreach (var row in value.Rows)
                    {
                        writer.NextRecord();

                        foreach (var columnTransitionName in _columnsOrder)
                        {
                            string resultColumnValue = null;

                            foreach (var columnTransitionOption in _columnTransitions[columnTransitionName])
                            {
                                string sourceColumnValue = null;
                                if (columnTransitionOption.SourceName != null && value.Columns.Contains(columnTransitionOption.SourceName))
                                {
                                    var sourceColumnIndex = value.Columns.IndexOf(columnTransitionOption.SourceName);
                                    sourceColumnValue = row[sourceColumnIndex] as string;

                                    foreach (var postProcessor in columnTransitionOption.PostProcessors)
                                    {
                                        sourceColumnValue = postProcessor.Execute(sourceColumnValue).SingleOrDefault();
                                    }
                                }
                                
                                resultColumnValue = sourceColumnValue ?? columnTransitionOption.Value;
                                if (resultColumnValue != null)
                                    break;
                            }

                            if (resultColumnValue != null)
                                writer.WriteField(resultColumnValue);
                        }
                    }
                }
            }

            yield return resultBuilder.ToString();
        }

        #endregion


        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            foreach (var columnTransition in _columnTransitions.Values.SelectMany(columnTransitionOptions => columnTransitionOptions))
                if (columnTransition.Value != null)
                    yield return columnTransition.Value;
        }

        #region Declarations

        public class CsvColumnTransition
        {
            public string Name;
            public StringWithDependencies Value;
            public string SourceName;
            public IList<PostProcessor> PostProcessors;

            public CsvColumnTransition(string name, StringWithDependencies value, string sourceName, IList<PostProcessor> postProcessors)
            {
                Name = name;
                Value = value;
                SourceName = sourceName;
                PostProcessors = postProcessors;
            }
        }


        #endregion
    }
}
