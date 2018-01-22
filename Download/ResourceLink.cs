﻿using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction;
using Rezgar.Crawler.Download.ResourceContentUnits;
using Rezgar.Utils.Http;
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

        public ResourceLink(StringWithDependencies url, string httpMethod, WebsiteConfig config)
        {
            Url = url;
            HttpMethod = httpMethod;
            Config = config;
        }

        public abstract Task<IList<ResourceContentUnit>> ProcessWebResponseAsync(WebResponse webResponse);
        public async Task<ResponseStringUnit> ReadResponseStringAsync(WebResponse webResponse)
        {
            return new ResponseStringUnit(await webResponse.GetResponseStringAsync());
        }

        public override string ToString()
        {
            return Url.ToString();
        }
    }
}
