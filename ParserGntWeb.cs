using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using static System.Console;
namespace ParserTenders
{
    public class ParserGntWeb : ParserWeb
    {
        private string _site = "https://www.gazneftetorg.ru";
        private string _url_list = "/trades/energo/ProposalRequest/?action=list_published&from=";

        private string[] _listUrls =
            {"https://www.gazneftetorg.ru/trades/energo/ProposalRequest/?action=list_published&from=0"};

        public ParserGntWeb(TypeArguments a) : base(a)
        {
        }

        public override void Parsing()
        {
            foreach (var lu in _listUrls)
            {
                try
                {
                    ParserListUrl(lu);
                }
                catch (Exception e)
                {
                    Log.Logger(e);
                }
            }
        }

        private void ParserListUrl(string url)
        {
            string str = DownloadString.DownL(url);
            if (!string.IsNullOrEmpty(str))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(str);
                string maxNumPage = htmlDoc.DocumentNode.SelectSingleNode("(//div[@class=\"page_nav\"]/a)[last()-1]")
                    .InnerText;
                //Console.WriteLine(maxNumPage);
                if (!string.IsNullOrEmpty(maxNumPage))
                {
                    if (Int32.TryParse(maxNumPage, out int page))
                    {
                        List<string> lPage = new List<string>();
                        int i = 0;
                        while (i < page)
                        {
                            lPage.Add($"{_site}{_url_list}{i * 20}");
                            i++;
                        }

                        foreach (var st in lPage)
                        {
                            try
                            {
                                ParserListTend(url);
                            }
                            catch (Exception e)
                            {
                                Log.Logger(e, url);
                            }
                        }
                    }
                }
            }
        }

        private void ParserListTend(string url)
        {
            string str = DownloadString.DownL(url);
            if (!string.IsNullOrEmpty(str))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(str);
                var ten = htmlDoc.DocumentNode.SelectNodes("//tr[@class = \"c1\"]");
                foreach (var v in ten)
                {
                    try
                    {
                        ParserTend(v);
                    }
                    catch (Exception e)
                    {
                        Log.Logger(e, url);
                    }
                }
            }
        }

        private void ParserTend(HtmlNode node)
        {
            string urlT = (node.SelectSingleNode("td/a/@href")?.InnerText ?? "").Trim();
            WriteLine(urlT);
        }
    }
}