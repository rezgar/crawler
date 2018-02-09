using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Utils.Collections;
using HtmlAgilityPack;
using System.Xml.XPath;
using Rezgar.Crawler.Download.ResourceLinks;
using System.Text.RegularExpressions;

namespace Rezgar.Crawler.DataExtraction.DocumentProcessors
{
    public class HtmlLocationXPathDocumentProcessor : DocumentProcessor
    {
        private readonly HtmlDocument _htmlDocument;
        private readonly XPathNavigator _xPathNavigator;

        public HtmlLocationXPathDocumentProcessor(WebResponse webResponse, DocumentLink documentLink) 
            : base(documentLink, Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.HtmlXPath)
        {
            _htmlDocument = new HtmlDocument();
            _htmlDocument.Load(webResponse.GetResponseStream(), true);
            _xPathNavigator = _htmlDocument.CreateNavigator();
        }
        public HtmlLocationXPathDocumentProcessor(string documentString, DocumentLink documentLink) 
            : base(documentLink, Configuration.WebsiteConfigSections.WebsiteCrawlingSettings.DocumentTypes.HtmlXPath)
        {
            _htmlDocument = new HtmlDocument();
            _htmlDocument.LoadHtml(documentString);
            _xPathNavigator = _htmlDocument.CreateNavigator();
        }

        private static readonly Regex XPathSelectorAttributeRegex = new Regex(@"/@\w+$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        protected override IEnumerable<(string ExtractedValue, ResponseParserPositionPointer ExtractedValuePosition)> ExtractItemValuesFromLocation(ExtractionLocation location, ResponseParserPositionPointer? relativeLocationBase = null)
        {
            var xPathNavigator = _xPathNavigator;

            if (relativeLocationBase != null)
            {
                var nodeSelector = XPathSelectorAttributeRegex.Replace(relativeLocationBase.Value.Location.Selector, string.Empty);
                xPathNavigator = _xPathNavigator.Select("(" + nodeSelector + ")[" + (relativeLocationBase.Value.ElementIndex + 1) + "]")
                    .Cast<XPathNavigator>()
                    .Single();
            }

            int extractedValueIndex = 0;
            foreach (var value in xPathNavigator.Select(location.Selector)
                                        .Cast<HtmlNodeNavigator>()
                                        .Select(nav => GetNodeValue(nav, location.LocationType, location.IncludeChildNodes))
                    )
            {
                yield return (value?.Trim(), new ResponseParserPositionPointer(location, extractedValueIndex++));
            }
        }

        private string GetNodeValue(HtmlNodeNavigator nav, ExtractionLocation.ExtractionLocationTypes locationType, bool includeChildNodes)
        {
            if (nav.NodeType == XPathNodeType.Attribute)
                return nav.Value;

            switch(locationType)
            {
                case ExtractionLocation.ExtractionLocationTypes.OuterHtml:
                    return nav.CurrentNode.OuterHtml;
                case ExtractionLocation.ExtractionLocationTypes.InnerHtml:
                    return nav.CurrentNode.InnerHtml;
                case ExtractionLocation.ExtractionLocationTypes.InnerText:
                    return GetNodeInnerText(nav.CurrentNode, includeChildNodes);
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
                        return string.Empty;

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
