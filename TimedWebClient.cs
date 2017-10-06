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
}