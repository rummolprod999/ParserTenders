using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class ParserWeb : IParserWeb
    {
        protected TypeArguments ar;

        public ParserWeb(TypeArguments ar)
        {
            this.ar = ar;
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