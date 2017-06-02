using System;
using System.Data;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class TenderType44 : Tender
    {
        public TenderType44(FileInfo f, string region, int region_id, JObject json)
            : base(f, region, region_id, json)
        {
        }

        public override void Parsing()
        {
            string xml = GetXml(file.ToString());
            JObject root = (JObject) t.SelectToken("export");
            JProperty firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("fcs"));
            if (firstOrDefault != null)
            {
                JToken tender = firstOrDefault.Value;
                string id_t = ((string) tender.SelectToken("id") ?? "").Trim();
                if (String.IsNullOrEmpty(id_t))
                {
                    Log.Logger("У тендера нет id", file_path);
                    return;
                }

                string purchaseNumber = ((string) tender.SelectToken("purchaseNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("У тендера нет purchaseNumber", file_path);
                }
                else
                {
                    if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                    {
                        Log.Logger("Тестовый тендер", purchaseNumber, file_path);
                        return;
                    }
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
                    string docPublishDate = ((string) tender.SelectToken("docPublishDate") ?? "").Trim();
                    string date_version = docPublishDate;
                    string href = ((string) tender.SelectToken("href") ?? "").Trim();
                    string printform = ((string) tender.SelectToken("printForm.url") ?? "").Trim();
                    string notice_version = "";
                    int num_version = 0;
                    int cancel_status = 0;
                    if (!String.IsNullOrEmpty(docPublishDate))
                    {
                        string select_date_t =
                            $"SELECT id_tender, doc_publish_date FROM {Program.Prefix}tender WHERE id_region = @id_region AND purchase_number = @purchase_number";
                        MySqlCommand cmd2 = new MySqlCommand(select_date_t, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@id_region", region_id);
                        cmd2.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        DataTable dt = new DataTable();
                        MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd2};
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            foreach (DataRow row in dt.Rows)
                            {
                                string date_trim = docPublishDate.Substring(0, 19);
                                DateTime date_new = DateTime.Parse(date_trim);
                                DateTime date_old = (DateTime) row["doc_publish_date"];
                                if (date_new > date_old)
                                {
                                    string update_tender_cancel =
                                        $"UPDATE {Program.Prefix}tender SET cancel = 1 WHERE id_tender = @id_tender";
                                    MySqlCommand cmd3 = new MySqlCommand(update_tender_cancel, connect);
                                    cmd3.Prepare();
                                    cmd3.Parameters.AddWithValue("id_tender", (int) row["id_tender"]);
                                    cmd3.ExecuteNonQuery();
                                }
                                else
                                {
                                    cancel_status = 1;
                                }
                            }
                        }
                    }

                    string purchaseObjectInfo = ((string) tender.SelectToken("purchaseObjectInfo") ?? "").Trim();
                    string organizer_reg_num =
                        ((string) tender.SelectToken("purchaseResponsible.responsibleOrg.regNum") ?? "").Trim();
                    string organizer_full_name =
                        ((string) tender.SelectToken("purchaseResponsible.responsibleOrg.fullName") ?? "").Trim();
                    string organizer_post_address =
                        ((string) tender.SelectToken("purchaseResponsible.responsibleOrg.postAddress") ?? "").Trim();
                    string organizer_fact_address =
                        ((string) tender.SelectToken("purchaseResponsible.responsibleOrg.factAddress") ?? "").Trim();
                    string organizer_inn = ((string) tender.SelectToken("purchaseResponsible.responsibleOrg.INN") ?? "")
                        .Trim();
                    string organizer_kpp = ((string) tender.SelectToken("purchaseResponsible.responsibleOrg.KPP") ?? "")
                        .Trim();
                    string organizer_responsible_role =
                        ((string) tender.SelectToken("purchaseResponsible.responsibleRole") ?? "").Trim();
                    string organizer_last_name =
                    ((string) tender.SelectToken("purchaseResponsible.responsibleInfo.contactPerson.lastName") ??
                     "").Trim();
                    string organizer_first_name =
                    ((string) tender.SelectToken("purchaseResponsible.responsibleInfo.contactPerson.firstName") ??
                     "").Trim();
                    string organizer_middle_name =
                    ((string) tender.SelectToken("purchaseResponsible.responsibleInfo.contactPerson.middleName") ??
                     "").Trim();
                    string organizer_contact = $"{organizer_last_name} {organizer_first_name} {organizer_middle_name}"
                        .Trim();
                    string organizer_email =
                        ((string) tender.SelectToken("purchaseResponsible.responsibleInfo.contactEMail") ?? "").Trim();
                    string organizer_fax =
                        ((string) tender.SelectToken("purchaseResponsible.responsibleInfo.contactFax") ?? "").Trim();
                    string organizer_phone =
                        ((string) tender.SelectToken("purchaseResponsible.responsibleInfo.contactPhone") ?? "").Trim();
                    int id_organizer = 0;
                    int id_customer = 0;
                    if (!String.IsNullOrEmpty(organizer_reg_num))
                    {
                        string select_org =
                            $"SELECT id_organizer FROM {Program.Prefix}organizer WHERE reg_num = @reg_num";
                        MySqlCommand cmd4 = new MySqlCommand(select_org, connect);
                        cmd4.Prepare();
                        cmd4.Parameters.AddWithValue("@reg_num", organizer_reg_num);
                        MySqlDataReader reader2 = cmd4.ExecuteReader();
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            id_organizer = reader2.GetInt32("id_organizer");
                            reader2.Close();
                        }
                        else
                        {
                            reader2.Close();
                            string add_organizer =
                                $"INSERT INTO {Program.Prefix}organizer SET reg_num = @reg_num, full_name = @full_name, post_address = @post_address, fact_address = @fact_address, inn = @inn, kpp = @kpp, responsible_role = @responsible_role, contact_person = @contact_person, contact_email = @contact_email, contact_phone = @contact_phone, contact_fax = @contact_fax";
                            MySqlCommand cmd5 = new MySqlCommand(add_organizer, connect);
                            cmd5.Prepare();
                            cmd5.Parameters.AddWithValue("@reg_num", organizer_reg_num);
                            cmd5.Parameters.AddWithValue("@full_name", organizer_full_name);
                            cmd5.Parameters.AddWithValue("@post_address", organizer_post_address);
                            cmd5.Parameters.AddWithValue("@fact_address", organizer_fact_address);
                            cmd5.Parameters.AddWithValue("@inn", organizer_inn);
                            cmd5.Parameters.AddWithValue("@kpp", organizer_kpp);
                            cmd5.Parameters.AddWithValue("@responsible_role", organizer_responsible_role);
                            cmd5.Parameters.AddWithValue("@contact_person", organizer_contact);
                            cmd5.Parameters.AddWithValue("@contact_email", organizer_email);
                            cmd5.Parameters.AddWithValue("@contact_phone", organizer_phone);
                            cmd5.Parameters.AddWithValue("@contact_fax", organizer_fax);
                            cmd5.ExecuteNonQuery();
                            id_customer = (int) cmd5.LastInsertedId;
                        }
                    }
                    else
                    {
                        Log.Logger("Нет organizer_reg_num", file_path);
                    }

                    int id_placing_way = 0;
                    string placingWay_code = ((string) tender.SelectToken("placingWay.code") ?? "").Trim();
                    string placingWay_name = ((string) tender.SelectToken("placingWay.name") ?? "").Trim();
                    if (!String.IsNullOrEmpty(placingWay_code))
                    {
                        string select_placing_way =
                            $"SELECT id_placing_way FROM {Program.Prefix}placing_way WHERE code = @code";
                        MySqlCommand cmd6 = new MySqlCommand(select_placing_way, connect);
                        cmd6.Prepare();
                        cmd6.Parameters.AddWithValue("@code", placingWay_code);
                        MySqlDataReader reader3 = cmd6.ExecuteReader();
                        if (reader3.HasRows)
                        {
                            reader3.Read();
                            id_placing_way = reader3.GetInt32("id_placing_way");
                            reader3.Close();
                        }
                        else
                        {
                            reader3.Close();
                            string insert_placing_way =
                                $"INSERT INTO {Program.Prefix}placing_way SET code= @code, name= @name";
                            MySqlCommand cmd7 = new MySqlCommand(insert_placing_way, connect);
                            cmd7.Prepare();
                            cmd7.Parameters.AddWithValue("@code", placingWay_code);
                            cmd7.Parameters.AddWithValue("@name", placingWay_name);
                            cmd7.ExecuteNonQuery();
                            id_placing_way = (int) cmd7.LastInsertedId;
                        }
                    }
                }
            }
            else
            {
                Log.Logger("Не могу найти тег тендера", file_path);
            }
        }
    }
}