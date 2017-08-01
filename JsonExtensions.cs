using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace ParserTenders
{
    public static class JsonExtensions
    {
        public static bool IsNullOrEmpty(this JToken token)
        {
            return (token == null) ||
                   (token.Type == JTokenType.Array && !token.HasValues) ||
                   (token.Type == JTokenType.Object && !token.HasValues) ||
                   (token.Type == JTokenType.String && token.ToString() == String.Empty) ||
                   (token.Type == JTokenType.Null);
        }
        public static XElement stripNS(XElement root) {
            return new XElement(
                root.Name.LocalName,
                root.HasElements ? 
                    root.Elements().Select(el => stripNS(el)) :
                    (object)root.Value
            );
        }
    }
}