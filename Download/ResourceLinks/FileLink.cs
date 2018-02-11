using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction.Dependencies;
using Rezgar.Crawler.Download.ResourceContentUnits;
using System.IO;
using Rezgar.Crawler.Configuration;

namespace Rezgar.Crawler.Download.ResourceLinks
{
    public class FileLink : ResourceLink
    {
        public new DocumentLink ReferrerResourceLink
        {
            get => base.ReferrerResourceLink as DocumentLink;
            set => base.ReferrerResourceLink = value;
        }

        public FileLink(
            string url,
            IDictionary<string, string> parameters,
            IDictionary<string, string> headers,
            WebsiteConfig config,
            WebsiteJob job,
            DocumentLink referrerResourceLink
        )
            : base(url, WebRequestMethods.Http.Get, parameters, headers, config, job, referrerResourceLink)
        {
        }

        public FileLink(
            StringWithDependencies urlWithDependencies,
            IDictionary<string, StringWithDependencies> parametersWithDependencies,
            IDictionary<string, StringWithDependencies> headersWithDependencies,
            WebsiteConfig config,
            WebsiteJob job,
            DocumentLink referrerResourceLink
        )
            : base(urlWithDependencies, WebRequestMethods.Http.Get, parametersWithDependencies, headersWithDependencies, config, job, referrerResourceLink)
        {
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

        public override ResourceLink Copy()
        {
            var result = new FileLink(
                null,
                null,
                null,
                Config,
                Job,
                ReferrerResourceLink?.Copy() as DocumentLink
            );
            
            result.CopyBaseData(this);

            return result;
        }
    }
}
