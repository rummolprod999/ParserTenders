using System;
using System.Xml;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParserTenders.TenderDir;

namespace ParserTenders.ParserDir
{
    public class ParserTendersWeb : ParserWeb
    {
        private int PageCount = 20;

        public ParserTendersWeb(TypeArguments a) : base(a)
        {
        }

        public override void Parsing()
        {
            for (int i = 1; i <= PageCount; i++)
            {
                string url =
                    Uri.EscapeUriString($"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&pageNumber={i}&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=");
                Console.WriteLine(url);
                try
                {
                    ParserPage(url);
                }
                catch (Exception e)
                {
                    Log.Logger("Error in ParserTendersWeb.Parsing", e);
                }
            }
        }

        private void ParserPage(string url)
        {
            string s = DownloadString.DownLUserAgent(url);
            if (String.IsNullOrEmpty(s))
            {
                Log.Logger("Empty string in ParserPage()", url);
                return;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(s);
            var tens = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'search-registry-entry-block')]/div[contains(@class, 'row')][1]//a[contains(@href, 'print-form')]") ??
                       new HtmlNodeCollection(null);
            foreach (var a in tens)
            {
                try
                {
                    ParserLink(a);
                }
                catch (Exception e)
                {
                    Log.Logger(e);
                }
            }
        }

        private void ParserLink(HtmlNode n)
        {
            string url = (n.Attributes["href"]?.Value ?? "").Trim();
            if (!String.IsNullOrEmpty(url))
            {
                string s = DownloadString.DownLUserAgent(url);
                if (String.IsNullOrEmpty(s))
                {
                    Log.Logger("Empty string in ParserLink()", url);
                    return;
                }

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(s);
                string xml = (htmlDoc.DocumentNode.SelectSingleNode("//div[@id= \"tabs-2\"]")?.InnerText ?? "").Trim();
                xml = System.Net.WebUtility.HtmlDecode(xml);
                if (String.IsNullOrEmpty(xml))
                {
                    Log.Logger("empty xml in ParserLink", url);
                    return;
                }

                if (url.Contains("223/purchase"))
                {
                    try
                    {
                        Parser223Web(xml, url);
                    }
                    catch (Exception e)
                    {
                        Log.Logger("Error in Parser223Web()", e);
                    }
                }
                else
                {
                    /*Log.Logger("can not fint 223 or 44 in url", url);*/
                }
            }
        }

        private void Parser223Web(string ftext, string url)
        {
            ftext = ClearText.ClearString(ftext);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(ftext);
            string jsons = JsonConvert.SerializeXmlNode(doc);
            JObject json = JObject.Parse(jsons);
            if (ftext.Contains("purchaseNoticeZPESMBO"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeZpesmbo);
            }
            if (ftext.Contains("purchaseNoticeZKESMBO"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeZkesmbo);
            }
            if (ftext.Contains("purchaseNoticeKESMBO"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeKesmbo);
            }
            if (ftext.Contains("purchaseNoticeAESMBO"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeAesmbo);
            }
            if (ftext.Contains("purchaseNoticeZK"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeZk);
            }
            else if (ftext.Contains("purchaseNoticeOK"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeOk);
            }
            else if (ftext.Contains("purchaseNoticeOA"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeOa);
            }
            else if (ftext.Contains("purchaseNoticeIS"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeIs);
            }
            else if (ftext.Contains("purchaseNoticeEP"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeEp);
            }
            else if (ftext.Contains("purchaseNoticeAE94"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeAe94);
            }
            else if (ftext.Contains("purchaseNoticeAE"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNoticeAe);
            }
            else if (ftext.Contains("purchaseNotice"))
            {
                Bolter223(url, json, TypeFile223.PurchaseNotice);
            }
            else
            {
                Log.Logger("Can not find root tag in xml", url);
            }
        }

        public void Bolter223(string url, JObject json, TypeFile223 typefile)
        {
            try
            {
                TenderType223Web a = new TenderType223Web(url, json, typefile);
                a.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при парсинге xml", e, url);
                Log.Logger(e.Source);
                Log.Logger(e.StackTrace);
            }
        }
    }
}