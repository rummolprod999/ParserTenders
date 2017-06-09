using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class TenderTypeOrgChange : Tender
    {
        public event Action<int> AddOrgChange;

        public TenderTypeOrgChange(FileInfo f, string region, int region_id, JObject json)
            : base(f, region, region_id, json)
        {
            AddOrgChange += delegate(int d)
            {
                if (d > 0)
                    Program.AddOrgChange++;
            };
        }

        public override void Parsing()
        {
            JObject root = (JObject) t.SelectToken("export");
            JProperty firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("fcs"));
            if (firstOrDefault != null)
            {
                JToken tender = firstOrDefault.Value;
                string purchaseNumber = ((string) tender.SelectToken("purchase.purchaseNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("Не могу найти purchaseNumber у TenderOrgChange", file_path);
                    return;
                }
                else
                {
                    if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                    {
                        Log.Logger("Тестовый тендер TenderOrgChange", purchaseNumber, file_path);
                        return;
                    }
                }

                string newRespOrg_regNum = ((string) tender.SelectToken("newRespOrg.regNum") ?? "").Trim();
                if (String.IsNullOrEmpty(newRespOrg_regNum))
                {
                    Log.Logger("Не могу найти newRespOrg_regNum у TenderOrgChange", file_path);
                    return;
                }

                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    int id_organizer = 0;
                    connect.Open();
                    string select_org = $"SELECT id_organizer FROM {Program.Prefix}organizer WHERE reg_num = @reg_num";
                    MySqlCommand cmd = new MySqlCommand(select_org, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@reg_num", newRespOrg_regNum);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        id_organizer = reader.GetInt32("id_organizer");
                        reader.Close();
                    }
                    else
                    {
                        reader.Close();
                        string add_org =
                            $"INSERT INTO {Program.Prefix}organizer SET reg_num = @reg_num, full_name = @full_name, post_address = @post_address, fact_address = @fact_address, inn = @inn, kpp = @kpp, responsible_role = @responsible_role";
                        string newRespOrg_fullName = ((string) tender.SelectToken("newRespOrg.fullName") ?? "").Trim();
                        string newRespOrg_postAddress = ((string) tender.SelectToken("newRespOrg.postAddress") ?? "")
                            .Trim();
                        string newRespOrg_factAddress = ((string) tender.SelectToken("newRespOrg.factAddress") ?? "")
                            .Trim();
                        string newRespOrg_INN = ((string) tender.SelectToken("newRespOrg.INN") ?? "").Trim();
                        string newRespOrg_KPP = ((string) tender.SelectToken("newRespOrg.KPP") ?? "").Trim();
                        string newRespOrg_responsibleRole =
                            ((string) tender.SelectToken("newRespOrg.responsibleRole") ?? "").Trim();
                        MySqlCommand cmd1 = new MySqlCommand(add_org, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@reg_num", newRespOrg_regNum);
                        cmd1.Parameters.AddWithValue("@full_name", newRespOrg_fullName);
                        cmd1.Parameters.AddWithValue("@post_address", newRespOrg_postAddress);
                        cmd1.Parameters.AddWithValue("@fact_address", newRespOrg_factAddress);
                        cmd1.Parameters.AddWithValue("@inn", newRespOrg_INN);
                        cmd1.Parameters.AddWithValue("@kpp", newRespOrg_KPP);
                        cmd1.Parameters.AddWithValue("@responsible_role", newRespOrg_responsibleRole);
                        cmd1.ExecuteNonQuery();
                        id_organizer = (int) cmd1.LastInsertedId;
                    }
                    string update_tender =
                        $"UPDATE {Program.Prefix}tender SET id_organizer = @id_organizer WHERE id_region = @id_region AND purchase_number = @purchase_number AND cancel=0";
                    MySqlCommand cmd2 = new MySqlCommand(update_tender, connect);
                    cmd2.Prepare();
                    cmd2.Parameters.AddWithValue("@id_organizer", id_organizer);
                    cmd2.Parameters.AddWithValue("@id_region", region_id);
                    cmd2.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    int res_upd = cmd2.ExecuteNonQuery();
                    AddOrgChange?.Invoke(res_upd);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег TenderOrgChange", file_path);
            }
        }
    }
}