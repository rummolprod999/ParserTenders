using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderSignProj44 : Tender
    {
        public TenderSignProj44(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddTenderSignProj44 += delegate(int d)
            {
                if (d > 0)
                    Program.AddTenderSignProj44++;
                else
                    Log.Logger("Не удалось добавить TenderSignProj44", FilePath);
            };
        }

        public event Action<int> AddTenderSignProj44;

        public override void Parsing()
        {
        }
    }
}