#region

using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

#endregion

namespace ParserTenders.TenderDir
{
    public class TenderTypeCancel : Tender
    {
        public event Action<int> AddCancel;

        public TenderTypeCancel(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddCancel += delegate(int d)
            {
                if (d > 0)
                {
                    Program.AddCancel++;
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
                    Log.Logger("Не могу найти purchaseNumber у TenderCancel", FilePath);
                    return;
                }

                if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                {
                    /*Log.Logger("Тестовый тендер TenderCancel", purchaseNumber, file_path);*/
                    return;
                }

                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    var updateTender =
                        $"UPDATE {Program.Prefix}tender SET cancel = 1 WHERE id_region = @id_region AND purchase_number = @purchase_number AND (type_fz = 504 OR type_fz = 44)";
                    var cmd = new MySqlCommand(updateTender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_region", RegionId);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    var resUpd = cmd.ExecuteNonQuery();
                    AddCancel?.Invoke(resUpd);
                }
            }
            else
            {
                firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("epN"));
                if (firstOrDefault != null)
                {
                    var tender = firstOrDefault.Value;
                    var purchaseNumber = ((string)tender.SelectToken("commonInfo.purchaseNumber") ?? "").Trim();
                    if (string.IsNullOrEmpty(purchaseNumber))
                    {
                        Log.Logger("Не могу найти purchaseNumber у TenderCancel", FilePath);
                        return;
                    }

                    if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                    {
                        /*Log.Logger("Тестовый тендер TenderCancel", purchaseNumber, file_path);*/
                        return;
                    }

                    using (var connect = ConnectToDb.GetDbConnection())
                    {
                        connect.Open();
                        var updateTender =
                            $"UPDATE {Program.Prefix}tender SET cancel = 1 WHERE id_region = @id_region AND purchase_number = @purchase_number AND (type_fz = 504 OR type_fz = 44)";
                        var cmd = new MySqlCommand(updateTender, connect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@id_region", RegionId);
                        cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        var resUpd = cmd.ExecuteNonQuery();
                        AddCancel?.Invoke(resUpd);
                    }
                }
                else
                {
                    Log.Logger("Не могу найти тег TenderCancel", FilePath);
                }
            }
        }
    }
}