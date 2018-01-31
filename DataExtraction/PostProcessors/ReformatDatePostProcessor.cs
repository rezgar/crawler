using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    using ParsePostProcessors;
    using Utils.Parsing;
    using Utils.Parsing.Parsers;

    public class ReformatDatePostProcessor : ParsePostProcessor<DateTime?>
    {
        public ReformatDatePostProcessor(string dateTimeFormatOriginal, string dateTimeFormatTarget)
            : base(new DateTimeParser(dateTimeFormatOriginal, dateTimeFormatTarget))
        {

        }
    }
}
