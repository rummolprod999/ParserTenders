using System;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ParserTenders.TenderDir;

namespace ParserTenders.ParserDir
{
    public class ParserMrsk : ParserWeb
    {
        private const int Count = 20;

        public ParserMrsk(TypeArguments ar) : base(ar)
        {
        }

        public override void Parsing()
        {
            for (var i = 1; i <= Count; i++)
            {
                var urlpage = $"http://www.mrsksevzap.ru/purchaseactive?page={i}";
                try
                {
                    ParsingPage(urlpage);
                }
                catch (Exception e)
                {
                    Log.Logger(e);
                }
            }
        }

        private void ParsingPage(string url)
        {
            var s = DownloadString.DownL(url);
            if (String.IsNullOrEmpty(s))
            {
                Log.Logger("Empty string in ParserPage()", url);
                return;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(s);
            var tens =
                htmlDoc.DocumentNode.SelectNodes("//div[@class = \"b-purchases\"]/div[@class = \"b-purchase\"]") ??
                new HtmlNodeCollection(null);
            foreach (var a in tens)
            {
                try
                {
                    ParserLink(a, url);
                }
                catch (Exception e)
                {
                    Log.Logger(e);
                }
            }
        }

        private void ParserLink(HtmlNode n, string url)
        {
            var href = (n.SelectSingleNode(".//a[@class = \"b-link\"]")?.Attributes["href"]?.Value ?? "").Trim();
            if (String.IsNullOrEmpty(href))
            {
                Log.Logger("Empty href", url);
                return;
            }

            var idTender = "";
            var regex = new Regex(@"purchase=(\d+)");
            var matches = regex.Matches(href);
            if (matches.Count > 0)
            {
                idTender = matches[0].Groups[1].Value;
            }

            if (String.IsNullOrEmpty(idTender))
            {
                Log.Logger("Empty idTender", url);
                return;
            }

            var datePub =
            (n.SelectSingleNode(".//ul[@class = 'b-purchase__footer b-clearfix']/li[@class = 'b-date' ][1]")
                 ?.InnerText ?? "").Trim();
            var dateUpd =
            (n.SelectSingleNode(".//ul[@class = 'b-purchase__footer b-clearfix']/li[@class = 'b-date' ][2]")
                 ?.InnerText ?? "").Trim();
            var tn = new TypeMrsk() {Href = href, IdTender = idTender, DatePub = datePub, DateUpd = dateUpd};
            try
            {
                var t = new TenderTypeMrsk(tn);
                t.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }
        }
    }
}