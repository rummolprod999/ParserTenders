using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderTypeDateChange615 : Tender
    {
        public event Action<int> AddDateChange;

        public TenderTypeDateChange615(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddDateChange += delegate(int d)
            {
                if (d > 0)
                    Program.AddDateChange615++;
            };
        }

        public override void Parsing()
        {
            JObject root = (JObject) T.SelectToken("export");
            JProperty firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("pprf615"));
            if (firstOrDefault != null)
            {
                JToken tender = firstOrDefault.Value;
                string purchaseNumber = ((string) tender.SelectToken("purchaseNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("Не могу найти purchaseNumber у TenderDateChange615", FilePath);
                    return;
                }

                if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                {
                    /*Log.Logger("Тестовый тендер TenderDateChange", purchaseNumber, file_path);*/
                    return;
                }

                string auctionTime =
                    (JsonConvert.SerializeObject(tender.SelectToken("auctionTime") ?? "") ?? "").Trim('"');
                if (String.IsNullOrEmpty(auctionTime))
                {
                    Log.Logger("Не могу найти AuctionDate у TenderDateChange615");
                    return;
                }

                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    string updateTender =
                        $"UPDATE {Program.Prefix}tender SET bidding_date = @bidding_date WHERE id_region = @id_region AND purchase_number = @purchase_number";
                    MySqlCommand cmd = new MySqlCommand(updateTender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_region", RegionId);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    cmd.Parameters.AddWithValue("@bidding_date", auctionTime);
                    int resUpd = cmd.ExecuteNonQuery();
                    AddDateChange?.Invoke(resUpd);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег TenderDateChange615", FilePath);
            }
        }
    }
}