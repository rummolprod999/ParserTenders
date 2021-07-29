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

        private TypeGnt[] _listUrls = {
            new TypeGnt()
            {
                Type = GntType.ProposalRequest,
                UrlType = "/trades/energo/ProposalRequest/?action=list_published&from=",
                UrlTypeList = "https://www.gazneftetorg.ru/trades/energo/ProposalRequest/?action=list_published&from=0"
            },
            new TypeGnt()
            {
                Type = GntType.ProposalRequest,
                UrlType = "/trades/energo/ProposalRequest2/?action=list_published&from=",
                UrlTypeList = "https://www.gazneftetorg.ru/trades/energo/ProposalRequest2/?action=list_published&from=0"
            },
            new TypeGnt()
            {
                Type = GntType.Tender,
                UrlType = "/trades/energo/Tender/?action=list_published&from=",
                UrlTypeList = "https://www.gazneftetorg.ru/trades/energo/Tender/?action=list_published&from=0"
            },
            new TypeGnt()
            {
            Type = GntType.ProposalRequest,
            UrlType = "/trades/gaz/ProposalRequest/?action=list_published&from=",
            UrlTypeList = "https://www.gazneftetorg.ru/trades/gaz/ProposalRequest/?action=list_published&from=0"
            },
            new TypeGnt()
            {
                Type = GntType.ProposalRequest,
                UrlType = "/trades/corp/ProposalRequest/?action=list_published&from=",
                UrlTypeList = "https://www.gazneftetorg.ru/trades/corp/ProposalRequest/?action=list_published&from=0"
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
            var str = DownloadString.DownL(t.UrlTypeList);
            if (!string.IsNullOrEmpty(str))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(str);
                var maxNumPage = htmlDoc.DocumentNode.SelectSingleNode("(//div[@class=\"page_nav\"]/a)[last()-1]")?.InnerText;
                //Console.WriteLine(maxNumPage);
                if (!string.IsNullOrEmpty(maxNumPage))
                {
                    if (Int32.TryParse(maxNumPage, out var page))
                    {
                        var lPage = new List<string>();
                        var i = 0;
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
            var str = DownloadString.DownL1251(url);
            if (!string.IsNullOrEmpty(str))
            {
                //str = str.Win1251ToUtf8();
                var htmlDoc = new HtmlDocument();

                //var encoding = htmlDoc.DetectEncoding(str);
                htmlDoc.LoadHtml(str);
                //WriteLine(htmlDoc.Encoding);
                var ten = htmlDoc.DocumentNode.SelectNodes("//tr[@class = \"c1\"]") ?? new HtmlNodeCollection(null);
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
            var urlT = (node.SelectSingleNode("td/a[@href]")?.Attributes["href"].Value ?? "").Trim();
            urlT = $"{_site}{urlT}";
            var title1 = (node.SelectSingleNode("td[2]").InnerText ?? "").Trim();
            var title2 = (node.SelectSingleNode("td[2]/a").InnerText ?? "").Trim();
            var entity = $"{title2} {title1}".Trim();
            entity = Regex.Replace(entity, @"\s+", " ");
            entity = System.Net.WebUtility.HtmlDecode(entity);
            var _urlOrg = (node.SelectSingleNode("td[3]/a[@href]")?.Attributes["href"].Value ?? "").Trim();
            var urlOrg = $"{_site}{_urlOrg}";
            var _price = (node.SelectSingleNode("td[4]").InnerText ?? "").Trim();
            var maxPrice = UtilsFromParsing.ParsePrice(_price);
            var _datePub =
                (node.SelectSingleNode("td/span[@title = \"Дата публикации\"]/span")?.InnerText ?? "").Trim();
            var _dateOpen =
                (node.SelectSingleNode("td/span[@title = \"Дата вскрытия конвертов\"]/span")?.InnerText ?? "").Trim();
            
            var _dateRes =
                (node.SelectSingleNode("td/span[@title = \"Дата рассмотрения предложений\"]/span")?.InnerText ?? "")
                .Trim();
            var _dateEnd =
                (node.SelectSingleNode("td/span[@title = \"Дата завершения процедуры\"]/span")?.InnerText ?? "").Trim();
            var datePub = UtilsFromParsing.ParseDateTend(_datePub);
            var dateOpen = UtilsFromParsing.ParseDateTend(_dateOpen);
            if (dateOpen == DateTime.MinValue)
            {
                _dateOpen =
                    (node.SelectSingleNode("td/span[@title = \"Дата вскрытия конвертов\"]/strong/span")?.InnerText ?? "").Trim();
                dateOpen = UtilsFromParsing.ParseDateTend(_dateOpen);
            }
            var dateRes = UtilsFromParsing.ParseDateTend(_dateRes);
            if (dateRes == DateTime.MinValue)
            {
                _dateRes =
                    (node.SelectSingleNode("td/span[@title = \"Дата рассмотрения предложений\"]/strong/span")?.InnerText ?? "").Trim();
                dateRes = UtilsFromParsing.ParseDateTend(_dateRes);
            }
            var dateEnd = UtilsFromParsing.ParseDateTend(_dateEnd);
            var _dateOpenEnd =
                (node.SelectSingleNode("td/span[@title = \"Дата окончания приема предложений\"]/span")?.InnerText ?? "").Trim();
            var dateOpenEnd = UtilsFromParsing.ParseDateTend(_dateOpenEnd);
            if (dateOpenEnd != DateTime.MinValue)
            {
                dateOpen = dateOpenEnd;
            }
            var t = new GntWebTender{UrlTender = urlT, UrlOrg = urlOrg, Entity = entity, MaxPrice = maxPrice, DateEnd = dateEnd, DateOpen = dateOpen, DatePub = datePub, DateRes = dateRes, TypeGnT = tp};
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