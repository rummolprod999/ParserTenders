using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ParserTenders.ParserDir
{
    public class ParserObTorgWeb: ParserWeb
    {
        public static string _site = "https://www.oborontorg.ru";
        
        private TypeObTorg[] _listUrls = {
            new TypeObTorg()
            {
                Type = ObTorgType.ProposalRequest,
                UrlType = "/trades/corporate/ProposalRequest/?action=list_published&from=",
                UrlTypeList = "https://www.oborontorg.ru/trades/corporate/ProposalRequest/?action=list_published&from=0"
            },
            new TypeObTorg()
            {
                Type = ObTorgType.ProposalRequest,
                UrlType = "/trades/corporate/ProposalRequest/?action=list_active&from=",
                UrlTypeList = "https://www.oborontorg.ru/trades/corporate/ProposalRequest/?action=list_active&from=0"
            },
            new TypeObTorg()
            {
                Type = ObTorgType.Auction,
                UrlType = "/market/?action=list_public_auctions&type=1560&status_group=sg_published&from=",
                UrlTypeList = "https://www.oborontorg.ru/market/?action=list_public_auctions&type=1560&status_group=sg_published&from=0"
            },
            new TypeObTorg()
            {
                Type = ObTorgType.Auction,
                UrlType = "/market/?action=list_public_auctions&type=1560&status_group=sg_active&from=",
                UrlTypeList = "https://www.oborontorg.ru/market/?action=list_public_auctions&type=1560&status_group=sg_active&from=0"
            },
            new TypeObTorg()
            {
                Type = ObTorgType.ProcedurePurchase,
                UrlType = "/trades/corporate/ProcedurePurchase/?action=list_published&from=",
                UrlTypeList = "https://www.oborontorg.ru/trades/corporate/ProcedurePurchase/?action=list_published"
            },
            new TypeObTorg()
            {
                Type = ObTorgType.ProcedurePurchase,
                UrlType = "/trades/corporate/ProcedurePurchase/?action=list_active&from=",
                UrlTypeList = "https://www.oborontorg.ru/trades/corporate/ProcedurePurchase/?action=list_active"
            }
        };
        
        public ParserObTorgWeb(TypeArguments a) : base(a)
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
        
        private void ParserListUrl(TypeObTorg t)
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
        
        private void ParserListTend(TypeObTorg t, string url)
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
                            case ObTorgType.ProposalRequest:
                            case ObTorgType.ProcedurePurchase:
                                ParserTend(t, v);
                                break;
                            case ObTorgType.Auction:
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
        
        private void ParserTend(TypeObTorg tp, HtmlNode node)
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
            if (string.IsNullOrEmpty(_dateEnd))
            {
                _dateEnd =
                    (node.SelectSingleNode("td/span[@title = \"Дата подведения итогов процедуры\"]")?.InnerText ?? "").Trim();
            }
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
            var status = (node.SelectSingleNode("td[7]").InnerText ?? "").Trim();
            var t = new ObTorgWebTender{UrlTender = urlT, UrlOrg = urlOrg, Entity = entity, MaxPrice = maxPrice, DateEnd = dateEnd, DateOpen = dateOpen, DatePub = datePub, DateRes = dateRes, TypeObTorgT = tp, Status = status};
            try
            {
                t.Parse();
            }
            catch (Exception e)
            {
                Log.Logger(e, urlT);
            }
        }
        
        private void ParserTendAuction(TypeObTorg tp, HtmlNode node)
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
            var t = new ObTorgWebTender{UrlTender = urlT, UrlOrg = urlOrg, Entity = entity, MaxPrice = maxPrice, DateEnd = dateEnd, DateOpen = dateOpen, DatePub = datePub, DateRes = dateRes, TypeObTorgT = tp};
            try
            {
                switch (tp.Type)
                {
                    case ObTorgType.ProposalRequest:
                    case ObTorgType.ProcedurePurchase:
                        t.Parse();
                        break;
                    case ObTorgType.Auction:
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
    }
}