using System.IO;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class TenderTypeCancelFailure:Tender
    {
        public TenderTypeCancelFailure(FileInfo f, string region, int region_id, JObject json)
            : base(f, region, region_id, json)
        {
        }

        public override void Parsing()
        {
        }
    }
}