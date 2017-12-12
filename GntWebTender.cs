using System;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using static System.Console;
namespace ParserTenders
{
    public class GntWebTender
    {
        public string UrlTender;
        public DateTime DatePub;
        public DateTime DateOpen;
        public DateTime DateRes;
        public DateTime DateEnd;
        public string Entity;
        public string UrlOrg;
        public decimal MaxPrice;
        public GntWebTender()
        {
        }

        public void Parse()
        {
            string str = DownloadString.DownL1251(UrlTender);
            if (!string.IsNullOrEmpty(str))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(str);
                string eis = (htmlDoc.DocumentNode.SelectSingleNode("//td[@class = \"fname\"]").InnerText ?? "").Trim();
                //WriteLine(eis);
                if (eis == "Номер извещения в ЕИС:")
                {
                    string num = (htmlDoc.DocumentNode.SelectSingleNode("//tr[@class = \"c1\"]/td/a[@href]").InnerText ?? "").Trim();
                    Log.Logger("Tender exist on zakupki.gov", num);
                    return;
                }
                string _pNum = (htmlDoc.DocumentNode.SelectSingleNode("//tr[@class = \"thead\"]/td[@colspan = \"2\"]").InnerText ?? "").Trim();
                string pNum = "";
                try
                {
                    pNum = Regex.Match(_pNum, @"\d+").Value;
                }
                catch
                {
                    //ignore
                }
                WriteLine(pNum);
                if (string.IsNullOrEmpty(pNum))
                {
                    Log.Logger("Not extract purchase number", _pNum);
                    return;
                }
            }
        }
    }
}