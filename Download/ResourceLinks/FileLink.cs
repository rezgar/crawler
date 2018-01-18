using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction;
using Rezgar.Crawler.Download.ResourceContentUnits;
using System.IO;

namespace Rezgar.Crawler.Download.ResourceLinks
{
    public class FileLink : ResourceLink
    {
        public FileLink(
            StringWithDependencies url,
            WebsiteJob job,
            DocumentLink referrerResourceLink
        ) 
            : base(url, WebRequestMethods.Http.Get, job)
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
