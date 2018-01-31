using System;
using System.Collections.Generic;
using System.Text;

namespace Rezgar.Crawler.DataExtraction.ExtractionItems
{
    /// <summary>
    /// Downloaded as part of the page (usable as HTML source or context for another items)
    /// </summary>
    public class ExtractionFrame : ExtractionLink
    {
        public override bool PostProcessOnDownload => true;
    }
}
