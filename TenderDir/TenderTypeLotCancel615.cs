using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderTypeLotCancel615 : Tender
    {
        public event Action<int> AddLotCancel;

        public TenderTypeLotCancel615(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddLotCancel += delegate(int d)
            {
                if (d > 0)
                    Program.AddLotCancel615++;
            };
        }

        public override void Parsing()
        {
            var root = (JObject) T.SelectToken("export");
            var firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("pprf615"));
            if (firstOrDefault != null)
            {
                var tender = firstOrDefault.Value;
                var purchaseNumber = ((string) tender.SelectToken("commonInfo.purchaseNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("Не могу найти purchaseNumber у TenderLotCancel615", FilePath);
                    return;
                }

                if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                {
                    /*Log.Logger("Тестовый тендер TenderLotCancel", purchaseNumber, file_path);*/
                    return;
                }

                var lotNumber = "1";
                using (var connect = ConnectToDb.GetDbConnection())
                {
                    var idTender = 0;
                    connect.Open();
                    var selectTender =
                        $"SELECT id_tender FROM {Program.Prefix}tender WHERE id_region = @id_region AND purchase_number = @purchase_number AND cancel=0";
                    var cmd = new MySqlCommand(selectTender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_region", RegionId);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    var reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        idTender = reader.GetInt32("id_tender");
                    }
                    reader.Close();
                    if (idTender == 0)
                    {
                        Log.Logger("Не могу найти id_tender у TenderLotCancel615", FilePath);
                        return;
                    }

                    var updateTender =
                        $"UPDATE {Program.Prefix}lot SET cancel=1 WHERE id_tender = @id_tender AND lot_number = @lot_number";
                    var cmd1 = new MySqlCommand(updateTender, connect);
                    cmd1.Prepare();
                    cmd1.Parameters.AddWithValue("@id_tender", idTender);
                    cmd1.Parameters.AddWithValue("@lot_number", lotNumber);
                    var resUpd = cmd1.ExecuteNonQuery();
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