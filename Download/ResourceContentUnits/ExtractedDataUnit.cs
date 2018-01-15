using Rezgar.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Download.ResourceContentUnits
{
    public class ExtractedDataUnit : ResourceContentUnit
    {
        public readonly CollectionDictionary<string, string> ExtractedData;

        public ExtractedDataUnit(CollectionDictionary<string, string> extractedData)
        {
            ExtractedData = extractedData;
        }
    }
}
