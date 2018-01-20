using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction.ExtractionItems
{
    public class ExtractionLink : ExtractionItem
    {
        public bool ExtractLinks = true;
        public bool ExtractData = true;
        public IDictionary<string, StringWithDependencies> Parameters;
        public string HttpMethod = System.Net.WebRequestMethods.Http.Get;
        public LinkTypes Type = LinkTypes.Auto;

        /// <summary>
        /// Items, nested within Link. These must be extracted before link download.
        /// May (and will probably) be relative to Link location
        /// Extracted values are to be placed into PreDownloadKnownData.
        /// </summary>
        public IDictionary<string, ExtractionItem> PredefinedExtractionItems = new Dictionary<string, ExtractionItem>();
        public bool IsPredefinedExtractionItemsLocationRelativeToLink = true;

        public override IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            foreach (var stringWithDependencies in base.GetStringsWithDependencies())
                yield return stringWithDependencies;

            foreach (var extractionItem in PredefinedExtractionItems.Values)
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
