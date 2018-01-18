using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Download
{
    public abstract class ResourceLink
    {
        public readonly StringWithDependencies Url;
        public readonly IDictionary<string, string> Parameters;
        public readonly string HttpMethod;
        public readonly WebsiteJob Job;
        public readonly WebsiteConfig Config;

        public ResourceLink ReferrerResourceLink;

        public Uri Uri
        {
            get
            {
                var uri = new Uri(Url, UriKind.RelativeOrAbsolute);
                if (!uri.IsAbsoluteUri)
                {
                    uri = new Uri(
                        ReferrerResourceLink.Uri, 
                        Url
                    );
                }
                return uri;
            }
        }

        public ResourceLink(StringWithDependencies url, string httpMethod, WebsiteJob job)
        {
            Url = url;
            HttpMethod = httpMethod;
            Job = job;
            Config = job.Config;
        }

        public abstract Task<IList<ResourceContentUnit>> ProcessWebResponseAsync(WebResponse webResponse);

        public override string ToString()
        {
            return Url.ToString();
        }
    }
}
