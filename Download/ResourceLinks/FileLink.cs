﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction;
using Rezgar.Crawler.Download.ResourceContentUnits;
using System.IO;
using Rezgar.Crawler.Configuration;

namespace Rezgar.Crawler.Download.ResourceLinks
{
    public class FileLink : ResourceLink
    {
        public FileLink(
            StringWithDependencies url,
            IDictionary<string, StringWithDependencies> parameters,
            WebsiteConfig config,
            DocumentLink referrerResourceLink
        ) 
            : base(url, WebRequestMethods.Http.Get, parameters, config)
        {
            ReferrerResourceLink = referrerResourceLink;
        }

        public new DocumentLink ReferrerResourceLink
        {
            get => base.ReferrerResourceLink as DocumentLink;
            set => base.ReferrerResourceLink = value;
        }

        public override async Task<IList<ResourceContentUnit>> ProcessWebResponseAsync(WebResponse webResponse)
        {
            using (var memoryStream = new MemoryStream())
            {
                await webResponse.GetResponseStream().CopyToAsync(memoryStream);

                memoryStream.Flush();

                return new[] { new DownloadedFilesUnit(memoryStream.ToArray(), webResponse.ContentType) };
            }
        }
    }
}
