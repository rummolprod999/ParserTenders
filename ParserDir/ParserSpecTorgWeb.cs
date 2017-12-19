using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ParserTenders.ParserDir
{
    public class ParserSpecTorgWeb: ParserWeb
    {
        public ParserSpecTorgWeb(TypeArguments a) : base(a)
        {
        }
        public static string _site = "https://www.sstorg.ru";

        private TypeSpecTorg[] _listUrls = new[]
        {
            new TypeSpecTorg()
            {
                Type = SpecTorgType.Advert,
                UrlType = "/market/?action=list_public_pdo_multilot&type=5990&status_group=sg_active&from=",
                UrlTypeList = "https://www.sstorg.ru/market/?action=list_public_pdo_multilot&type=5990&status_group=sg_active&from=0"
            }
        };

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

        private void ParserListUrl(TypeSpecTorg t)
        {
            string str = DownloadString.DownL(t.UrlTypeList);
            if (!string.IsNullOrEmpty(str))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(str);
                string maxNumPage = htmlDoc.DocumentNode.SelectSingleNode("(//div[@class=\"navbar\"]/a)[last()-1]")?.InnerText;
                //Console.WriteLine(maxNumPage);              
                if (!string.IsNullOrEmpty(maxNumPage))
                {
                    maxNumPage = (System.Net.WebUtility.HtmlDecode(maxNumPage)).Trim();
                    if (Int32.TryParse(maxNumPage, out int page))
                    {
                        List<string> lPage = new List<string>();
                        int i = 0;
                        while (i < page)
                        {
                            lPage.Add($"{_site}{t.UrlType}{i * 20}");
                            i++;
                        }

                        foreach (var st in lPage)
                        {
                            try
                            {
                                ParserListTend(t, st);
                            }
                            catch (Exception e)
                            {
                                Log.Logger(e, st);
                            }
                        }
                    }
                }
                else
                {
                    try
                    {
                        ParserListTend(t, t.UrlTypeList);
                    }
                    catch (Exception e)
                    {
                        Log.Logger(e, t.UrlTypeList);
                    }
                }
            }
        }

        private void ParserListTend(TypeSpecTorg t, string url)
        {
            string str = DownloadString.DownL1251(url);
            if (!string.IsNullOrEmpty(str))
            {
                //str = str.Win1251ToUtf8();
                var htmlDoc = new HtmlDocument();

                //var encoding = htmlDoc.DetectEncoding(str);
                htmlDoc.LoadHtml(str);
                //WriteLine(htmlDoc.Encoding);
                var ten = htmlDoc.DocumentNode.SelectNodes("//tr[@class = \"c1\"]");
                foreach (var v in ten)
                {
                    try
                    {
                        //Console.WriteLine(v);
                        switch (t.Type)
                        {
                            case SpecTorgType.Advert:
                                ParserTendAdvert(t, v);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        
                    }
                    catch (Exception e)
                    {
                        Log.Logger(e, url);
                    }
                }
            }
        }

        private void ParserTendAdvert(TypeSpecTorg tp, HtmlNode node)
        {
        }
    }
}