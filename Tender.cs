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

        public Tender(FileInfo f, string region, int region_id, JObject json)
        {
            t = json;
            file = f;
            this.region = region;
            this.region_id = region_id;
        }

        public virtual void Parsing()
        {
            
        }
    }
}