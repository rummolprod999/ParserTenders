using System;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using ParserTenders.TenderDir;

namespace ParserTenders.ParserDir
{
    public class ParserTektorgRzd : AbstractParserTektorg
    {
        private int _dateMinus => 35;

        public ParserTektorgRzd(TypeArguments ar) : base(ar)
        {
        }

        public override void Parsing()
        {
            var dateM = DateTime.Now.AddMinutes(-1 * _dateMinus * 24 * 60);
            var urlStart = $"https://www.tektorg.ru/rzd/procedures?dpfrom={dateM:dd.MM.yyyy}";
            int max = 0;
            try
            {
                max = GetCountPage(urlStart);
            }
            catch (Exception e)
            {
                Log.Logger(
                    $"Exception recieve count page in {GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name}",
                    e, urlStart);
            }

            if (max == 0)
            {
                Log.Logger(
                    $"Null count page in {GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name}",
                    urlStart);
                return;
            }

            for (int i = 1; i <= max; i++)
            {
                var url = $"{urlStart}&page={i}";
                try
                {
                    ParsingPage(url);
                }
                catch (Exception e)
                {
                    Log.Logger(
                        $"Exception in {GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name}",
                        e, urlStart);
                }
            }
        }

        private void ParsingPage(string url)
        {
            string s = DownloadString.DownL(url);
            if (String.IsNullOrEmpty(s))
            {
                Log.Logger($"Empty string in {GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name}",
                    url);
            }

            var parser = new HtmlParser();
            var document = parser.Parse(s);
            var tens = document.All.Where(m => m.ClassList.Contains("section-procurement__item") && m.TagName == "DIV");
            foreach (var t in tens)
            {
                try
                {
                    ParsingTender(t, url);
                }
                catch (Exception e)
                {
                    Log.Logger(e);
                }
            }
        }

        private void ParsingTender(IElement t, string url)
        {
            var urlT = (t.QuerySelector("a.section-procurement__item-title")?.GetAttribute("href") ?? "").Trim();
            if (string.IsNullOrEmpty(urlT))
            {
                Log.Logger($"Empty string in {GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name}",
                    url);
            }

            var tenderUrl = urlT;
            if (!urlT.Contains("https://")) tenderUrl = $"https://www.tektorg.ru{urlT}";
            try
            {
                var ten = new TenderTypeTektorgRzd("ТЭК Торг РЖД",
                    "https://www.tektorg.ru/rzd/procedures", 25, tenderUrl);
                ten.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e, tenderUrl);
            }
        }
    }
}