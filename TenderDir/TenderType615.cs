using System;
using System.Data;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderType615 : Tender
    {
        public event Action<int> AddTender615;
        private bool Up = default ;
        public TenderType615(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddTender615 += delegate(int d)
            {
                if (d > 0 && !Up)
                    Program.AddTender615++;
                else if (d > 0 && Up)
                    Program.UpdateTender615++;
                else
                    Log.Logger("Не удалось добавить Tender615", FilePath);
            };
        }

        public override void Parsing()
        {
            var xml = GetXml(File.ToString());
            var root = (JObject) T.SelectToken("export");
            var firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("pprf615"));
            if (firstOrDefault != null)
            {
                var tender = firstOrDefault.Value;
                var idT = ((string) tender.SelectToken("id") ?? "").Trim();
                if (string.IsNullOrEmpty(idT))
                {
                    Log.Logger("У тендера нет id", FilePath);
                    return;
                }

                var purchaseNumber = ((string) tender.SelectToken("commonInfo.purchaseNumber") ?? "").Trim();
                if (string.IsNullOrEmpty(purchaseNumber))
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
                    var docPublishDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("commonInfo.docPublishDate") ?? "") ??
                         "").Trim('"');
                    var dateVersion = docPublishDate;
                    var href = ((string) tender.SelectToken("commonInfo.href") ?? "").Trim();
                    var printform = ((string) tender.SelectToken("printForm.url") ?? "").Trim();
                    if (string.IsNullOrEmpty(printform))
                    {
                        printform = ((string) tender.SelectToken("printFormInfo.url") ?? "").Trim();
                    }

                    if (!string.IsNullOrEmpty(printform) && printform.IndexOf("CDATA") != -1)
                        printform = printform.Substring(9, printform.Length - 12);
                    var noticeVersion = "";
                    var numVersion = (int?) tender.SelectToken("versionNumber") ?? 1;
                    var cancelStatus = 0;
                    if (!string.IsNullOrEmpty(docPublishDate))
                    {
                        var selectDateT =
                            $"SELECT id_tender, doc_publish_date FROM {Program.Prefix}tender WHERE id_region = @id_region AND purchase_number = @purchase_number";
                        var cmd2 = new MySqlCommand(selectDateT, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@id_region", RegionId);
                        cmd2.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        var dt = new DataTable();
                        var adapter = new MySqlDataAdapter {SelectCommand = cmd2};
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            Up = true;
                            foreach (DataRow row in dt.Rows)
                            {
                                var dateNew = DateTime.Parse(docPublishDate);
                                var dateOld = (DateTime) row["doc_publish_date"];
                                if (dateNew >= dateOld)
                                {
                                    var updateTenderCancel =
                                        $"UPDATE {Program.Prefix}tender SET cancel = 1 WHERE id_tender = @id_tender";
                                    var cmd3 = new MySqlCommand(updateTenderCancel, connect);
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

                    var purchaseObjectInfo =
                        ((string) tender.SelectToken("commonInfo.purchaseObjectInfo") ?? "").Trim();
                    var organizerRegNum =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.regNum") ?? "").Trim();
                    var organizerFullName =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.fullName") ?? "")
                        .Trim();
                    var organizerPostAddress =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.postAddress") ?? "")
                        .Trim();
                    var organizerFactAddress =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.factAddress") ?? "")
                        .Trim();
                    var organizerInn =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.INN") ?? "")
                        .Trim();
                    var organizerKpp =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.KPP") ?? "")
                        .Trim();
                    var organizerResponsibleRole =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.publisherRole") ?? "").Trim();
                    var organizerLastName =
                        ((string) tender.SelectToken(
                             "purchaseResponsibleInfo.responsibleInfo.contactPerson.lastName") ??
                         "").Trim();
                    var organizerFirstName =
                        ((string) tender.SelectToken(
                             "purchaseResponsibleInfo.responsibleInfo.contactPerson.firstName") ??
                         "").Trim();
                    var organizerMiddleName =
                        ((string) tender.SelectToken(
                             "purchaseResponsibleInfo.responsibleInfo.contactPerson.middleName") ??
                         "").Trim();
                    var organizerContact = $"{organizerLastName} {organizerFirstName} {organizerMiddleName}"
                        .Trim();
                    var organizerEmail =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleInfo.contactEMail") ?? "")
                        .Trim();
                    var organizerFax =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleInfo.contactFax") ?? "")
                        .Trim();
                    var organizerPhone =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleInfo.contactPhone") ?? "")
                        .Trim();
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

                    var idPlacingWay = 0;
                    var placingWayCode = ((string) tender.SelectToken("placingWayInfo.code") ?? "").Trim();
                    var placingWayName = ((string) tender.SelectToken("placingWayInfo.name") ?? "").Trim();
                    if (!string.IsNullOrEmpty(placingWayCode))
                    {
                        var selectPlacingWay =
                            $"SELECT id_placing_way FROM {Program.Prefix}placing_way WHERE code = @code";
                        var cmd6 = new MySqlCommand(selectPlacingWay, connect);
                        cmd6.Prepare();
                        cmd6.Parameters.AddWithValue("@code", placingWayCode);
                        var reader3 = cmd6.ExecuteReader();
                        if (reader3.HasRows)
                        {
                            reader3.Read();
                            idPlacingWay = reader3.GetInt32("id_placing_way");
                            reader3.Close();
                        }
                        else
                        {
                            reader3.Close();
                            var conformity = GetConformity(placingWayName);
                            var insertPlacingWay =
                                $"INSERT INTO {Program.Prefix}placing_way SET code= @code, name= @name, conformity = @conformity";
                            var cmd7 = new MySqlCommand(insertPlacingWay, connect);
                            cmd7.Prepare();
                            cmd7.Parameters.AddWithValue("@code", placingWayCode);
                            cmd7.Parameters.AddWithValue("@name", placingWayName);
                            cmd7.Parameters.AddWithValue("@conformity", conformity);
                            cmd7.ExecuteNonQuery();
                            idPlacingWay = (int) cmd7.LastInsertedId;
                        }
                    }

                    var idEtp = 0;
                    var etpCode = ((string) tender.SelectToken("notificationInfo.ETPInfo.code") ?? "").Trim();
                    var etpName = ((string) tender.SelectToken("notificationInfo.ETPInfo.name") ?? "").Trim();
                    var etpUrl = ((string) tender.SelectToken("notificationInfo.ETPInfo.url") ?? "").Trim();
                    if (!string.IsNullOrEmpty(etpCode))
                    {
                        var selectEtp = $"SELECT id_etp FROM {Program.Prefix}etp WHERE code = @code";
                        var cmd7 = new MySqlCommand(selectEtp, connect);
                        cmd7.Prepare();
                        cmd7.Parameters.AddWithValue("@code", etpCode);
                        var reader4 = cmd7.ExecuteReader();
                        if (reader4.HasRows)
                        {
                            reader4.Read();
                            idEtp = reader4.GetInt32("id_etp");
                            reader4.Close();
                        }
                        else
                        {
                            reader4.Close();
                            var insertEtp =
                                $"INSERT INTO {Program.Prefix}etp SET code= @code, name= @name, url= @url, conf=0";
                            var cmd8 = new MySqlCommand(insertEtp, connect);
                            cmd8.Prepare();
                            cmd8.Parameters.AddWithValue("@code", etpCode);
                            cmd8.Parameters.AddWithValue("@name", etpName);
                            cmd8.Parameters.AddWithValue("@url", etpUrl);
                            cmd8.ExecuteNonQuery();
                            idEtp = (int) cmd8.LastInsertedId;
                        }
                    }

                    var endDate =
                        (JsonConvert.SerializeObject(
                             tender.SelectToken("notificationInfo.procedureInfo.collectingEndDate") ?? "") ??
                         "").Trim('"');
                    var scoringDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("notificationInfo.procedureInfo.scoringDate") ??
                                                     "") ??
                         "").Trim('"');
                    var scoringDateT = scoringDate.ParseDateUn("yyyy-MM-ddzzz");
                    var biddingDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("notificationInfo.procedureInfo.biddingDate") ??
                                                     "") ??
                         "").Trim('"');
                    var biddingDateT = biddingDate.ParseDateUn("yyyy-MM-ddzzz");
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
                    cmd9.Parameters.AddWithValue("@type_fz", 615);
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
                    if (biddingDateT == DateTime.MinValue)
                    {
                        cmd9.Parameters.AddWithValue("@bidding_date", biddingDate);
                    }
                    else
                    {
                        cmd9.Parameters.AddWithValue("@bidding_date", biddingDateT);
                    }
                    cmd9.Parameters.AddWithValue("@cancel", cancelStatus);
                    cmd9.Parameters.AddWithValue("@date_version", dateVersion);
                    cmd9.Parameters.AddWithValue("@num_version", numVersion);
                    cmd9.Parameters.AddWithValue("@notice_version", noticeVersion);
                    cmd9.Parameters.AddWithValue("@xml", xml);
                    cmd9.Parameters.AddWithValue("@print_form", printform);
                    var resInsertTender = cmd9.ExecuteNonQuery();
                    var idTender = (int) cmd9.LastInsertedId;
                    AddTender615?.Invoke(resInsertTender);
                    if (cancelStatus == 0)
                    {
                        var updateContract =
                            $"UPDATE {Program.Prefix}contract_sign SET id_tender = @id_tender WHERE purchase_number = @purchase_number";
                        var cmd10 = new MySqlCommand(updateContract, connect);
                        cmd10.Prepare();
                        cmd10.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        cmd10.Parameters.AddWithValue("@id_tender", idTender);
                        cmd10.ExecuteNonQuery();
                    }

                    var attachments = GetElements(tender, "attachmentsInfo.attachmentInfo");
                    foreach (var att in attachments)
                    {
                        var attachName = ((string) att.SelectToken("fileName") ?? "").Trim();
                        var attachDescription = ((string) att.SelectToken("docDescription") ?? "").Trim();
                        var attachUrl = ((string) att.SelectToken("url") ?? "").Trim();
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
                    var lots = GetElements(tender, "notificationInfo.purchaseSubjectInfo");
                    foreach (var lot in lots)
                    {
                        var lotMaxPrice =
                            ((string) tender.SelectToken("notificationInfo.contractCondition.maxPriceInfo.maxPrice") ??
                             "").Trim();
                        var lotCurrency = "";
                        var lotFinanceSource = "";
                        var lotName = ((string) lot.SelectToken("name") ?? "").Trim();
                        var insertLot =
                            $"INSERT INTO {Program.Prefix}lot SET id_tender = @id_tender, lot_number = @lot_number, max_price = @max_price, currency = @currency, finance_source = @finance_source";
                        var cmd12 = new MySqlCommand(insertLot, connect);
                        cmd12.Prepare();
                        cmd12.Parameters.AddWithValue("@id_tender", idTender);
                        cmd12.Parameters.AddWithValue("@lot_number", lotNumber);
                        cmd12.Parameters.AddWithValue("@max_price", lotMaxPrice);
                        cmd12.Parameters.AddWithValue("@currency", lotCurrency);
                        cmd12.Parameters.AddWithValue("@finance_source", lotFinanceSource);
                        cmd12.ExecuteNonQuery();
                        var idLot = (int) cmd12.LastInsertedId;
                        if (idLot < 1)
                            Log.Logger("Не получили id лота", FilePath);
                        lotNumber++;
                        if (!string.IsNullOrEmpty(organizerRegNum))
                        {
                            var selectCustomer =
                                $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                            var cmd13 = new MySqlCommand(selectCustomer, connect);
                            cmd13.Prepare();
                            cmd13.Parameters.AddWithValue("@reg_num", organizerRegNum);
                            var reader5 = cmd13.ExecuteReader();
                            if (reader5.HasRows)
                            {
                                reader5.Read();
                                idCustomer = reader5.GetInt32("id_customer");
                                reader5.Close();
                            }
                            else
                            {
                                reader5.Close();
                                var customerInn = "";
                                if (!string.IsNullOrEmpty(organizerInn))
                                {
                                    customerInn = organizerInn;
                                }

                                var insertCustomer =
                                    $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, inn = @inn";
                                var cmd14 = new MySqlCommand(insertCustomer, connect);
                                cmd14.Prepare();
                                cmd14.Parameters.AddWithValue("@reg_num", organizerRegNum);
                                cmd14.Parameters.AddWithValue("@full_name", organizerFullName);
                                cmd14.Parameters.AddWithValue("@inn", customerInn);
                                cmd14.ExecuteNonQuery();
                                idCustomer = (int) cmd14.LastInsertedId;
                            }
                        }

                        var applicationGuaranteeAmount =
                            ((string) tender.SelectToken(
                                 "notificationInfo.contractCondition.maxPriceInfo.applicationGuarantee.amount") ?? "")
                            .Trim();
                        var contractGuaranteeAmount =
                            ((string) tender.SelectToken(
                                 "notificationInfo.contractCondition.maxPriceInfo.contractGuarantee.amount") ?? "")
                            .Trim();
                        var deliveryTerm =
                            ((string) tender.SelectToken("notificationInfo.contractCondition.deliveryTerm") ?? "")
                            .Trim();
                        var deliveryTerm1 =
                            ((string) tender.SelectToken("notificationInfo.contractCondition.deliveryConditions") ?? "")
                            .Trim();
                        deliveryTerm = $"{deliveryTerm} {deliveryTerm1}".Trim();
                        var customerRequirements =
                            GetElements(tender, "notificationInfo.contractCondition.kladrPlacesInfo.kladrPlace");
                        foreach (var customerRequirement in customerRequirements)
                        {
                            var kladrPlace =
                                ((string) customerRequirement.SelectToken("kladr.fullName") ??
                                 "").Trim();
                            var deliveryPlace =
                                ((string) customerRequirement.SelectToken("deliveryPlace") ?? "")
                                .Trim();
                            var insertCustomerRequirement =
                                $"INSERT INTO {Program.Prefix}customer_requirement SET id_lot = @id_lot, id_customer = @id_customer, kladr_place = @kladr_place, delivery_place = @delivery_place, delivery_term = @delivery_term, application_guarantee_amount = @application_guarantee_amount, contract_guarantee_amount = @contract_guarantee_amount, max_price = @max_price";
                            var cmd16 = new MySqlCommand(insertCustomerRequirement, connect);
                            cmd16.Prepare();
                            cmd16.Parameters.AddWithValue("@id_lot", idLot);
                            cmd16.Parameters.AddWithValue("@id_customer", idCustomer);
                            cmd16.Parameters.AddWithValue("@kladr_place", kladrPlace);
                            cmd16.Parameters.AddWithValue("@delivery_place", deliveryPlace);
                            cmd16.Parameters.AddWithValue("@delivery_term", deliveryTerm);
                            cmd16.Parameters.AddWithValue("@application_guarantee_amount",
                                applicationGuaranteeAmount);
                            cmd16.Parameters.AddWithValue("@contract_guarantee_amount", contractGuaranteeAmount);
                            cmd16.Parameters.AddWithValue("@max_price", lotMaxPrice);
                            cmd16.ExecuteNonQuery();
                        }

                        var purchaseobjects = GetElements(tender,
                            "notificationInfo.purchaseObjectsInfo.servicesWorksKindCh1St166Info.serviceWorkKindCh1St166Info");
                        var purchaseobjects1 = GetElements(tender,
                            "notificationInfo.purchaseObjectsInfo.servicesWorksKindNPAInfo.servicesWorksKindCh2St166Info.serviceWorkKindCh2St166Info");
                        purchaseobjects.AddRange(purchaseobjects1);
                        foreach (var purchaseobject in purchaseobjects)
                        {
                            var name = ((string) purchaseobject.SelectToken("name") ?? "").Trim();
                            name = $"{name} {lotName}".Trim();
                            var insertCustomerquantity =
                                $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, name = @name";
                            var cmd24 = new MySqlCommand(insertCustomerquantity, connect);
                            cmd24.Prepare();
                            cmd24.Parameters.AddWithValue("@id_lot", idLot);
                            cmd24.Parameters.AddWithValue("@id_customer", idCustomer);
                            cmd24.Parameters.AddWithValue("@name", name);
                            cmd24.ExecuteNonQuery();
                        }
                    }
                    TenderKwords(connect, idTender);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег Tender615", FilePath);
            }
        }

        private int GetConformity(string conf)
        {
            var sLower = conf.ToLower();
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