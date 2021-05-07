using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParserTenders.TenderDir;

namespace ParserTenders.ParserDir
{
    public class ParserTendersWeb44 : ParserWeb
    {
        private const int PageCount = 20;

        private readonly List<string> _listUrls = new List<string>
        {
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277317&customerPlaceCodes=OKER30&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom={DateTime.Now.AddDays(-1):dd.MM.yyyy}+-+{DateTime.Now.AddDays(+1):dd.MM.yyyy}&publishDateTo={DateTime.Now.AddDays(+1):dd.MM.yyyy}&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277336&customerPlaceCodes=OKER31&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277377&customerPlaceCodes=OKER34&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=9409197&customerPlaceCodes=OKER38&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277399&customerPlaceCodes=OKER36&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277384&customerPlaceCodes=OKER35&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=5277362&customerPlaceCodes=OKER33&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&customerPlaceWithNested=on&customerPlace=9371527&customerPlaceCodes=OKER40&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?morphology=on&search-filter=Дате+размещения&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=PUBLISH_DATE&af=on&currencyIdGeneral=-1&customerPlaceWithNested=on&customerPlace=5277409&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=UPDATE_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom={DateTime.Now.AddDays(-1):dd.MM.yyyy}+-+{DateTime.Now.AddDays(+1):dd.MM.yyyy}&publishDateTo={DateTime.Now.AddDays(+1):dd.MM.yyyy}&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom={DateTime.Now:dd.MM.yyyy}+-+{DateTime.Now:dd.MM.yyyy}&publishDateTo={DateTime.Now:dd.MM.yyyy}&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber=",
            $"https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=Дате+размещения&search-filter=&sortDirection=true&recordsPerPage=_50&showLotsInfoHidden=false&fz44=on&sortBy=PUBLISH_DATE&okpd2Ids=&okpd2IdsCodes=&af=on&placingWaysList=&placingWaysList223=&placingChildWaysList=&publishDateFrom={DateTime.Now:dd.MM.yyyy}+-+{DateTime.Now:dd.MM.yyyy}&publishDateTo={DateTime.Now:dd.MM.yyyy}&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&customerTitle=&customerCode=&customerFz94id=&customerFz223id=&customerInn=&orderPlacement94_0=&orderPlacement94_1=&orderPlacement94_2=&npaHidden=&restrictionsToPurchase44=&pageNumber="
        };

        public ParserTendersWeb44(TypeArguments a) : base(a)
        {
        }

        public override void Parsing()
        {
            _listUrls.ForEach(ParsingPage);
        }

        private void ParsingPage(string u)
        {
            var maxP = MaxPage(Uri.EscapeUriString($"{u}1"));
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
                    Log.Logger("Error in ParserTendersWeb44.ParserPage", e);
                }
            }
        }

        private void ParserPage(string url)
        {
            if(DownloadString.MaxDownload > 1000) return;
            var s = DownloadString.DownLUserAgentEis(url);
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
                (n.SelectSingleNode(".//a[contains(@href, 'printForm/view')]")?.Attributes["href"]?.Value ?? "").Trim();
            var purNumT = (n.SelectSingleNode(".//div[contains(@class, 'registry-entry__header-mid__number')]/a")
                               ?.Attributes["href"]?.Value ?? "").Trim();
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(purNumT)) return;
            var purNum = purNumT.GetDateFromRegex(@"regNumber=(\d+)");
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
            url = url.Replace("view.html", "viewXml.html");
            url = $"http://zakupki.gov.ru{url}";
            var s = DownloadString.DownLUserAgentEis(url);
            if (string.IsNullOrEmpty(s))
            {
                Log.Logger("Empty string in ParserLink()", url);
                return;
            }

            var xml = s;
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

        private void Parser44Web(string ftext, string url)
        {
            ftext = ClearText.ClearString(ftext);
            var doc = new XmlDocument();
            doc.LoadXml(ftext);
            var jsons = JsonConvert.SerializeXmlNode(doc);
            var json = JObject.Parse(jsons);
            var firstOrDefault = json.Properties().FirstOrDefault(p => p.Name.Contains("fcs"));
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
                    Log.Logger("cannot define type tender", url);
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
                        var a = new TenderType44Web(url, json, typefile);
                        a.Parsing();
                        break;

                    case TypeFile44.TypeTen504:
                        var b = new TenderType504Web(url, json, typefile);
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