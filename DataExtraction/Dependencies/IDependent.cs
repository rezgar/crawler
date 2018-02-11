using System;
using System.Collections.Generic;
using System.Text;

namespace Rezgar.Crawler.DataExtraction.Dependencies
{
    public interface IDependent
    {
        IEnumerable<StringWithDependencies> GetStringsWithDependencies();
    }
}
