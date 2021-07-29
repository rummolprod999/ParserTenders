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

        private TypeSpecTorg[] _listUrls = {
            new TypeSpecTorg()
            {
                Type = SpecTorgType.Advert,
                UrlType = "/market/?action=list_public_pdo_multilot&type=5990&status_group=sg_active&from=",
                UrlTypeList = "https://www.sstorg.ru/market/?action=list_public_pdo_multilot&type=5990&status_group=sg_active&from=0"
            },
            new TypeSpecTorg()
            {
                Type = SpecTorgType.RequestCustomer,
                UrlType = "/market/?action=list_public_pdo_multilot&type=5360&status_group=sg_active&from=",
                UrlTypeList = "https://www.sstorg.ru/market/?action=list_public_pdo_multilot&type=5360&status_group=sg_active&from=0"
            },
            new TypeSpecTorg()
            {
                Type = SpecTorgType.Request,
                UrlType = "/trades/corporate/ProposalRequest/?action=list_published&from=",
                UrlTypeList = "https://www.sstorg.ru/trades/corporate/ProposalRequest/?action=list_published&from=0"
            },
            new TypeSpecTorg()
            {
                Type = SpecTorgType.Auction,
                UrlType = "/market/?action=list_public_auctions&type=5560&status_group=sg_published&from=",
                UrlTypeList = "https://www.sstorg.ru/market/?action=list_public_auctions&type=5560&status_group=sg_published&from=0"
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
            var str = DownloadString.DownL(t.UrlTypeList);
            if (!string.IsNullOrEmpty(str))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(str);
                var maxNumPage = htmlDoc.DocumentNode.SelectSingleNode("(//div[@class=\"navbar\"]/a)[last()-1]")?.InnerText;
                //Console.WriteLine(maxNumPage);              
                if (!string.IsNullOrEmpty(maxNumPage))
                {
                    maxNumPage = (System.Net.WebUtility.HtmlDecode(maxNumPage)).Trim();
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

        private void ParserListTend(TypeSpecTorg t, string url)
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
                        //Console.WriteLine(v);
                        switch (t.Type)
                        {
                            case SpecTorgType.Advert:
                            case SpecTorgType.RequestCustomer:
                                ParserTendAdvert(t, v);
                                break;
                            case SpecTorgType.Request:
                                ParserRequest(t, v);
                                break;
                            case SpecTorgType.Auction:
                                ParserTendAuction(t, v);
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
            var urlT = (node.SelectSingleNode("td[3]/a[@href]")?.Attributes["href"].Value ?? "").Trim();
            urlT = $"{_site}{urlT}";
            //string title1 = (node.SelectSingleNode("td[3]").InnerText ?? "").Trim();
            var title2 = (node.SelectSingleNode("td[3]/a").InnerText ?? "").Trim();
            var entity = $"{title2}".Trim();
            entity = Regex.Replace(entity, @"\s+", " ");
            entity = System.Net.WebUtility.HtmlDecode(entity);
            var _urlOrg = (node.SelectSingleNode("td[2]/a[@href]")?.Attributes["href"].Value ?? "").Trim();
            var urlOrg = $"{_site}{_urlOrg}";
            
            var _datePub =
                (node.SelectSingleNode("td[5]/text()[1]")?.InnerText ?? "").Trim();
            var _dateOpen =
                (node.SelectSingleNode("td[5]/text()[2]")?.InnerText ?? "").Trim();
            var datePub = UtilsFromParsing.ParseDateTend(_datePub);
            var dateOpen = UtilsFromParsing.ParseDateTend(_dateOpen);
            var t = new SpecTorgTender{UrlTender = urlT, Entity = entity, DateOpen = dateOpen, DatePub = datePub, TypeSpecTorgT = tp};
            try
            {
                
                switch (tp.Type)
                {
                    case SpecTorgType.Advert:
                        t.ParseAdvert();
                        break;
                    case SpecTorgType.RequestCustomer:
                        t.ParseRequestCustomer();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                Log.Logger(e, urlT);
            }
            
        }
        
        private void ParserTendAuction(TypeSpecTorg tp, HtmlNode node)
        {
            var urlT = (node.SelectSingleNode("td[3]/a[@href]")?.Attributes["href"].Value ?? "").Trim();
            urlT = $"{_site}{urlT}";
            //string title1 = (node.SelectSingleNode("td[3]").InnerText ?? "").Trim();
            var title2 = (node.SelectSingleNode("td[3]/a").InnerText ?? "").Trim();
            var entity = $"{title2}".Trim();
            entity = Regex.Replace(entity, @"\s+", " ");
            entity = System.Net.WebUtility.HtmlDecode(entity);
            var _urlOrg = (node.SelectSingleNode("td[2]/a[@href]")?.Attributes["href"].Value ?? "").Trim();
            var urlOrg = $"{_site}{_urlOrg}";
            var _price = (node.SelectSingleNode("td[5]").InnerText ?? "").Trim();
            var maxPrice = UtilsFromParsing.ParsePrice(_price);
            var _datePub =
                (node.SelectSingleNode("td[6]/text()[1]")?.InnerText ?? "").Trim();
            var _dateOpen =
                (node.SelectSingleNode("td[6]/text()[2]")?.InnerText ?? "").Trim();
            
            var _dateRes =
                (node.SelectSingleNode("td[6]/text()[2]")?.InnerText ?? "")
                .Trim();
            var _dateEnd =
                (node.SelectSingleNode("td[6]/text()[3]")?.InnerText ?? "").Trim();
            var datePub = UtilsFromParsing.ParseDateTend(_datePub);
            var dateOpen = UtilsFromParsing.ParseDateTend(_dateOpen);
            /*if (dateOpen == DateTime.MinValue)
            {
                _dateOpen =
                    (node.SelectSingleNode("td/span[@title = \"Дата окончания приема заявок\"]/strong")?.InnerText ?? "").Trim();
                dateOpen = UtilsFromParsing.ParseDateTend(_dateOpen);
            }*/
            var dateRes = UtilsFromParsing.ParseDateTend(_dateRes);
            /*if (dateRes == DateTime.MinValue)
            {
                _dateRes =
                    (node.SelectSingleNode("td/span[@title = \"Дата рассмотрения заявок\"]/strong")?.InnerText ?? "").Trim();
                dateRes = UtilsFromParsing.ParseDateTend(_dateRes);
            }*/
            var dateEnd = UtilsFromParsing.ParseDateTend(_dateEnd);
            var t = new SpecTorgTender{UrlTender = urlT, UrlOrg = urlOrg, Entity = entity, MaxPrice = maxPrice, DateEnd = dateEnd, DateOpen = dateOpen, DatePub = datePub, DateRes = dateRes, TypeSpecTorgT = tp};
            try
            {
                switch (tp.Type)
                {
                    
                    case SpecTorgType.Auction:
                        t.ParseAuction();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
            }
            catch (Exception e)
            {
                Log.Logger(e, urlT);
            }
        }
        
        private void ParserRequest(TypeSpecTorg tp, HtmlNode node)
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
                (node.SelectSingleNode("td/span[@title = \"Дата публикации\"]")?.InnerText ?? "").Trim();
            var _dateOpen =
                (node.SelectSingleNode("td/span[@title = \"Дата окончания приема заявок\"]")?.InnerText ?? "").Trim();
            
            var _dateRes =
                (node.SelectSingleNode("td/span[@title = \"Дата рассмотрения заявок\"]")?.InnerText ?? "")
                .Trim();
            var _dateEnd =
                (node.SelectSingleNode("td/span[@title = \"Дата подведения итогов закупки\"]")?.InnerText ?? "").Trim();
            var datePub = UtilsFromParsing.ParseDateTend(_datePub);
            var dateOpen = UtilsFromParsing.ParseDateTend(_dateOpen);
            if (dateOpen == DateTime.MinValue)
            {
                _dateOpen =
                    (node.SelectSingleNode("td/span[@title = \"Дата окончания приема заявок\"]/strong")?.InnerText ?? "").Trim();
                dateOpen = UtilsFromParsing.ParseDateTend(_dateOpen);
            }
            var dateRes = UtilsFromParsing.ParseDateTend(_dateRes);
            if (dateRes == DateTime.MinValue)
            {
                _dateRes =
                    (node.SelectSingleNode("td/span[@title = \"Дата рассмотрения заявок\"]/strong")?.InnerText ?? "").Trim();
                dateRes = UtilsFromParsing.ParseDateTend(_dateRes);
            }
            var dateEnd = UtilsFromParsing.ParseDateTend(_dateEnd);
            var t = new SpecTorgTender{UrlTender = urlT, UrlOrg = urlOrg, Entity = entity, MaxPrice = maxPrice, DateEnd = dateEnd, DateOpen = dateOpen, DatePub = datePub, DateRes = dateRes, TypeSpecTorgT = tp};
            try
            {
                t.ParseRequest();
            }
            catch (Exception e)
            {
                Log.Logger(e, urlT);
            }
        }
    }
}