using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ParserTenders.ParserDir
{
    public class ParserGntWeb : ParserWeb
    {
        public static string _site = "https://www.gazneftetorg.ru";
        //private string _url_list = "/trades/energo/ProposalRequest/?action=list_published&from=";

        private TypeGnt[] _listUrls = new[]
        {
            new TypeGnt()
            {
                Type = GntType.ProposalRequest,
                UrlType = "/trades/energo/ProposalRequest/?action=list_published&from=",
                UrlTypeList = "https://www.gazneftetorg.ru/trades/energo/ProposalRequest/?action=list_published&from=0"
            },
            new TypeGnt()
            {
                Type = GntType.Tender,
                UrlType = "/trades/energo/Tender/?action=list_published&from=",
                UrlTypeList = "https://www.gazneftetorg.ru/trades/energo/Tender/?action=list_published&from=0"
            }
        };

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

        private void ParserListUrl(TypeGnt t)
        {
            string str = DownloadString.DownL(t.UrlTypeList);
            if (!string.IsNullOrEmpty(str))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(str);
                string maxNumPage = htmlDoc.DocumentNode.SelectSingleNode("(//div[@class=\"page_nav\"]/a)[last()-1]")?.InnerText;
                //Console.WriteLine(maxNumPage);
                if (!string.IsNullOrEmpty(maxNumPage))
                {
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

        private void ParserListTend(TypeGnt t, string url)
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
                        ParserTend(t, v);
                    }
                    catch (Exception e)
                    {
                        Log.Logger(e, url);
                    }
                }
            }
        }

        private void ParserTend(TypeGnt tp, HtmlNode node)
        {
            string urlT = (node.SelectSingleNode("td/a[@href]")?.Attributes["href"].Value ?? "").Trim();
            urlT = $"{_site}{urlT}";
            string title1 = (node.SelectSingleNode("td[2]").InnerText ?? "").Trim();
            string title2 = (node.SelectSingleNode("td[2]/a").InnerText ?? "").Trim();
            string entity = $"{title2} {title1}".Trim();
            entity = Regex.Replace(entity, @"\s+", " ");
            entity = System.Net.WebUtility.HtmlDecode(entity);
            string _urlOrg = (node.SelectSingleNode("td[3]/a[@href]")?.Attributes["href"].Value ?? "").Trim();
            string urlOrg = $"{_site}{_urlOrg}";
            string _price = (node.SelectSingleNode("td[4]").InnerText ?? "").Trim();
            decimal maxPrice = UtilsFromParsing.ParsePrice(_price);
            string _datePub =
                (node.SelectSingleNode("td/span[@title = \"Дата публикации\"]/span")?.InnerText ?? "").Trim();
            string _dateOpen =
                (node.SelectSingleNode("td/span[@title = \"Дата вскрытия конвертов\"]/span")?.InnerText ?? "").Trim();
            
            string _dateRes =
                (node.SelectSingleNode("td/span[@title = \"Дата рассмотрения предложений\"]/span")?.InnerText ?? "")
                .Trim();
            string _dateEnd =
                (node.SelectSingleNode("td/span[@title = \"Дата завершения процедуры\"]/span")?.InnerText ?? "").Trim();
            DateTime datePub = UtilsFromParsing.ParseDateTend(_datePub);
            DateTime dateOpen = UtilsFromParsing.ParseDateTend(_dateOpen);
            if (dateOpen == DateTime.MinValue)
            {
                _dateOpen =
                    (node.SelectSingleNode("td/span[@title = \"Дата вскрытия конвертов\"]/strong/span")?.InnerText ?? "").Trim();
                dateOpen = UtilsFromParsing.ParseDateTend(_dateOpen);
            }
            DateTime dateRes = UtilsFromParsing.ParseDateTend(_dateRes);
            if (dateRes == DateTime.MinValue)
            {
                _dateRes =
                    (node.SelectSingleNode("td/span[@title = \"Дата рассмотрения предложений\"]/strong/span")?.InnerText ?? "").Trim();
                dateRes = UtilsFromParsing.ParseDateTend(_dateRes);
            }
            DateTime dateEnd = UtilsFromParsing.ParseDateTend(_dateEnd);
            GntWebTender t = new GntWebTender{UrlTender = urlT, UrlOrg = urlOrg, Entity = entity, MaxPrice = maxPrice, DateEnd = dateEnd, DateOpen = dateOpen, DatePub = datePub, DateRes = dateRes, TypeGnT = tp};
            try
            {
                t.Parse();
            }
            catch (Exception e)
            {
                Log.Logger(e, urlT);
            }
        }
    }
}