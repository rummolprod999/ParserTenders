using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class TenderType44 : Tender
    {
        public event Action<int> AddTender44;

        public TenderType44(FileInfo f, string region, int region_id, JObject json)
            : base(f, region, region_id, json)
        {
            AddTender44 += delegate(int d)
            {
                if (d > 0)
                    Program.Addtender44++;
                else
                    Log.Logger("Не удалось добавить Tender44", file_path);
            };
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
                    string docPublishDate = (JsonConvert.SerializeObject(tender.SelectToken("docPublishDate") ?? "") ??
                                             "").Trim('"');
                    string date_version = docPublishDate;
                    /*JsonReader readerj = new JsonTextReader(new StringReader(tender.ToString()));
                    readerj.DateParseHandling = DateParseHandling.None;
                    JObject o = JObject.Load(readerj);
                    Console.WriteLine(o["docPublishDate"]);*/
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
                    int id_etp = 0;
                    string ETP_code = ((string) tender.SelectToken("ETP.code") ?? "").Trim();
                    string ETP_name = ((string) tender.SelectToken("ETP.name") ?? "").Trim();
                    string ETP_url = ((string) tender.SelectToken("ETP.url") ?? "").Trim();
                    if (!String.IsNullOrEmpty(ETP_code))
                    {
                        string select_etp = $"SELECT id_etp FROM {Program.Prefix}etp WHERE code = @code";
                        MySqlCommand cmd7 = new MySqlCommand(select_etp, connect);
                        cmd7.Prepare();
                        cmd7.Parameters.AddWithValue("@code", ETP_code);
                        MySqlDataReader reader4 = cmd7.ExecuteReader();
                        if (reader4.HasRows)
                        {
                            reader4.Read();
                            id_etp = reader4.GetInt32("id_etp");
                            reader4.Close();
                        }
                        else
                        {
                            reader4.Close();
                            string insert_etp =
                                $"INSERT INTO {Program.Prefix}etp SET code= @code, name= @name, url= @url, conf=0";
                            MySqlCommand cmd8 = new MySqlCommand(insert_etp, connect);
                            cmd8.Prepare();
                            cmd8.Parameters.AddWithValue("@code", ETP_code);
                            cmd8.Parameters.AddWithValue("@name", ETP_name);
                            cmd8.Parameters.AddWithValue("@url", ETP_url);
                            cmd8.ExecuteNonQuery();
                            id_etp = (int) cmd8.LastInsertedId;
                        }
                    }
                    string end_date =
                    (JsonConvert.SerializeObject(tender.SelectToken("procedureInfo.collecting.endDate") ?? "") ??
                     "").Trim('"');
                    string scoring_date =
                    (JsonConvert.SerializeObject(tender.SelectToken("procedureInfo.scoring.date") ?? "") ??
                     "").Trim('"');
                    string bidding_date =
                    (JsonConvert.SerializeObject(tender.SelectToken("procedureInfo.bidding.date") ?? "") ??
                     "").Trim('"');
                    string insert_tender =
                        $"INSERT INTO {Program.Prefix}tender SET id_region = @id_region, id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, purchase_object_info = @purchase_object_info, type_fz = @type_fz, id_organizer = @id_organizer, id_placing_way = @id_placing_way, id_etp = @id_etp, end_date = @end_date, scoring_date = @scoring_date, bidding_date = @bidding_date, cancel = @cancel, date_version = @date_version, num_version = @num_version, notice_version = @notice_version, xml = @xml, print_form = @print_form";
                    MySqlCommand cmd9 = new MySqlCommand(insert_tender, connect);
                    cmd9.Prepare();
                    cmd9.Parameters.AddWithValue("@id_region", region_id);
                    cmd9.Parameters.AddWithValue("@id_xml", id_t);
                    cmd9.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    cmd9.Parameters.AddWithValue("@doc_publish_date", docPublishDate);
                    cmd9.Parameters.AddWithValue("@href", href);
                    cmd9.Parameters.AddWithValue("@purchase_object_info", purchaseObjectInfo);
                    cmd9.Parameters.AddWithValue("@type_fz", 44);
                    cmd9.Parameters.AddWithValue("@id_organizer", id_organizer);
                    cmd9.Parameters.AddWithValue("@id_placing_way", id_placing_way);
                    cmd9.Parameters.AddWithValue("@id_etp", id_etp);
                    cmd9.Parameters.AddWithValue("@end_date", end_date);
                    cmd9.Parameters.AddWithValue("@scoring_date", scoring_date);
                    cmd9.Parameters.AddWithValue("@bidding_date", bidding_date);
                    cmd9.Parameters.AddWithValue("@cancel", cancel_status);
                    cmd9.Parameters.AddWithValue("@date_version", date_version);
                    cmd9.Parameters.AddWithValue("@num_version", num_version);
                    cmd9.Parameters.AddWithValue("@notice_version", notice_version);
                    cmd9.Parameters.AddWithValue("@xml", xml);
                    cmd9.Parameters.AddWithValue("@print_form", printform);
                    int res_insert_tender = cmd9.ExecuteNonQuery();
                    int id_tender = (int) cmd9.LastInsertedId;
                    AddTender44?.Invoke(res_insert_tender);
                    if (cancel_status == 0)
                    {
                        string update_contract =
                            $"UPDATE {Program.Prefix}contract_sign SET id_tender = @id_tender WHERE purchase_number = @purchase_number";
                        MySqlCommand cmd10 = new MySqlCommand(update_contract, connect);
                        cmd10.Prepare();
                        cmd10.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        cmd10.Parameters.AddWithValue("@id_tender", id_tender);
                        cmd10.ExecuteNonQuery();
                    }
                    List<JToken> attachments = GetElements(tender, "attachments.attachment");
                    foreach (var att in attachments)
                    {
                        string attach_name = ((string) att.SelectToken("fileName") ?? "").Trim();
                        string attach_description = ((string) att.SelectToken("docDescription") ?? "").Trim();
                        string attach_url = ((string) att.SelectToken("url") ?? "").Trim();
                        if (!String.IsNullOrEmpty(attach_name))
                        {
                            string insert_attach =
                                $"INSERT INTO {Program.Prefix}attachment SET id_tender = @id_tender, file_name = @file_name, url = @url, description = @description";
                            MySqlCommand cmd11 = new MySqlCommand(insert_attach, connect);
                            cmd11.Prepare();
                            cmd11.Parameters.AddWithValue("@id_tender", id_tender);
                            cmd11.Parameters.AddWithValue("@file_name", attach_name);
                            cmd11.Parameters.AddWithValue("@url", attach_url);
                            cmd11.Parameters.AddWithValue("@description", attach_description);
                            cmd11.ExecuteNonQuery();
                        }
                    }

                    int lotNumber = 1;
                    List<JToken> lots = GetElements(tender, "lot");
                    if (lots.Count == 0)
                        lots = GetElements(tender, "lots.lot");
                    foreach (var lot in lots)
                    {
                        string lot_max_price = ((string) lot.SelectToken("maxPrice") ?? "").Trim();
                        string lot_currency = ((string) lot.SelectToken("currency.name") ?? "").Trim();
                        string lot_finance_source = ((string) lot.SelectToken("financeSource") ?? "").Trim();
                        string insert_lot =
                            $"INSERT INTO {Program.Prefix}lot SET id_tender = @id_tender, lot_number = @lot_number, max_price = @max_price, currency = @currency, finance_source = @finance_source";
                        MySqlCommand cmd12 = new MySqlCommand(insert_lot, connect);
                        cmd12.Prepare();
                        cmd12.Parameters.AddWithValue("@id_tender", id_tender);
                        cmd12.Parameters.AddWithValue("@lot_number", lotNumber);
                        cmd12.Parameters.AddWithValue("@max_price", lot_max_price);
                        cmd12.Parameters.AddWithValue("@currency", lot_currency);
                        cmd12.Parameters.AddWithValue("@finance_source", lot_finance_source);
                        cmd12.ExecuteNonQuery();
                        int id_lot = (int) cmd12.LastInsertedId;
                        if (id_lot < 1)
                            Log.Logger("Не получили id лота", file_path);
                        lotNumber++;
                        List<JToken> customerRequirements =
                            GetElements(lot, "customerRequirements.customerRequirement");
                        foreach (var customerRequirement in customerRequirements)
                        {
                            string kladr_place =
                            ((string) customerRequirement.SelectToken("kladrPlaces.kladrPlace.kladr.fullName") ??
                             "").Trim();
                            if (String.IsNullOrEmpty(kladr_place))
                                kladr_place =
                                ((string) customerRequirement.SelectToken(
                                     "kladrPlaces.kladrPlace[0].kladr.fullName") ?? "").Trim();
                            string delivery_place =
                                ((string) customerRequirement.SelectToken("kladrPlaces.kladrPlace.deliveryPlace") ?? "")
                                .Trim();
                            if (String.IsNullOrEmpty(delivery_place))
                                delivery_place =
                                ((string) customerRequirement.SelectToken(
                                     "kladrPlaces.kladrPlace[0].deliveryPlace") ?? "").Trim();
                            string delivery_term =
                                ((string) customerRequirement.SelectToken("deliveryTerm") ?? "").Trim();
                            string application_guarantee_amount =
                                ((string) customerRequirement.SelectToken("applicationGuarantee.amount") ?? "").Trim();
                            string contract_guarantee_amount =
                                ((string) customerRequirement.SelectToken("contractGuarantee.amount") ?? "").Trim();
                            string application_settlement_account =
                            ((string) customerRequirement.SelectToken("applicationGuarantee.settlementAccount") ??
                             "").Trim();
                            string application_personal_account =
                                ((string) customerRequirement.SelectToken("applicationGuarantee.personalAccount") ?? "")
                                .Trim();
                            string application_bik =
                                ((string) customerRequirement.SelectToken("applicationGuarantee.bik") ?? "").Trim();
                            string contract_settlement_account =
                                ((string) customerRequirement.SelectToken("contractGuarantee.settlementAccount") ?? "")
                                .Trim();
                            string contract_personal_account =
                                ((string) customerRequirement.SelectToken("contractGuarantee.personalAccount") ?? "")
                                .Trim();
                            string contract_bik =
                                ((string) customerRequirement.SelectToken("contractGuarantee.bik") ?? "").Trim();
                            string customer_regNum = ((string) customerRequirement.SelectToken("customer.regNum") ?? "")
                                .Trim();
                            string customer_full_name =
                                ((string) customerRequirement.SelectToken("customer.fullName") ?? "").Trim();
                            string customer_requirement_max_price =
                                ((string) customerRequirement.SelectToken("maxPrice") ?? "").Trim();

                            if (!String.IsNullOrEmpty(customer_regNum))
                            {
                                string select_customer =
                                    $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                                MySqlCommand cmd13 = new MySqlCommand(select_customer, connect);
                                cmd13.Prepare();
                                cmd13.Parameters.AddWithValue("@reg_num", customer_regNum);
                                MySqlDataReader reader5 = cmd13.ExecuteReader();
                                if (reader5.HasRows)
                                {
                                    reader5.Read();
                                    id_customer = reader5.GetInt32("id_customer");
                                    reader5.Close();
                                }
                                else
                                {
                                    reader5.Close();
                                    string customer_inn = "";
                                    if (!String.IsNullOrEmpty(organizer_inn))
                                    {
                                        if (organizer_reg_num == customer_regNum)
                                        {
                                            customer_inn = organizer_inn;
                                        }
                                    }
                                    string insert_customer =
                                        $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, inn = @inn";
                                    MySqlCommand cmd14 = new MySqlCommand(insert_customer, connect);
                                    cmd14.Prepare();
                                    cmd14.Parameters.AddWithValue("@reg_num", customer_regNum);
                                    cmd14.Parameters.AddWithValue("@full_name", customer_full_name);
                                    cmd14.Parameters.AddWithValue("@inn", customer_inn);
                                    cmd14.ExecuteNonQuery();
                                    id_customer = (int) cmd14.LastInsertedId;
                                }
                            }
                            else
                            {
                                if (!String.IsNullOrEmpty(customer_full_name))
                                {
                                    string select_cust_name =
                                        $"SELECT id_customer FROM {Program.Prefix}customer WHERE full_name = @full_name";
                                    MySqlCommand cmd15 = new MySqlCommand(select_cust_name, connect);
                                    cmd15.Prepare();
                                    cmd15.Parameters.AddWithValue("@full_name", customer_full_name);
                                    MySqlDataReader reader6 = cmd15.ExecuteReader();
                                    if (reader6.HasRows)
                                    {
                                        reader6.Read();
                                        id_customer = reader6.GetInt32("id_customer");
                                        Log.Logger("Получили id_customer по customer_full_name", file_path);
                                    }
                                    reader6.Close();
                                }
                            }

                            string insert_customer_requirement =
                                $"INSERT INTO {Program.Prefix}customer_requirement SET id_lot = @id_lot, id_customer = @id_customer, kladr_place = @kladr_place, delivery_place = @delivery_place, delivery_term = @delivery_term, application_guarantee_amount = @application_guarantee_amount, application_settlement_account = @application_settlement_account, application_personal_account = @application_personal_account, application_bik = @application_bik, contract_guarantee_amount = @contract_guarantee_amount, contract_settlement_account = @contract_settlement_account, contract_personal_account = @contract_personal_account, contract_bik = @contract_bik, max_price = @max_price";
                            MySqlCommand cmd16 = new MySqlCommand(insert_customer_requirement, connect);
                            cmd16.Prepare();
                            cmd16.Parameters.AddWithValue("@id_lot", id_lot);
                            cmd16.Parameters.AddWithValue("@id_customer", id_customer);
                            cmd16.Parameters.AddWithValue("@kladr_place", kladr_place);
                            cmd16.Parameters.AddWithValue("@delivery_place", delivery_place);
                            cmd16.Parameters.AddWithValue("@delivery_term", delivery_term);
                            cmd16.Parameters.AddWithValue("@application_guarantee_amount",
                                application_guarantee_amount);
                            cmd16.Parameters.AddWithValue("@application_settlement_account",
                                application_settlement_account);
                            cmd16.Parameters.AddWithValue("@application_personal_account",
                                application_personal_account);
                            cmd16.Parameters.AddWithValue("@application_bik", application_bik);
                            cmd16.Parameters.AddWithValue("@contract_guarantee_amount", contract_guarantee_amount);
                            cmd16.Parameters.AddWithValue("@contract_settlement_account", contract_settlement_account);
                            cmd16.Parameters.AddWithValue("@contract_personal_account", contract_personal_account);
                            cmd16.Parameters.AddWithValue("@contract_bik", contract_bik);
                            cmd16.Parameters.AddWithValue("@max_price", customer_requirement_max_price);
                            cmd16.ExecuteNonQuery();
                            if (id_customer == 0)
                            {
                                Log.Logger("Нет id_customer", file_path);
                            }
                        }

                        List<JToken> preferenses = GetElements(lot, "preferenses.preferense");
                        foreach (var preferense in preferenses)
                        {
                            string preferense_name = ((string) preferense.SelectToken("name") ?? "").Trim();
                            string insert_preference =
                                $"INSERT INTO {Program.Prefix}preferense SET id_lot = @id_lot, name = @name";
                            MySqlCommand cmd17 = new MySqlCommand(insert_preference, connect);
                            cmd17.Prepare();
                            cmd17.Parameters.AddWithValue("@id_lot", id_lot);
                            cmd17.Parameters.AddWithValue("@name", preferense_name);
                            cmd17.ExecuteNonQuery();
                        }

                        List<JToken> requirements = GetElements(lot, "requirements.requirement");
                        foreach (var requirement in requirements)
                        {
                            string requirement_name = ((string) requirement.SelectToken("name") ?? "").Trim();
                            string requirement_content = ((string) requirement.SelectToken("content") ?? "").Trim();
                            string requirement_code = ((string) requirement.SelectToken("code") ?? "").Trim();
                            string insert_requirement =
                                $"INSERT INTO {Program.Prefix}requirement SET id_lot = @id_lot, name = @name, content = @content, code = @code";
                            MySqlCommand cmd18 = new MySqlCommand(insert_requirement, connect);
                            cmd18.Prepare();
                            cmd18.Parameters.AddWithValue("@id_lot", id_lot);
                            cmd18.Parameters.AddWithValue("@name", requirement_name);
                            cmd18.Parameters.AddWithValue("@content", requirement_content);
                            cmd18.Parameters.AddWithValue("@code", requirement_code);
                            cmd18.ExecuteNonQuery();
                        }
                        string restrict_info = ((string) lot.SelectToken("restrictInfo") ?? "").Trim();
                        string foreign_info = ((string) lot.SelectToken("restrictForeignsInfo") ?? "").Trim();
                        string insert_restrict =
                            $"INSERT INTO {Program.Prefix}restricts SET id_lot = @id_lot, foreign_info = @foreign_info, info = @info";
                        MySqlCommand cmd19 = new MySqlCommand(insert_restrict, connect);
                        cmd19.Prepare();
                        cmd19.Parameters.AddWithValue("@id_lot", id_lot);
                        cmd19.Parameters.AddWithValue("@foreign_info", foreign_info);
                        cmd19.Parameters.AddWithValue("@info", restrict_info);
                        cmd19.ExecuteNonQuery();
                        List<JToken> purchaseobjects = GetElements(lot, "purchaseObjects.purchaseObject");
                        foreach (var purchaseobject in purchaseobjects)
                        {
                            string okpd2_code = ((string) purchaseobject.SelectToken("OKPD2.code") ?? "").Trim();
                            string okpd_code = ((string) purchaseobject.SelectToken("OKPD.code") ?? "").Trim();
                            string okpd_name = ((string) purchaseobject.SelectToken("OKPD2.name") ?? "").Trim();
                            if (String.IsNullOrEmpty(okpd_name))
                                okpd_name = ((string) purchaseobject.SelectToken("OKPD.name") ?? "").Trim();
                            string name = ((string) purchaseobject.SelectToken("name") ?? "").Trim();
                            string quantity_value = ((string) purchaseobject.SelectToken("quantity.value") ?? "")
                                .Trim();
                            string price = ((string) purchaseobject.SelectToken("price") ?? "").Trim();
                            string okei = ((string) purchaseobject.SelectToken("OKEI.nationalCode") ?? "").Trim();
                            string sum_p = ((string) purchaseobject.SelectToken("sum") ?? "").Trim();
                            int okpd2_group_code = 0;
                            string okpd2_group_level1_code = "";
                            if (!String.IsNullOrEmpty(okpd2_code))
                            {
                                GetOKPD(okpd2_code, out okpd2_group_code, out okpd2_group_level1_code);
                            }

                            List<JToken> customerquantities =
                                GetElements(purchaseobject, "customerQuantities.customerQuantity");
                            foreach (var customerquantity in customerquantities)
                            {
                                string customer_quantity_value =
                                    ((string) customerquantity.SelectToken("quantity") ?? "").Trim();
                                string cust_regNum = ((string) customerquantity.SelectToken("customer.regNum") ?? "")
                                    .Trim();
                                string cust_full_name =
                                    ((string) customerquantity.SelectToken("customer.fullName") ?? "").Trim();
                                int id_customer_q = 0;
                                if (!String.IsNullOrEmpty(cust_regNum))
                                {
                                    string select_customer_q =
                                        $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                                    MySqlCommand cmd20 = new MySqlCommand(select_customer_q, connect);
                                    cmd20.Prepare();
                                    cmd20.Parameters.AddWithValue("@reg_num", cust_regNum);
                                    MySqlDataReader reader7 = cmd20.ExecuteReader();
                                    if (reader7.HasRows)
                                    {
                                        reader7.Read();
                                        id_customer_q = reader7.GetInt32("id_customer");
                                        reader7.Close();
                                    }
                                    else
                                    {
                                        reader7.Close();
                                        string insert_customer_q =
                                            $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name";
                                        MySqlCommand cmd21 = new MySqlCommand(insert_customer_q, connect);
                                        cmd21.Prepare();
                                        cmd21.Parameters.AddWithValue("@reg_num", cust_regNum);
                                        cmd21.Parameters.AddWithValue("@full_name", cust_full_name);
                                        cmd21.ExecuteNonQuery();
                                        id_customer_q = (int) cmd21.LastInsertedId;
                                    }
                                }
                                else
                                {
                                    if (!String.IsNullOrEmpty(cust_full_name))
                                    {
                                        string select_cust_name_q =
                                            $"SELECT id_customer FROM {Program.Prefix}customer WHERE full_name = @full_name";
                                        MySqlCommand cmd22 = new MySqlCommand(select_cust_name_q, connect);
                                        cmd22.Prepare();
                                        cmd22.Parameters.AddWithValue("@full_name", cust_full_name);
                                        MySqlDataReader reader8 = cmd22.ExecuteReader();
                                        if (reader8.HasRows)
                                        {
                                            reader8.Read();
                                            id_customer_q = reader8.GetInt32("id_customer");
                                            Log.Logger("Получили id_customer_q по customer_full_name", file_path);
                                        }
                                        reader8.Close();
                                    }
                                }
                                string insert_customerquantity =
                                    $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, okpd2_group_code = @okpd2_group_code, okpd2_group_level1_code = @okpd2_group_level1_code, okpd_code = @okpd_code, okpd_name = @okpd_name, name = @name, quantity_value = @quantity_value, price = @price, okei = @okei, sum = @sum, customer_quantity_value = @customer_quantity_value";
                                MySqlCommand cmd23 = new MySqlCommand(insert_customerquantity, connect);
                                cmd23.Prepare();
                                cmd23.Parameters.AddWithValue("@id_lot", id_lot);
                                cmd23.Parameters.AddWithValue("@id_customer", id_customer_q);
                                cmd23.Parameters.AddWithValue("@okpd2_code", okpd2_code);
                                cmd23.Parameters.AddWithValue("@okpd2_group_code", okpd2_group_code);
                                cmd23.Parameters.AddWithValue("@okpd2_group_level1_code", okpd2_group_level1_code);
                                cmd23.Parameters.AddWithValue("@okpd_code", okpd_code);
                                cmd23.Parameters.AddWithValue("@okpd_name", okpd_name);
                                cmd23.Parameters.AddWithValue("@name", name);
                                cmd23.Parameters.AddWithValue("@quantity_value", quantity_value);
                                cmd23.Parameters.AddWithValue("@price", price);
                                cmd23.Parameters.AddWithValue("@okei", okei);
                                cmd23.Parameters.AddWithValue("@sum", sum_p);
                                cmd23.Parameters.AddWithValue("@customer_quantity_value", customer_quantity_value);
                                cmd23.ExecuteNonQuery();
                                if (id_customer_q == 0)
                                    Log.Logger("Нет id_customer_q", file_path);
                            }

                            if (customerquantities.Count == 0)
                            {
                                string insert_customerquantity =
                                    $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, okpd2_group_code = @okpd2_group_code, okpd2_group_level1_code = @okpd2_group_level1_code, okpd_code = @okpd_code, okpd_name = @okpd_name, name = @name, quantity_value = @quantity_value, price = @price, okei = @okei, sum = @sum, customer_quantity_value = @customer_quantity_value";
                                MySqlCommand cmd24 = new MySqlCommand(insert_customerquantity, connect);
                                cmd24.Prepare();
                                cmd24.Parameters.AddWithValue("@id_lot", id_lot);
                                cmd24.Parameters.AddWithValue("@id_customer", id_customer);
                                cmd24.Parameters.AddWithValue("@okpd2_code", okpd2_code);
                                cmd24.Parameters.AddWithValue("@okpd2_group_code", okpd2_group_code);
                                cmd24.Parameters.AddWithValue("@okpd2_group_level1_code", okpd2_group_level1_code);
                                cmd24.Parameters.AddWithValue("@okpd_code", okpd_code);
                                cmd24.Parameters.AddWithValue("@okpd_name", okpd_name);
                                cmd24.Parameters.AddWithValue("@name", name);
                                cmd24.Parameters.AddWithValue("@quantity_value", quantity_value);
                                cmd24.Parameters.AddWithValue("@price", price);
                                cmd24.Parameters.AddWithValue("@okei", okei);
                                cmd24.Parameters.AddWithValue("@sum", sum_p);
                                cmd24.Parameters.AddWithValue("@customer_quantity_value", quantity_value);
                                cmd24.ExecuteNonQuery();
                            }
                        }
                    }
                    TenderKwords(connect, id_tender);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег тендера", file_path);
            }
        }
    }
}