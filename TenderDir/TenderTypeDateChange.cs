#region

using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace ParserTenders.TenderDir
{
    public class TenderTypeDateChange : Tender
    {
        public event Action<int> AddDateChange;

        public TenderTypeDateChange(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddDateChange += delegate(int d)
            {
                if (d > 0)
                {
                    Program.AddDateChange++;
                }
            };
        }

        public override void Parsing()
        {
            var root = (JObject)T.SelectToken("export");
            var firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("fcs"));
            if (firstOrDefault != null)
            {
                var tender = firstOrDefault.Value;
                var purchaseNumber = ((string)tender.SelectToken("purchaseNumber") ?? "").Trim();
                if (string.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("Не могу найти purchaseNumber у TenderDateChange", FilePath);
                    return;
                }

                if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                {
                    /*Log.Logger("Тестовый тендер TenderDateChange", purchaseNumber, file_path);*/
                    return;
                }

                var auctionTime =
                    (JsonConvert.SerializeObject(tender.SelectToken("auctionTime") ?? "") ?? "").Trim('"');
                var newAuctionDate = (JsonConvert.SerializeObject(tender.SelectToken("newAuctionDate") ?? "") ?? "")
                    .Trim('"');
                if (string.IsNullOrEmpty(newAuctionDate) || string.IsNullOrEmpty(auctionTime))
                {
                    Log.Logger("Не могу найти newAuctionDate or newAuctionDate у TenderDateChange");
                    return;
                }

                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    var updateTender =
                        $"UPDATE {Program.Prefix}tender SET bidding_date = @bidding_date WHERE id_region = @id_region AND purchase_number = @purchase_number";
                    var cmd = new MySqlCommand(updateTender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_region", RegionId);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    cmd.Parameters.AddWithValue("@bidding_date", newAuctionDate);
                    var resUpd = cmd.ExecuteNonQuery();
                    AddDateChange?.Invoke(resUpd);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег TenderDateChange", FilePath);
            }
        }
    }
}