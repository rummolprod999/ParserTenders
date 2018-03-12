using System;
using System.Linq;
using AngleSharp.Parser.Html;

namespace ParserTenders.ParserDir
{
    public class ParserRosneft : ParserWeb
    {
        private const int Count = 20;
        public ParserRosneft(TypeArguments ar) : base(ar)
        {
        }
        
        public override void Parsing()
        {
            for (int i = 0; i <= Count; i++)
            {
                string urlpage = $"http://zakupki.rosneft.ru/zakupki?page={i}";
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
            string s = DownloadString.DownL(url);
            if (String.IsNullOrEmpty(s))
            {
                Log.Logger("Empty string in ParserPage()", url);
                return;
            }
            var parser = new HtmlParser();
            var document = parser.Parse(s);
            var tens = document.All.Where(m => m.ClassList.Contains("even") || m.ClassList.Contains("odd"));
            foreach (var t in tens)
            {
                Console.WriteLine(t.InnerHtml);
            }
        }
    }
}