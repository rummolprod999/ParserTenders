using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

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
                    Program.AddCancel++;
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
                    Log.Logger("Не могу найти purchaseNumber у TenderCancel", FilePath);
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

                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    string updateTender =
                        $"UPDATE {Program.Prefix}tender SET cancel = 1 WHERE id_region = @id_region AND purchase_number = @purchase_number";
                    MySqlCommand cmd = new MySqlCommand(updateTender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_region", RegionId);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    int resUpd = cmd.ExecuteNonQuery();
                    AddCancel?.Invoke(resUpd);
                }
            }
            else
            {
                firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("epN"));
                if (firstOrDefault != null)
                {
                    JToken tender = firstOrDefault.Value;
                    string purchaseNumber = ((string) tender.SelectToken("commonInfo.purchaseNumber") ?? "").Trim();
                    if (String.IsNullOrEmpty(purchaseNumber))
                    {
                        Log.Logger("Не могу найти purchaseNumber у TenderCancel", FilePath);
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

                    using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                    {
                        connect.Open();
                        string updateTender =
                            $"UPDATE {Program.Prefix}tender SET cancel = 1 WHERE id_region = @id_region AND purchase_number = @purchase_number";
                        MySqlCommand cmd = new MySqlCommand(updateTender, connect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@id_region", RegionId);
                        cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        int resUpd = cmd.ExecuteNonQuery();
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