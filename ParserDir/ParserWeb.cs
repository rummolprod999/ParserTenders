using System.Collections.Generic;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace ParserTenders.ParserDir
{
    public class ParserWeb : IParserWeb
    {
        protected TypeArguments Ar;

        public ParserWeb(TypeArguments ar)
        {
            this.Ar = ar;
        }

        public virtual void Parsing()
        {
        }
        
        public virtual void ParsingProc(ProcedureGpB pr)
        {
        }
        
        public List<JToken> GetElements(JToken j, string s)
        {
            List<JToken> els = new List<JToken>();
            var elsObj = j.SelectToken(s);
            if (elsObj != null && elsObj.Type != JTokenType.Null)
            {
                switch (elsObj.Type)
                {
                    case JTokenType.Object:
                        els.Add(elsObj);
                        break;
                    case JTokenType.Array:
                        els.AddRange(elsObj);
                        break;
                }
            }

            return els;
        }
        
        protected int MaxPage(string u)
        {
            if (DownloadString.MaxDownload >= 1000) return 1;
            var s = DownloadString.DownLUserAgentEis(u);
            if (string.IsNullOrEmpty(s))
            {
                Log.Logger("cannot get first page from EIS", u);
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(s);
            var maxPageS =
                htmlDoc.DocumentNode.SelectSingleNode("//ul[@class = 'pages']/li[last()]/a/span")?.InnerText ?? "1";
            if (int.TryParse(maxPageS, out var maxP))
            {
                return maxP;
            }

            return 10;
        }
    }
}