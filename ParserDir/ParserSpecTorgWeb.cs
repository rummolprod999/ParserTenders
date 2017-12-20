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
            string urlT = (node.SelectSingleNode("td[3]/a[@href]")?.Attributes["href"].Value ?? "").Trim();
            urlT = $"{_site}{urlT}";
            //string title1 = (node.SelectSingleNode("td[3]").InnerText ?? "").Trim();
            string title2 = (node.SelectSingleNode("td[3]/a").InnerText ?? "").Trim();
            string entity = $"{title2}".Trim();
            entity = Regex.Replace(entity, @"\s+", " ");
            entity = System.Net.WebUtility.HtmlDecode(entity);
            string _urlOrg = (node.SelectSingleNode("td[2]/a[@href]")?.Attributes["href"].Value ?? "").Trim();
            string urlOrg = $"{_site}{_urlOrg}";
            
            string _datePub =
                (node.SelectSingleNode("td[5]/text()[1]")?.InnerText ?? "").Trim();
            string _dateOpen =
                (node.SelectSingleNode("td[5]/text()[2]")?.InnerText ?? "").Trim();
            DateTime datePub = UtilsFromParsing.ParseDateTend(_datePub);
            DateTime dateOpen = UtilsFromParsing.ParseDateTend(_dateOpen);
            SpecTorgTender t = new SpecTorgTender{UrlTender = urlT, Entity = entity, DateOpen = dateOpen, DatePub = datePub, TypeSpecTorgT = tp};
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
            string urlT = (node.SelectSingleNode("td[3]/a[@href]")?.Attributes["href"].Value ?? "").Trim();
            urlT = $"{_site}{urlT}";
            //string title1 = (node.SelectSingleNode("td[3]").InnerText ?? "").Trim();
            string title2 = (node.SelectSingleNode("td[3]/a").InnerText ?? "").Trim();
            string entity = $"{title2}".Trim();
            entity = Regex.Replace(entity, @"\s+", " ");
            entity = System.Net.WebUtility.HtmlDecode(entity);
            string _urlOrg = (node.SelectSingleNode("td[2]/a[@href]")?.Attributes["href"].Value ?? "").Trim();
            string urlOrg = $"{_site}{_urlOrg}";
            string _price = (node.SelectSingleNode("td[5]").InnerText ?? "").Trim();
            decimal maxPrice = UtilsFromParsing.ParsePrice(_price);
            string _datePub =
                (node.SelectSingleNode("td[6]/text()[1]")?.InnerText ?? "").Trim();
            string _dateOpen =
                (node.SelectSingleNode("td[6]/text()[2]")?.InnerText ?? "").Trim();
            
            string _dateRes =
                (node.SelectSingleNode("td[6]/text()[2]")?.InnerText ?? "")
                .Trim();
            string _dateEnd =
                (node.SelectSingleNode("td[6]/text()[3]")?.InnerText ?? "").Trim();
            DateTime datePub = UtilsFromParsing.ParseDateTend(_datePub);
            DateTime dateOpen = UtilsFromParsing.ParseDateTend(_dateOpen);
            /*if (dateOpen == DateTime.MinValue)
            {
                _dateOpen =
                    (node.SelectSingleNode("td/span[@title = \"Дата окончания приема заявок\"]/strong")?.InnerText ?? "").Trim();
                dateOpen = UtilsFromParsing.ParseDateTend(_dateOpen);
            }*/
            DateTime dateRes = UtilsFromParsing.ParseDateTend(_dateRes);
            /*if (dateRes == DateTime.MinValue)
            {
                _dateRes =
                    (node.SelectSingleNode("td/span[@title = \"Дата рассмотрения заявок\"]/strong")?.InnerText ?? "").Trim();
                dateRes = UtilsFromParsing.ParseDateTend(_dateRes);
            }*/
            DateTime dateEnd = UtilsFromParsing.ParseDateTend(_dateEnd);
            SpecTorgTender t = new SpecTorgTender{UrlTender = urlT, UrlOrg = urlOrg, Entity = entity, MaxPrice = maxPrice, DateEnd = dateEnd, DateOpen = dateOpen, DatePub = datePub, DateRes = dateRes, TypeSpecTorgT = tp};
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
                (node.SelectSingleNode("td/span[@title = \"Дата публикации\"]")?.InnerText ?? "").Trim();
            string _dateOpen =
                (node.SelectSingleNode("td/span[@title = \"Дата окончания приема заявок\"]")?.InnerText ?? "").Trim();
            
            string _dateRes =
                (node.SelectSingleNode("td/span[@title = \"Дата рассмотрения заявок\"]")?.InnerText ?? "")
                .Trim();
            string _dateEnd =
                (node.SelectSingleNode("td/span[@title = \"Дата подведения итогов закупки\"]")?.InnerText ?? "").Trim();
            DateTime datePub = UtilsFromParsing.ParseDateTend(_datePub);
            DateTime dateOpen = UtilsFromParsing.ParseDateTend(_dateOpen);
            if (dateOpen == DateTime.MinValue)
            {
                _dateOpen =
                    (node.SelectSingleNode("td/span[@title = \"Дата окончания приема заявок\"]/strong")?.InnerText ?? "").Trim();
                dateOpen = UtilsFromParsing.ParseDateTend(_dateOpen);
            }
            DateTime dateRes = UtilsFromParsing.ParseDateTend(_dateRes);
            if (dateRes == DateTime.MinValue)
            {
                _dateRes =
                    (node.SelectSingleNode("td/span[@title = \"Дата рассмотрения заявок\"]/strong")?.InnerText ?? "").Trim();
                dateRes = UtilsFromParsing.ParseDateTend(_dateRes);
            }
            DateTime dateEnd = UtilsFromParsing.ParseDateTend(_dateEnd);
            SpecTorgTender t = new SpecTorgTender{UrlTender = urlT, UrlOrg = urlOrg, Entity = entity, MaxPrice = maxPrice, DateEnd = dateEnd, DateOpen = dateOpen, DatePub = datePub, DateRes = dateRes, TypeSpecTorgT = tp};
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