using Rezgar.Utils.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Rezgar.Crawler.Download.ResourceContentUnits
{
    public class ResponseStringUnit : ResourceContentUnit
    {
        public string Content;

        public ResponseStringUnit(string content)
        {
            Content = content;
        }
    }
}
