using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class TenderType223 : Tender
    {
        public event Action<int> AddTender223;
        private TypeFile223 purchase;
        private string extend_scoring_date = "";

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
                    string docPublishDate =
                    (JsonConvert.SerializeObject(tender.SelectToken("publicationDateTime") ?? "") ??
                     "").Trim('"');
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
                                DateTime date_new = DateTime.Parse(docPublishDate);
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

                    string href = ((string) tender.SelectToken("href") ?? "").Trim();
                    string purchaseObjectInfo = ((string) tender.SelectToken("name") ?? "").Trim();
                    string date_version = (JsonConvert.SerializeObject(tender.SelectToken("modificationDate") ?? "") ??
                                           "").Trim('"');
                    string num_version = ((string) tender.SelectToken("version") ?? "").Trim();
                    string notice_version = ((string) tender.SelectToken("modificationDescription") ?? "").Trim();
                    string printform = ((string) tender.SelectToken("printForm.url") ?? "").Trim();
                    if (!String.IsNullOrEmpty(printform) && printform.IndexOf("CDATA") != 1)
                        printform = printform.Substring(9, printform.Length - 12);
                    string organizer_full_name = ((string) tender.SelectToken("placer.mainInfo.fullName") ?? "").Trim();
                    string organizer_post_address = ((string) tender.SelectToken("placer.mainInfo.postalAddress") ?? "")
                        .Trim();
                    string organizer_fact_address = ((string) tender.SelectToken("placer.mainInfo.legalAddress") ?? "")
                        .Trim();
                    string organizer_inn = ((string) tender.SelectToken("placer.mainInfo.inn") ?? "").Trim();
                    string organizer_kpp = ((string) tender.SelectToken("placer.mainInfo.kpp") ?? "").Trim();
                    string organizer_email = ((string) tender.SelectToken("placer.mainInfo.email") ?? "").Trim();
                    string organizer_phone = ((string) tender.SelectToken("placer.mainInfo.phone") ?? "").Trim();
                    string organizer_fax = ((string) tender.SelectToken("placer.mainInfo.fax") ?? "").Trim();
                    int id_organizer = 0;
                    if (!String.IsNullOrEmpty(organizer_inn))
                    {
                        string select_org =
                            $"SELECT id_organizer FROM {Program.Prefix}organizer WHERE inn = @inn AND kpp = @kpp";
                        MySqlCommand cmd2 = new MySqlCommand(select_org, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@inn", organizer_inn);
                        cmd2.Parameters.AddWithValue("@kpp", organizer_kpp);
                        MySqlDataReader reader1 = cmd2.ExecuteReader();
                        if (reader1.HasRows)
                        {
                            reader1.Read();
                            id_organizer = reader1.GetInt32("id_organizer");
                            reader1.Close();
                        }
                        else
                        {
                            reader1.Close();
                            string add_organizer =
                                $"INSERT INTO {Program.Prefix}organizer SET full_name = @full_name, post_address = @post_address, fact_address = @fact_address, inn = @inn, kpp = @kpp, contact_email = @contact_email, contact_phone = @contact_phone, contact_fax = @contact_fax";
                            MySqlCommand cmd3 = new MySqlCommand(add_organizer, connect);
                            cmd3.Prepare();
                            cmd3.Parameters.AddWithValue("@full_name", organizer_full_name);
                            cmd3.Parameters.AddWithValue("@post_address", organizer_post_address);
                            cmd3.Parameters.AddWithValue("@fact_address", organizer_fact_address);
                            cmd3.Parameters.AddWithValue("@inn", organizer_inn);
                            cmd3.Parameters.AddWithValue("@kpp", organizer_kpp);
                            cmd3.Parameters.AddWithValue("@contact_email", organizer_email);
                            cmd3.Parameters.AddWithValue("@contact_phone", organizer_phone);
                            cmd3.Parameters.AddWithValue("@contact_fax", organizer_fax);
                            cmd3.ExecuteNonQuery();
                            id_organizer = (int) cmd3.LastInsertedId;
                        }
                    }
                    else
                    {
                        Log.Logger("Нет organizer_inn", file_path);
                    }
                    int id_placing_way = 0;
                    string placingWay_code = ((string) tender.SelectToken("purchaseMethodCode") ?? "").Trim();
                    string placingWay_name = ((string) tender.SelectToken("purchaseCodeName") ?? "").Trim();
                    int conformity = GetConformity(placingWay_name);
                    if (!String.IsNullOrEmpty(placingWay_code))
                    {
                        string select_placing_way =
                            $"SELECT id_placing_way FROM {Program.Prefix}placing_way WHERE code = @code";
                        MySqlCommand cmd4 = new MySqlCommand(select_placing_way, connect);
                        cmd4.Prepare();
                        cmd4.Parameters.AddWithValue("@code", placingWay_code);
                        MySqlDataReader reader2 = cmd4.ExecuteReader();
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            id_placing_way = reader2.GetInt32("id_placing_way");
                            reader2.Close();
                        }
                        else
                        {
                            reader2.Close();
                            string insert_placing_way =
                                $"INSERT INTO {Program.Prefix}placing_way SET code= @code, name= @name, conformity = @conformity";
                            MySqlCommand cmd5 = new MySqlCommand(insert_placing_way, connect);
                            cmd5.Prepare();
                            cmd5.Parameters.AddWithValue("@code", placingWay_code);
                            cmd5.Parameters.AddWithValue("@name", placingWay_name);
                            cmd5.Parameters.AddWithValue("@conformity", conformity);
                            cmd5.ExecuteNonQuery();
                            id_placing_way = (int) cmd5.LastInsertedId;
                        }
                    }

                    int id_etp = 0;
                    string ETP_code =
                        ((string) tender.SelectToken("electronicPlaceInfo.electronicPlaceId") ?? "").Trim();
                    string ETP_name = ((string) tender.SelectToken("electronicPlaceInfo.name") ?? "").Trim();
                    string ETP_url = ((string) tender.SelectToken("electronicPlaceInfo.url") ?? "").Trim();
                    if (!String.IsNullOrEmpty(ETP_code))
                    {
                        string select_etp = $"SELECT id_etp FROM {Program.Prefix}etp WHERE code = @code";
                        MySqlCommand cmd6 = new MySqlCommand(select_etp, connect);
                        cmd6.Prepare();
                        cmd6.Parameters.AddWithValue("@code", ETP_code);
                        MySqlDataReader reader3 = cmd6.ExecuteReader();
                        if (reader3.HasRows)
                        {
                            reader3.Read();
                            id_etp = reader3.GetInt32("id_etp");
                            reader3.Close();
                        }
                        else
                        {
                            reader3.Close();
                            string insert_etp =
                                $"INSERT INTO {Program.Prefix}etp SET code= @code, name= @name, url= @url, conf=0";
                            MySqlCommand cmd7 = new MySqlCommand(insert_etp, connect);
                            cmd7.Prepare();
                            cmd7.Parameters.AddWithValue("@code", ETP_code);
                            cmd7.Parameters.AddWithValue("@name", ETP_name);
                            cmd7.Parameters.AddWithValue("@url", ETP_url);
                            cmd7.ExecuteNonQuery();
                            id_etp = (int) cmd7.LastInsertedId;
                        }
                    }
                    string end_date =
                    (JsonConvert.SerializeObject(tender.SelectToken("submissionCloseDateTime") ?? "") ??
                     "").Trim('"');
                    string scoring_date = GetScogingDate(tender);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег Tender223", file_path);
            }
        }

        private int GetConformity(string conf)
        {
            string sLower = conf.ToLower();
            if (sLower.IndexOf("открыт", StringComparison.Ordinal) != 1)
            {
                return 5;
            }
            else if (sLower.IndexOf("аукцион", StringComparison.Ordinal) != 1)
            {
                return 1;
            }
            else if (sLower.IndexOf("котиров", StringComparison.Ordinal) != 1)
            {
                return 2;
            }
            else if (sLower.IndexOf("предложен", StringComparison.Ordinal) != 1)
            {
                return 3;
            }
            else if (sLower.IndexOf("единств", StringComparison.Ordinal) != 1)
            {
                return 4;
            }

            return 6;
        }

        private string GetScogingDate(JToken ten)
        {
            string scoring_date = "";
            scoring_date =
            (JsonConvert.SerializeObject(ten.SelectToken("placingProcedure.examinationDateTime") ?? "") ??
             "").Trim('"');
            if (String.IsNullOrEmpty(scoring_date))
            {
                scoring_date = (JsonConvert.SerializeObject(ten.SelectToken("applExamPeriodTime") ?? "") ??
                                "").Trim('"');
            }
            if (String.IsNullOrEmpty(scoring_date))
            {
                scoring_date = (JsonConvert.SerializeObject(ten.SelectToken("examinationDateTime") ?? "") ??
                                "").Trim('"');
            }
            if (String.IsNullOrEmpty(scoring_date))
            {
                scoring_date = ParsingScoringDate(ten);
            }
            return scoring_date;
        }

        private string GetBiddingDate(JToken ten)
        {
            string bidding_date = "";
            return bidding_date;
        }

        private string ParsingScoringDate(JToken tn)
        {
            string date = "";
            List<JToken> noticeExtendField = GetElements(tn, "extendFields.noticeExtendField");
            foreach (JToken n in noticeExtendField)
            {
                List<JToken> extendField = GetElements(n, "extendField");
                foreach (JToken b in extendField)
                {
                    string desc = ((string) b.SelectToken("description") ?? "").Trim();

                    if (desc.ToLower().IndexOf("дата", StringComparison.Ordinal) != -1 &&
                        desc.ToLower().IndexOf("рассмотр", StringComparison.Ordinal) != -1)
                    {
                        string dm = ((string) b.SelectToken("value.text") ?? "").Trim();
                        if (String.IsNullOrEmpty(dm))
                        {
                            return (JsonConvert.SerializeObject(b.SelectToken("value.dateTime") ?? "") ??
                                    "").Trim('"');
                        }

                        date = FindDate(dm);
                        if (String.IsNullOrEmpty(date))
                        {
                            extend_scoring_date = dm;
                        }
                        return date;
                    }
                }
            }

            return date;
        }

        private string FindDate(string date)
        {
            string d = "";
            string pattern = @"((\d{2})\.(\d{2})\.(\d{4}))";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            Match match = regex.Match(date);
            if (match.Success)
            {
                DateTime i = DateTime.Parse(match.Value);
                d = i.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return d;
        }
    }
}