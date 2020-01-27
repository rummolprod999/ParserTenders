using System;
using System.Collections.Generic;
using System.Xml;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParserTenders.TenderDir;

namespace ParserTenders.ParserDir
{
    public class ParserTendersWeb : ParserWeb
    {
        private const int PageCount = 20;

        private readonly List<string> _listUrls = new List<string>
        {
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277317&customerPlaceCodes=OKER30&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&publishDateFrom={DateTime.Now.AddDays(-1):dd.MM.yyyy}+-+{DateTime.Now.AddDays(+1):dd.MM.yyyy}&publishDateTo={DateTime.Now.AddDays(+1):dd.MM.yyyy}&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277336&customerPlaceCodes=OKER31&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277377&customerPlaceCodes=OKER34&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=9409197&customerPlaceCodes=OKER38&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277399&customerPlaceCodes=OKER36&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277384&customerPlaceCodes=OKER35&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277362&customerPlaceCodes=OKER33&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=9371527&customerPlaceCodes=OKER40&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277409&customerPlaceCodes=OKER40&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber="
        };

        public ParserTendersWeb(TypeArguments a) : base(a)
        {
        }

        public override void Parsing()
        {
            _listUrls.ForEach(ParsingPage);
        }

        private void ParsingPage(string u)
        {
            var maxP = MaxPage(u);
            for (var i = 1; i <= maxP; i++)
            {
                var url =
                    Uri.EscapeUriString($"{u}{i}");
                try
                {
                    ParserPage(url);
                }
                catch (Exception e)
                {
                    Log.Logger("Error in ParserTendersWeb.ParserPage", e);
                }
            }
        }

        private void ParserPage(string url)
        {
            if (DownloadString.MaxDownload > 1000) return;
            var s = DownloadString.DownLUserAgent(url);
            if (string.IsNullOrEmpty(s))
            {
                Log.Logger("Empty string in ParserPage()", url);
                return;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(s);
            var tens = htmlDoc.DocumentNode.SelectNodes(
                           "//div[contains(@class, 'search-registry-entry-block')]/div[contains(@class, 'row')][1]") ??
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
            if (DownloadString.MaxDownload > 1000) return;
            var url =
                (n.SelectSingleNode(".//a[contains(@href, 'print-form')]")?.Attributes["href"]?.Value ?? "").Trim();
            if (!url.Contains("223/purchase")) return;
            var purNumT = (n.SelectSingleNode(".//div[contains(@class, 'registry-entry__header-mid__number')]/a")
                               ?.Attributes["href"]?.Value ?? "").Trim();
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(purNumT)) return;
            var purNum = purNumT.GetDateFromRegex(@"regNumber=(\d+)$");
            if (purNum == "")
            {
                Log.Logger("purNum not found");
                return;
            }

            using (var connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                var selectTender =
                    $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number";
                var cmd = new MySqlCommand(selectTender, connect);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@purchase_number", purNum);
                var reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Close();
                    return;
                }
                reader.Close();
            }

            var s = DownloadString.DownLUserAgent(url);
            if (string.IsNullOrEmpty(s))
            {
                Log.Logger("Empty string in ParserLink()", url);
                return;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(s);
            var xml = (htmlDoc.DocumentNode.SelectSingleNode("//div[@id= \"tabs-2\"]")?.InnerText ?? "").Trim();
            xml = System.Net.WebUtility.HtmlDecode(xml);
            if (string.IsNullOrEmpty(xml))
            {
                Log.Logger("empty xml in ParserLink", url);
                return;
            }
                
            try
            {
                Parser223Web(xml, url);
            }
            catch (Exception e)
            {
                Log.Logger("Error in Parser223Web()", e);
            }
        }

        private void Parser223Web(string ftext, string url)
        {
            ftext = ClearText.ClearString(ftext);
            var doc = new XmlDocument();
            doc.LoadXml(ftext);
            var jsons = JsonConvert.SerializeXmlNode(doc);
            var json = JObject.Parse(jsons);
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
                var a = new TenderType223Web(url, json, typefile);
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