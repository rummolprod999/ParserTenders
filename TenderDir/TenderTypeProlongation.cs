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
    public class TenderTypeProlongation : Tender
    {
        public event Action<int> AddProlongation;

        public TenderTypeProlongation(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddProlongation += delegate(int d)
            {
                if (d > 0)
                {
                    Program.AddProlongation++;
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
                    Log.Logger("Не могу найти purchaseNumber у TenderProlongation", FilePath);
                    return;
                }

                if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                {
                    /*Log.Logger("Тестовый тендер TenderProlongation", purchaseNumber, file_path);*/
                    return;
                }

                var collectingEndDate =
                    (JsonConvert.SerializeObject(tender.SelectToken("collectingEndDate") ?? "") ?? "").Trim('"');
                var collectingProlongationDate =
                    (JsonConvert.SerializeObject(tender.SelectToken("collectingProlongationDate") ?? "") ?? "")
                    .Trim('"');
                var scoringDate =
                    (JsonConvert.SerializeObject(tender.SelectToken("scoringDate") ?? "") ?? "").Trim('"');
                var scoringProlongationDate =
                    (JsonConvert.SerializeObject(tender.SelectToken("scoringProlongationDate") ?? "") ?? "").Trim('"');
                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    if (!string.IsNullOrEmpty(collectingEndDate) && !string.IsNullOrEmpty(collectingProlongationDate))
                    {
                        var updateTenderEnd =
                            $"UPDATE {Program.Prefix}tender SET end_date = @end_date WHERE id_region = @id_region AND purchase_number = @purchase_number";
                        var cmd = new MySqlCommand(updateTenderEnd, connect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@id_region", RegionId);
                        cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        cmd.Parameters.AddWithValue("@end_date", collectingProlongationDate);
                        var resEnd = cmd.ExecuteNonQuery();
                        AddProlongation?.Invoke(resEnd);
                    }

                    if (!string.IsNullOrEmpty(scoringDate) && !string.IsNullOrEmpty(scoringProlongationDate))
                    {
                        var updateTenderScor =
                            $"UPDATE {Program.Prefix}tender SET scoring_date = @scoring_date WHERE id_region = @id_region AND purchase_number = @purchase_number";
                        var cmd = new MySqlCommand(updateTenderScor, connect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@id_region", RegionId);
                        cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        cmd.Parameters.AddWithValue("@scoring_date", scoringProlongationDate);
                        var resScor = cmd.ExecuteNonQuery();
                        AddProlongation?.Invoke(resScor);
                    }

                    if (string.IsNullOrEmpty(collectingProlongationDate) &&
                        string.IsNullOrEmpty(scoringProlongationDate))
                    {
                        Log.Logger("Не могу найти изменяемые даты у TenderProlongation", FilePath);
                    }
                }
            }
            else
            {
                firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("epP"));
                if (firstOrDefault != null)
                {
                    var tender = firstOrDefault.Value;
                    var purchaseNumber = ((string)tender.SelectToken("commonInfo.purchaseNumber") ?? "").Trim();
                    if (string.IsNullOrEmpty(purchaseNumber))
                    {
                        Log.Logger("Не могу найти purchaseNumber у TenderProlongation", FilePath);
                        return;
                    }

                    if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                    {
                        /*Log.Logger("Тестовый тендер TenderProlongation", purchaseNumber, file_path);*/
                        return;
                    }

                    var collectingEndDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("prolongationInfo.collectingEndDate") ?? "") ??
                         "").Trim('"');

                    var collectingProlongationDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("prolongationInfo.newCollectingEndDT") ?? "") ??
                         "")
                        .Trim('"');
                    if (collectingProlongationDate == "")
                    {
                        collectingProlongationDate =
                            (JsonConvert.SerializeObject(tender.SelectToken("prolongationInfo.collectingInfo.endDT") ??
                                                         "") ?? "").Trim('"');
                    }

                    var scoringDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("prolongationInfo.firstPartsDT") ?? "") ?? "")
                        .Trim('"');
                    var scoringProlongationDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("prolongationInfo.firstPartsDT") ?? "") ?? "")
                        .Trim('"');
                    if (scoringProlongationDate == "")
                    {
                        scoringProlongationDate =
                            (JsonConvert.SerializeObject(
                                tender.SelectToken("prolongationInfo.scoringInfo.firstPartsDT") ?? "") ?? "").Trim('"');
                    }

                    using (var connect = ConnectToDb.GetDbConnection())
                    {
                        connect.Open();
                        if (!string.IsNullOrEmpty(collectingEndDate) &&
                            !string.IsNullOrEmpty(collectingProlongationDate))
                        {
                            var updateTenderEnd =
                                $"UPDATE {Program.Prefix}tender SET end_date = @end_date WHERE id_region = @id_region AND purchase_number = @purchase_number";
                            var cmd = new MySqlCommand(updateTenderEnd, connect);
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@id_region", RegionId);
                            cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                            cmd.Parameters.AddWithValue("@end_date", collectingProlongationDate);
                            var resEnd = cmd.ExecuteNonQuery();
                            AddProlongation?.Invoke(resEnd);
                        }

                        if (!string.IsNullOrEmpty(scoringProlongationDate))
                        {
                            var updateTenderScor =
                                $"UPDATE {Program.Prefix}tender SET scoring_date = @scoring_date WHERE id_region = @id_region AND purchase_number = @purchase_number";
                            var cmd = new MySqlCommand(updateTenderScor, connect);
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@id_region", RegionId);
                            cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                            cmd.Parameters.AddWithValue("@scoring_date", scoringProlongationDate);
                            var resScor = cmd.ExecuteNonQuery();
                            AddProlongation?.Invoke(resScor);
                        }

                        if (string.IsNullOrEmpty(collectingProlongationDate) &&
                            string.IsNullOrEmpty(scoringProlongationDate))
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
}