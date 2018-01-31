using System;
using System.Collections.Generic;
using System.Text;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    using ParsePostProcessors;

    public class ReformatCsvPostProcessor : ParseCsvPostProcessor
    {
        #region Overrides of PostProcessor

        public override IEnumerable<string> Execute(CsvDocument value)
        {
            return base.Execute(value);
        }

        #endregion

        #region Declarations

        public class CsvColumnTransition
        {
            public string Name;
        }


        #endregion
    }
}
