using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class TenderType44 : Tender
    {
        public TenderType44(FileInfo f, string region, int region_id, JObject json)
            : base(f, region, region_id, json)
        {
        }

        public override void Parsing()
        {
            string xml = GetXml(file.ToString());
            JObject root = (JObject)t.SelectToken("export");
            JProperty firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("fcs"));
            if (firstOrDefault != null)
            {
                JToken tender = firstOrDefault.First;
                int id = (int)tender.SelectToken("id");
                Console.WriteLine(id);
            }
        }
    }
}