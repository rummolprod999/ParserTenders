using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderTypeProlongation : Tender
    {
        public event Action<int> AddProlongation;

        public TenderTypeProlongation(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddProlongation += delegate(int d)
            {
                if (d > 0)
                    Program.AddProlongation++;
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
                    Log.Logger("Не могу найти purchaseNumber у TenderProlongation", FilePath);
                    return;
                }
                else
                {
                    if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                    {
                        /*Log.Logger("Тестовый тендер TenderProlongation", purchaseNumber, file_path);*/
                        return;
                    }
                }

                string collectingEndDate =
                    (JsonConvert.SerializeObject(tender.SelectToken("collectingEndDate") ?? "") ?? "").Trim('"');
                string collectingProlongationDate =
                    (JsonConvert.SerializeObject(tender.SelectToken("collectingProlongationDate") ?? "") ?? "")
                    .Trim('"');
                string scoringDate =
                    (JsonConvert.SerializeObject(tender.SelectToken("scoringDate") ?? "") ?? "").Trim('"');
                string scoringProlongationDate =
                    (JsonConvert.SerializeObject(tender.SelectToken("scoringProlongationDate") ?? "") ?? "").Trim('"');
                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    if (!String.IsNullOrEmpty(collectingEndDate) && !String.IsNullOrEmpty(collectingProlongationDate))
                    {
                        string updateTenderEnd =
                            $"UPDATE {Program.Prefix}tender SET end_date = @end_date WHERE id_region = @id_region AND purchase_number = @purchase_number";
                        MySqlCommand cmd = new MySqlCommand(updateTenderEnd, connect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@id_region", RegionId);
                        cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        cmd.Parameters.AddWithValue("@end_date", collectingProlongationDate);
                        int resEnd = cmd.ExecuteNonQuery();
                        AddProlongation?.Invoke(resEnd);
                    }
                    if (!String.IsNullOrEmpty(scoringDate) && !String.IsNullOrEmpty(scoringProlongationDate))
                    {
                        string updateTenderScor =
                            $"UPDATE {Program.Prefix}tender SET scoring_date = @scoring_date WHERE id_region = @id_region AND purchase_number = @purchase_number";
                        MySqlCommand cmd = new MySqlCommand(updateTenderScor, connect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@id_region", RegionId);
                        cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        cmd.Parameters.AddWithValue("@scoring_date", scoringProlongationDate);
                        int resScor = cmd.ExecuteNonQuery();
                        AddProlongation?.Invoke(resScor);
                    }
                    if (String.IsNullOrEmpty(collectingProlongationDate) &&
                        String.IsNullOrEmpty(scoringProlongationDate))
                    {
                        Log.Logger("Не могу найти изменяемые даты у TenderProlongation", FilePath);
                    }
                }
            }
            else
            {
                Log.Logger("Не могу найти тег TenderProlongation", FilePath);
            }
        }
    }
}