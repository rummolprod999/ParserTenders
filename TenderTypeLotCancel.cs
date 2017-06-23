using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class TenderTypeLotCancel : Tender
    {
        public event Action<int> AddLotCancel;

        public TenderTypeLotCancel(FileInfo f, string region, int region_id, JObject json)
            : base(f, region, region_id, json)
        {
            AddLotCancel += delegate(int d)
            {
                if (d > 0)
                    Program.AddLotCancel++;
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
                    Log.Logger("Не могу найти purchaseNumber у TenderLotCancel", file_path);
                    return;
                }
                else
                {
                    if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                    {
                        /*Log.Logger("Тестовый тендер TenderLotCancel", purchaseNumber, file_path);*/
                        return;
                    }
                }

                string lotNumber = ((string) tender.SelectToken("lot.lotNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(lotNumber))
                {
                    Log.Logger("Не могу найти lotNumber у TenderLotCancel", file_path);
                    return;
                }

                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    int id_tender = 0;
                    connect.Open();
                    string select_tender =
                        $"SELECT id_tender FROM {Program.Prefix}tender WHERE id_region = @id_region AND purchase_number = @purchase_number AND cancel=0";
                    MySqlCommand cmd = new MySqlCommand(select_tender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_region", region_id);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        id_tender = reader.GetInt32("id_tender");
                    }
                    reader.Close();
                    if (id_tender == 0)
                    {
                        Log.Logger("Не могу найти id_tender у TenderLotCancel", file_path);
                        return;
                    }

                    string update_tender =
                        $"UPDATE {Program.Prefix}lot SET cancel=1 WHERE id_tender = @id_tender AND lot_number = @lot_number";
                    MySqlCommand cmd1 = new MySqlCommand(update_tender, connect);
                    cmd1.Prepare();
                    cmd1.Parameters.AddWithValue("@id_tender", id_tender);
                    cmd1.Parameters.AddWithValue("@lot_number", lotNumber);
                    int res_upd = cmd1.ExecuteNonQuery();
                    AddLotCancel?.Invoke(res_upd);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег TenderLotCancel", file_path);
            }
        }
    }
}