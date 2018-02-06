using Rezgar.Crawler.Download;
using Rezgar.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Configuration.WebsiteConfigSections
{
    public class WebsiteCrawlingSettings
    {
        #region Settings

        public const int MaxAutomaticRedirects = 10;

        public string PageGenuityMarkerItemId = null;

        public CultureInfo Culture;
        public TimeZoneInfo TimeZone = TimeZoneInfo.Local;

        public DocumentTypes DocumentType = DocumentTypes.HtmlCSS;
        public Encoding FallBackEncoding;

        public Regex UrlUniquePartRegex;

        public bool UseProxy;
        public bool UseProxyForImages;
        public bool UseProxyForInlineDownloads;
        public bool UseProxyForLinkDownloads;

        public bool AutoDecompression = true;
        public int MaxExistingIncrementalUrls = 100;//Settings.Default.MaxExistingIncrementalUrlsDefault; //per referrer Url
        public int MaxOutdatedIncrementalUrls = 100;//Settings.Default.MaxOutdatedIncrementalUrlsDefault; //per referrer Url
        public bool PersistIgnoredUrls = true;
        public bool KeepAlive = true;
        public bool TraceDownloadErrors = true;
        public bool TraceProxyErrors;
        public bool PessimizeProxyFailures;
        public bool ProxyInheritance = true;
        public bool RegisterProxyFailures = true;
        public TimeSpan ProxyBlockPeriod = TimeSpan.FromHours(6);
        public bool ExclusiveProxyLocking = false;
        public bool IgnoreServerErrors;
        public int DefaultBufferSize = 1024 * 256;//Settings.Default.DefaultBufferSize;
        public HashSet<string> Domains;
        public IList<string> UserAgents = new[] {
            "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1",
            "Mozilla/5.0 (Windows NT 6.3; rv:36.0) Gecko/20100101 Firefox/36.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_10; rv:33.0) Gecko/20100101 Firefox/33.0",

            "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_10_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2227.1 Safari/537.36",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2227.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2227.0 Safari/537.36",

            "Opera/9.80 (X11; Linux i686; Ubuntu/14.10) Presto/2.12.388 Version/12.16",
            "Opera/9.80 (Windows NT 6.0) Presto/2.12.388 Version/12.14",
            "Mozilla/5.0 (Windows NT 6.0; rv:2.0) Gecko/20100101 Firefox/4.0 Opera 12.14",
            "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.0) Opera 12.14",
            "Opera/12.80 (Windows NT 5.1; U; en) Presto/2.10.289 Version/12.02",

            "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; AS; rv:11.0) like Gecko",
            "Mozilla/5.0 (compatible, MSIE 11, Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko",
            "Mozilla/5.0 (compatible; MSIE 10.6; Windows NT 6.1; Trident/5.0; InfoPath.2; SLCC1; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; .NET CLR 2.0.50727) 3gpp-gba UNTRUSTED/1.0",
            "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 7.0; InfoPath.3; .NET CLR 3.1.40767; Trident/6.0; en-IN)",
            "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; WOW64; Trident/6.0)",
            "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0)",

            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.246",

            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_9_3) AppleWebKit/537.75.14 (KHTML, like Gecko) Version/7.0.3 Safari/7046A194A",
            "Mozilla/5.0 (iPad; CPU OS 6_0 like Mac OS X) AppleWebKit/536.26 (KHTML, like Gecko) Version/6.0 Mobile/10A5355d Safari/8536.25",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_6_8) AppleWebKit/537.13+ (KHTML, like Gecko) Version/5.1.7 Safari/534.57.2",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_7_3) AppleWebKit/534.55.3 (KHTML, like Gecko) Version/5.1.3 Safari/534.53.10",
            "Mozilla/5.0 (iPad; CPU OS 5_1 like Mac OS X) AppleWebKit/534.46 (KHTML, like Gecko ) Version/5.1 Mobile/9B176 Safari/7534.48.3",
            "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10_6_8; de-at) AppleWebKit/533.21.1 (KHTML, like Gecko) Version/5.0.5 Safari/533.21.1",
            "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10_6_7; da-dk) AppleWebKit/533.21.1 (KHTML, like Gecko) Version/5.0.5 Safari/533.21.1",
            "Mozilla/5.0 (Windows; U; Windows NT 6.1; tr-TR) AppleWebKit/533.20.25 (KHTML, like Gecko) Version/5.0.4 Safari/533.20.27",
        };
        public IDictionary<string, string> Headers = new Dictionary<string, string>();
        public bool ValidateDomain = true;
        public TimeSpan DownloadTimeout = TimeSpan.FromMinutes(1);//Settings.Default.DownloadTimeout; //ms
        public int DownloadDelay = -1; //ms
        public int DownloadDelayRandomizationMax = 0;
        public int MaxThreads = 10;//Settings.Default.MaxThreadsPerJob;
        public bool PersistCookies = true;

        public int PageErrorRetryTimes = 5;
        public int ImageErrorRetryTimes = 5;
        public int ErrorRetryTimeout = 2000; //ms

        #endregion
        
        public System.Net.WebRequest SetUpWebRequest(System.Net.HttpWebRequest webRequest, ResourceLink resourceLink)
        {
            webRequest.Method = resourceLink.HttpMethod;

            webRequest.AllowAutoRedirect = true;
            webRequest.MaximumAutomaticRedirections = MaxAutomaticRedirects;
            webRequest.CachePolicy = new System.Net.Cache.HttpRequestCachePolicy(System.Net.Cache.HttpRequestCacheLevel.BypassCache);
            
            webRequest.AutomaticDecompression = AutoDecompression
                ? DecompressionMethods.GZip | DecompressionMethods.Deflate
                : DecompressionMethods.None;

            webRequest.Timeout = (int)DownloadTimeout.TotalMilliseconds;
            webRequest.ReadWriteTimeout = webRequest.Timeout;

            var crawlingState = resourceLink.Job?.CrawlingState ?? resourceLink.Config.CrawlingState;
            if (crawlingState.Cookies != null && webRequest.SupportsCookieContainer)
            {
                webRequest.CookieContainer = new CookieContainer();
                webRequest.CookieContainer.Add(crawlingState.Cookies);
            }

            //if (data.Job.SourceDomainCookie != null)
            //    webRequest.Headers.Add(HttpRequestHeader.Cookie, data.Job.SourceDomainCookie);

            //http://stackoverflow.com/questions/2764577/forcing-basic-authentication-in-webrequest
            if (!string.IsNullOrEmpty(webRequest.RequestUri.UserInfo))
                webRequest.Headers.Set(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(webRequest.RequestUri.UserInfo)));

            webRequest.Pipelined = true;
            webRequest.KeepAlive = KeepAlive;
            
            if (webRequest.KeepAlive)
                webRequest.Headers.Set(HttpRequestHeader.KeepAlive, @"300");

            // NOTE: Not available on .NET Standard 2.0
            // http://www.webmonkeys.org.uk/2012/09/c-the-server-committed-a-protocol-violation-sectionresponsestatusline/
            //webRequest.ServicePoint.Expect100Continue = false;

            #region Browser mimicking headers

            webRequest.Host = resourceLink.Uri.Host;
            
            var originUriBuilder = new UriBuilder(resourceLink.Uri.Scheme, resourceLink.Uri.Host);
            if (!resourceLink.Uri.IsDefaultPort)
                originUriBuilder.Port = resourceLink.Uri.Port;

            webRequest.Headers["Origin"] = originUriBuilder.ToString();
            if (resourceLink.ReferrerResourceLink != null)
                webRequest.Referer = resourceLink.ReferrerResourceLink.Url;

            webRequest.UserAgent = resourceLink.UserAgent ?? UserAgents.GetRandomElement();

            webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            webRequest.Headers.Set(HttpRequestHeader.AcceptLanguage, "en,ru;q=0.9,ro;q=0.8,es;q=0.7");
            webRequest.Headers.Set(HttpRequestHeader.AcceptCharset, @"utf-8;q=0.7,*;q=0.7"); // ISO-8859-1,

            if (!AutoDecompression)
                webRequest.Headers.Set(HttpRequestHeader.AcceptEncoding, @"gzip, deflate, br");

            #endregion

            //webRequest.Proxy = data.WebProxy;

            //#if DEBUG
            //            webRequest.Proxy = new WebProxy("127.0.0.1:8888");
            //#endif

            #region Default headers' override 
            
            // Set up config-based headers
            if (resourceLink.Config.CrawlingSettings.Headers != null)
                foreach (var header in resourceLink.Config.CrawlingSettings.Headers)
                    webRequest.Headers[header.Key] = header.Value;

            // Override(or add) link-based headers
            if (resourceLink.Headers != null)
                foreach (var header in resourceLink.Headers)
                    webRequest.Headers[header.Key] = header.Value;

            #endregion

            #region Parameters

            if (resourceLink.HttpMethod == System.Net.WebRequestMethods.Http.Post)
            {
                if (resourceLink.Parameters != null && resourceLink.Parameters.Count > 0)
                {
                    var formUrlEncodedContent = new System.Net.Http.FormUrlEncodedContent(
                        resourceLink.Parameters
                        .Select(pred => new KeyValuePair<string, string>(pred.Key, pred.Value))
                    );
                    var formBytes = formUrlEncodedContent.ReadAsByteArrayAsync().Result;

                    webRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                    webRequest.ContentLength = formBytes.Length;

                    using (var stream = webRequest.GetRequestStream())
                    {
                        stream.Write(formBytes, 0, formBytes.Length);
                    }
                }
            }

            #endregion

            return webRequest;
        }

        public string GenerateUrlUniquePart(string url)
        {
            if (UrlUniquePartRegex != null)
            {
                var urlUniquePartRegexMatch = UrlUniquePartRegex.Match(url);
                if (urlUniquePartRegexMatch.Success)
                {
                    return urlUniquePartRegexMatch.Groups[1].Value;
                }
                else
                {
                    Trace.TraceWarning("GenerateUrlUniquePart: Could not match unique url regex for url {0}, regex {1}", url, UrlUniquePartRegex);
                }
            }

            return url;
        }

        public enum DocumentTypes
        {
            HtmlCSS,
            HtmlXPath,
            Xml,
            Json,
            Text // includes all text-based non-selectable types like CSV etc
        }
    }
}
