using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderTypeCancel223 : Tender
    {
        public event Action<int> AddCancel223;

        public TenderTypeCancel223(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddCancel223 += delegate(int d)
            {
                if (d > 0)
                    Program.AddCancel223++;
            };
        }

        public override void Parsing()
        {
            JProperty tend = null;
            var firstOrDefault = T.Properties()
                .FirstOrDefault(p => p.Name.StartsWith("purchase", StringComparison.Ordinal));
            var firstOrDefault2 = ((JObject) firstOrDefault?.Value)?.Properties()
                .FirstOrDefault(p => p.Name.StartsWith("body", StringComparison.Ordinal));
            var firstOrDefault3 = ((JObject) firstOrDefault2?.Value)?.Properties()
                .FirstOrDefault(p => p.Name.StartsWith("item", StringComparison.Ordinal));
            if (firstOrDefault3 != null)
            {
                tend = ((JObject) firstOrDefault3.Value).Properties()
                    .FirstOrDefault(p => p.Name.StartsWith("purchase", StringComparison.Ordinal));
            }

            if (tend != null)
            {
                var tender = tend.Value;
                var purchaseNumber = ((string) tender.SelectToken("cancelNoticeRegistrationNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("У тендера нет purchaseNumber", FilePath);
                    return;
                }

                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    var updateTender =
                        $"UPDATE {Program.Prefix}tender SET cancel = 1 WHERE id_region = @id_region AND purchase_number = @purchase_number";
                    var cmd = new MySqlCommand(updateTender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_region", RegionId);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    var resUpd = cmd.ExecuteNonQuery();
                    AddCancel223?.Invoke(resUpd);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег TenderCancel223", FilePath);
            }
        }
    }
}