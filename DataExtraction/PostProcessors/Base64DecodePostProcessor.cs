using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Crawler.DataExtraction.Dependencies;
using Rezgar.Utils.Collections;

namespace Rezgar.Crawler.DataExtraction.PostProcessors
{
    public class Base64DecodePostProcessor : PostProcessor
    {
        public override IEnumerable<string> Execute(string value, DependencyDataSource dependencyDataSource)
        {
            var bytes = Convert.FromBase64String(value);
            yield return Encoding.UTF8.GetString(bytes);
        }
    }
}
