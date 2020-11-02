using System;
using System.Linq;
using AngleSharp.Parser.Html;

namespace ParserTenders.ParserDir
{
    public abstract class AbstractParserTektorg : IParserWeb
    {
        private TypeArguments Ar { get; set; }

        protected AbstractParserTektorg(TypeArguments ar)
        {
            Ar = ar;
        }

        protected int GetCountPage(string url)
        {
            var i = 1;
            var s = DownloadString.DownL(url);
            if (String.IsNullOrEmpty(s))
            {
                Log.Logger($"Empty string in {GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name}",
                    url);
                return i;
            }

            var parser = new HtmlParser();
            var document = parser.Parse(s);
            var pages = document.QuerySelectorAll("ul.pagination:first-of-type > li > a[aria-label *= Страница]");
            if (pages.Length > 0)
            {
                i = (from p in pages where p == pages.Last() select int.Parse(p.TextContent)).First();
            }

            return i;
        }

        public abstract void Parsing();
    }
}