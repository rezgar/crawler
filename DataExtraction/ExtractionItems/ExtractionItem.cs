using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction.Dependencies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction.ExtractionItems
{
    public class ExtractionItem : IDependent
    {
        public string Name;
        public StringWithDependencies Value;
        public ExtractionLocation Location;
        public IList<string> DependsOn;

        /// <summary>
        /// Extraction context. 
        /// Can be either NULL, which means the entire document. Or can be a reference to another item. 
        /// When this item must be extracted from the contents of another (ex: Item1 is JSON extracted by inline web request, Item2 is data, extracted from this JSON)
        /// </summary>
        public ExtractionContext Context { get; protected set; }

        public IList<PostProcessor> PostProcessors = new List<PostProcessor>();
        public virtual bool PostProcessOnExtraction => true;

        #region Methods

        public void SetExtractionLocation(string location, ExtractionLocation.ExtractionLocationTypes locationType, bool includeChildNodes)
        {
            if (location != null)
            {
                Location = new ExtractionLocation(
                    location,
                    locationType,
                    includeChildNodes
                );
            }
        }

        public void SetExtractionContext(StringWithDependencies contextItemName, WebsiteCrawlingSettings.DocumentTypes contextDocumentType)
        {
            if (contextItemName != null)
            {
                Context = new ExtractionContext(contextItemName, contextDocumentType);
            }
        }

        #endregion

        #region Idependent

        public virtual IEnumerable<StringWithDependencies> GetStringsWithDependencies()
        {
            if (Value != null)
                yield return Value;

            if (Context != null)
                yield return ("{" + Context.ContextItemName + "}"); // TODO: Refactor? Artificial StringWithDependencies to trigger resolve of contextitemname

            if (DependsOn != null)
                yield return string.Join(",", DependsOn.Select(dependency => $"{{{dependency}}}"));

            foreach (var postProcessor in PostProcessors)
                foreach (var stringWithDependencies in postProcessor.GetStringsWithDependencies())
                    yield return stringWithDependencies;
        }

        #endregion

        public override string ToString()
        {
            return Name;
        }
    }
}
