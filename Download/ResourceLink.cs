using Rezgar.Crawler.Configuration;
using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction.Dependencies;
using Rezgar.Crawler.Download.ResourceContentUnits;
using Rezgar.Utils.Collections;
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
        public virtual string Url { get; protected set; }
        public virtual IDictionary<string, string> Parameters { get; protected set; }
        public virtual IDictionary<string, string> Headers { get; protected set; }

        public StringWithDependencies UrlWithDependencies { get; protected set; }
        public IDictionary<string, StringWithDependencies> ParametersWithDependencies { get; protected set; }
        public IDictionary<string, StringWithDependencies> HeadersWithDependencies { get; protected set; }

        public readonly string HttpMethod;
        public readonly WebsiteConfig Config;
        public WebsiteJob Job; // can be set when job is copied

        public ResourceLink ReferrerResourceLink;
        public string UserAgent;

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

        public ResourceLink(
            string url, 
            string httpMethod, 
            IDictionary<string, string> parameters, 
            IDictionary<string, string> headers,
            WebsiteConfig config, 
            WebsiteJob job, 
            ResourceLink referrerResourceLink
        )
        {
            Url = url;
            HttpMethod = httpMethod;
            Config = config;
            Job = job;
            Parameters = parameters;
            Headers = headers;
            ReferrerResourceLink = referrerResourceLink;
            UserAgent = referrerResourceLink?.UserAgent ?? config.CrawlingSettings.UserAgents.GetRandomElement();
        }


        public ResourceLink(
            StringWithDependencies urlWithDependencies,
            string httpMethod,
            IDictionary<string, StringWithDependencies> parametersWithDependencies,
            IDictionary<string, StringWithDependencies> headersWithDependencies,
            WebsiteConfig config,
            WebsiteJob job,
            ResourceLink referrerResourceLink
        ) : this (null, httpMethod, null, null, config, job, referrerResourceLink)
        {
            UrlWithDependencies = urlWithDependencies;
            ParametersWithDependencies = parametersWithDependencies;
            HeadersWithDependencies = headersWithDependencies;
        }

        public abstract ResourceLink Copy();
        protected void CopyBaseData(ResourceLink fromObject)
        {
            Url = fromObject.Url;
            UrlWithDependencies = fromObject.UrlWithDependencies;

            if (fromObject.Parameters != null)
                Parameters = new Dictionary<string, string>(fromObject.Parameters);
            if (fromObject.ParametersWithDependencies != null)
                ParametersWithDependencies = new Dictionary<string, StringWithDependencies>(fromObject.ParametersWithDependencies);

            if (fromObject.Headers != null)
                Headers = new Dictionary<string, string>(fromObject.Headers);
            if (fromObject.HeadersWithDependencies != null)
                HeadersWithDependencies = new Dictionary<string, StringWithDependencies>(fromObject.HeadersWithDependencies);
        }

        #region HTTP

        public virtual System.Net.WebRequest SetUpWebRequest(HttpWebRequest webRequest)
        {
            webRequest.Method = HttpMethod;

            webRequest.AllowAutoRedirect = true;
            webRequest.MaximumAutomaticRedirections = WebsiteCrawlingSettings.MaxAutomaticRedirects;
            webRequest.CachePolicy = new System.Net.Cache.HttpRequestCachePolicy(System.Net.Cache.HttpRequestCacheLevel.BypassCache);

            webRequest.AutomaticDecompression = Config.CrawlingSettings.AutoDecompression
                ? DecompressionMethods.GZip | DecompressionMethods.Deflate
                : DecompressionMethods.None;

            webRequest.Timeout = (int)Config.CrawlingSettings.DownloadTimeout.TotalMilliseconds;
            webRequest.ReadWriteTimeout = webRequest.Timeout;

            var crawlingState = Job?.CrawlingState ?? Config.CrawlingState;
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
            webRequest.KeepAlive = Config.CrawlingSettings.KeepAlive;

            if (webRequest.KeepAlive)
                webRequest.Headers.Set(HttpRequestHeader.KeepAlive, @"300");

            // NOTE: Not available on .NET Standard 2.0
            // http://www.webmonkeys.org.uk/2012/09/c-the-server-committed-a-protocol-violation-sectionresponsestatusline/
            //webRequest.ServicePoint.Expect100Continue = false;

            #region Browser mimicking headers

            webRequest.Host = Uri.Host;

            var originUriBuilder = new UriBuilder(Uri.Scheme, Uri.Host);
            if (!Uri.IsDefaultPort)
                originUriBuilder.Port = Uri.Port;

            webRequest.Headers["Origin"] = originUriBuilder.ToString();
            if (ReferrerResourceLink != null)
                webRequest.Referer = ReferrerResourceLink.Url;

            webRequest.UserAgent = UserAgent ?? Config.CrawlingSettings.UserAgents.GetRandomElement();

            webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            webRequest.Headers.Set(HttpRequestHeader.AcceptLanguage, "en,ru;q=0.9,ro;q=0.8,es;q=0.7");
            webRequest.Headers.Set(HttpRequestHeader.AcceptCharset, @"utf-8;q=0.7,*;q=0.7"); // ISO-8859-1,

            if (!Config.CrawlingSettings.AutoDecompression)
                webRequest.Headers.Set(HttpRequestHeader.AcceptEncoding, @"gzip, deflate, br");

            #endregion

            // TODO: Proxy

            //webRequest.Proxy = data.WebProxy;

            //#if DEBUG
            //            webRequest.Proxy = new WebProxy("127.0.0.1:8888");
            //#endif

            #region Default headers' override 

            // Set up config-based headers
            if (Config.CrawlingSettings.Headers != null)
                foreach (var header in Config.CrawlingSettings.Headers)
                    webRequest.Headers[header.Key] = header.Value;

            // Override(or add) link-based headers
            if (Headers != null)
                foreach (var header in Headers)
                    webRequest.Headers[header.Key] = header.Value;

            #endregion

            #region Parameters

            if (HttpMethod == System.Net.WebRequestMethods.Http.Post)
            {
                if (Parameters != null && Parameters.Count > 0)
                {
                    var formUrlEncodedContent = new System.Net.Http.FormUrlEncodedContent(
                        Parameters
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

        public abstract Task<IList<ResourceContentUnit>> ProcessWebResponseAsync(WebResponse webResponse);
        public async Task<ResponseStringUnit> ReadResponseStringAsync(WebResponse webResponse)
        {
            return new ResponseStringUnit(await webResponse.GetResponseStringAsync());
        }

        #endregion

        public virtual void Resolve(DependencyDataSource dependencyDataSource)
        {
            Url = dependencyDataSource.Resolve(UrlWithDependencies);
            Parameters = ParametersWithDependencies.ToDictionary(pred => pred.Key, pred => dependencyDataSource.Resolve(pred.Value));
            Headers = HeadersWithDependencies.ToDictionary(pred => pred.Key, pred => dependencyDataSource.Resolve(pred.Value));
        }

        public override string ToString()
        {
            return Url.ToString();
        }
    }
}
