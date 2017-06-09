using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class TenderTypeDateChange : Tender
    {
        public event Action<int> AddDateChange;

        public TenderTypeDateChange(FileInfo f, string region, int region_id, JObject json)
            : base(f, region, region_id, json)
        {
            AddDateChange += delegate(int d)
            {
                if (d > 0)
                    Program.AddDateChange++;
            };
        }

        public override void Parsing()
        {
            JObject root = (JObject) t.SelectToken("export");
            JProperty firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("fcs"));
            if (firstOrDefault != null)
            {
                JToken tender = firstOrDefault.Value;
                string purchaseNumber = ((string) tender.SelectToken("purchaseNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("Не могу найти purchaseNumber у TenderDateChange", file_path);
                    return;
                }
                else
                {
                    if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                    {
                        Log.Logger("Тестовый тендер TenderDateChange", purchaseNumber, file_path);
                        return;
                    }
                }

                string auctionTime =
                    (JsonConvert.SerializeObject(tender.SelectToken("auctionTime") ?? "") ?? "").Trim('"');
                string newAuctionDate = (JsonConvert.SerializeObject(tender.SelectToken("newAuctionDate") ?? "") ?? "")
                    .Trim('"');
                if (String.IsNullOrEmpty(newAuctionDate) || String.IsNullOrEmpty(auctionTime))
                {
                    Log.Logger("Не могу найти newAuctionDate or newAuctionDate у TenderDateChange");
                    return;
                }

                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    connect.Open();
                    string update_tender =
                        $"UPDATE {Program.Prefix}tender SET bidding_date = @bidding_date WHERE id_region = @id_region AND purchase_number = @purchase_number";
                    MySqlCommand cmd = new MySqlCommand(update_tender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_region", region_id);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    cmd.Parameters.AddWithValue("@bidding_date", newAuctionDate);
                    int res_upd = cmd.ExecuteNonQuery();
                    AddDateChange?.Invoke(res_upd);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег TenderDateChange", file_path);
            }
        }
    }
}