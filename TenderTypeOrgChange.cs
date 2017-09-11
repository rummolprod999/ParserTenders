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

        public TenderTypeOrgChange(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddOrgChange += delegate(int d)
            {
                if (d > 0)
                    Program.AddOrgChange++;
            };
        }

        public override void Parsing()
        {
            JObject root = (JObject) T.SelectToken("export");
            JProperty firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("fcs"));
            if (firstOrDefault != null)
            {
                JToken tender = firstOrDefault.Value;
                string purchaseNumber = ((string) tender.SelectToken("purchase.purchaseNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("Не могу найти purchaseNumber у TenderOrgChange", FilePath);
                    return;
                }
                else
                {
                    if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                    {
                        /*Log.Logger("Тестовый тендер TenderOrgChange", purchaseNumber, file_path);*/
                        return;
                    }
                }

                string newRespOrgRegNum = ((string) tender.SelectToken("newRespOrg.regNum") ?? "").Trim();
                if (String.IsNullOrEmpty(newRespOrgRegNum))
                {
                    Log.Logger("Не могу найти newRespOrg_regNum у TenderOrgChange", FilePath);
                    return;
                }

                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    int idOrganizer = 0;
                    connect.Open();
                    string selectOrg = $"SELECT id_organizer FROM {Program.Prefix}organizer WHERE reg_num = @reg_num";
                    MySqlCommand cmd = new MySqlCommand(selectOrg, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@reg_num", newRespOrgRegNum);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        idOrganizer = reader.GetInt32("id_organizer");
                        reader.Close();
                    }
                    else
                    {
                        reader.Close();
                        string addOrg =
                            $"INSERT INTO {Program.Prefix}organizer SET reg_num = @reg_num, full_name = @full_name, post_address = @post_address, fact_address = @fact_address, inn = @inn, kpp = @kpp, responsible_role = @responsible_role";
                        string newRespOrgFullName = ((string) tender.SelectToken("newRespOrg.fullName") ?? "").Trim();
                        string newRespOrgPostAddress = ((string) tender.SelectToken("newRespOrg.postAddress") ?? "")
                            .Trim();
                        string newRespOrgFactAddress = ((string) tender.SelectToken("newRespOrg.factAddress") ?? "")
                            .Trim();
                        string newRespOrgInn = ((string) tender.SelectToken("newRespOrg.INN") ?? "").Trim();
                        string newRespOrgKpp = ((string) tender.SelectToken("newRespOrg.KPP") ?? "").Trim();
                        string newRespOrgResponsibleRole =
                            ((string) tender.SelectToken("newRespOrg.responsibleRole") ?? "").Trim();
                        MySqlCommand cmd1 = new MySqlCommand(addOrg, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@reg_num", newRespOrgRegNum);
                        cmd1.Parameters.AddWithValue("@full_name", newRespOrgFullName);
                        cmd1.Parameters.AddWithValue("@post_address", newRespOrgPostAddress);
                        cmd1.Parameters.AddWithValue("@fact_address", newRespOrgFactAddress);
                        cmd1.Parameters.AddWithValue("@inn", newRespOrgInn);
                        cmd1.Parameters.AddWithValue("@kpp", newRespOrgKpp);
                        cmd1.Parameters.AddWithValue("@responsible_role", newRespOrgResponsibleRole);
                        cmd1.ExecuteNonQuery();
                        idOrganizer = (int) cmd1.LastInsertedId;
                    }
                    string updateTender =
                        $"UPDATE {Program.Prefix}tender SET id_organizer = @id_organizer WHERE id_region = @id_region AND purchase_number = @purchase_number AND cancel=0";
                    MySqlCommand cmd2 = new MySqlCommand(updateTender, connect);
                    cmd2.Prepare();
                    cmd2.Parameters.AddWithValue("@id_organizer", idOrganizer);
                    cmd2.Parameters.AddWithValue("@id_region", RegionId);
                    cmd2.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    int resUpd = cmd2.ExecuteNonQuery();
                    AddOrgChange?.Invoke(resUpd);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег TenderOrgChange", FilePath);
            }
        }
    }
}