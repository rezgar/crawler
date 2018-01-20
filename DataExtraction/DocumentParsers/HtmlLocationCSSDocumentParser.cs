using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Rezgar.Crawler.Configuration;
using System.Xml.XPath;
using Rezgar.Crawler.Download.ResourceLinks;
using System.Text.RegularExpressions;

namespace Rezgar.Crawler.DataExtraction.DocumentParsers
{
    public class HtmlLocationCSSDocumentParser : DocumentParser
    {
        private HtmlDocument _htmlDocument;

        public HtmlLocationCSSDocumentParser(WebsiteConfig websiteConfig, WebResponse webResponse, DocumentLink documentLink) : base(websiteConfig, documentLink)
        {
            _htmlDocument = new HtmlDocument();
            _htmlDocument.Load(webResponse.GetResponseStream(), true);

            ExtractItems();
        }
        public HtmlLocationCSSDocumentParser(WebsiteConfig websiteConfig, string documentString, DocumentLink documentLink) : base(websiteConfig, documentLink)
        {
            _htmlDocument = new HtmlDocument();
            _htmlDocument.LoadHtml(documentString);

            ExtractItems();
        }

        protected override IEnumerable<(string ExtractedValue, ResponseParserPositionPointer ExtractedValuePosition)> ExtractItemValuesFromLocation(ExtractionLocation location, ResponseParserPositionPointer? relativeLocationBase = null)
        {
            var selector = location.Selector;
            if (relativeLocationBase != null)
            {
                selector = $"{relativeLocationBase.Value.Location.Selector}[{relativeLocationBase.Value.ElementIndex + 1}] {location.Selector}";
            }

            int extractedValueIndex = 0;
            foreach (var value in _htmlDocument.QuerySelectorAll(selector)
                                    .Select(node => GetNodeValue(node, location.LocationType, location.IncludeChildNodes))
                    )
            {
                yield return (value?.Trim(), new ResponseParserPositionPointer(location, extractedValueIndex++));
            }
        }

        private string GetNodeValue(HtmlNode node, ExtractionLocation.ExtractionLocationTypes locationType, bool includeChildNodes)
        {
            var nav = node.CreateNavigator();
            if (nav.NodeType == XPathNodeType.Attribute)
                return nav.Value;
            
            switch (locationType)
            {
                case ExtractionLocation.ExtractionLocationTypes.OuterHtml:
                    return nav.OuterXml;
                case ExtractionLocation.ExtractionLocationTypes.InnerHtml:
                    return nav.InnerXml;
                case ExtractionLocation.ExtractionLocationTypes.InnerText:
                    return GetNodeInnerText(node, includeChildNodes);
                default:
                    throw new NotSupportedException();
            }
        }

        private static string GetNodeInnerText(HtmlNode node, bool includeChildNodes)
        {
            //NOTE: Reimplementation of the HtmlAgilityPack HtmlNode.InnerText to customize behavior and improve performance

            switch (node.NodeType)
            {
                case HtmlNodeType.Text:
                    return ((HtmlTextNode)node).Text;

                case HtmlNodeType.Element:
                    if (!node.HasChildNodes)
                        return node.GetAttributeValue("value", 
                                    node.GetAttributeValue("src",
                                        node.GetAttributeValue("href",
                                        string.Empty)));

                    #region Get inner elements text
                    var valueBuilder = new StringBuilder();

                    foreach (var child in node.ChildNodes)
                    {
                        switch (child.NodeType)
                        {
                            case HtmlNodeType.Text:
                                valueBuilder.Append(((HtmlTextNode)child).Text).Append(" ");
                                break;
                            //NOTE: avoid HTML comments
                            //case HtmlNodeType.Comment:
                            //    valueBuilder.Append(((HtmlCommentNode)child).Comment).Append(" ");
                            //    break;
                            case HtmlNodeType.Element:
                                if (includeChildNodes)
                                    valueBuilder.Append(GetNodeInnerText(child, true)).Append(" ");
                                break;
                        }
                    }

                    return valueBuilder.ToString();

                    #endregion
            }

            return string.Empty;
        }
    }
}
