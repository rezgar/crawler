using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Rezgar.Utils.Parsing;
using Rezgar.Utils.Collections;
using CsvHelper;

namespace Rezgar.Crawler.DataExtraction.PostProcessors.ParsePostProcessors
{
    using System.IO;

    public abstract class ParseCsvPostProcessor : ParsePostProcessor<ParseCsvPostProcessor.CsvDocument>
    {
        protected ParseCsvPostProcessor() 
            : base(new CsvDocumentParser()) { }

        #region Declarations

        public class CsvDocument
        {
            public IList<string> Columns;
            public IList<string[]> Rows = new List<string[]>();

            public CsvDocument(IList<string> columns)
            {
                Columns = columns;
            }
            
            public override string ToString()
            {
                var resultBuilder = new StringBuilder();

                using (var stringWriter = new StringWriter(resultBuilder))
                {
                    using (var writer = new CsvWriter(stringWriter))
                    {
                        foreach (var column in Columns)
                            writer.WriteField(column);

                        foreach (var row in Rows)
                        {
                            writer.NextRecord();
                            foreach (var field in row)
                            {
                                writer.WriteField(field);
                            }
                        }
                    }
                }

                return resultBuilder.ToString();
            }
        }

        public class CsvDocumentParser : IParser<CsvDocument>
        {
            public CsvDocumentParser()
            {
            }

            #region Implementation of IParser<CsvDocument>

            // http://joshclose.github.io/CsvHelper/2.x/
            public CsvDocument Parse(string value)
            {
                using (var textReader = new StringReader(value))
                using (var parser = new CsvParser(textReader))
                {
                    var result = new CsvDocument(parser.Read());

                    while (true)
                    {
                        var row = parser.Read();
                        if (row == null)
                            break;

                        result.Rows.Add(row);
                    }

                    return result;
                }
            }

            public string ToString(CsvDocument data)
            {
                return data.ToString();
            }

            #endregion
        }

        #endregion
    }
}
