using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
        private string extend_bidding_date = "";

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

                    string href = ((string) tender.SelectToken("urlVSRZ") ?? "").Trim();
                    string purchaseObjectInfo = ((string) tender.SelectToken("name") ?? "").Trim();
                    string date_version = (JsonConvert.SerializeObject(tender.SelectToken("modificationDate") ?? "") ??
                                           "").Trim('"');
                    string num_version = ((string) tender.SelectToken("version") ?? "").Trim();
                    string notice_version = ((string) tender.SelectToken("modificationDescription") ?? "").Trim();
                    string printform = ((string) tender.SelectToken("urlOOS") ?? "").Trim();
                    if (!String.IsNullOrEmpty(printform) && printform.IndexOf("CDATA") != -1)
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
                    string bidding_date = GetBiddingDate(tender);
                    if (purchase == TypeFile223.purchaseNotice)
                    {
                        if (String.IsNullOrEmpty(bidding_date) && !String.IsNullOrEmpty(scoring_date))
                        {
                            bidding_date = scoring_date;
                        }
                    }
                    else if (purchase == TypeFile223.purchaseNoticeOK)
                    {
                        scoring_date = (JsonConvert.SerializeObject(tender.SelectToken("envelopeOpeningTime") ?? "") ??
                                        "").Trim('"');
                        bidding_date = (JsonConvert.SerializeObject(tender.SelectToken("examinationDateTime") ?? "") ??
                                        "").Trim('"');
                    }
                    else if (purchase == TypeFile223.purchaseNoticeZK)
                    {
                        scoring_date = bidding_date =
                        (JsonConvert.SerializeObject(tender.SelectToken("quotationExaminationTime") ?? "") ??
                         "").Trim('"');
                    }
                    string insert_tender =
                        $"INSERT INTO {Program.Prefix}tender SET id_region = @id_region, id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, purchase_object_info = @purchase_object_info, type_fz = @type_fz, id_organizer = @id_organizer, id_placing_way = @id_placing_way, id_etp = @id_etp, end_date = @end_date, scoring_date = @scoring_date, bidding_date = @bidding_date, cancel = @cancel, date_version = @date_version, num_version = @num_version, notice_version = @notice_version, xml = @xml, print_form = @print_form, extend_scoring_date = @extend_scoring_date, extend_bidding_date = @extend_bidding_date";
                    MySqlCommand cmd8 = new MySqlCommand(insert_tender, connect);
                    cmd8.Prepare();
                    cmd8.Parameters.AddWithValue("@id_region", region_id);
                    cmd8.Parameters.AddWithValue("@id_xml", id_t);
                    cmd8.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    cmd8.Parameters.AddWithValue("@doc_publish_date", docPublishDate);
                    cmd8.Parameters.AddWithValue("@href", href);
                    cmd8.Parameters.AddWithValue("@purchase_object_info", purchaseObjectInfo);
                    cmd8.Parameters.AddWithValue("@type_fz", 223);
                    cmd8.Parameters.AddWithValue("@id_organizer", id_organizer);
                    cmd8.Parameters.AddWithValue("@id_placing_way", id_placing_way);
                    cmd8.Parameters.AddWithValue("@id_etp", id_etp);
                    cmd8.Parameters.AddWithValue("@end_date", end_date);
                    cmd8.Parameters.AddWithValue("@scoring_date", scoring_date);
                    cmd8.Parameters.AddWithValue("@bidding_date", bidding_date);
                    cmd8.Parameters.AddWithValue("@cancel", cancel_status);
                    cmd8.Parameters.AddWithValue("@date_version", date_version);
                    cmd8.Parameters.AddWithValue("@num_version", num_version);
                    cmd8.Parameters.AddWithValue("@notice_version", notice_version);
                    cmd8.Parameters.AddWithValue("@xml", xml);
                    cmd8.Parameters.AddWithValue("@print_form", printform);
                    cmd8.Parameters.AddWithValue("@extend_scoring_date", extend_scoring_date);
                    cmd8.Parameters.AddWithValue("@extend_bidding_date", extend_bidding_date);
                    int res_insert_tender = cmd8.ExecuteNonQuery();
                    int id_tender = (int) cmd8.LastInsertedId;
                    AddTender223?.Invoke(res_insert_tender);
                    List<JToken> attachments = GetElements(tender, "attachments.document");
                    foreach (var att in attachments)
                    {
                        string attach_name = ((string) att.SelectToken("fileName") ?? "").Trim();
                        string attach_description = ((string) att.SelectToken("description") ?? "").Trim();
                        string attach_url = ((string) att.SelectToken("url") ?? "").Trim();
                        string insert_attach =
                            $"INSERT INTO {Program.Prefix}attachment SET id_tender = @id_tender, file_name = @file_name, url = @url, description = @description";
                        MySqlCommand cmd9 = new MySqlCommand(insert_attach, connect);
                        cmd9.Prepare();
                        cmd9.Parameters.AddWithValue("@id_tender", id_tender);
                        cmd9.Parameters.AddWithValue("@file_name", attach_name);
                        cmd9.Parameters.AddWithValue("@url", attach_url);
                        cmd9.Parameters.AddWithValue("@description", attach_description);
                        cmd9.ExecuteNonQuery();
                    }

                    string customer_inn = ((string) tender.SelectToken("customer.mainInfo.inn") ?? "").Trim();
                    string customer_full_name = ((string) tender.SelectToken("customer.mainInfo.fullName") ?? "")
                        .Trim();
                    string customer_kpp = ((string) tender.SelectToken("customer.mainInfo.kpp") ?? "").Trim();
                    string customer_ogrn = ((string) tender.SelectToken("customer.mainInfo.ogrn") ?? "").Trim();
                    string customer_post_address =
                        ((string) tender.SelectToken("customer.mainInfo.postalAddress") ?? "").Trim();
                    string customer_phone = ((string) tender.SelectToken("customer.mainInfo.phone") ?? "").Trim();
                    string customer_fax = ((string) tender.SelectToken("customer.mainInfo.fax") ?? "").Trim();
                    string customer_email = ((string) tender.SelectToken("customer.mainInfo.email") ?? "").Trim();
                    string cus_ln = ((string) tender.SelectToken("contact.lastName") ?? "").Trim();
                    string cus_fn = ((string) tender.SelectToken("contact.firstName") ?? "").Trim();
                    string cus_mn = ((string) tender.SelectToken("contact.middleName") ?? "").Trim();
                    string cus_contact = $"{cus_ln} {cus_fn} {cus_mn}".Trim();
                    int id_customer = 0;
                    string customer_regNumber = "";
                    if (!String.IsNullOrEmpty(customer_inn))
                    {
                        string select_od_customer =
                            $"SELECT regNumber FROM od_customer WHERE inn = @inn AND kpp = @kpp";
                        MySqlCommand cmd10 = new MySqlCommand(select_od_customer, connect);
                        cmd10.Prepare();
                        cmd10.Parameters.AddWithValue("@inn", customer_inn);
                        cmd10.Parameters.AddWithValue("@kpp", customer_kpp);
                        MySqlDataReader reader4 = cmd10.ExecuteReader();
                        if (reader4.HasRows)
                        {
                            reader4.Read();
                            customer_regNumber = (string) reader4["regNumber"];
                        }
                        reader4.Close();
                        if (String.IsNullOrEmpty(customer_regNumber))
                        {
                            string select_od_customer_from_ftp =
                                $"SELECT regNumber FROM od_customer_from_ftp WHERE inn = @inn AND kpp = @kpp";
                            MySqlCommand cmd11 = new MySqlCommand(select_od_customer_from_ftp, connect);
                            cmd11.Prepare();
                            cmd11.Parameters.AddWithValue("@inn", customer_inn);
                            cmd11.Parameters.AddWithValue("@kpp", customer_kpp);
                            MySqlDataReader reader5 = cmd11.ExecuteReader();
                            if (reader5.HasRows)
                            {
                                reader5.Read();
                                customer_regNumber = (string) reader5["regNumber"];
                            }
                            reader5.Close();
                        }
                        if (String.IsNullOrEmpty(customer_regNumber))
                        {
                            string select_od_customer_from_ftp223 =
                                $"SELECT regNumber FROM od_customer_from_ftp223 WHERE inn = @inn AND kpp = @kpp";
                            MySqlCommand cmd12 = new MySqlCommand(select_od_customer_from_ftp223, connect);
                            cmd12.Prepare();
                            cmd12.Parameters.AddWithValue("@inn", customer_inn);
                            cmd12.Parameters.AddWithValue("@kpp", customer_kpp);
                            MySqlDataReader reader6 = cmd12.ExecuteReader();
                            if (reader6.HasRows)
                            {
                                reader6.Read();
                                customer_regNumber = (string) reader6["regNumber"];
                            }
                            reader6.Close();
                        }
                        if (!String.IsNullOrEmpty(customer_regNumber))
                        {
                            string select_customer =
                                $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                            MySqlCommand cmd13 = new MySqlCommand(select_customer, connect);
                            cmd13.Prepare();
                            cmd13.Parameters.AddWithValue("@reg_num", customer_regNumber);
                            MySqlDataReader reader7 = cmd13.ExecuteReader();
                            if (reader7.HasRows)
                            {
                                reader7.Read();
                                id_customer = (int) reader7["id_customer"];
                                reader7.Close();
                            }
                            else
                            {
                                reader7.Close();
                                string insert_customer =
                                    $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, inn = @inn, is223=1";
                                MySqlCommand cmd14 = new MySqlCommand(insert_customer, connect);
                                cmd14.Prepare();
                                cmd14.Parameters.AddWithValue("@reg_num", customer_regNumber);
                                cmd14.Parameters.AddWithValue("@full_name", customer_full_name);
                                cmd14.Parameters.AddWithValue("@inn", customer_inn);
                                cmd14.ExecuteNonQuery();
                                id_customer = (int) cmd14.LastInsertedId;
                            }
                        }
                        else
                        {
                            string select_customer_inn =
                                $"SELECT id_customer FROM {Program.Prefix}customer WHERE inn = @inn";
                            MySqlCommand cmd15 = new MySqlCommand(select_customer_inn, connect);
                            cmd15.Prepare();
                            cmd15.Parameters.AddWithValue("@inn", customer_inn);
                            MySqlDataReader reader8 = cmd15.ExecuteReader();
                            if (reader8.HasRows)
                            {
                                reader8.Read();
                                id_customer = (int) reader8["id_customer"];
                                reader8.Close();
                            }
                            else
                            {
                                reader8.Close();
                                string reg_num223 = $"00000223{customer_inn}";
                                string insert_customer =
                                    $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, inn = @inn, is223=1";
                                MySqlCommand cmd16 = new MySqlCommand(insert_customer, connect);
                                cmd16.Prepare();
                                cmd16.Parameters.AddWithValue("@reg_num", reg_num223);
                                cmd16.Parameters.AddWithValue("@full_name", customer_full_name);
                                cmd16.Parameters.AddWithValue("@inn", customer_inn);
                                cmd16.ExecuteNonQuery();
                                id_customer = (int) cmd16.LastInsertedId;
                                string insert_customer223 =
                                    $"INSERT INTO {Program.Prefix}customer223 SET inn = @inn, full_name = @full_name, contact = @contact, kpp = @kpp, ogrn = @ogrn, post_address = @post_address, phone = @phone, fax = @fax, email = @email";
                                MySqlCommand cmd17 = new MySqlCommand(insert_customer223, connect);
                                cmd17.Prepare();
                                cmd17.Parameters.AddWithValue("@full_name", customer_full_name);
                                cmd17.Parameters.AddWithValue("@inn", customer_inn);
                                cmd17.Parameters.AddWithValue("@contact", cus_contact);
                                cmd17.Parameters.AddWithValue("@kpp", customer_kpp);
                                cmd17.Parameters.AddWithValue("@ogrn", customer_ogrn);
                                cmd17.Parameters.AddWithValue("@post_address", customer_post_address);
                                cmd17.Parameters.AddWithValue("@phone", customer_phone);
                                cmd17.Parameters.AddWithValue("@fax", customer_fax);
                                cmd17.Parameters.AddWithValue("@email", customer_email);
                                cmd17.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {
                        Log.Logger("У customer нет inn", file_path);
                    }
                    int lotNumber = 1;
                    List<JToken> lots = GetElements(tender, "lots.lot");
                    if (lots.Count == 0)
                        lots = GetElements(tender, "lot");
                    foreach (var lot in lots)
                    {
                        string lot_max_price = ((string) lot.SelectToken("lotData.initialSum") ?? "").Trim();
                        string lot_currency = ((string) lot.SelectToken("lotData.currency.name") ?? "").Trim();
                        string insert_lot =
                            $"INSERT INTO {Program.Prefix}lot SET id_tender = @id_tender, lot_number = @lot_number, max_price = @max_price, currency = @currency";
                        MySqlCommand cmd18 = new MySqlCommand(insert_lot, connect);
                        cmd18.Prepare();
                        cmd18.Parameters.AddWithValue("@id_tender", id_tender);
                        cmd18.Parameters.AddWithValue("@lot_number", lotNumber);
                        cmd18.Parameters.AddWithValue("@max_price", lot_max_price);
                        cmd18.Parameters.AddWithValue("@currency", lot_currency);
                        cmd18.ExecuteNonQuery();
                        int id_lot = (int) cmd18.LastInsertedId;
                        lotNumber++;
                        List<JToken> lotitems = GetElements(lot, "lotData.lotItems.lotItem");
                        foreach (var lotitem in lotitems)
                        {
                            string okpd2_code = ((string) lotitem.SelectToken("okpd2.code") ?? "").Trim();
                            string okpd_name = ((string) lotitem.SelectToken("okpd2.name") ?? "").Trim();
                            string name = okpd_name;
                            string quantity_value = ((string) lotitem.SelectToken("qty") ?? "")
                                .Trim();
                            string okei = ((string) lotitem.SelectToken("okei.name") ?? "").Trim();
                            int okpd2_group_code = 0;
                            string okpd2_group_level1_code = "";
                            if (!String.IsNullOrEmpty(okpd2_code))
                            {
                                GetOKPD(okpd2_code, out okpd2_group_code, out okpd2_group_level1_code);
                            }
                            string insert_lotitem =
                                $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, okpd2_group_code = @okpd2_group_code, okpd2_group_level1_code = @okpd2_group_level1_code, okpd_name = @okpd_name, name = @name, quantity_value = @quantity_value, okei = @okei, customer_quantity_value = @customer_quantity_value";
                            MySqlCommand cmd19 = new MySqlCommand(insert_lotitem, connect);
                            cmd19.Prepare();
                            cmd19.Parameters.AddWithValue("@id_lot", id_lot);
                            cmd19.Parameters.AddWithValue("@id_customer", id_customer);
                            cmd19.Parameters.AddWithValue("@okpd2_code", okpd2_code);
                            cmd19.Parameters.AddWithValue("@okpd2_group_code", okpd2_group_code);
                            cmd19.Parameters.AddWithValue("@okpd2_group_level1_code", okpd2_group_level1_code);
                            cmd19.Parameters.AddWithValue("@okpd_name", okpd_name);
                            cmd19.Parameters.AddWithValue("@name", name);
                            cmd19.Parameters.AddWithValue("@quantity_value", quantity_value);
                            cmd19.Parameters.AddWithValue("@okei", okei);
                            cmd19.Parameters.AddWithValue("@customer_quantity_value", quantity_value);
                            cmd19.ExecuteNonQuery();
                        }
                    }

                    TenderKwords(connect, id_tender);
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
            if (sLower.IndexOf("открыт", StringComparison.Ordinal) != -1)
            {
                return 5;
            }
            else if (sLower.IndexOf("аукцион", StringComparison.Ordinal) != -1)
            {
                return 1;
            }
            else if (sLower.IndexOf("котиров", StringComparison.Ordinal) != -1)
            {
                return 2;
            }
            else if (sLower.IndexOf("предложен", StringComparison.Ordinal) != -1)
            {
                return 3;
            }
            else if (sLower.IndexOf("единств", StringComparison.Ordinal) != -1)
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
            bidding_date =
            (JsonConvert.SerializeObject(ten.SelectToken("auctionTime") ?? "") ??
             "").Trim('"');
            if (String.IsNullOrEmpty(bidding_date))
            {
                bidding_date = ParsingBiddingDate(ten);
            }
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

        private string ParsingBiddingDate(JToken tn)
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
                        desc.ToLower().IndexOf("подвед", StringComparison.Ordinal) != -1)
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
                            extend_bidding_date = dm;
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
                DateTime i = DateTime.ParseExact(match.Value, "dd.MM.yyyy", CultureInfo.InvariantCulture);
                d = i.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return d;
        }
    }
}