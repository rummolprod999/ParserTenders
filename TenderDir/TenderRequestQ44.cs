#region

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace ParserTenders.TenderDir
{
    public class TenderRequestQ44 : Tender
    {
        private bool Up;

        public TenderRequestQ44(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddRequest44 += delegate(int d)
            {
                if (d > 0 && !Up)
                {
                    Program.AddRequestQ44++;
                }
                else if (d > 0 && Up)
                {
                    Program.UpdateRequestQ44++;
                }
                else
                {
                    Log.Logger("Не удалось добавить RequestQ44", FilePath);
                }
            };
        }

        public event Action<int> AddRequest44;

        public override void Parsing()
        {
            var xml = GetXml(File.ToString());
            var root = (JObject)T.SelectToken("export");
            var firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("fcs"));
            if (firstOrDefault != null)
            {
                var tender = firstOrDefault.Value;
                var idT = ((string)tender.SelectToken("id") ?? "").Trim();
                if (string.IsNullOrEmpty(idT))
                {
                    Log.Logger("У тендера нет id", FilePath);
                    return;
                }

                var purchaseNumber = ((string)tender.SelectToken("registryNum") ?? "").Trim();
                if (string.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("У тендера нет registryNum", FilePath);
                }

                purchaseNumber = $"{purchaseNumber}_PR";

                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    var selectTender =
                        $"SELECT id_tender FROM {Program.Prefix}tender WHERE id_xml = @id_xml AND id_region = @id_region AND purchase_number = @purchase_number";
                    var cmd = new MySqlCommand(selectTender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_xml", idT);
                    cmd.Parameters.AddWithValue("@id_region", RegionId);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    var reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Close();
                        return;
                    }

                    reader.Close();
                    var docPublishDate = (JsonConvert.SerializeObject(tender.SelectToken("docPublishDate") ?? "") ??
                                          "").Trim('"');
                    var dateVersion = docPublishDate;
                    var href = ((string)tender.SelectToken("href") ?? "").Trim();
                    var printform = ((string)tender.SelectToken("printForm.url") ?? "").Trim();
                    if (!string.IsNullOrEmpty(printform) && printform.IndexOf("CDATA") != -1)
                    {
                        printform = printform.Substring(9, printform.Length - 12);
                    }

                    var noticeVersion = "";
                    var numVersion = 0;
                    var cancelStatus = 0;
                    var idEtp = 0;
                    var EtpName = "Единая информационная система в сфере закупок";
                    var EtpUrl = "https://zakupki.gov.ru/";
                    var selectEtp = $"SELECT id_etp FROM {Program.Prefix}etp WHERE name = @name AND url = @url";
                    var cmd7 = new MySqlCommand(selectEtp, connect);
                    cmd7.Prepare();
                    cmd7.Parameters.AddWithValue("@name", EtpName);
                    cmd7.Parameters.AddWithValue("@url", EtpUrl);
                    var dt5 = new DataTable();
                    var adapter5 = new MySqlDataAdapter { SelectCommand = cmd7 };
                    adapter5.Fill(dt5);
                    if (dt5.Rows.Count > 0)
                    {
                        idEtp = (int)dt5.Rows[0].ItemArray[0];
                    }
                    else
                    {
                        var insertEtp =
                            $"INSERT INTO {Program.Prefix}etp SET name = @name, url = @url, conf=0";
                        var cmd8 = new MySqlCommand(insertEtp, connect);
                        cmd8.Prepare();
                        cmd8.Parameters.AddWithValue("@name", EtpName);
                        cmd8.Parameters.AddWithValue("@url", EtpUrl);
                        cmd8.ExecuteNonQuery();
                        idEtp = (int)cmd8.LastInsertedId;
                    }

                    if (!string.IsNullOrEmpty(docPublishDate))
                    {
                        var selectDateT =
                            $"SELECT id_tender, doc_publish_date FROM {Program.Prefix}tender WHERE (id_region = @id_region OR id_region = 0) AND purchase_number = @purchase_number AND id_etp = @id_etp";
                        var cmd2 = new MySqlCommand(selectDateT, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@id_region", RegionId);
                        cmd2.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        cmd2.Parameters.AddWithValue("@id_etp", idEtp);
                        var dt = new DataTable();
                        var adapter = new MySqlDataAdapter { SelectCommand = cmd2 };
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            Up = true;
                            foreach (DataRow row in dt.Rows)
                            {
                                var dateNew = DateTime.Parse(docPublishDate);
                                var dateOld = (DateTime)row["doc_publish_date"];
                                if (dateNew >= dateOld)
                                {
                                    var updateTenderCancel =
                                        $"UPDATE {Program.Prefix}tender SET cancel = 1 WHERE id_tender = @id_tender";
                                    var cmd3 = new MySqlCommand(updateTenderCancel, connect);
                                    cmd3.Prepare();
                                    cmd3.Parameters.AddWithValue("id_tender", (int)row["id_tender"]);
                                    cmd3.ExecuteNonQuery();
                                }
                                else
                                {
                                    cancelStatus = 1;
                                }
                            }
                        }
                    }

                    var purchaseObjectInfo = ((string)tender.SelectToken("requestObjectInfo") ?? "").Trim();
                    var organizerRegNum =
                        ((string)tender.SelectToken("publishOrg.regNum") ?? "").Trim();
                    var organizerFullName =
                        ((string)tender.SelectToken("publishOrg.fullName") ?? "").Trim();
                    var organizerInn = ((string)tender.SelectToken("publishOrg.INN") ?? "")
                        .Trim();
                    var organizerKpp = ((string)tender.SelectToken("publishOrg.KPP") ?? "")
                        .Trim();
                    var organizerResponsibleRole =
                        ((string)tender.SelectToken("publishOrg.responsibleRole") ?? "").Trim();
                    var organizerPostAddress =
                        ((string)tender.SelectToken("responsibleInfo.place") ?? "").Trim();
                    var organizerFactAddress =
                        ((string)tender.SelectToken("responsibleInfo.place") ?? "").Trim();
                    var organizerLastName =
                        ((string)tender.SelectToken("responsibleInfo.contactPerson.lastName") ??
                         "").Trim();
                    var organizerFirstName =
                        ((string)tender.SelectToken("responsibleInfo.contactPerson.firstName") ??
                         "").Trim();
                    var organizerMiddleName =
                        ((string)tender.SelectToken("responsibleInfo.contactPerson.middleName") ??
                         "").Trim();
                    var organizerContact = $"{organizerLastName} {organizerFirstName} {organizerMiddleName}"
                        .Trim();
                    var organizerEmail =
                        ((string)tender.SelectToken("responsibleInfo.contactEMail") ?? "").Trim();
                    var organizerFax =
                        ((string)tender.SelectToken("responsibleInfo.contactFax") ?? "").Trim();
                    var organizerPhone =
                        ((string)tender.SelectToken("responsibleInfo.contactPhone") ?? "").Trim();
                    var idOrganizer = 0;
                    var idCustomer = 0;
                    if (!string.IsNullOrEmpty(organizerRegNum))
                    {
                        var selectOrg =
                            $"SELECT id_organizer FROM {Program.Prefix}organizer WHERE reg_num = @reg_num";
                        var cmd4 = new MySqlCommand(selectOrg, connect);
                        cmd4.Prepare();
                        cmd4.Parameters.AddWithValue("@reg_num", organizerRegNum);
                        var reader2 = cmd4.ExecuteReader();
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            idOrganizer = reader2.GetInt32("id_organizer");
                            reader2.Close();
                        }
                        else
                        {
                            reader2.Close();
                            var addOrganizer =
                                $"INSERT INTO {Program.Prefix}organizer SET reg_num = @reg_num, full_name = @full_name, post_address = @post_address, fact_address = @fact_address, inn = @inn, kpp = @kpp, responsible_role = @responsible_role, contact_person = @contact_person, contact_email = @contact_email, contact_phone = @contact_phone, contact_fax = @contact_fax";
                            var cmd5 = new MySqlCommand(addOrganizer, connect);
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
                            idOrganizer = (int)cmd5.LastInsertedId;
                        }
                    }
                    else
                    {
                        Log.Logger("Нет organizer_reg_num", FilePath);
                    }

                    GetPlacingWay(connect, out var idPlacingWay, "Запрос цен");

                    var endDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("procedureInfo.request.endDate") ?? "") ??
                         "").Trim('"');
                    var scoringDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("procedureInfo.request.startDate") ?? "") ??
                         "").Trim('"');
                    var biddingDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("procedureInfo.request.endDate") ?? "") ??
                         "").Trim('"');
                    var insertTender =
                        $"INSERT INTO {Program.Prefix}tender SET id_region = @id_region, id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, purchase_object_info = @purchase_object_info, type_fz = @type_fz, id_organizer = @id_organizer, id_placing_way = @id_placing_way, id_etp = @id_etp, end_date = @end_date, scoring_date = @scoring_date, bidding_date = @bidding_date, cancel = @cancel, date_version = @date_version, num_version = @num_version, notice_version = @notice_version, xml = @xml, print_form = @print_form";
                    var cmd9 = new MySqlCommand(insertTender, connect);
                    cmd9.Prepare();
                    cmd9.Parameters.AddWithValue("@id_region", RegionId);
                    cmd9.Parameters.AddWithValue("@id_xml", idT);
                    cmd9.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    cmd9.Parameters.AddWithValue("@doc_publish_date", docPublishDate);
                    cmd9.Parameters.AddWithValue("@href", href);
                    cmd9.Parameters.AddWithValue("@purchase_object_info", purchaseObjectInfo);
                    cmd9.Parameters.AddWithValue("@type_fz", 44);
                    cmd9.Parameters.AddWithValue("@id_organizer", idOrganizer);
                    cmd9.Parameters.AddWithValue("@id_placing_way", idPlacingWay);
                    cmd9.Parameters.AddWithValue("@id_etp", idEtp);
                    cmd9.Parameters.AddWithValue("@end_date", endDate);
                    cmd9.Parameters.AddWithValue("@scoring_date", scoringDate);
                    cmd9.Parameters.AddWithValue("@bidding_date", biddingDate);
                    cmd9.Parameters.AddWithValue("@cancel", cancelStatus);
                    cmd9.Parameters.AddWithValue("@date_version", dateVersion);
                    cmd9.Parameters.AddWithValue("@num_version", numVersion);
                    cmd9.Parameters.AddWithValue("@notice_version", noticeVersion);
                    cmd9.Parameters.AddWithValue("@xml", xml);
                    cmd9.Parameters.AddWithValue("@print_form", printform);
                    var resInsertTender = cmd9.ExecuteNonQuery();
                    var idTender = (int)cmd9.LastInsertedId;
                    AddRequest44?.Invoke(resInsertTender);
                    var attachments = GetElements(tender, "attachments.attachment");
                    attachments.AddRange(GetElements(tender, "notificationAttachments.attachment"));
                    foreach (var att in attachments)
                    {
                        var attachName = ((string)att.SelectToken("fileName") ?? "").Trim();
                        var attachDescription = ((string)att.SelectToken("docDescription") ?? "").Trim();
                        var attachUrl = ((string)att.SelectToken("url") ?? "").Trim();
                        if (!string.IsNullOrEmpty(attachName))
                        {
                            var insertAttach =
                                $"INSERT INTO {Program.Prefix}attachment SET id_tender = @id_tender, file_name = @file_name, url = @url, description = @description";
                            var cmd11 = new MySqlCommand(insertAttach, connect);
                            cmd11.Prepare();
                            cmd11.Parameters.AddWithValue("@id_tender", idTender);
                            cmd11.Parameters.AddWithValue("@file_name", attachName);
                            cmd11.Parameters.AddWithValue("@url", attachUrl);
                            cmd11.Parameters.AddWithValue("@description", attachDescription);
                            cmd11.ExecuteNonQuery();
                        }
                    }

                    var lotNumber = 1;
                    var lotMaxPrice = "";
                    var lotCurrency = "";
                    var lotFinanceSource = "";
                    var lotName = "";
                    var insertLot =
                        $"INSERT INTO {Program.Prefix}lot SET id_tender = @id_tender, lot_number = @lot_number, max_price = @max_price, currency = @currency, finance_source = @finance_source, lot_name = @lot_name";
                    var cmd12 = new MySqlCommand(insertLot, connect);
                    cmd12.Prepare();
                    cmd12.Parameters.AddWithValue("@id_tender", idTender);
                    cmd12.Parameters.AddWithValue("@lot_number", lotNumber);
                    cmd12.Parameters.AddWithValue("@max_price", lotMaxPrice);
                    cmd12.Parameters.AddWithValue("@currency", lotCurrency);
                    cmd12.Parameters.AddWithValue("@finance_source", lotFinanceSource);
                    cmd12.Parameters.AddWithValue("@lot_name", lotName);
                    cmd12.ExecuteNonQuery();
                    var idLot = (int)cmd12.LastInsertedId;
                    if (idLot < 1)
                    {
                        Log.Logger("Не получили id лота", FilePath);
                    }

                    var deliveryPlace1 =
                        ((string)tender.SelectToken("responsibleInfo.place") ?? "")
                        .Trim();
                    var deliveryPlace2 =
                        ((string)tender.SelectToken("responsibleInfo.addInfo") ?? "")
                        .Trim();
                    var deliveryPlace =
                        $"{deliveryPlace1} | {deliveryPlace2} | {organizerContact} | {organizerEmail} | {organizerPhone}"
                            .Trim();
                    var deliveryTerm1 =
                        (JsonConvert.SerializeObject(tender.SelectToken("procedureInfo.purchase.startDate") ?? "") ??
                         "").Trim('"');
                    var deliveryTerm2 =
                        (JsonConvert.SerializeObject(tender.SelectToken("procedureInfo.purchase.endDate") ?? "") ??
                         "").Trim('"');
                    var deliveryTerm3 =
                        ((string)tender.SelectToken("conditions.main") ?? "").Trim();
                    var deliveryTerm4 =
                        ((string)tender.SelectToken("conditions.payment") ?? "").Trim();
                    var deliveryTerm5 =
                        ((string)tender.SelectToken("conditions.delivery") ?? "").Trim();
                    var deliveryTerm6 =
                        ((string)tender.SelectToken("conditions.warranty") ?? "").Trim();
                    var deliveryTerm7 =
                        ((string)tender.SelectToken("conditions.addInfo") ?? "").Trim();
                    var deliveryTerm8 =
                        ((string)tender.SelectToken("conditions.contractGuarantee") ?? "").Trim();
                    var deliveryTerm =
                        $"Предполагаемые сроки проведения закупки  | {deliveryTerm1} | {deliveryTerm2} | {deliveryTerm3} | {deliveryTerm4} | {deliveryTerm5} | {deliveryTerm6} | {deliveryTerm7} | обеспечение контракта:  {deliveryTerm8}"
                            .Trim();
                    var insertCustomerRequirement =
                        $"INSERT INTO {Program.Prefix}customer_requirement SET id_lot = @id_lot, id_customer = @id_customer, kladr_place = @kladr_place, delivery_place = @delivery_place, delivery_term = @delivery_term, application_guarantee_amount = @application_guarantee_amount, application_settlement_account = @application_settlement_account, application_personal_account = @application_personal_account, application_bik = @application_bik, contract_guarantee_amount = @contract_guarantee_amount, contract_settlement_account = @contract_settlement_account, contract_personal_account = @contract_personal_account, contract_bik = @contract_bik, max_price = @max_price, plan_number = @plan_number, position_number = @position_number, prov_war_amount = @prov_war_amount, prov_war_part = @prov_war_part, OKPD2_code = @OKPD2_code, OKPD2_name = @OKPD2_name";
                    var cmd16 = new MySqlCommand(insertCustomerRequirement, connect);
                    cmd16.Prepare();
                    cmd16.Parameters.AddWithValue("@id_lot", idLot);
                    cmd16.Parameters.AddWithValue("@id_customer", idCustomer);
                    cmd16.Parameters.AddWithValue("@kladr_place", "");
                    cmd16.Parameters.AddWithValue("@delivery_place", deliveryPlace);
                    cmd16.Parameters.AddWithValue("@delivery_term", deliveryTerm);
                    cmd16.Parameters.AddWithValue("@application_guarantee_amount",
                        "");
                    cmd16.Parameters.AddWithValue("@application_settlement_account",
                        "");
                    cmd16.Parameters.AddWithValue("@application_personal_account",
                        "");
                    cmd16.Parameters.AddWithValue("@application_bik", "");
                    cmd16.Parameters.AddWithValue("@contract_guarantee_amount", "");
                    cmd16.Parameters.AddWithValue("@contract_settlement_account", "");
                    cmd16.Parameters.AddWithValue("@contract_personal_account", "");
                    cmd16.Parameters.AddWithValue("@contract_bik", "");
                    cmd16.Parameters.AddWithValue("@max_price", "");
                    cmd16.Parameters.AddWithValue("@plan_number", "");
                    cmd16.Parameters.AddWithValue("@position_number", "");
                    cmd16.Parameters.AddWithValue("@prov_war_amount", "");
                    cmd16.Parameters.AddWithValue("@prov_war_part", "");
                    cmd16.Parameters.AddWithValue("@OKPD2_code", "");
                    cmd16.Parameters.AddWithValue("@OKPD2_name", "");
                    cmd16.ExecuteNonQuery();
                    var purchaseobjects = GetElements(tender, "products.product");
                    foreach (var purchaseobject in purchaseobjects)
                    {
                        var okpd2Code = ((string)purchaseobject.SelectToken("OKPD2.code")
                                         ?? "").Trim();
                        if (string.IsNullOrEmpty(okpd2Code))
                        {
                            okpd2Code = ((string)purchaseobject.SelectToken("KTRU.code")
                                         ?? "").Trim();
                        }

                        var okpdName = ((string)purchaseobject.SelectToken("OKPD2.name") ?? "").Trim();
                        var name = ((string)purchaseobject.SelectToken("name") ?? "").Trim();
                        if (string.IsNullOrEmpty(name))
                        {
                            name = ((string)purchaseobject.SelectToken("KTRU.name")
                                    ?? "").Trim();
                        }

                        if (!string.IsNullOrEmpty(name))
                        {
                            name = Regex.Replace(name, @"\s+", " ");
                        }

                        var quantityValue = ((string)purchaseobject.SelectToken("quantity") ?? "")
                            .Trim();
                        var okei = ((string)purchaseobject.SelectToken("OKEI.name") ?? "").Trim();
                        var insertCustomerquantity =
                            $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, okpd2_group_code = @okpd2_group_code, okpd2_group_level1_code = @okpd2_group_level1_code, okpd_code = @okpd_code, okpd_name = @okpd_name, name = @name, quantity_value = @quantity_value, price = @price, okei = @okei, sum = @sum, customer_quantity_value = @customer_quantity_value";
                        var cmd23 = new MySqlCommand(insertCustomerquantity, connect);
                        cmd23.Prepare();
                        cmd23.Parameters.AddWithValue("@id_lot", idLot);
                        cmd23.Parameters.AddWithValue("@id_customer", 0);
                        cmd23.Parameters.AddWithValue("@okpd2_code", okpd2Code);
                        cmd23.Parameters.AddWithValue("@okpd2_group_code", "");
                        cmd23.Parameters.AddWithValue("@okpd2_group_level1_code", "");
                        cmd23.Parameters.AddWithValue("@okpd_code", "");
                        cmd23.Parameters.AddWithValue("@okpd_name", okpdName);
                        cmd23.Parameters.AddWithValue("@name", name);
                        cmd23.Parameters.AddWithValue("@quantity_value", quantityValue);
                        cmd23.Parameters.AddWithValue("@price", "");
                        cmd23.Parameters.AddWithValue("@okei", okei);
                        cmd23.Parameters.AddWithValue("@sum", "");
                        cmd23.Parameters.AddWithValue("@customer_quantity_value", quantityValue);
                        cmd23.ExecuteNonQuery();
                        TenderKwords(connect, idTender, false, new List<string>().ToArray());
                        AddVerNumber(connect, purchaseNumber);
                    }
                }
            }
            else
            {
                Log.Logger("Не могу найти тег RequestQ44", FilePath);
            }
        }

        protected void GetPlacingWay(MySqlConnection connect, out int idPlacingWay, string PlacingWay)
        {
            if (!string.IsNullOrEmpty(PlacingWay))
            {
                var selectPlacingWay =
                    $"SELECT id_placing_way FROM {Program.Prefix}placing_way WHERE name = @name";
                var cmd5 = new MySqlCommand(selectPlacingWay, connect);
                cmd5.Prepare();
                cmd5.Parameters.AddWithValue("@name", PlacingWay);
                var dt4 = new DataTable();
                var adapter4 = new MySqlDataAdapter { SelectCommand = cmd5 };
                adapter4.Fill(dt4);
                if (dt4.Rows.Count > 0)
                {
                    idPlacingWay = (int)dt4.Rows[0].ItemArray[0];
                }
                else
                {
                    var insertPlacingWay =
                        $"INSERT INTO {Program.Prefix}placing_way SET name= @name, conformity = @conformity";
                    var cmd6 = new MySqlCommand(insertPlacingWay, connect);
                    cmd6.Prepare();
                    var conformity = GetConformity(PlacingWay);
                    cmd6.Parameters.AddWithValue("@name", PlacingWay);
                    cmd6.Parameters.AddWithValue("@conformity", conformity);
                    cmd6.ExecuteNonQuery();
                    idPlacingWay = (int)cmd6.LastInsertedId;
                }
            }
            else
            {
                idPlacingWay = 0;
            }
        }

        private static int GetConformity(string conf)
        {
            var sLower = conf.ToLower();
            if (sLower.IndexOf("открыт", StringComparison.Ordinal) != -1)
            {
                return 5;
            }

            if (sLower.IndexOf("аукцион", StringComparison.Ordinal) != -1)
            {
                return 1;
            }

            if (sLower.IndexOf("котиров", StringComparison.Ordinal) != -1)
            {
                return 2;
            }

            if (sLower.IndexOf("предложен", StringComparison.Ordinal) != -1)
            {
                return 3;
            }

            if (sLower.IndexOf("единств", StringComparison.Ordinal) != -1)
            {
                return 4;
            }

            return 6;
        }
    }
}