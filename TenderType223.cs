using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class TenderType223 : Tender
    {
        public event Action<int> AddTender223;
        private TypeFile223 purchase;

        public TenderType223(FileInfo f, string region, int region_id, JObject json, TypeFile223 p)
            : base(f, region, region_id, json)
        {
            purchase = p;
            AddTender223 += delegate(int d)
            {
                if (d > 0)
                    Program.AddTender223++;
                else
                    Log.Logger("Не удалось добавить Tender223", file_path);
            };
        }

        public override void Parsing()
        {
            JProperty tend = null;
            string xml = GetXml(file.ToString());
            JProperty firstOrDefault = t.Properties()
                .FirstOrDefault(p => p.Name.StartsWith("purchase", StringComparison.Ordinal));
            JProperty firstOrDefault2 = ((JObject) firstOrDefault?.Value)?.Properties()
                .FirstOrDefault(p => p.Name.StartsWith("body", StringComparison.Ordinal));
            JProperty firstOrDefault3 = ((JObject) firstOrDefault2?.Value)?.Properties()
                .FirstOrDefault(p => p.Name.StartsWith("item", StringComparison.Ordinal));
            if (firstOrDefault3 != null)
            {
                tend = ((JObject) firstOrDefault3.Value).Properties()
                    .FirstOrDefault(p => p.Name.StartsWith("purchase", StringComparison.Ordinal));
            }
            if (tend != null)
            {
                JToken tender = tend.Value;
                string id_t = ((string) tender.SelectToken("guid") ?? "").Trim();
                if (String.IsNullOrEmpty(id_t))
                {
                    Log.Logger("У тендера нет id", file_path);
                    return;
                }

                string purchaseNumber = ((string) tender.SelectToken("registrationNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("У тендера нет purchaseNumber", file_path);
                }

                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    connect.Open();
                    string select_tender =
                        $"SELECT id_tender FROM {Program.Prefix}tender WHERE id_xml = @id_xml AND id_region = @id_region AND purchase_number = @purchase_number";
                    MySqlCommand cmd = new MySqlCommand(select_tender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_xml", id_t);
                    cmd.Parameters.AddWithValue("@id_region", region_id);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Close();
                        return;
                    }

                    reader.Close();
                    string docPublishDate = (JsonConvert.SerializeObject(tender.SelectToken("publicationDateTime") ?? "") ??
                                             "").Trim('"');
                }
            }
            else
            {
                Log.Logger("Не могу найти тег Tender223", file_path);
            }
        }
    }
}