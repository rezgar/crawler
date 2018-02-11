using Rezgar.Crawler.Configuration;
using Rezgar.Utils.Collections;
using SmartFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.DataExtraction.Dependencies
{
    public class StringWithDependencies
    {
        public StringWithDependencies(
            /// Can contain string value and/or another CrawlDataItem reference
            string valueFormatString
        )
        {
            FormatString = valueFormatString;

            int dependencyReferenceSearchLastCharacterIndex = 0;
            do
            {
                var dependencyReferenceStart = valueFormatString.IndexOf('{', dependencyReferenceSearchLastCharacterIndex);
                if (dependencyReferenceStart > -1)
                {
                    var dependencyReferenceEnd = valueFormatString.IndexOf('}', dependencyReferenceStart);
                    if (dependencyReferenceEnd < 0)
                        throw new FormatException();

                    var dependencyReference = valueFormatString.Substring(dependencyReferenceStart + 1, dependencyReferenceEnd - dependencyReferenceStart - 1);
                    DependencyNames.Add(dependencyReference);
                    dependencyReferenceSearchLastCharacterIndex = dependencyReferenceEnd;
                }
                else
                    break;
            }
            while (true);

            if (DependencyNames.Count == 0)
            {
                RequiresResolve = false;
            }
            else
                RequiresResolve = true;
        }

        public readonly string FormatString;
        public readonly HashSet<string> DependencyNames = new HashSet<string>();
        public readonly bool RequiresResolve;
        
        public static implicit operator StringWithDependencies(string valueFormatString)
        {
            if (valueFormatString == null)
                return null;

            return new StringWithDependencies(valueFormatString);
        }

        //public static implicit operator string(StringWithDependencies extractionValue)
        //{
        //    return extractionValue?.ToString();
        //}
    }
}
