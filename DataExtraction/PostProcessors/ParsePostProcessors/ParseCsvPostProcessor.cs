using System;
using System.Collections.Generic;
using System.Text;

namespace Rezgar.Crawler.DataExtraction.PostProcessors.ParsePostProcessors
{
    using Utils.Parsing;

    public abstract class ParseCsvPostProcessor : ParsePostProcessor<ParseCsvPostProcessor.CsvDocument>
    {
        protected ParseCsvPostProcessor() : base(new CsvDocumentParser()) { }

        #region Declarations

        public class CsvDocument
        {

        }

        public class CsvDocumentParser : IParser<CsvDocument>
        {

        }

        #endregion
    }
}
