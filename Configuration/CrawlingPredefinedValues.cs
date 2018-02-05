using Rezgar.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Rezgar.Crawler.Configuration
{
    public class CrawlingPredefinedValues
    {
        #region Constants
        
        public const string USER_NAME = "user_name";
        public const string PASSWORD = "password";
        public const string REQUEST_VERIFICATION_TOKEN = "request_verification_token";

        #endregion

        internal readonly HashSet<string> Required = new HashSet<string>();
        public readonly CollectionDictionary<string, string> Dictionary = new CollectionDictionary<string, string>();

        public CrawlingPredefinedValues() { }
        public CrawlingPredefinedValues(CrawlingPredefinedValues template)
        {
            Required = template.Required.ToHashSet();
            Dictionary = new CollectionDictionary<string, string>(template.Dictionary);
        }

        public bool Validate()
        {
            var result = true;
            foreach(var required in Required)
                if (!Dictionary.ContainsKey(required))
                {
                    Trace.TraceError($"Required item {required} missing from dictionary items [{string.Join(",", Dictionary.Keys)}]");
                    result = false;
                }

            return result;
        }
    }
}
