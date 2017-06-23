using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class TenderTypeCancel : Tender
    {
        public event Action<int> AddCancel;

        public TenderTypeCancel(FileInfo f, string region, int region_id, JObject json)
            : base(f, region, region_id, json)
        {
            AddCancel += delegate(int d)
            {
                if (d > 0)
                    Program.AddCancel++;
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
                    Log.Logger("Не могу найти purchaseNumber у TenderCancel", file_path);
                    return;
                }
                else
                {
                    if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                    {
                        /*Log.Logger("Тестовый тендер TenderCancel", purchaseNumber, file_path);*/
                        return;
                    }
                }

                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    connect.Open();
                    string update_tender =
                        $"UPDATE {Program.Prefix}tender SET cancel = 1 WHERE id_region = @id_region AND purchase_number = @purchase_number";
                    MySqlCommand cmd = new MySqlCommand(update_tender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_region", region_id);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    int res_upd = cmd.ExecuteNonQuery();
                    AddCancel?.Invoke(res_upd);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег TenderCancel", file_path);
            }
        }
    }
}