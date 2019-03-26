using System;
using System.IO;
using System.Linq;
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
            var xml = GetXml(File.ToString());
            var root = (JObject) T.SelectToken("export");
            var firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.StartsWith("cpContractSign"));
            if (firstOrDefault is null)
            {
                Log.Logger("Не могу найти тег TenderSignProj44", FilePath);
                return;
            }

            var tender = firstOrDefault.Value;
            var purchaseNumber = ((string) tender.SelectToken("foundationInfo.purchaseNumber") ?? "").Trim();
            Console.WriteLine(purchaseNumber);
            if (string.IsNullOrEmpty(purchaseNumber))
            {
                Log.Logger("Не могу найти purchaseNumber у sign", FilePath);
                //return;
            }

            using (var connect = ConnectToDb.GetDbConnection())
            {
                var idTender = 0;
                connect.Open();
            }
        }
    }
}