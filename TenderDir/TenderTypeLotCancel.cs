using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderTypeLotCancel : Tender
    {
        public event Action<int> AddLotCancel;

        public TenderTypeLotCancel(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddLotCancel += delegate(int d)
            {
                if (d > 0)
                    Program.AddLotCancel++;
            };
        }

        public override void Parsing()
        {
            JObject root = (JObject) T.SelectToken("export");
            JProperty firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("fcs"));
            if (firstOrDefault != null)
            {
                JToken tender = firstOrDefault.Value;
                string purchaseNumber = ((string) tender.SelectToken("purchaseNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("Не могу найти purchaseNumber у TenderLotCancel", FilePath);
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
                    Log.Logger("Не могу найти lotNumber у TenderLotCancel", FilePath);
                    return;
                }

                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    int idTender = 0;
                    connect.Open();
                    string selectTender =
                        $"SELECT id_tender FROM {Program.Prefix}tender WHERE id_region = @id_region AND purchase_number = @purchase_number AND cancel=0";
                    MySqlCommand cmd = new MySqlCommand(selectTender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_region", RegionId);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        idTender = reader.GetInt32("id_tender");
                    }
                    reader.Close();
                    if (idTender == 0)
                    {
                        Log.Logger("Не могу найти id_tender у TenderLotCancel", FilePath);
                        return;
                    }

                    string updateTender =
                        $"UPDATE {Program.Prefix}lot SET cancel=1 WHERE id_tender = @id_tender AND lot_number = @lot_number";
                    MySqlCommand cmd1 = new MySqlCommand(updateTender, connect);
                    cmd1.Prepare();
                    cmd1.Parameters.AddWithValue("@id_tender", idTender);
                    cmd1.Parameters.AddWithValue("@lot_number", lotNumber);
                    int resUpd = cmd1.ExecuteNonQuery();
                    AddLotCancel?.Invoke(resUpd);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег TenderLotCancel", FilePath);
            }
        }
    }
}