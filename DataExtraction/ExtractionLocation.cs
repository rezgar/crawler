using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction
{
    public class ExtractionLocation
    {
        public ExtractionLocation(string selector, ExtractionLocationTypes locationType, bool includeChildNodes)
        {
            Selector = selector;
            LocationType = locationType;
            IncludeChildNodes = includeChildNodes;
        }

        public readonly string Selector;
        public readonly ExtractionLocationTypes LocationType;
        public readonly bool IncludeChildNodes;

        public enum ExtractionLocationTypes
        {
            InnerText,
            InnerHtml,
            OuterHtml
        }

        public override string ToString()
        {
            return Selector;
        }
    }
}
