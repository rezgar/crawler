using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Download.ResourceContentUnits
{
    public class HttpResultUnit : ResourceContentUnit
    {
        public string RequestUrl;
        public string ResponseUrl;
        public string Url
        {
            get => ResponseUrl ?? RequestUrl;
        }

        public HttpStatusCode HttpStatus;
        public string HttpStatusDescription;
        public string ContentType;
        public long ContentLength;
        public string ContentEncoding;
        public CookieCollection Cookies;
        public WebHeaderCollection Headers;

        public Exception Exception;
    }
}
