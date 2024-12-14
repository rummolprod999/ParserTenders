#region

using System;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using ParserTenders.TenderDir;

#endregion

namespace ParserTenders.ParserDir
{
    public class ParserRosneft : ParserWeb
    {
        private const int Count = 100;
        private bool ct = true;

        public ParserRosneft(TypeArguments ar) : base(ar)
        {
        }

        public override void Parsing()
        {
            for (var i = 0; i <= Count; i++)
            {
                if (!ct)
                {
                    break;
                }

                var urlpage = $"http://zakupki.rosneft.ru/zakupki?page={i}";
                try
                {
                    ParsingPage(urlpage);
                }
                catch (Exception e)
                {
                    Log.Logger(e, urlpage);
                }
            }
        }

        private void ParsingPage(string url)
        {
            var s = DownloadString.DownL(url);
            if (string.IsNullOrEmpty(s))
            {
                Log.Logger("Empty string in ParserPage()", url);
                return;
            }

            var parser = new HtmlParser();
            var document = parser.Parse(s);
            var tens = document.All.Where(m => m.ClassList.Contains("even") || m.ClassList.Contains("odd"));
            var enumerable = tens.ToList();
            if (!enumerable.Any())
            {
                ct = false;
                Log.Logger("Last page", url);
                return;
            }

            foreach (var t in enumerable)
            {
                try
                {
                    ParsingTender(t);
                }
                catch (Exception e)
                {
                    Log.Logger(e);
                }
            }
        }

        private void ParsingTender(IElement t)
        {
            var purNum = (t.QuerySelector("td.views-field-field-number-zakup-value > a")?.TextContent ?? "").Trim();
            var url =
                (t.QuerySelector("td.views-field-field-number-zakup-value > a")?.GetAttribute("href") ?? "").Trim();
            var placingWay = (t.QuerySelector("td.views-field-field-zakup-type-value")?.TextContent ?? "").Trim();
            var datePubT = (t.QuerySelector("td.views-field-created")?.TextContent ?? "").Trim();
            var dateEndT = (t.QuerySelector("td.views-field-field-zakup-end-value")?.TextContent ?? "").Trim();
            var datePub = datePubT.ParseDateUn("dd.MM.yyyy - HH:mm");
            var dateEnd = dateEndT.ParseDateUn("dd.MM.yyyy - HH:mm");
            try
            {
                var ten = new TenderTypeRosneft(purNum, url, placingWay, datePub, dateEnd);
                ten.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e, url);
            }
        }
    }
}