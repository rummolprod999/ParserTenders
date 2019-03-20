using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderType504Web : TenderWeb
    {
        private bool Up = default;

        public TenderType504Web(string url, JObject json, TypeFile44 p)
            : base(json, url)
        {
            AddTender504 += delegate(int d)
            {
                if (d > 0 && !Up)
                    Program.AddTender504++;
                else if (d > 0 && Up)
                    Program.UpdateTender504++;
                else
                    Log.Logger("Не удалось добавить Tender504", FilePath);
            };
        }

        public event Action<int> AddTender504;

        public override void Parsing()
        {
            string xml = GetXml();
            JProperty firstOrDefault = T.Properties().FirstOrDefault(p => p.Name.Contains("epN"));
            if (firstOrDefault != null)
            {
                JToken tender = firstOrDefault.Value;
                string idT = ((string) tender.SelectToken("id") ?? "").Trim();
                if (String.IsNullOrEmpty(idT))
                {
                    Log.Logger("У тендера нет id", FilePath);
                    return;
                }

                string purchaseNumber = ((string) tender.SelectToken("commonInfo.purchaseNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("У тендера нет purchaseNumber", FilePath);
                }
                else
                {
                    if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                    {
                        /*Log.Logger("Тестовый тендер", purchaseNumber, file_path);*/
                        return;
                    }
                }

                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    string selectTender =
                        $"SELECT id_tender FROM {Program.Prefix}tender WHERE id_xml = @id_xml AND purchase_number = @purchase_number";
                    MySqlCommand cmd = new MySqlCommand(selectTender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_xml", idT);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Close();
                        return;
                    }

                    reader.Close();
                    string docPublishDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("commonInfo.publishDTInEIS") ?? "") ??
                         "").Trim('"');
                    string dateVersion = docPublishDate;
                    var pils = false;
                    string href = ((string) tender.SelectToken("commonInfo.href") ?? "").Trim();
                    string printform = ((string) tender.SelectToken("printForm.url") ?? "").Trim();
                    if (string.IsNullOrEmpty(printform))
                    {
                        printform = ((string) tender.SelectToken("printFormInfo.url") ?? "").Trim();
                    }

                    if (!String.IsNullOrEmpty(printform) && printform.IndexOf("CDATA") != -1)
                        printform = printform.Substring(9, printform.Length - 12);
                    string noticeVersion = "";
                    int numVersion = (int?) tender.SelectToken("versionNumber") ?? 1;
                    int cancelStatus = 0;
                    if (!String.IsNullOrEmpty(docPublishDate))
                    {
                        string selectDateT =
                            $"SELECT id_tender, doc_publish_date FROM {Program.Prefix}tender WHERE id_region = @id_region AND purchase_number = @purchase_number";
                        MySqlCommand cmd2 = new MySqlCommand(selectDateT, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@id_region", RegionId);
                        cmd2.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        DataTable dt = new DataTable();
                        MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd2};
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            Up = true;
                            foreach (DataRow row in dt.Rows)
                            {
                                DateTime dateNew = DateTime.Parse(docPublishDate);
                                DateTime dateOld = (DateTime) row["doc_publish_date"];
                                if (dateNew >= dateOld)
                                {
                                    string updateTenderCancel =
                                        $"UPDATE {Program.Prefix}tender SET cancel = 1 WHERE id_tender = @id_tender";
                                    MySqlCommand cmd3 = new MySqlCommand(updateTenderCancel, connect);
                                    cmd3.Prepare();
                                    cmd3.Parameters.AddWithValue("id_tender", (int) row["id_tender"]);
                                    cmd3.ExecuteNonQuery();
                                }
                                else
                                {
                                    cancelStatus = 1;
                                }
                            }
                        }
                    }

                    string purchaseObjectInfo =
                        ((string) tender.SelectToken("commonInfo.purchaseObjectInfo") ?? "").Trim();
                    string organizerRegNum =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.regNum") ?? "").Trim();
                    string organizerFullName =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.fullName") ?? "")
                        .Trim();
                    string organizerPostAddress =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.postAddress") ?? "")
                        .Trim();
                    string organizerFactAddress =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.factAddress") ?? "")
                        .Trim();
                    string organizerInn =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.INN") ?? "")
                        .Trim();
                    string organizerKpp =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.KPP") ?? "")
                        .Trim();
                    string organizerResponsibleRole =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleRole") ?? "").Trim();
                    string organizerLastName =
                        ((string) tender.SelectToken(
                             "purchaseResponsibleInfo.responsibleInfo.contactPersonInfo.lastName") ??
                         "").Trim();
                    string organizerFirstName =
                        ((string) tender.SelectToken(
                             "purchaseResponsibleInfo.responsibleInfo.contactPersonInfo.firstName") ??
                         "").Trim();
                    string organizerMiddleName =
                        ((string) tender.SelectToken(
                             "purchaseResponsibleInfo.responsibleInfo.contactPersonInfo.middleName") ??
                         "").Trim();
                    string organizerContact = $"{organizerLastName} {organizerFirstName} {organizerMiddleName}"
                        .Trim();
                    string organizerEmail =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleInfo.contactEMail") ?? "")
                        .Trim();
                    string organizerFax =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleInfo.contactFax") ?? "")
                        .Trim();
                    string organizerPhone =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleInfo.contactPhone") ?? "")
                        .Trim();
                    var addr = GetRegionString(organizerFactAddress) != "" ? organizerFactAddress :
                        GetRegionString(organizerPostAddress) != "" ? organizerPostAddress :
                        GetRegionString(organizerFactAddress) != "" ? organizerFactAddress : organizerPostAddress;
                    if (addr != "")
                    {
                        var regionS = GetRegionString(addr);
                        if (regionS != "")
                        {
                            string selectReg = $"SELECT id FROM {Program.Prefix}region WHERE name LIKE @name";
                            MySqlCommand cmd46 = new MySqlCommand(selectReg, connect);
                            cmd46.Prepare();
                            cmd46.Parameters.AddWithValue("@name", "%" + regionS + "%");
                            MySqlDataReader reader46 = cmd46.ExecuteReader();
                            if (reader46.HasRows)
                            {
                                reader46.Read();
                                RegionId = reader46.GetInt32("id");
                                reader46.Close();
                            }
                            else
                            {
                                reader46.Close();
                            }
                        }
                    }

                    int idOrganizer = 0;
                    int idCustomer = 0;
                    if (!String.IsNullOrEmpty(organizerRegNum))
                    {
                        string selectOrg =
                            $"SELECT id_organizer FROM {Program.Prefix}organizer WHERE reg_num = @reg_num";
                        MySqlCommand cmd4 = new MySqlCommand(selectOrg, connect);
                        cmd4.Prepare();
                        cmd4.Parameters.AddWithValue("@reg_num", organizerRegNum);
                        MySqlDataReader reader2 = cmd4.ExecuteReader();
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            idOrganizer = reader2.GetInt32("id_organizer");
                            reader2.Close();
                        }
                        else
                        {
                            reader2.Close();
                            string addOrganizer =
                                $"INSERT INTO {Program.Prefix}organizer SET reg_num = @reg_num, full_name = @full_name, post_address = @post_address, fact_address = @fact_address, inn = @inn, kpp = @kpp, responsible_role = @responsible_role, contact_person = @contact_person, contact_email = @contact_email, contact_phone = @contact_phone, contact_fax = @contact_fax";
                            MySqlCommand cmd5 = new MySqlCommand(addOrganizer, connect);
                            cmd5.Prepare();
                            cmd5.Parameters.AddWithValue("@reg_num", organizerRegNum);
                            cmd5.Parameters.AddWithValue("@full_name", organizerFullName);
                            cmd5.Parameters.AddWithValue("@post_address", organizerPostAddress);
                            cmd5.Parameters.AddWithValue("@fact_address", organizerFactAddress);
                            cmd5.Parameters.AddWithValue("@inn", organizerInn);
                            cmd5.Parameters.AddWithValue("@kpp", organizerKpp);
                            cmd5.Parameters.AddWithValue("@responsible_role", organizerResponsibleRole);
                            cmd5.Parameters.AddWithValue("@contact_person", organizerContact);
                            cmd5.Parameters.AddWithValue("@contact_email", organizerEmail);
                            cmd5.Parameters.AddWithValue("@contact_phone", organizerPhone);
                            cmd5.Parameters.AddWithValue("@contact_fax", organizerFax);
                            cmd5.ExecuteNonQuery();
                            idOrganizer = (int) cmd5.LastInsertedId;
                        }
                    }
                    else
                    {
                        Log.Logger("Нет organizer_reg_num", FilePath);
                    }

                    int idPlacingWay = 0;
                    string placingWayCode = ((string) tender.SelectToken("commonInfo.placingWay.code") ?? "").Trim();
                    string placingWayName = ((string) tender.SelectToken("commonInfo.placingWay.name") ?? "").Trim();
                    if (!String.IsNullOrEmpty(placingWayCode))
                    {
                        string selectPlacingWay =
                            $"SELECT id_placing_way FROM {Program.Prefix}placing_way WHERE code = @code";
                        MySqlCommand cmd6 = new MySqlCommand(selectPlacingWay, connect);
                        cmd6.Prepare();
                        cmd6.Parameters.AddWithValue("@code", placingWayCode);
                        MySqlDataReader reader3 = cmd6.ExecuteReader();
                        if (reader3.HasRows)
                        {
                            reader3.Read();
                            idPlacingWay = reader3.GetInt32("id_placing_way");
                            reader3.Close();
                        }
                        else
                        {
                            reader3.Close();
                            int conformity = GetConformity(placingWayName);
                            string insertPlacingWay =
                                $"INSERT INTO {Program.Prefix}placing_way SET code= @code, name= @name, conformity = @conformity";
                            MySqlCommand cmd7 = new MySqlCommand(insertPlacingWay, connect);
                            cmd7.Prepare();
                            cmd7.Parameters.AddWithValue("@code", placingWayCode);
                            cmd7.Parameters.AddWithValue("@name", placingWayName);
                            cmd7.Parameters.AddWithValue("@conformity", conformity);
                            cmd7.ExecuteNonQuery();
                            idPlacingWay = (int) cmd7.LastInsertedId;
                        }
                    }

                    int idEtp = 0;
                    string etpCode = ((string) tender.SelectToken("commonInfo.ETP.code") ?? "").Trim();
                    string etpName = ((string) tender.SelectToken("commonInfo.ETP.name") ?? "").Trim();
                    string etpUrl = ((string) tender.SelectToken("commonInfo.ETP.url") ?? "").Trim();
                    if (!String.IsNullOrEmpty(etpCode))
                    {
                        string selectEtp = $"SELECT id_etp FROM {Program.Prefix}etp WHERE code = @code";
                        MySqlCommand cmd7 = new MySqlCommand(selectEtp, connect);
                        cmd7.Prepare();
                        cmd7.Parameters.AddWithValue("@code", etpCode);
                        MySqlDataReader reader4 = cmd7.ExecuteReader();
                        if (reader4.HasRows)
                        {
                            reader4.Read();
                            idEtp = reader4.GetInt32("id_etp");
                            reader4.Close();
                        }
                        else
                        {
                            reader4.Close();
                            string insertEtp =
                                $"INSERT INTO {Program.Prefix}etp SET code= @code, name= @name, url= @url, conf=0";
                            MySqlCommand cmd8 = new MySqlCommand(insertEtp, connect);
                            cmd8.Prepare();
                            cmd8.Parameters.AddWithValue("@code", etpCode);
                            cmd8.Parameters.AddWithValue("@name", etpName);
                            cmd8.Parameters.AddWithValue("@url", etpUrl);
                            cmd8.ExecuteNonQuery();
                            idEtp = (int) cmd8.LastInsertedId;
                        }
                    }

                    string endDate =
                        (JsonConvert.SerializeObject(
                             tender.SelectToken("notificationInfo.procedureInfo.collectingInfo.endDT") ?? "") ??
                         "").Trim('"');
                    if (string.IsNullOrEmpty(endDate))
                    {
                        endDate =
                            (JsonConvert.SerializeObject(
                                 tender.SelectToken(
                                     "notificationInfo.procedureInfo.firstStageInfo.collectingInfo.endDT") ?? "") ??
                             "").Trim('"');
                    }

                    string scoringDate =
                        (JsonConvert.SerializeObject(
                             tender.SelectToken("notificationInfo.procedureInfo.scoringInfo.endDate") ?? "") ??
                         "").Trim('"');
                    if (string.IsNullOrEmpty(scoringDate))
                    {
                        scoringDate =
                            (JsonConvert.SerializeObject(
                                 tender.SelectToken("notificationInfo.procedureInfo.scoringInfo.firstPartsDT") ?? "") ??
                             "").Trim('"');
                    }

                    if (string.IsNullOrEmpty(scoringDate))
                    {
                        scoringDate =
                            (JsonConvert.SerializeObject(
                                 tender.SelectToken(
                                     "notificationInfo.procedureInfo.firstStageInfo.scoringInfo.dateTime") ?? "") ??
                             "").Trim('"');
                    }

                    var scoringDateT = scoringDate.ParseDateUn("yyyy-MM-ddzzz");
                    string insertTender =
                        $"INSERT INTO {Program.Prefix}tender SET id_region = @id_region, id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, purchase_object_info = @purchase_object_info, type_fz = @type_fz, id_organizer = @id_organizer, id_placing_way = @id_placing_way, id_etp = @id_etp, end_date = @end_date, scoring_date = @scoring_date, bidding_date = @bidding_date, cancel = @cancel, date_version = @date_version, num_version = @num_version, notice_version = @notice_version, xml = @xml, print_form = @print_form";
                    MySqlCommand cmd9 = new MySqlCommand(insertTender, connect);
                    cmd9.Prepare();
                    cmd9.Parameters.AddWithValue("@id_region", RegionId);
                    cmd9.Parameters.AddWithValue("@id_xml", idT);
                    cmd9.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    cmd9.Parameters.AddWithValue("@doc_publish_date", docPublishDate);
                    cmd9.Parameters.AddWithValue("@href", href);
                    cmd9.Parameters.AddWithValue("@purchase_object_info", purchaseObjectInfo);
                    cmd9.Parameters.AddWithValue("@type_fz", 504);
                    cmd9.Parameters.AddWithValue("@id_organizer", idOrganizer);
                    cmd9.Parameters.AddWithValue("@id_placing_way", idPlacingWay);
                    cmd9.Parameters.AddWithValue("@id_etp", idEtp);
                    cmd9.Parameters.AddWithValue("@end_date", endDate);
                    if (scoringDateT == DateTime.MinValue)
                    {
                        cmd9.Parameters.AddWithValue("@scoring_date", scoringDate);
                    }
                    else
                    {
                        cmd9.Parameters.AddWithValue("@scoring_date", scoringDateT);
                    }

                    cmd9.Parameters.AddWithValue("@bidding_date", DateTime.MinValue);
                    cmd9.Parameters.AddWithValue("@cancel", cancelStatus);
                    cmd9.Parameters.AddWithValue("@date_version", dateVersion);
                    cmd9.Parameters.AddWithValue("@num_version", numVersion);
                    cmd9.Parameters.AddWithValue("@notice_version", noticeVersion);
                    cmd9.Parameters.AddWithValue("@xml", xml);
                    cmd9.Parameters.AddWithValue("@print_form", printform);
                    int resInsertTender = cmd9.ExecuteNonQuery();
                    int idTender = (int) cmd9.LastInsertedId;
                    AddTender504?.Invoke(resInsertTender);
                    if (cancelStatus == 0)
                    {
                        string updateContract =
                            $"UPDATE {Program.Prefix}contract_sign SET id_tender = @id_tender WHERE purchase_number = @purchase_number";
                        MySqlCommand cmd10 = new MySqlCommand(updateContract, connect);
                        cmd10.Prepare();
                        cmd10.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        cmd10.Parameters.AddWithValue("@id_tender", idTender);
                        cmd10.ExecuteNonQuery();
                    }

                    List<JToken> attachments = GetElements(tender, "attachmentsInfo.attachmentInfo");
                    attachments.AddRange(GetElements(tender, "notificationAttachments.attachment"));
                    foreach (var att in attachments)
                    {
                        string attachName = ((string) att.SelectToken("fileName") ?? "").Trim();
                        string attachDescription = ((string) att.SelectToken("docDescription") ?? "").Trim();
                        string attachUrl = ((string) att.SelectToken("url") ?? "").Trim();
                        if (!String.IsNullOrEmpty(attachName))
                        {
                            string insertAttach =
                                $"INSERT INTO {Program.Prefix}attachment SET id_tender = @id_tender, file_name = @file_name, url = @url, description = @description";
                            MySqlCommand cmd11 = new MySqlCommand(insertAttach, connect);
                            cmd11.Prepare();
                            cmd11.Parameters.AddWithValue("@id_tender", idTender);
                            cmd11.Parameters.AddWithValue("@file_name", attachName);
                            cmd11.Parameters.AddWithValue("@url", attachUrl);
                            cmd11.Parameters.AddWithValue("@description", attachDescription);
                            cmd11.ExecuteNonQuery();
                        }
                    }

                    int lotNumber = 1;
                    string lotMaxPrice =
                        ((string) tender.SelectToken("notificationInfo.contractConditionsInfo.maxPriceInfo.maxPrice") ??
                         "").Trim();
                    string lotCurrency =
                        ((string) tender.SelectToken(
                             "notificationInfo.contractConditionsInfo.maxPriceInfo.currency.name") ?? "").Trim();
                    string lotFinanceSource =
                        ((string) tender.SelectToken(
                             "notificationInfo.contractConditionsInfo.maxPriceInfo.financeSource") ?? "").Trim();
                    string insertLot =
                        $"INSERT INTO {Program.Prefix}lot SET id_tender = @id_tender, lot_number = @lot_number, max_price = @max_price, currency = @currency, finance_source = @finance_source";
                    MySqlCommand cmd12 = new MySqlCommand(insertLot, connect);
                    cmd12.Prepare();
                    cmd12.Parameters.AddWithValue("@id_tender", idTender);
                    cmd12.Parameters.AddWithValue("@lot_number", lotNumber);
                    cmd12.Parameters.AddWithValue("@max_price", lotMaxPrice);
                    cmd12.Parameters.AddWithValue("@currency", lotCurrency);
                    cmd12.Parameters.AddWithValue("@finance_source", lotFinanceSource);
                    cmd12.ExecuteNonQuery();
                    int idLot = (int) cmd12.LastInsertedId;
                    if (idLot < 1)
                        Log.Logger("Не получили id лота", FilePath);
                    lotNumber++;
                    List<JToken> customerRequirements =
                        GetElements(tender, "notificationInfo.customerRequirementsInfo.customerRequirementInfo");
                    foreach (var customerRequirement in customerRequirements)
                    {
                        string kladrPlace =
                            ((string) customerRequirement.SelectToken(
                                 "contractConditionsInfo.deliveryPlacesInfo.deliveryPlaceInfo.kladr.fullName") ??
                             "").Trim();
                        if (String.IsNullOrEmpty(kladrPlace))
                            kladrPlace =
                                ((string) customerRequirement.SelectToken(
                                     "contractConditionsInfo.deliveryPlacesInfo.deliveryPlaceInfo[0].kladr.fullName") ??
                                 "").Trim();
                        string deliveryPlace =
                            ((string) customerRequirement.SelectToken(
                                 "contractConditionsInfo.deliveryPlacesInfo.deliveryPlaceInfo.deliveryPlace") ?? "")
                            .Trim();
                        if (String.IsNullOrEmpty(deliveryPlace))
                            deliveryPlace =
                                ((string) customerRequirement.SelectToken(
                                     "contractConditionsInfo.deliveryPlacesInfo.deliveryPlaceInfo[0].kladr.fullName[0].deliveryPlace") ??
                                 "").Trim();
                        string deliveryTerm =
                            ((string) customerRequirement.SelectToken("contractConditionsInfo.deliveryTerm") ?? "")
                            .Trim();
                        string applicationGuaranteeAmount =
                            ((string) customerRequirement.SelectToken("applicationGuarantee.amount") ?? "").Trim();
                        string contractGuaranteeAmount =
                            ((string) customerRequirement.SelectToken("contractGuarantee.amount") ?? "").Trim();
                        string applicationSettlementAccount =
                            ((string) customerRequirement.SelectToken(
                                 "applicationGuarantee.account.settlementAccount") ??
                             "").Trim();
                        string applicationPersonalAccount =
                            ((string) customerRequirement.SelectToken("applicationGuarantee.account.personalAccount") ??
                             "")
                            .Trim();
                        string applicationBik =
                            ((string) customerRequirement.SelectToken("applicationGuarantee.account.bik") ?? "").Trim();
                        string contractSettlementAccount =
                            ((string) customerRequirement.SelectToken("contractGuarantee.account.settlementAccount") ??
                             "")
                            .Trim();
                        string contractPersonalAccount =
                            ((string) customerRequirement.SelectToken("contractGuarantee.account.personalAccount") ??
                             "")
                            .Trim();
                        string contractBik =
                            ((string) customerRequirement.SelectToken("contractGuarantee.account.bik") ?? "").Trim();
                        string customerRegNum = ((string) customerRequirement.SelectToken("customer.regNum") ?? "")
                            .Trim();
                        string customerFullName =
                            ((string) customerRequirement.SelectToken("customer.fullName") ?? "").Trim();
                        string customerRequirementMaxPrice =
                            ((string) customerRequirement.SelectToken("contractConditionsInfo.maxPriceInfo.maxPrice") ??
                             "").Trim();
                        string purchaseObjectDescription =
                            ((string) customerRequirement.SelectToken("purchaseObjectDescription") ?? "").Trim();
                        if (!string.IsNullOrEmpty(purchaseObjectDescription))
                        {
                            deliveryTerm = $"{deliveryTerm} {purchaseObjectDescription}".Trim();
                        }

                        if (!String.IsNullOrEmpty(customerRegNum))
                        {
                            string selectCustomer =
                                $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                            MySqlCommand cmd13 = new MySqlCommand(selectCustomer, connect);
                            cmd13.Prepare();
                            cmd13.Parameters.AddWithValue("@reg_num", customerRegNum);
                            MySqlDataReader reader5 = cmd13.ExecuteReader();
                            if (reader5.HasRows)
                            {
                                reader5.Read();
                                idCustomer = reader5.GetInt32("id_customer");
                                reader5.Close();
                            }
                            else
                            {
                                reader5.Close();
                                string customerInn = "";
                                if (!String.IsNullOrEmpty(organizerInn))
                                {
                                    if (organizerRegNum == customerRegNum)
                                    {
                                        customerInn = organizerInn;
                                    }
                                }

                                string insertCustomer =
                                    $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, inn = @inn";
                                MySqlCommand cmd14 = new MySqlCommand(insertCustomer, connect);
                                cmd14.Prepare();
                                cmd14.Parameters.AddWithValue("@reg_num", customerRegNum);
                                cmd14.Parameters.AddWithValue("@full_name", customerFullName);
                                cmd14.Parameters.AddWithValue("@inn", customerInn);
                                cmd14.ExecuteNonQuery();
                                idCustomer = (int) cmd14.LastInsertedId;
                            }
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(customerFullName))
                            {
                                string selectCustName =
                                    $"SELECT id_customer FROM {Program.Prefix}customer WHERE full_name = @full_name";
                                MySqlCommand cmd15 = new MySqlCommand(selectCustName, connect);
                                cmd15.Prepare();
                                cmd15.Parameters.AddWithValue("@full_name", customerFullName);
                                MySqlDataReader reader6 = cmd15.ExecuteReader();
                                if (reader6.HasRows)
                                {
                                    reader6.Read();
                                    idCustomer = reader6.GetInt32("id_customer");
                                    Log.Logger("Получили id_customer по customer_full_name", FilePath);
                                }

                                reader6.Close();
                            }
                        }

                        string insertCustomerRequirement =
                            $"INSERT INTO {Program.Prefix}customer_requirement SET id_lot = @id_lot, id_customer = @id_customer, kladr_place = @kladr_place, delivery_place = @delivery_place, delivery_term = @delivery_term, application_guarantee_amount = @application_guarantee_amount, application_settlement_account = @application_settlement_account, application_personal_account = @application_personal_account, application_bik = @application_bik, contract_guarantee_amount = @contract_guarantee_amount, contract_settlement_account = @contract_settlement_account, contract_personal_account = @contract_personal_account, contract_bik = @contract_bik, max_price = @max_price";
                        MySqlCommand cmd16 = new MySqlCommand(insertCustomerRequirement, connect);
                        cmd16.Prepare();
                        cmd16.Parameters.AddWithValue("@id_lot", idLot);
                        cmd16.Parameters.AddWithValue("@id_customer", idCustomer);
                        cmd16.Parameters.AddWithValue("@kladr_place", kladrPlace);
                        cmd16.Parameters.AddWithValue("@delivery_place", deliveryPlace);
                        cmd16.Parameters.AddWithValue("@delivery_term", deliveryTerm);
                        cmd16.Parameters.AddWithValue("@application_guarantee_amount",
                            applicationGuaranteeAmount);
                        cmd16.Parameters.AddWithValue("@application_settlement_account",
                            applicationSettlementAccount);
                        cmd16.Parameters.AddWithValue("@application_personal_account",
                            applicationPersonalAccount);
                        cmd16.Parameters.AddWithValue("@application_bik", applicationBik);
                        cmd16.Parameters.AddWithValue("@contract_guarantee_amount", contractGuaranteeAmount);
                        cmd16.Parameters.AddWithValue("@contract_settlement_account", contractSettlementAccount);
                        cmd16.Parameters.AddWithValue("@contract_personal_account", contractPersonalAccount);
                        cmd16.Parameters.AddWithValue("@contract_bik", contractBik);
                        cmd16.Parameters.AddWithValue("@max_price", customerRequirementMaxPrice);
                        cmd16.ExecuteNonQuery();
                        if (idCustomer == 0)
                        {
                            Log.Logger("Нет id_customer", FilePath);
                        }
                    }

                    List<JToken> preferenses = GetElements(tender, "notificationInfo.preferensesInfo.preferenseInfo");
                    foreach (var preferense in preferenses)
                    {
                        string preferenseName =
                            ((string) preferense.SelectToken("preferenseRequirementInfo.name") ?? "").Trim();
                        string insertPreference =
                            $"INSERT INTO {Program.Prefix}preferense SET id_lot = @id_lot, name = @name";
                        MySqlCommand cmd17 = new MySqlCommand(insertPreference, connect);
                        cmd17.Prepare();
                        cmd17.Parameters.AddWithValue("@id_lot", idLot);
                        cmd17.Parameters.AddWithValue("@name", preferenseName);
                        cmd17.ExecuteNonQuery();
                    }

                    List<JToken> requirements =
                        GetElements(tender, "notificationInfo.requirementsInfo.requirementInfo");
                    foreach (var requirement in requirements)
                    {
                        string requirementName =
                            ((string) requirement.SelectToken("preferenseRequirementInfo.name") ?? "").Trim();
                        string requirementContent = ((string) requirement.SelectToken("content") ?? "").Trim();
                        string requirementCode = ((string) requirement.SelectToken("code") ?? "").Trim();
                        string insertRequirement =
                            $"INSERT INTO {Program.Prefix}requirement SET id_lot = @id_lot, name = @name, content = @content, code = @code";
                        MySqlCommand cmd18 = new MySqlCommand(insertRequirement, connect);
                        cmd18.Prepare();
                        cmd18.Parameters.AddWithValue("@id_lot", idLot);
                        cmd18.Parameters.AddWithValue("@name", requirementName);
                        cmd18.Parameters.AddWithValue("@content", requirementContent);
                        cmd18.Parameters.AddWithValue("@code", requirementCode);
                        cmd18.ExecuteNonQuery();
                    }

                    List<JToken> restricts = GetElements(tender, "notificationInfo.restrictionsInfo.restrictionInfo");
                    foreach (var restrict in restricts)
                    {
                        string rInfo = ((string) restrict.SelectToken("preferenseRequirementInfo.name") ?? "").Trim();
                        string fInfo = ((string) restrict.SelectToken("content") ?? "").Trim();
                        string insertRestrict =
                            $"INSERT INTO {Program.Prefix}restricts SET id_lot = @id_lot, foreign_info = @foreign_info, info = @info";
                        MySqlCommand cmd19 = new MySqlCommand(insertRestrict, connect);
                        cmd19.Prepare();
                        cmd19.Parameters.AddWithValue("@id_lot", idLot);
                        cmd19.Parameters.AddWithValue("@foreign_info", fInfo);
                        cmd19.Parameters.AddWithValue("@info", rInfo);
                        cmd19.ExecuteNonQuery();
                    }

                    List<JToken> purchaseobjects = GetElements(tender,
                        "notificationInfo.purchaseObjectsInfo.notDrugPurchaseObjectsInfo.purchaseObject");
                    foreach (var purchaseobject in purchaseobjects)
                    {
                        string okpd2Code = ((string) purchaseobject.SelectToken("OKPD2.OKPDCode") ?? "").Trim();
                        string okpdCode = ((string) purchaseobject.SelectToken("OKPD.code") ?? "").Trim();
                        string okpdName = ((string) purchaseobject.SelectToken("OKPD2.OKPDName") ?? "").Trim();
                        if (String.IsNullOrEmpty(okpdName))
                            okpdName = ((string) purchaseobject.SelectToken("OKPD.name") ?? "").Trim();
                        string name = ((string) purchaseobject.SelectToken("name") ?? "").Trim();
                        if (!String.IsNullOrEmpty(name))
                            name = Regex.Replace(name, @"\s+", " ");
                        string quantityValue = ((string) purchaseobject.SelectToken("quantity.value") ?? "")
                            .Trim();
                        string price = ((string) purchaseobject.SelectToken("price") ?? "").Trim();
                        price = price.Replace(",", ".");
                        string okei = ((string) purchaseobject.SelectToken("OKEI.nationalCode") ?? "").Trim();
                        string sumP = ((string) purchaseobject.SelectToken("sum") ?? "").Trim();
                        sumP = sumP.Replace(",", ".");
                        int okpd2GroupCode = 0;
                        string okpd2GroupLevel1Code = "";
                        if (!String.IsNullOrEmpty(okpd2Code))
                        {
                            GetOkpd(okpd2Code, out okpd2GroupCode, out okpd2GroupLevel1Code);
                        }

                        List<JToken> customerquantities =
                            GetElements(purchaseobject, "customerQuantities.customerQuantity");
                        foreach (var customerquantity in customerquantities)
                        {
                            string customerQuantityValue =
                                ((string) customerquantity.SelectToken("quantity") ?? "").Trim();
                            string custRegNum = ((string) customerquantity.SelectToken("customer.regNum") ?? "")
                                .Trim();
                            string custFullName =
                                ((string) customerquantity.SelectToken("customer.fullName") ?? "").Trim();
                            int idCustomerQ = 0;
                            if (!String.IsNullOrEmpty(custRegNum))
                            {
                                string selectCustomerQ =
                                    $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                                MySqlCommand cmd20 = new MySqlCommand(selectCustomerQ, connect);
                                cmd20.Prepare();
                                cmd20.Parameters.AddWithValue("@reg_num", custRegNum);
                                MySqlDataReader reader7 = cmd20.ExecuteReader();
                                if (reader7.HasRows)
                                {
                                    reader7.Read();
                                    idCustomerQ = reader7.GetInt32("id_customer");
                                    reader7.Close();
                                }
                                else
                                {
                                    reader7.Close();
                                    string insertCustomerQ =
                                        $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name";
                                    MySqlCommand cmd21 = new MySqlCommand(insertCustomerQ, connect);
                                    cmd21.Prepare();
                                    cmd21.Parameters.AddWithValue("@reg_num", custRegNum);
                                    cmd21.Parameters.AddWithValue("@full_name", custFullName);
                                    cmd21.ExecuteNonQuery();
                                    idCustomerQ = (int) cmd21.LastInsertedId;
                                }
                            }
                            else
                            {
                                if (!String.IsNullOrEmpty(custFullName))
                                {
                                    string selectCustNameQ =
                                        $"SELECT id_customer FROM {Program.Prefix}customer WHERE full_name = @full_name";
                                    MySqlCommand cmd22 = new MySqlCommand(selectCustNameQ, connect);
                                    cmd22.Prepare();
                                    cmd22.Parameters.AddWithValue("@full_name", custFullName);
                                    MySqlDataReader reader8 = cmd22.ExecuteReader();
                                    if (reader8.HasRows)
                                    {
                                        reader8.Read();
                                        idCustomerQ = reader8.GetInt32("id_customer");
                                        Log.Logger("Получили id_customer_q по customer_full_name", FilePath);
                                    }

                                    reader8.Close();
                                }
                            }

                            string insertCustomerquantity =
                                $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, okpd2_group_code = @okpd2_group_code, okpd2_group_level1_code = @okpd2_group_level1_code, okpd_code = @okpd_code, okpd_name = @okpd_name, name = @name, quantity_value = @quantity_value, price = @price, okei = @okei, sum = @sum, customer_quantity_value = @customer_quantity_value";
                            MySqlCommand cmd23 = new MySqlCommand(insertCustomerquantity, connect);
                            cmd23.Prepare();
                            cmd23.Parameters.AddWithValue("@id_lot", idLot);
                            cmd23.Parameters.AddWithValue("@id_customer", idCustomerQ);
                            cmd23.Parameters.AddWithValue("@okpd2_code", okpd2Code);
                            cmd23.Parameters.AddWithValue("@okpd2_group_code", okpd2GroupCode);
                            cmd23.Parameters.AddWithValue("@okpd2_group_level1_code", okpd2GroupLevel1Code);
                            cmd23.Parameters.AddWithValue("@okpd_code", okpdCode);
                            cmd23.Parameters.AddWithValue("@okpd_name", okpdName);
                            cmd23.Parameters.AddWithValue("@name", name);
                            cmd23.Parameters.AddWithValue("@quantity_value", quantityValue);
                            cmd23.Parameters.AddWithValue("@price", price);
                            cmd23.Parameters.AddWithValue("@okei", okei);
                            cmd23.Parameters.AddWithValue("@sum", sumP);
                            cmd23.Parameters.AddWithValue("@customer_quantity_value", customerQuantityValue);
                            cmd23.ExecuteNonQuery();
                            if (idCustomerQ == 0)
                                Log.Logger("Нет id_customer_q", FilePath);
                        }

                        if (customerquantities.Count == 0)
                        {
                            string insertCustomerquantity =
                                $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, okpd2_group_code = @okpd2_group_code, okpd2_group_level1_code = @okpd2_group_level1_code, okpd_code = @okpd_code, okpd_name = @okpd_name, name = @name, quantity_value = @quantity_value, price = @price, okei = @okei, sum = @sum, customer_quantity_value = @customer_quantity_value";
                            MySqlCommand cmd24 = new MySqlCommand(insertCustomerquantity, connect);
                            cmd24.Prepare();
                            cmd24.Parameters.AddWithValue("@id_lot", idLot);
                            cmd24.Parameters.AddWithValue("@id_customer", idCustomer);
                            cmd24.Parameters.AddWithValue("@okpd2_code", okpd2Code);
                            cmd24.Parameters.AddWithValue("@okpd2_group_code", okpd2GroupCode);
                            cmd24.Parameters.AddWithValue("@okpd2_group_level1_code", okpd2GroupLevel1Code);
                            cmd24.Parameters.AddWithValue("@okpd_code", okpdCode);
                            cmd24.Parameters.AddWithValue("@okpd_name", okpdName);
                            cmd24.Parameters.AddWithValue("@name", name);
                            cmd24.Parameters.AddWithValue("@quantity_value", quantityValue);
                            cmd24.Parameters.AddWithValue("@price", price);
                            cmd24.Parameters.AddWithValue("@okei", okei);
                            cmd24.Parameters.AddWithValue("@sum", sumP);
                            cmd24.Parameters.AddWithValue("@customer_quantity_value", quantityValue);
                            cmd24.ExecuteNonQuery();
                        }
                    }

                    List<JToken> drugPurchaseObjectsInfo = GetElements(tender,
                        "notificationInfo.purchaseObjectsInfo.drugPurchaseObjectsInfo.drugPurchaseObjectInfo");
                    foreach (var drugPurchaseObjectInfo in drugPurchaseObjectsInfo)
                    {
                        pils = true;
                        List<JToken> drugQuantityCustomersInfo =
                            GetElements(drugPurchaseObjectInfo, "drugQuantityCustomersInfo.drugQuantityCustomerInfo");
                        foreach (var drugQuantityCustomerInfo in drugQuantityCustomersInfo)
                        {
                            string customerQuantityValue =
                                ((string) drugQuantityCustomerInfo.SelectToken("quantity") ?? "").Trim();
                            string custRegNum = ((string) drugQuantityCustomerInfo.SelectToken("customer.regNum") ?? "")
                                .Trim();
                            string custFullName =
                                ((string) drugQuantityCustomerInfo.SelectToken("customer.fullName") ?? "").Trim();
                            int idCustomerQ = 0;
                            if (!String.IsNullOrEmpty(custRegNum))
                            {
                                string selectCustomerQ =
                                    $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                                MySqlCommand cmd20 = new MySqlCommand(selectCustomerQ, connect);
                                cmd20.Prepare();
                                cmd20.Parameters.AddWithValue("@reg_num", custRegNum);
                                MySqlDataReader reader7 = cmd20.ExecuteReader();
                                if (reader7.HasRows)
                                {
                                    reader7.Read();
                                    idCustomerQ = reader7.GetInt32("id_customer");
                                    reader7.Close();
                                }
                                else
                                {
                                    reader7.Close();
                                    string insertCustomerQ =
                                        $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name";
                                    MySqlCommand cmd21 = new MySqlCommand(insertCustomerQ, connect);
                                    cmd21.Prepare();
                                    cmd21.Parameters.AddWithValue("@reg_num", custRegNum);
                                    cmd21.Parameters.AddWithValue("@full_name", custFullName);
                                    cmd21.ExecuteNonQuery();
                                    idCustomerQ = (int) cmd21.LastInsertedId;
                                }
                            }
                            else
                            {
                                if (!String.IsNullOrEmpty(custFullName))
                                {
                                    string selectCustNameQ =
                                        $"SELECT id_customer FROM {Program.Prefix}customer WHERE full_name = @full_name";
                                    MySqlCommand cmd22 = new MySqlCommand(selectCustNameQ, connect);
                                    cmd22.Prepare();
                                    cmd22.Parameters.AddWithValue("@full_name", custFullName);
                                    MySqlDataReader reader8 = cmd22.ExecuteReader();
                                    if (reader8.HasRows)
                                    {
                                        reader8.Read();
                                        idCustomerQ = reader8.GetInt32("id_customer");
                                        Log.Logger("Получили id_customer_q по customer_full_name", FilePath);
                                    }

                                    reader8.Close();
                                }
                            }

                            var drugsInfo = GetElements(drugPurchaseObjectInfo,
                                "objectInfoUsingTextForm.drugsInfo.drugInfo");
                            foreach (var drugInfo in drugsInfo)
                            {
                                string okpd2Code =
                                    ((string) drugInfo.SelectToken("MNNInfo.MNNExternalCode") ?? "").Trim();
                                string name = ((string) drugInfo.SelectToken("MNNInfo.MNNName") ?? "").Trim();
                                string medicamentalFormName =
                                    ((string) drugInfo.SelectToken("medicamentalFormInfo.medicamentalFormName") ?? "")
                                    .Trim();
                                if (!string.IsNullOrEmpty(medicamentalFormName))
                                {
                                    name = $"{name} {medicamentalFormName}";
                                }

                                string dosageGrlsValue =
                                    ((string) drugInfo.SelectToken("dosageInfo.dosageGRLSValue") ?? "").Trim();
                                if (!string.IsNullOrEmpty(dosageGrlsValue))
                                {
                                    name = $"{name} {dosageGrlsValue}";
                                }

                                if (!String.IsNullOrEmpty(name))
                                    name = Regex.Replace(name, @"\s+", " ");
                                string quantityValue = ((string) drugInfo.SelectToken("drugQuantity") ?? "")
                                    .Trim();
                                string okei = ((string) drugInfo.SelectToken("dosageInfo.dosageUserOKEI.name") ?? "")
                                    .Trim();
                                string price =
                                    ((string) drugPurchaseObjectInfo.SelectToken("pricePerUnit") ?? "").Trim();
                                price = price.Replace(",", ".");
                                string sumP =
                                    ((string) drugPurchaseObjectInfo.SelectToken("positionPrice") ?? "").Trim();
                                sumP = sumP.Replace(",", ".");
                                string insertCustomerquantity =
                                    $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, name = @name, quantity_value = @quantity_value, price = @price, okei = @okei, sum = @sum, customer_quantity_value = @customer_quantity_value";
                                MySqlCommand cmd23 = new MySqlCommand(insertCustomerquantity, connect);
                                cmd23.Prepare();
                                cmd23.Parameters.AddWithValue("@id_lot", idLot);
                                cmd23.Parameters.AddWithValue("@id_customer", idCustomerQ);
                                cmd23.Parameters.AddWithValue("@okpd2_code", okpd2Code);
                                cmd23.Parameters.AddWithValue("@name", name);
                                cmd23.Parameters.AddWithValue("@quantity_value", quantityValue);
                                cmd23.Parameters.AddWithValue("@price", price);
                                cmd23.Parameters.AddWithValue("@okei", okei);
                                cmd23.Parameters.AddWithValue("@sum", sumP);
                                cmd23.Parameters.AddWithValue("@customer_quantity_value", customerQuantityValue);
                                cmd23.ExecuteNonQuery();
                                if (idCustomerQ == 0)
                                    Log.Logger("Нет id_customer_q", FilePath);
                            }
                        }

                        if (drugQuantityCustomersInfo.Count == 0)
                        {
                            var drugsInfo = GetElements(drugPurchaseObjectInfo,
                                "objectInfoUsingReferenceInfo.drugsInfo.drugInfo");
                            foreach (var drugInfo in drugsInfo)
                            {
                                string okpd2Code =
                                    ((string) drugInfo.SelectToken("MNNInfo.MNNExternalCode") ?? "").Trim();
                                string name = ((string) drugInfo.SelectToken("MNNInfo.MNNName") ?? "").Trim();
                                string medicamentalFormName =
                                    ((string) drugInfo.SelectToken("medicamentalFormInfo.medicamentalFormName") ?? "")
                                    .Trim();
                                if (!string.IsNullOrEmpty(medicamentalFormName))
                                {
                                    name = $"{name} {medicamentalFormName}";
                                }

                                string dosageGrlsValue =
                                    ((string) drugInfo.SelectToken("dosageInfo.dosageGRLSValue") ?? "").Trim();
                                if (!string.IsNullOrEmpty(dosageGrlsValue))
                                {
                                    name = $"{name} {dosageGrlsValue}";
                                }

                                if (!String.IsNullOrEmpty(name))
                                    name = Regex.Replace(name, @"\s+", " ");
                                string quantityValue = ((string) drugInfo.SelectToken("drugQuantity") ?? "")
                                    .Trim();
                                string okei = ((string) drugInfo.SelectToken("dosageInfo.dosageUserOKEI.name") ?? "")
                                    .Trim();
                                string price =
                                    ((string) drugPurchaseObjectInfo.SelectToken("pricePerUnit") ?? "").Trim();
                                price = price.Replace(",", ".");
                                string sumP =
                                    ((string) drugPurchaseObjectInfo.SelectToken("positionPrice") ?? "").Trim();
                                sumP = sumP.Replace(",", ".");
                                string insertCustomerquantity =
                                    $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, name = @name, quantity_value = @quantity_value, price = @price, okei = @okei, sum = @sum, customer_quantity_value = @customer_quantity_value";
                                MySqlCommand cmd23 = new MySqlCommand(insertCustomerquantity, connect);
                                cmd23.Prepare();
                                cmd23.Parameters.AddWithValue("@id_lot", idLot);
                                cmd23.Parameters.AddWithValue("@id_customer", idCustomer);
                                cmd23.Parameters.AddWithValue("@okpd2_code", okpd2Code);
                                cmd23.Parameters.AddWithValue("@name", name);
                                cmd23.Parameters.AddWithValue("@quantity_value", quantityValue);
                                cmd23.Parameters.AddWithValue("@price", price);
                                cmd23.Parameters.AddWithValue("@okei", okei);
                                cmd23.Parameters.AddWithValue("@sum", sumP);
                                cmd23.Parameters.AddWithValue("@customer_quantity_value", quantityValue);
                                cmd23.ExecuteNonQuery();
                                if (idCustomer == 0)
                                    Log.Logger("Нет id_customer", FilePath);
                            }
                        }
                    }

                    Tender.TenderKwords(connect, idTender, pils);
                    AddVerNumber(connect, purchaseNumber);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег Tender504", FilePath);
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
    }
}