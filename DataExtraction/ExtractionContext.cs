using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Rezgar.Crawler.Configuration.WebsiteConfigSections.WebsiteCrawlingSettings;

namespace Rezgar.Crawler.DataExtraction
{
    public class ExtractionContext
    {
        public string ContextItemName;
        public DocumentTypes ContextDocumentType;

        public ExtractionContext(string contextItemName, DocumentTypes contextDocumentType)
        {
            ContextItemName = contextItemName;
            ContextDocumentType = contextDocumentType;
        }
    }
}
