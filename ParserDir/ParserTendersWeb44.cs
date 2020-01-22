using System;
using System.Linq;
using System.Xml;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParserTenders.TenderDir;

namespace ParserTenders.ParserDir
{
    public class ParserTendersWeb44 : ParserWeb
    {
        private int PageCount = 20;

        public ParserTendersWeb44(TypeArguments a) : base(a)
        {
        }

        public override void Parsing()
        {
            for (int i = 1; i <= PageCount; i++)
            {
                string url =
                    Uri.EscapeUriString($"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&pageNumber={i}&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=");
                try
                {
                    ParserPage(url);
                }
                catch (Exception e)
                {
                    Log.Logger("Error in ParserTendersWeb44.Parsing", e);
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
            var tens = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'search-registry-entry-block')]/div[contains(@class, 'row')][1]//a[contains(@href, 'printForm/view')]") ??
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
                url = url.Replace("view.html", "viewXml.html");
                url = $"http://zakupki.gov.ru{url}";
                string s = DownloadString.DownLUserAgent(url);
                if (String.IsNullOrEmpty(s))
                {
                    Log.Logger("Empty string in ParserLink()", url);
                    return;
                }

                string xml = s;
                try
                {
                    Parser44Web(xml, url);
                    //Console.WriteLine(xml);
                }
                catch (Exception e)
                {
                    Log.Logger("Error in Parser44Web()", e, url);
                }
            }
        }

        private void Parser44Web(string ftext, string url)
        {
            ftext = ClearText.ClearString(ftext);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(ftext);
            string jsons = JsonConvert.SerializeXmlNode(doc);
            JObject json = JObject.Parse(jsons);
            JProperty firstOrDefault = json.Properties().FirstOrDefault(p => p.Name.Contains("fcs"));
            if (firstOrDefault != null)
            {
                Bolter44(url, json, TypeFile44.TypeTen44);
            }
            else
            {
                firstOrDefault = json.Properties().FirstOrDefault(p => p.Name.Contains("epN"));
                if (firstOrDefault != null)
                {
                    Bolter44(url, json, TypeFile44.TypeTen504);
                }
                else
                {
                    Log.Logger("Can not define type tender", url);
                }
            }
        }

        public void Bolter44(string url, JObject json, TypeFile44 typefile)
        {
            try
            {
                switch (typefile)
                {
                    case TypeFile44.TypeTen44:
                        TenderType44Web a = new TenderType44Web(url, json, typefile);
                        a.Parsing();
                        break;

                    case TypeFile44.TypeTen504:
                        TenderType504Web b = new TenderType504Web(url, json, typefile);
                        b.Parsing();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(typefile), typefile, null);
                }
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }
        }
    }
}