using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Rezgar.Crawler.Configuration.WebsiteConfigSections
{
    public class WebsitePredefinedValues
    {
        #region Constants
        
        public const string USER_NAME = "user_name";
        public const string PASSWORD = "password";
        public const string REQUEST_VERIFICATION_TOKEN = "request_verification_token";

        #endregion

        internal readonly HashSet<string> Required = new HashSet<string>();
        private readonly IDictionary<string, string> Dictionary = new Dictionary<string, string>();

        internal bool Validate()
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

        #region Public methods

        public string this[string name]
        {
            get { return Dictionary[name]; }
            set { Dictionary[name] = value; }
        }

        public bool ContainsKey(string name)
        {
            return Dictionary.ContainsKey(name);
        }

        #endregion
    }
}
