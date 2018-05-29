using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderTypeSign615: Tender
    {
        public event Action<int> AddTenderSign;

        public TenderTypeSign615(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddTenderSign += delegate(int d)
            {
                if (d > 0)
                    Program.AddTenderSign++;
                else
                    Log.Logger("Не удалось добавить TenderSign", FilePath);
            };
        }

        public override void Parsing()
        {
            string xml = GetXml(File.ToString());
            JObject root = (JObject) T.SelectToken("export");
            JProperty firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("pprf615"));
            if (firstOrDefault != null)
            {
                JToken tender = firstOrDefault.Value;
                string purchaseNumber = ((string) tender.SelectToken("id") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("Не могу найти purchaseNumber у sign", FilePath);
                    //return;
                }
                else
                {
                    if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                    {
                        /*Log.Logger("Тестовый тендер sign", purchaseNumber, file_path);*/
                        return;
                    }
                }
                
            }
            else
            {
                Log.Logger("Не могу найти тег TenderSign615", FilePath);
            }

        }
    }
}