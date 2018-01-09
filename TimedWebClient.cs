using System;
using System.Net;

namespace ParserTenders
{
    public class TimedWebClient: WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest wr = base.GetWebRequest(address);
            wr.Timeout = 600000;
            return wr;
        }
    }
    
    public class TimedWebClientUa: WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest wr = (HttpWebRequest)base.GetWebRequest(address);
            wr.Timeout = 600000;
            wr.UserAgent = "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:55.0) Gecko/20100101 Firefox/55.0";
            /*wr.Headers[HttpRequestHeader.UserAgent] =
                "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:55.0) Gecko/20100101 Firefox/55.0";*/
            /*wr.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:55.0) Gecko/20100101 Firefox/55.0");*/
            return wr;
        }
    }
}