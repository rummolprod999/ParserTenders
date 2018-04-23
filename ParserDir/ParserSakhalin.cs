using System;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using ParserTenders.TenderDir;

namespace ParserTenders.ParserDir
{
    public class ParserSakhalin : ParserWeb
    {
        public ParserSakhalin(TypeArguments ar) : base(ar)
        {
        }

        public override void Parsing()
        {
            string urlpage = "http://www.sakhalinenergy.ru/ru/contractors/tenders/tenders.wbp";
            try
            {
                ParsingPage(urlpage);
            }
            catch (Exception e)
            {
                Log.Logger(e, urlpage);
            }
        }

        private void ParsingPage(string url)
        {
            string s = DownloadString.DownL(url);
            if (String.IsNullOrEmpty(s))
            {
                Log.Logger("Empty string in ParserPage()", url);
                return;
            }
            var parser = new HtmlParser();
            var document = parser.Parse(s);
            var tens = document.All.Where(m => m.ClassList.Contains("vacancies__list__item") && m.TagName == "LI");
            foreach (var t in tens)
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
            var urlT = (t.QuerySelector("div > a")?.GetAttribute("href") ?? "").Trim();
            var purNum = urlT.GetDateFromRegex("tenderId=(.*)");
            if (string.IsNullOrEmpty(purNum))
            {
                purNum = urlT.GetDateFromRegex("ID=(.*)");
            }
            if (string.IsNullOrEmpty(purNum))
            {
                Log.Logger("Cannot find purchase number", urlT);
                return;
            }
            var url = $"http://www.sakhalinenergy.ru/ru/contractors/tenders/{urlT}";
            try
            {
                var ten = new TenderTypeSakhalin(purNum, url);
                ten.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e, url);
            }
        }
        
    }
}