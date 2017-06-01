using System.IO;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class Tender
    {
        protected readonly JObject t;
        protected readonly FileInfo file;
        protected readonly string region;
        protected readonly int region_id;
        protected readonly string file_path;

        public Tender(FileInfo f, string region, int region_id, JObject json)
        {
            t = json;
            file = f;
            this.region = region;
            this.region_id = region_id;
            file_path = file.ToString();
        }

        public virtual void Parsing()
        {
        }

        public string GetXml(string xml)
        {
            string[] xmlt = xml.Split('/');
            int t = xmlt.Length;
            if (t >= 2)
            {
                xml = xmlt[t - 2] + "/" + xmlt[t - 1];
                return xml;
            }

            return "";
        }
    }
}