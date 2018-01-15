using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Download.ResourceContentUnits
{
    public class DownloadedFilesUnit : ResourceContentUnit
    {
        public byte[] Bytes;
        public string MimeType;

        public DownloadedFilesUnit(byte[] bytes, string mimeType)
        {
            Bytes = bytes;
            MimeType = mimeType;
        }
    }
}
