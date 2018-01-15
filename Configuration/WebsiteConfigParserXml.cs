﻿using Rezgar.Crawler.Configuration.WebsiteConfigSections;
using Rezgar.Crawler.DataExtraction;
using Rezgar.Crawler.DataExtraction.PostProcessors;
using Rezgar.Crawler.Download;
using Rezgar.Crawler.Download.ResourceLinks;
using Rezgar.Crawler.Queue;
using Rezgar.Utils.Collections;
using Rezgar.Utils.Parsing.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Rezgar.Crawler.Configuration
{
    public class WebsiteConfigParserXml : IWebsiteConfigParser
    {
        public WebsiteConfig Parse(string filePath)
        {
            using (var reader = new XmlTextReader(filePath))
            {
                return ReadWebsiteConfig(reader);
            }
        }

        private static WebsiteConfig ReadWebsiteConfig(XmlTextReader reader)
        {
            var config = new WebsiteConfig();

            while (reader.Read())
            {
                if (reader.IsStartElement())
                switch (reader.Name)
                {
                    case "config":
                        config.Name = reader.GetAttribute("name");
                        break;

                    case "settings":
                        config.CrawlingSettings = ReadWebsiteCrawlingSettingsSection(reader);
                        break;

                    case "items":
                        config.ExtractionItems = ReadExtractionItemsSection(reader);
                        break;

                    case "links":
                        config.ExtractionLinks = ReadExtractionLinksSection(reader);
                        break;

                    //case "conditionals":
                    //    websiteConfig.CrawlingConditionals = ReadCrawlingConditionalsSection(reader);
                    //    break;

                    //case "actions":
                    //    websiteConfig.CrawlingCustomActions = ReadCrawlingCustomActionsSection(reader);
                    //    break;

                    case "jobs":
                        var websiteJobs = ReadWebsiteJobsSection(reader, config);

                        foreach (var job in websiteJobs)
                        {
                            Debug.Assert(!string.IsNullOrEmpty(job.Name));
                            //Debug.Assert(job.Id > 0);

                            job.Config = config;
                            config.JobsByName.Add(job.Name, job);
                        }
                        break;
                }

                //if (!reader.IsStartElement())
                //    continue;
            }

            return config;
        }

        #region Website Config

        //private static IDictionary<string, CrawlingConditional> ReadCrawlingConditionalsSection(XmlReader reader)
        //{
        //    var result = new Dictionary<string, CrawlingConditional>();
        //    while (!(reader.Name == "conditionals" && reader.NodeType == XmlNodeType.EndElement) && reader.Read())
        //    {
        //        if (reader.IsStartElement("conditional"))
        //        {
        //            var conditional = new CrawlingConditional();
        //            result[conditional.Name] = conditional;

        //            conditional.Name = reader.GetAttribute("name");

        //            if (reader.IsStartElement("conditional"))
        //            {
        //                reader.GetAttribute(ref conditional.Action, "action");
        //                reader.GetAttribute(ref conditional.Logic, "logic");

        //                conditional.Conditions = new List<CrawlingConditional.Condition>();

        //                do
        //                {
        //                    if (reader.IsStartElement("condition"))
        //                    {
        //                        Debug.Assert(!string.IsNullOrEmpty(reader.GetAttribute("type")));

        //                        var type = CrawlingConditional.Condition.ConditionType.Equals;
        //                        reader.GetAttribute(ref type, "type");

        //                        string items = reader.GetAttribute("items");
        //                        string argument = reader.GetAttribute("argument");

        //                        conditional.Conditions.Add(new CrawlingConditional.Condition(type, argument, items));
        //                    }
        //                }
        //                while (!(reader.Name == "conditional" && reader.NodeType == XmlNodeType.EndElement) && reader.Read());
        //            }
        //        }
        //    }

        //    return result;
        //}

        //private static IDictionary<string, CrawlingCustomAction> ReadCrawlingCustomActionsSection(XmlReader reader)
        //{
        //    string tagName = reader.Name;
        //    var result = new Dictionary<string, CrawlingCustomAction>(StringComparer.OrdinalIgnoreCase);

        //    if (!reader.IsEmptyElement)
        //    {
        //        while (!(reader.Name == tagName && reader.NodeType == XmlNodeType.EndElement) && reader.Read())
        //        {
        //            if (reader.IsStartElement("action"))
        //            {
        //                var action = new CrawlingCustomAction(
        //                    reader.GetAttribute("id"),
        //                    reader.GetAttribute("language"),
        //                    reader.ReadElementContentAsString()
        //                );

        //                result.Add(action.Id, action);
        //            }
        //        }
        //    }

        //    return result.Count > 0
        //        ? result
        //        : null;
        //}

        private static WebsiteCrawlingSettings ReadWebsiteCrawlingSettingsSection(XmlReader reader)
        {
            var result = new WebsiteCrawlingSettings();

            while (!(reader.Name == "settings" && reader.NodeType == XmlNodeType.EndElement) && reader.Read())
            {
                if (reader.IsStartElement())
                {
                    switch (reader.Name)
                    {
                        case "downloads":
                            reader.GetAttribute(ref result.DownloadTimeout, "timeout");
                            reader.GetAttribute(ref result.DownloadDelay, "delay");
                            reader.GetAttribute(ref result.DownloadDelayRandomizationMax, "delay_randomization_max");
                            reader.GetAttribute(ref result.PersistCookies, "persist_cookies");
                            //reader.GetAttribute(ref result.SupplyRefererUrl, "supply_referer_url");
                            reader.GetAttribute(ref result.KeepAlive, "keep_alive");

                            reader.GetAttribute(ref result.UseProxy, "use_proxy");
                            reader.GetAttribute(ref result.UseProxyForImages, "use_proxy_for_images");
                            reader.GetAttribute(ref result.UseProxyForInlineDownloads, "use_proxy_for_inline_downloads");
                            reader.GetAttribute(ref result.UseProxyForLinkDownloads, "use_proxy_for_link_downloads");

                            reader.GetAttribute(ref result.PessimizeProxyFailures, "pessimize_proxy_failures");
                            reader.GetAttribute(ref result.RegisterProxyFailures, "register_proxy_failures");

                            string proxyBlockPeriodString = null;
                            reader.GetAttribute(ref proxyBlockPeriodString, "proxy_block_period");
                            if (!string.IsNullOrEmpty(proxyBlockPeriodString))
                                result.ProxyBlockPeriod = TimeSpan.Parse(proxyBlockPeriodString);

                            reader.GetAttribute(ref result.ExclusiveProxyLocking, "exclusive_proxy_locking");
                            reader.GetAttribute(ref result.ProxyInheritance, "proxy_inheritance");
                            reader.GetAttribute(ref result.IgnoreServerErrors, "ignore_server_errors");
                            //reader.GetAttribute(ref result.ValidateActionName, "validate_action");

                            string fallBackEncoding = reader.GetAttribute("fall_back_encoding");
                            if (!string.IsNullOrEmpty(fallBackEncoding))
                                result.FallBackEncoding = Encoding.GetEncoding(fallBackEncoding);

                            //string failUrlRegex = reader.GetAttribute("fail_url_regex");
                            //if (!string.IsNullOrEmpty(failUrlRegex))
                            //    result.FailUrlRegex = new Regex(failUrlRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);

                            //string proxyFailUrlRegex = reader.GetAttribute("proxy_fail_url_regex");
                            //if (!string.IsNullOrEmpty(proxyFailUrlRegex))
                            //    result.ProxyFailUrlRegex = new Regex(proxyFailUrlRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            
                            reader.GetAttribute(ref result.MaxThreads, "max_threads");

                            reader.GetAttribute(ref result.MaxExistingIncrementalUrls, "max_existing_incremental_urls");
                            reader.GetAttribute(ref result.MaxOutdatedIncrementalUrls, "max_outdated_incremental_urls");
                            //reader.GetAttribute(ref result.Output, "output");
                            reader.GetAttribute(ref result.DefaultBufferSize, "default_buffer_size");
                            //reader.GetAttribute(ref result.CompleteIncrementalRecrawl, "complete_incremental_recrawl");
                            //reader.GetAttribute(ref result.CrossJobKnownUrls, "cross_job_known_urls");

                            reader.GetAttribute(ref result.ValidateDomain, "validate_domain");
                            //reader.GetAttribute(ref result.DownloadInlineItemsImmediately, "download_inline_items_immediately");

                            var domains = reader.GetAttribute("domains");
                            if (!string.IsNullOrEmpty(domains))
                                result.Domains = new HashSet<string>(domains.Split('|'), StringComparer.OrdinalIgnoreCase);

                            // Read children
                            var userAgents = new List<string>();
                            reader.ProcessChildren((childName, childReader) =>
                            {
                                switch (childName)
                                {
                                    case "user_agents":
                                        childReader.ProcessChildren((userAgentsChildName, userAgentsChildReader) =>
                                        {
                                            switch (userAgentsChildName)
                                            {
                                                case "user_agent":
                                                    userAgentsChildReader.Read();
                                                    userAgents.Add(userAgentsChildReader.Value);
                                                    break;
                                            }
                                        });
                                        break;
                                }
                            });

                            if (userAgents.Count > 0)
                                result.UserAgents = userAgents;

                            break;

                        case "processing":
                            reader.GetAttribute(ref result.DocumentType, "document_type");
                            reader.GetAttribute(ref result.UrlUniquePartRegex, "url_unique_part_regex");
                            //reader.GetAttribute(ref result.PreserveLinebreaks, "preserve_linebreaks");
                            //reader.GetAttribute(ref result.IndexingMode, "indexing_mode");
                            //reader.GetAttribute(ref result.OverwriteFutureDates, "overwrite_future_dates");
                            reader.GetAttribute(ref result.PersistIgnoredUrls, "persist_ignored_urls");
                            //reader.GetAttribute(ref result.OutputData, "output_data");
                            //reader.GetAttribute(ref result.OutputPiratedData, "output_pirated_data");
                            //reader.GetAttribute(ref result.OutputFiles, "output_files");
                            reader.GetAttribute(ref result.PageGenuityMarkerItemId, "page_genuity_marker_item");
                            break;

                        case "locale":
                            string timeZone = reader.GetAttribute("time_zone");
                            if (!string.IsNullOrEmpty(timeZone))
                                result.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);

                            string culture = reader.GetAttribute("culture");
                            if (!string.IsNullOrEmpty(culture))
                                result.Culture = CultureInfo.GetCultureInfo(culture);

                            break;

                        case "error_handling":
                            reader.GetAttribute(ref result.PageErrorRetryTimes, "page_retry_times");
                            reader.GetAttribute(ref result.ImageErrorRetryTimes, "image_retry_times");
                            reader.GetAttribute(ref result.ErrorRetryTimeout, "retry_timeout");
                            reader.GetAttribute(ref result.TraceDownloadErrors, "trace_download_errors");
                            reader.GetAttribute(ref result.TraceProxyErrors, "trace_proxy_errors");
                            break;
                        //case "author":
                            //reader.GetAttribute(ref result.ExtractAuthor, "extract");
                            //break;
                        default:
                            throw new ArgumentException("Unrecognized element", reader.Name);
                            break;
                    }
                }
            }

            return result;
        }

        #endregion

        #region Website Jobs
        
        private static IList<WebsiteJob> ReadWebsiteJobsSection(XmlTextReader reader, WebsiteConfig config)
        {
            var result = new List<WebsiteJob>();
            while (!(reader.Name == "jobs" && reader.NodeType == XmlNodeType.EndElement) && reader.Read())
            {
                if (reader.IsStartElement("job"))
                {
                    var websiteJob = new WebsiteJob(config);

                    websiteJob.Name = reader.GetAttribute("name") ?? "default";
                    //reader.GetAttribute(ref websiteJob.Id, "id");

                    //var domain = reader.GetAttribute("domain");
                    //if (!string.IsNullOrEmpty(domain))
                    //    websiteJob.SourceDomainUri = new Uri(domain, UriKind.RelativeOrAbsolute);

                    //websiteJob.SourceDomainCookie = reader.GetAttribute("cookie");

                    //reader.GetAttribute(ref websiteJob.SyncMode, "sync_mode");

                    while (!(reader.Name == "job" && reader.NodeType == XmlNodeType.EndElement) && reader.Read())
                    {
                        if (!reader.IsStartElement())
                            continue;

                        switch (reader.Name)
                        {
                            case "links":
                                #region Pages

                                foreach (var extractionLink in ReadExtractionLinksSection(reader).Values)
                                {
                                    // NOTE: This are entry links, so they can't have any location to extract items from, only constant values

                                    var extractedItems = new CollectionDictionary<string, string>();
                                    foreach(var extractionItem in extractionLink.ExtractionItems.Values)
                                    {
                                        extractedItems.AddValue(extractionItem.Name, extractionItem.Value);
                                    }

                                    var extractedLink = new DocumentLink(
                                            extractionLink.Value,
                                            extractionLink.HttpMethod,
                                            websiteJob,
                                            extractionLink.ExtractLinks,
                                            extractionLink.ExtractData,
                                            extractedItems
                                        );

                                    websiteJob.EntryCrawlingQueueItems.Add(new CrawlingQueueItem(extractedLink));
                                }

                                #endregion
                                break;

                            default:
                                throw new ArgumentException("Unrecognized element", reader.Name);
                        }
                    }

                    result.Add(websiteJob);
                }
            }

            return result;
        }
        
        private static IDictionary<string, ExtractionLink> ReadExtractionLinksSection(XmlReader reader)
        {
            var links = new Dictionary<string, ExtractionLink>();

            while (!(reader.Name == "links" && reader.NodeType == XmlNodeType.EndElement) && reader.Read())
            {
                if (!reader.IsStartElement())
                    continue;

                switch (reader.Name)
                {
                    case "link":
                        var extractionLink = new ExtractionLink();

                        ReadExtractionItemAttributes(extractionLink, reader);
                        extractionLink.ExtractLinks = reader.GetAttribute("extract_links", true);
                        extractionLink.ExtractData = reader.GetAttribute("extract_data", false);
                        extractionLink.HttpMethod = reader.GetAttribute("http_method", "GET");
                        extractionLink.Type = reader.GetAttribute("type", ExtractionLink.LinkTypes.Auto);
                        
                        reader.ProcessChildren((childName, childReader) =>
                        {
                            switch (childName)
                            {
                                case "items":
                                    extractionLink.ExtractionItems = ReadExtractionItemsSection(childReader);
                                    break;
                                case "post_processors":
                                    ReadExtractionItemPostProcessors(extractionLink, childReader);
                                    break;
                            }
                        });
                        
                        links.Add(extractionLink.Name, extractionLink);
                        break;

                    default:
                        throw new ArgumentException("Unrecognized element", reader.Name);
                }
            }

            return links;
        }

        #endregion

        #region Extraction items

        private static IDictionary<string, ExtractionItem> ReadExtractionItemsSection(XmlReader reader)
        {
            var result = new Dictionary<string, ExtractionItem>();
            
            while (!(reader.Name == "items" && reader.NodeType == XmlNodeType.EndElement) && reader.Read())
            {
                if (reader.IsStartElement("item"))
                {
                    var extractionItem = new ExtractionItem();

                    ReadExtractionItemAttributes(extractionItem, reader);
                    ReadExtractionItemPostProcessors(extractionItem, reader);

                    result.Add(extractionItem.Name, extractionItem);
                }
            }

            return result;
        }

        private static void ReadExtractionItemAttributes(ExtractionItem extractionItem, XmlReader reader)
        {
            extractionItem.Name = XmlReaderExtensions.GetAttribute(reader, "name", "default");
            extractionItem.Value = reader.GetAttribute("value");

            extractionItem.SetExtractionLocation(
                reader.GetAttribute("location"),
                reader.GetAttribute("location_type", ExtractionLocation.ExtractionLocationTypes.InnerText),
                reader.GetAttribute("include_child_nodes", true)
            );

            extractionItem.SetExtractionContext(
                reader.GetAttribute("context"),
                reader.GetAttribute("context_document_type", WebsiteCrawlingSettings.DocumentTypes.Html)
            );
        }
        private static void ReadExtractionItemPostProcessors(ExtractionItem extractionItem, XmlReader reader)
        {
            reader.ProcessChildren((childName, childReader) =>
            {
                extractionItem.PostProcessors.Add(ReadExtractionItemPostProcessor(childReader));
            });
        }

        private static PostProcessorBase ReadExtractionItemPostProcessor(XmlReader reader)
        {
            switch (reader.Name)
            {
                case "regex_extract":
                    return new RegexExtractPostProcessor(
                            reader.GetAttribute("pattern"),
                            reader.GetAttribute("case_sensitive", false),
                            reader.GetAttribute("multi_line", false),
                            reader.GetAttribute("default", string.Empty)
                        );
                case "regex_replace":
                    return new RegexReplacePostProcessor(
                            reader.GetAttribute("pattern"),
                            reader.GetAttribute("replace"),
                            reader.GetAttribute("case_sensitive", false)
                        );
                case "filter":
                    return new FilterPostProcessor(
                            reader.GetAttribute("eq"),
                            reader.GetAttribute("ne")
                        );
                case "match_enum":
                    return new MatchEnumPostProcessor(
                            reader.GetAttribute("type")
                        );
                case "add":
                    return new AddPostProcessor(
                            reader.GetAttribute("addendum")
                        );
                case "multiply":
                    return new MultiplyPostProcessor(
                            reader.GetAttribute("multiplier")
                        );
                case "html_decode":
                    return new HtmlDecodePostProcessor();
                case "url_decode":
                    return new UrlDecodePostProcessor();
                case "base64_decode":
                    return new Base64DecodePostProcessor();
                case "if_no_values":
                    return new IfNoValuesPostProcessor(
                            reader.GetAttribute("value")
                        );
                case "concat":
                    return new ConcatPostProcessor(
                            reader.GetAttribute("separator")
                        );
                case "split":
                    return new SplitPostProcessor(
                            reader.GetAttribute("separator"),
                            reader.GetAttribute("ignore_empty_entries", true)
                        );
                case "eval":
                    return new EvalJavaScriptPostProcessor();
                case "get_url_parameter":
                    return new GetUrlParameterPostProcessor(
                            reader.GetAttribute("name")
                        );
                case "set_url_parameter":
                    return new SetUrlParameterPostProcessor(
                            reader.GetAttribute("name"),
                            reader.GetAttribute("value")
                        );
                case "sum":
                    return new SumPostProcessor();
                case "take":
                    return new TakePostProcessor(
                            reader.GetAttribute("number")
                        );
                case "repeat":
                    return new RepeatPostProcessor(
                            reader.GetAttribute("number")
                        );
                case "reformat_date":
                    return new ReformatDatePostProcessor(
                            reader.GetAttribute("original"),
                            reader.GetAttribute("target", "s")
                        );
                default:
                    throw new NotSupportedException();
            }
        }

        #endregion
    }
}
