using System;
using AngleSharp.Parser.Html;

namespace ParserTenders.TenderDir
{
    public class TenderTypeTektorgGazprom : TenderBase, ITenderWeb
    {
        public TenderTypeTektorgGazprom(string etpName, string etpUrl, int typeFz, string urltender)
        {
            EtpName = etpName ?? throw new ArgumentNullException(nameof(etpName));
            EtpUrl = etpUrl ?? throw new ArgumentNullException(nameof(etpUrl));
            TypeFz = typeFz;
            UrlTender = urltender;
        }
        public string UrlTender { get;}
        public string EtpName { get; set; }
        public string EtpUrl { get; set; }
        public int TypeFz { get; set; }

        public void Parsing()
        {
            string s = DownloadString.DownL(UrlTender);
            if (String.IsNullOrEmpty(s))
            {
                Log.Logger($"Empty string in {GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name}",
                    UrlTender);
            }

            var parser = new HtmlParser();
            var document = parser.Parse(s);
        }
    }
}