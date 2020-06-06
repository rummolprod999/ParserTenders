using System;
using System.Net;

namespace ParserTenders
{
    public class TimedWebClient: WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest wr = base.GetWebRequest(address);
            wr.Timeout = 300000;
            return wr;
        }
    }
    
    public class TimedWebClientUa: WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest wr = (HttpWebRequest)base.GetWebRequest(address);
            wr.Timeout = 60000;
            wr.UserAgent = "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:55.0) Gecko/20100101 Firefox/55.0";
            /*wr.Headers[HttpRequestHeader.UserAgent] =
                "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:55.0) Gecko/20100101 Firefox/55.0";*/
            /*wr.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:55.0) Gecko/20100101 Firefox/55.0");*/
            return wr;
        }
    }
    public class TimedWebClientUaEis: WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest wr = (HttpWebRequest)base.GetWebRequest(address);
            wr.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            wr.Timeout = 90000;
            //wr.UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.131 Safari/537.36";
            wr.UserAgent = RandonUaNET.RandomUa.RandomUserAgent;
            //wr.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,#1#*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            /*wr.Headers.Add(HttpRequestHeader.AcceptLanguage, "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            wr.Headers.Add(HttpRequestHeader.Cookie, "routeepz7=1; routeepz5=4; routeepz0=0; routeepzCons=0; routeepz2=5; SL_GWPT_Show_Hide_tmp=1; SL_wptGlobTipTmp=1; jforumUserId=1");
            wr.Headers.Add(HttpRequestHeader.Pragma, "no-cache");
            wr.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");*/
            /*wr.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");*/
            wr.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.None;
            return wr;
        }
    }
}