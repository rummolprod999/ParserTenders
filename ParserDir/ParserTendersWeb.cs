using System;

namespace ParserTenders.ParserDir
{
    public class ParserTendersWeb: ParserWeb
    {
        private int PageCount = 20;
        public ParserTendersWeb(TypeArguments a) : base(a)
        {
        }

        public override void Parsing()
        {
            for (int i = 1; i <= PageCount; i++)
            {
                string url = $"http://zakupki.gov.ru/epz/order/extendedsearch/results.html?searchString=&morphology=on&pageNumber={i}&sortDirection=false&recordsPerPage=_50&showLotsInfoHidden=false&fz223=on&priceFrom=&priceTo=&currencyId=1&regions=&af=true&ca=true&pc=true&pa=true&sortBy=UPDATE_DATE&openMode=USE_DEFAULT_PARAMS";
                try
                {
                    ParserPage(url);
                }
                catch (Exception e)
                {
                    Log.Logger("Error in ParserTendersWeb.Parsing", e);
                }
            }
        }

        private void ParserPage(string url)
        {
            string s = DownloadString.DownLUserAgent(url);
            Console.WriteLine(s);
        }
    }
}