using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction
{
    public class ExtractionLink : ExtractionItem
    {
        public bool ExtractLinks;
        public bool ExtractData;
        public IDictionary<string, StringWithDependencies> Parameters;
        public string HttpMethod;
        public LinkTypes Type;

        /// <summary>
        /// Items, nested within Link. These must be extracted before link download.
        /// May (and will probably) be relative to Link location
        /// Extracted values are to be placed into PreDownloadKnownData.
        /// </summary>
        public IDictionary<string, ExtractionItem> ExtractionItems = new Dictionary<string, ExtractionItem>();

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            foreach (var stringWithDependencies in base.GetStringsWithDependencies())
                yield return stringWithDependencies;

            foreach (var extractionItem in ExtractionItems.Values)
                foreach (var stringWithDependencies in extractionItem.GetStringsWithDependencies())
                    yield return stringWithDependencies;

            foreach (var parameter in Parameters)
                yield return parameter.Value;
        }

        #region Declarations

        public enum LinkTypes
        {
            Auto = 0,
            Document = 1,
            File = 2
        }

        #endregion
    }
}
