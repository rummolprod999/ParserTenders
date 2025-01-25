#region

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParserTenders.TenderDir;

#endregion

namespace ParserTenders.ParserDir
{
    public class ParserRequestQ44Web : ParserWeb
    {
        private const int PageCount = 20;

        private readonly List<string> _listUrls = new List<string>
        {
            "https://zakupki.gov.ru/epz/pricereq/search/results.html?searchString=&morphology=on&search-filter=%D0%94%D0%B0%D1%82%D0%B5+%D0%BE%D0%B1%D0%BD%D0%BE%D0%B2%D0%BB%D0%B5%D0%BD%D0%B8%D1%8F&savedSearchSettingsIdHidden=&published=on&proposed=on&ended=on&customerPlace=&customerPlaceCodes=&publishDateFrom=&publishDateTo=&updateDateFrom=&updateDateTo=&sortBy=UPDATE_DATE&sortDirection=false&recordsPerPage=_10&showLotsInfoHidden=false&pageNumber="
        };

        public ParserRequestQ44Web(TypeArguments arg) : base(arg)
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
                    Log.Logger("Error in ParserTendersWebReq44.ParserPage", e);
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
            //Log.Logger("count tender on page " + url + ": " + tens.Count);
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
                (n.SelectSingleNode(".//a[contains(@href, 'printForm/view')]")?.Attributes["href"]?.Value ?? "").Trim();
            var purNumT = (n.SelectSingleNode(".//span[contains(@class, 'registry-entry__header-mid__number')]/a")
                ?.InnerText ?? "").Trim();
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(purNumT))
            {
                return;
            }

            var purNum = purNumT.Replace("\u2116", "").Trim();
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
            url = $"https://zakupki.gov.ru{url}";
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
            var a = new TenderRequestQ44Web(url, json);
            a.Parsing();
        }
    }
}