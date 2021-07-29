using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public static class JsonExtensions
    {
        public static bool IsNullOrEmpty(this JToken token)
        {
            return (token == null) ||
                   (token.Type == JTokenType.Array && !token.HasValues) ||
                   (token.Type == JTokenType.Object && !token.HasValues) ||
                   (token.Type == JTokenType.String && token.ToString() == string.Empty) ||
                   (token.Type == JTokenType.Null);
        }
        public static XElement StripNs(XElement root) {
            return new XElement(
                root.Name.LocalName,
                root.HasElements ? 
                    root.Elements().Select(el => StripNs(el)) :
                    (object)root.Value
            );
        }

        public static string CheckIsObjOrString(this JToken token)
        {
            if (token.IsNullOrEmpty())
            {
                return null;
            }
            switch (token.Type)
            {
              case JTokenType.String:
                  return (string)token;
              case JTokenType.Object:
                  return null;
              default:
                  return null;
            }
        }
    }
}