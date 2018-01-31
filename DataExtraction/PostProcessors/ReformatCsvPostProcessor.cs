﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using CsvHelper;
using Rezgar.Crawler.DataExtraction.PostProcessors.ParsePostProcessors;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{

    public class ReformatCsvPostProcessor : ParseCsvPostProcessor
    {
        private readonly IList<CsvColumnTransition> _columnTransitions;

        public ReformatCsvPostProcessor(
            IList<CsvColumnTransition> columnTransitions
        )
        {
            _columnTransitions = columnTransitions;
        }

        #region Overrides of PostProcessor

        public override IEnumerable<string> Execute(CsvDocument value)
        {
            var resultBuilder = new StringBuilder();

            using (var stringWriter = new StringWriter(resultBuilder))
            {
                using (var writer = new CsvWriter(stringWriter))
                {
                    foreach (var column in _columnTransitions.Select(transition => transition.Name))
                        writer.WriteField(column);

                    foreach (var row in value.Rows)
                    {
                        writer.NextRecord();

                        foreach (var columnTransition in _columnTransitions)
                        {
                            var sourceColumnIndex = value.Columns.IndexOf(columnTransition.SourceName);
                            var sourceColumnValue = row[sourceColumnIndex] as string;

                            foreach (var postProcessor in columnTransition.PostProcessors)
                            {
                                sourceColumnValue = postProcessor.Execute(sourceColumnValue).SingleOrDefault();
                            }

                            writer.WriteField(sourceColumnValue);
                        }
                    }
                }
            }

            yield return resultBuilder.ToString();
        }

        #endregion

        #region Declarations

        public class CsvColumnTransition
        {
            public string Name;
            public string SourceName;
            public IList<PostProcessor> PostProcessors;

            public CsvColumnTransition(string name, string sourceName, IList<PostProcessor> postProcessors)
            {
                Name = name;
                SourceName = sourceName;
                PostProcessors = postProcessors;
            }
        }


        #endregion
    }
}
