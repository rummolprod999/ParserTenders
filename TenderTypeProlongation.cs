using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class TenderTypeProlongation : Tender
    {
        public event Action<int> AddProlongation;

        public TenderTypeProlongation(FileInfo f, string region, int region_id, JObject json)
            : base(f, region, region_id, json)
        {
            AddProlongation += delegate(int d)
            {
                if (d > 0)
                    Program.AddProlongation++;
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
                    Log.Logger("Не могу найти purchaseNumber у TenderProlongation", file_path);
                    return;
                }
                else
                {
                    if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                    {
                        Log.Logger("Тестовый тендер TenderProlongation", purchaseNumber, file_path);
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
                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    connect.Open();
                    if (!String.IsNullOrEmpty(collectingEndDate) && !String.IsNullOrEmpty(collectingProlongationDate))
                    {
                        string update_tender_end =
                            $"UPDATE {Program.Prefix}tender SET end_date = @end_date WHERE id_region = @id_region AND purchase_number = @purchase_number";
                        MySqlCommand cmd = new MySqlCommand(update_tender_end, connect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@id_region", region_id);
                        cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        cmd.Parameters.AddWithValue("@end_date", collectingProlongationDate);
                        int res_end = cmd.ExecuteNonQuery();
                        AddProlongation?.Invoke(res_end);
                    }
                    if (!String.IsNullOrEmpty(scoringDate) && !String.IsNullOrEmpty(scoringProlongationDate))
                    {
                        string update_tender_scor =
                            $"UPDATE {Program.Prefix}tender SET scoring_date = @scoring_date WHERE id_region = @id_region AND purchase_number = @purchase_number";
                        MySqlCommand cmd = new MySqlCommand(update_tender_scor, connect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@id_region", region_id);
                        cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        cmd.Parameters.AddWithValue("@scoring_date", scoringProlongationDate);
                        int res_scor = cmd.ExecuteNonQuery();
                        AddProlongation?.Invoke(res_scor);
                    }
                    if (String.IsNullOrEmpty(collectingProlongationDate) &&
                        String.IsNullOrEmpty(scoringProlongationDate))
                    {
                        Log.Logger("Не могу найти изменяемые даты у TenderProlongation", file_path);
                    }
                }
            }
            else
            {
                Log.Logger("Не могу найти тег TenderProlongation", file_path);
            }
        }
    }
}