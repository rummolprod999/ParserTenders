#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParserTenders.TenderDir;

#endregion

namespace ParserTenders.ParserDir
{
    public class ParserTendersWeb615 : ParserWeb
    {
        private const int PageCount = 200;

        private readonly List<string> _listUrls = new List<string>
        {
            "https://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&search-filter=%D0%94%D0%B0%D1%82%D0%B5+%D0%BE%D0%B1%D0%BD%D0%BE%D0%B2%D0%BB%D0%B5%D0%BD%D0%B8%D1%8F&sortDirection=false&recordsPerPage=_10&showLotsInfoHidden=false&savedSearchSettingsIdHidden=&sortBy=UPDATE_DATE&ppRf615=on&af=on&ca=on&pc=on&pa=on&placingWayList=&selectedLaws=&priceFromGeneral=&priceFromGWS=&priceFromUnitGWS=&priceToGeneral=&priceToGWS=&priceToUnitGWS=&currencyIdGeneral=-1&publishDateFrom=&publishDateTo=&applSubmissionCloseDateFrom=&applSubmissionCloseDateTo=&customerIdOrg=&customerFz94id=&customerTitle=&okpd2Ids=&okpd2IdsCodes=&pageNumber=",
        };

        public ParserTendersWeb615(TypeArguments a) : base(a)
        {
        }

        public override void Parsing()
        {
            //_listUrls.Shuffle();
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
                    Log.Logger("Error in ParserTendersWeb.ParserPage", e);
                }
            }
        }

        private void ParserPage(string url)
        {
            if (DownloadString.MaxDownload > 1000)
            {
                return;
            }

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
            if (DownloadString.MaxDownload > 1000)
            {
                return;
            }

            var url =
                (n.SelectSingleNode(".//a[contains(@href, 'printForm/view.html')]")?.Attributes["href"]?.Value ?? "").Trim();
            if (!url.Contains("notice"))
            {
                return;
            }

            var purNumT = (n.SelectSingleNode(".//div[contains(@class, 'registry-entry__header-mid__number')]/a")
                ?.InnerText.Replace("№", "") ?? "").Trim();
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(purNumT))
            {
                return;
            }

            var purNum = purNumT;
            if (purNum == "")
            {
                Log.Logger("purNum not found");
                return;
            }

            using (var connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                var selectTender =
                    $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND type_fz = 615";
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
            url = $"https://zakupki.gov.ru{url}";
            var s = DownloadString.DownLUserAgentEis(url);
            if (string.IsNullOrEmpty(s))
            {
                Log.Logger("Empty string in ParserLink()", url);
                return;
            }

            var xml = s;
            if (string.IsNullOrEmpty(xml))
            {
                Log.Logger("empty xml in ParserLink", url);
                return;
            }

            try
            {
                Parser615Web(xml, url, purNum);
            }
            catch (Exception e)
            {
                Log.Logger("Error in Parser615Web()", e);
            }
        }

        private void Parser615Web(string ftext, string url, string purNum)
        {
            ftext = ClearText.ClearString(ftext);
            var doc = new XmlDocument();
            doc.LoadXml(ftext);
            var jsons = JsonConvert.SerializeXmlNode(doc);
            var json = JObject.Parse(jsons);
            try
            {
                var a = new TenderType615Web(url, json, purNum);
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