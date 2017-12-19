using System.Collections.Generic;
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
    }
}