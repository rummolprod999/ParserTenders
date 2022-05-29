using System;
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
        private bool PoExist = default;
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
            var xml = GetXml();
            var firstOrDefault = T.Properties().FirstOrDefault(p => p.Name.Contains("epN"));
            if (firstOrDefault != null)
            {
                var rootName = firstOrDefault.Name;
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
                        $"SELECT id_tender FROM {Program.Prefix}tender WHERE id_xml = @id_xml AND purchase_number = @purchase_number";
                    var cmd = new MySqlCommand(selectTender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_xml", idT);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    var reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Close();
                        return;
                    }

                    reader.Close();
                    var docPublishDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("commonInfo.publishDTInEIS") ?? "") ??
                         "").Trim('"');
                    var dateVersion = docPublishDate;
                    if (rootName == "epNotificationEOK2020" || rootName == "epNotificationEZK2020")
                    {
                        dateVersion = (JsonConvert.SerializeObject(
                                           tender.SelectToken(
                                               "commonInfo.publishDTInEIS") ?? dateVersion) ??
                                       "").Trim('"');
                        docPublishDate =
                            (JsonConvert.SerializeObject(tender.SelectToken("notificationInfo.procedureInfo.collectingInfo.startDT") ?? docPublishDate) ??
                             "").Trim('"');
                    }
                    var pils = false;
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
                    var idEtp = 0;
                    var etpCode = ((string) tender.SelectToken("commonInfo.ETP.code") ?? "").Trim();
                    var etpName = ((string) tender.SelectToken("commonInfo.ETP.name") ?? "").Trim();
                    var etpUrl = ((string) tender.SelectToken("commonInfo.ETP.url") ?? "").Trim();
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
                    if (string.IsNullOrEmpty(purchaseObjectInfo))
                    {
                        var elT = GetElements(tender, "..purchaseObjectsInfo.purchaseObject");
                        if (elT.Count > 0)
                        {
                            purchaseObjectInfo =
                                elT.Select(z => ((string)z.SelectToken("name")?? "").Trim()).Aggregate((x, y) => $"{x}, {y}".Trim(',').Trim());
                        }
                    }
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
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleRole") ?? "").Trim();
                    var organizerLastName =
                        ((string) tender.SelectToken(
                             "purchaseResponsibleInfo.responsibleInfo.contactPersonInfo.lastName") ??
                         "").Trim();
                    var organizerFirstName =
                        ((string) tender.SelectToken(
                             "purchaseResponsibleInfo.responsibleInfo.contactPersonInfo.firstName") ??
                         "").Trim();
                    var organizerMiddleName =
                        ((string) tender.SelectToken(
                             "purchaseResponsibleInfo.responsibleInfo.contactPersonInfo.middleName") ??
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
                    var addr = GetRegionString(organizerFactAddress) != "" ? organizerFactAddress :
                        GetRegionString(organizerPostAddress) != "" ? organizerPostAddress :
                        GetRegionString(organizerFactAddress) != "" ? organizerFactAddress : organizerPostAddress;
                    /*if (addr != "")
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
                    }*/

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
                    var placingWayCode = ((string) tender.SelectToken("commonInfo.placingWay.code") ?? "").Trim();
                    var placingWayName = ((string) tender.SelectToken("commonInfo.placingWay.name") ?? "").Trim();
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
                    var endDate =
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

                    var scoringDate =
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

                    var biddingDate = "";
                    var dop_info = "{}";
                    if (rootName == "epNotificationEF2020")
                    {
                        biddingDate =
                            (JsonConvert.SerializeObject(
                                 tender.SelectToken(
                                     "notificationInfo.procedureInfo.summarizingDate") ?? "") ??
                             "").Trim('"');
                        scoringDate =
                            (JsonConvert.SerializeObject(
                                 tender.SelectToken(
                                     "notificationInfo.procedureInfo.biddingDate") ?? "") ??
                             "").Trim('"');
                        var dop = GetElements(tender, "..customerRequirementInfo.contractConditionsInfo");
                        if (dop.Count > 0)
                        {
                            dop_info = dop[0]?.ToString() ?? "{}";
                        }
                    }
                    var extend_scoring_date = "";
                    var extend_bidding_date = "";
                    if (rootName == "epNotificationEOK2020" || rootName == "epNotificationEZK2020")
                    {
                        biddingDate =
                            (JsonConvert.SerializeObject(
                                 tender.SelectToken(
                                     "notificationInfo.procedureInfo.submissionProcedureDate") ?? "") ??
                             "").Trim('"');
                        scoringDate =
                            (JsonConvert.SerializeObject(
                                 tender.SelectToken(
                                     "notificationInfo.procedureInfo.firstPartsDate") ?? "") ??
                             "").Trim('"');
                        extend_scoring_date = (JsonConvert.SerializeObject(
                                                   tender.SelectToken(
                                                       "notificationInfo.procedureInfo.secondPartsDate") ?? "") ??
                                               "").Trim('"');
                        extend_bidding_date = (JsonConvert.SerializeObject(
                                                   tender.SelectToken(
                                                       "notificationInfo.procedureInfo.summarizingDate") ?? "") ??
                                               "").Trim('"');
                        var dop = GetElements(tender, "..tenderPlan2020Info");
                        if (dop.Count > 0)
                        {
                            dop_info = dop[0]?.ToString() ?? "";
                        }
                    }

                    var scoringDateT = scoringDate.ParseDateUn("yyyy-MM-ddzzz");
                    var biddingDateT = biddingDate.ParseDateUn("yyyy-MM-ddzzz");
                    var insertTender =
                        $"INSERT INTO {Program.Prefix}tender SET id_region = @id_region, id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, purchase_object_info = @purchase_object_info, type_fz = @type_fz, id_organizer = @id_organizer, id_placing_way = @id_placing_way, id_etp = @id_etp, end_date = @end_date, scoring_date = @scoring_date, bidding_date = @bidding_date, cancel = @cancel, date_version = @date_version, num_version = @num_version, notice_version = @notice_version, xml = @xml, print_form = @print_form, dop_info = @dop_info, extend_scoring_date = @extend_scoring_date, extend_bidding_date = @extend_bidding_date";
                    var cmd9 = new MySqlCommand(insertTender, connect);
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

                    cmd9.Parameters.AddWithValue("@bidding_date", biddingDateT);
                    cmd9.Parameters.AddWithValue("@cancel", cancelStatus);
                    cmd9.Parameters.AddWithValue("@date_version", dateVersion);
                    cmd9.Parameters.AddWithValue("@num_version", numVersion);
                    cmd9.Parameters.AddWithValue("@notice_version", noticeVersion);
                    cmd9.Parameters.AddWithValue("@xml", xml);
                    cmd9.Parameters.AddWithValue("@print_form", printform);
                    cmd9.Parameters.AddWithValue("@dop_info", dop_info);
                    cmd9.Parameters.AddWithValue("@extend_scoring_date", extend_scoring_date);
                    cmd9.Parameters.AddWithValue("@extend_bidding_date", extend_bidding_date);
                    var resInsertTender = cmd9.ExecuteNonQuery();
                    var idTender = (int) cmd9.LastInsertedId;
                    AddTender504?.Invoke(resInsertTender);
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
                    attachments.AddRange(GetElements(tender, "notificationAttachments.attachment"));
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
                    var lotMaxPrice =
                        ((string) tender.SelectToken("notificationInfo.contractConditionsInfo.maxPriceInfo.maxPrice") ??
                         "").Trim();
                    var lotCurrency =
                        ((string) tender.SelectToken(
                             "notificationInfo.contractConditionsInfo.maxPriceInfo.currency.name") ?? "").Trim();
                    var lotFinanceSource =
                        ((string) tender.SelectToken(
                             "notificationInfo.contractConditionsInfo.maxPriceInfo.financeSource") ?? "").Trim();
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
                    var customerRequirements =
                        GetElements(tender, "notificationInfo.customerRequirementsInfo.customerRequirementInfo");
                    foreach (var customerRequirement in customerRequirements)
                    {
                        var kladrPlace =
                            ((string) customerRequirement.SelectToken(
                                 "contractConditionsInfo.deliveryPlacesInfo.deliveryPlaceInfo.kladr.fullName") ??
                             "").Trim();
                        if (string.IsNullOrEmpty(kladrPlace))
                            kladrPlace =
                                ((string) customerRequirement.SelectToken(
                                     "contractConditionsInfo.deliveryPlacesInfo.deliveryPlaceInfo[0].kladr.fullName") ??
                                 "").Trim();
                        var deliveryPlace =
                            ((string) customerRequirement.SelectToken(
                                 "contractConditionsInfo.deliveryPlacesInfo.deliveryPlaceInfo.deliveryPlace") ?? "")
                            .Trim();
                        if (string.IsNullOrEmpty(deliveryPlace))
                            deliveryPlace =
                                ((string) customerRequirement.SelectToken(
                                     "contractConditionsInfo.deliveryPlacesInfo.deliveryPlaceInfo[0].kladr.fullName[0].deliveryPlace") ??
                                 "").Trim();
                        if (string.IsNullOrEmpty(deliveryPlace))
                        {
                            var deliveryPlace1 =
                                ((string) tender.SelectToken(
                                     "..notificationInfo.contractConditionsInfo.deliveryPlaceInfo.OKTMO.name") ??
                                 "").Trim();
                            var deliveryPlace2 =
                                ((string) tender.SelectToken(
                                     "..notificationInfo.contractConditionsInfo.deliveryPlaceInfo.deliveryPlace") ??
                                 "").Trim();
                            deliveryPlace = $"{deliveryPlace1} {deliveryPlace2}";
                        }

                        var deliveryTerm =
                            ((string) customerRequirement.SelectToken("contractConditionsInfo.deliveryTerm") ?? "")
                            .Trim();
                        var applicationGuaranteeAmount =
                            ((string) customerRequirement.SelectToken("applicationGuarantee.amount") ?? "").Trim();
                        var contractGuaranteeAmount =
                            ((string) customerRequirement.SelectToken("contractGuarantee.amount") ?? "").Trim();
                        var applicationSettlementAccount =
                            ((string) customerRequirement.SelectToken(
                                 "applicationGuarantee.account.settlementAccount") ??
                             "").Trim();
                        var applicationPersonalAccount =
                            ((string) customerRequirement.SelectToken("applicationGuarantee.account.personalAccount") ??
                             "")
                            .Trim();
                        var applicationBik =
                            ((string) customerRequirement.SelectToken("applicationGuarantee.account.bik") ?? "").Trim();
                        var contractSettlementAccount =
                            ((string) customerRequirement.SelectToken("contractGuarantee.account.settlementAccount") ??
                             "")
                            .Trim();
                        var contractPersonalAccount =
                            ((string) customerRequirement.SelectToken("contractGuarantee.account.personalAccount") ??
                             "")
                            .Trim();
                        var contractBik =
                            ((string) customerRequirement.SelectToken("contractGuarantee.account.bik") ?? "").Trim();
                        var customerRegNum = ((string) customerRequirement.SelectToken("customer.regNum") ?? "")
                            .Trim();
                        UpdateRegionId(customerRegNum, idTender, connect);
                        var customerFullName =
                            ((string) customerRequirement.SelectToken("customer.fullName") ?? "").Trim();
                        var customerRequirementMaxPrice =
                            ((string) customerRequirement.SelectToken("contractConditionsInfo.maxPriceInfo.maxPrice") ??
                             "").Trim();
                        var purchaseObjectDescription =
                            ((string) customerRequirement.SelectToken("purchaseObjectDescription") ?? "").Trim();
                        var oneSideRejectionSt95 = ((string) customerRequirement.SelectToken("contractConditionsInfo.oneSideRejectionSt95") ?? "")
                            .Trim();
                        var procedureInfo = ((string) customerRequirement.SelectToken("contractGuarantee.procedureInfo") ?? "")
                            .Trim();
                        if (!string.IsNullOrEmpty(purchaseObjectDescription))
                        {
                            deliveryTerm = $"{deliveryTerm} | {purchaseObjectDescription}".Trim();
                        }
                        if (!string.IsNullOrEmpty(procedureInfo))
                        {
                            deliveryTerm = $"{deliveryTerm} | {procedureInfo}".Trim();
                        }
                        if (!string.IsNullOrEmpty(oneSideRejectionSt95))
                        {
                            deliveryTerm = $"{deliveryTerm} | {oneSideRejectionSt95}".Trim();
                        }

                        if (!string.IsNullOrEmpty(customerRegNum))
                        {
                            var selectCustomer =
                                $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                            var cmd13 = new MySqlCommand(selectCustomer, connect);
                            cmd13.Prepare();
                            cmd13.Parameters.AddWithValue("@reg_num", customerRegNum);
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
                                    if (organizerRegNum == customerRegNum)
                                    {
                                        customerInn = organizerInn;
                                    }
                                }

                                var insertCustomer =
                                    $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, inn = @inn";
                                var cmd14 = new MySqlCommand(insertCustomer, connect);
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
                            if (!string.IsNullOrEmpty(customerFullName))
                            {
                                var selectCustName =
                                    $"SELECT id_customer FROM {Program.Prefix}customer WHERE full_name = @full_name";
                                var cmd15 = new MySqlCommand(selectCustName, connect);
                                cmd15.Prepare();
                                cmd15.Parameters.AddWithValue("@full_name", customerFullName);
                                var reader6 = cmd15.ExecuteReader();
                                if (reader6.HasRows)
                                {
                                    reader6.Read();
                                    idCustomer = reader6.GetInt32("id_customer");
                                    Log.Logger("Получили id_customer по customer_full_name", FilePath);
                                }

                                reader6.Close();
                            }
                        }
                        var planNumber =
                            ((string) customerRequirement.SelectToken("contractConditionsInfo.tenderPlanInfo.plan2017Number") ?? "").Trim();
                        if (string.IsNullOrEmpty(planNumber))
                        {
                            planNumber = ((string) customerRequirement.SelectToken("contractConditionsInfo.tenderPlan2020Info.plan2020Number") ??
                                          "").Trim();
                        }
                        if (string.IsNullOrEmpty(planNumber))
                        {
                            planNumber = ((string) customerRequirement.SelectToken("contractConditionsInfo.tenderPlanInfo.planNumber") ?? "").Trim();
                        }
                        var positionNumber =
                            ((string) customerRequirement.SelectToken("contractConditionsInfo.tenderPlanInfo.position2017Number") ?? "").Trim();
                        if (string.IsNullOrEmpty(positionNumber))
                        {
                            positionNumber =
                                ((string) customerRequirement.SelectToken("contractConditionsInfo.tenderPlan2020Info.plan2020Number") ?? "")
                                .Trim();
                        }
                        if (string.IsNullOrEmpty(positionNumber))
                        {
                            positionNumber = ((string) customerRequirement.SelectToken("contractConditionsInfo.tenderPlanInfo.positionNumber") ?? "").Trim();
                        }
                        var provWarAmount =
                            ((string) customerRequirement.SelectToken("provisionWarranty.amount") ?? "")
                            .Trim();
                        var provWarPart =
                            ((string) customerRequirement.SelectToken("provisionWarranty.part") ?? "")
                            .Trim();
                        var cusReqDopInfo = "{}";
                        if (rootName == "epNotificationEF2020")
                        {
                            provWarAmount =
                                ((string) customerRequirement.SelectToken("contractGuarantee.amount") ?? "")
                                .Trim();
                            provWarPart =
                                ((string) customerRequirement.SelectToken("contractGuarantee.part") ?? "")
                                .Trim();
                        }
                        var OKPD2_code = ((string) customerRequirement.SelectToken("..OKPD2.OKPDCode") ?? "")
                            .Trim();
                        var OKPD2_name = ((string) customerRequirement.SelectToken("..OKPD2.OKPDName") ?? "")
                            .Trim();
                        if (rootName == "epNotificationEOK2020" || rootName == "epNotificationEZK2020")
                        {
                            var dop = GetElements(customerRequirement, "..contractGuarantee");
                            if (dop.Count > 0)
                            {
                                cusReqDopInfo = dop[0]?.ToString() ?? "{}";
                            }
                            dop = GetElements(customerRequirement, "..IKZInfo");
                            if (dop.Count > 0)
                            {
                                cusReqDopInfo = "[" + cusReqDopInfo + "," + (dop[0]?.ToString() ?? "{}") + "]";
                            }
                        }

                        var insertCustomerRequirement =
                            $"INSERT INTO {Program.Prefix}customer_requirement SET id_lot = @id_lot, id_customer = @id_customer, kladr_place = @kladr_place, delivery_place = @delivery_place, delivery_term = @delivery_term, application_guarantee_amount = @application_guarantee_amount, application_settlement_account = @application_settlement_account, application_personal_account = @application_personal_account, application_bik = @application_bik, contract_guarantee_amount = @contract_guarantee_amount, contract_settlement_account = @contract_settlement_account, contract_personal_account = @contract_personal_account, contract_bik = @contract_bik, max_price = @max_price, plan_number = @plan_number, position_number = @position_number, prov_war_amount = @prov_war_amount, prov_war_part = @prov_war_part, dop_info = @dop_info, OKPD2_code = @OKPD2_code, OKPD2_name = @OKPD2_name";
                        var cmd16 = new MySqlCommand(insertCustomerRequirement, connect);
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
                        cmd16.Parameters.AddWithValue("@plan_number", planNumber);
                        cmd16.Parameters.AddWithValue("@position_number", positionNumber);
                        cmd16.Parameters.AddWithValue("@prov_war_amount", provWarAmount);
                        cmd16.Parameters.AddWithValue("@prov_war_part", provWarPart);
                        cmd16.Parameters.AddWithValue("@dop_info", cusReqDopInfo);
                        cmd16.Parameters.AddWithValue("@OKPD2_code", OKPD2_code);
                        cmd16.Parameters.AddWithValue("@OKPD2_name", OKPD2_name);
                        cmd16.ExecuteNonQuery();
                        if (idCustomer == 0)
                        {
                            Log.Logger("Нет id_customer", FilePath);
                        }
                    }

                    var preferenses = GetElements(tender, "notificationInfo.preferensesInfo.preferenseInfo");
                    foreach (var preferense in preferenses)
                    {
                        var preferenseName =
                            ((string) preferense.SelectToken("preferenseRequirementInfo.name") ?? "").Trim();
                        var insertPreference =
                            $"INSERT INTO {Program.Prefix}preferense SET id_lot = @id_lot, name = @name";
                        var cmd17 = new MySqlCommand(insertPreference, connect);
                        cmd17.Prepare();
                        cmd17.Parameters.AddWithValue("@id_lot", idLot);
                        cmd17.Parameters.AddWithValue("@name", preferenseName);
                        cmd17.ExecuteNonQuery();
                    }

                    var requirements =
                        GetElements(tender, "notificationInfo.requirementsInfo.requirementInfo");
                    foreach (var requirement in requirements)
                    {
                        var requirementName =
                            ((string) requirement.SelectToken("preferenseRequirementInfo.name") ?? "").Trim();
                        var requirementContent = ((string) requirement.SelectToken("content") ?? "").Trim();
                        var requirementCode = ((string) requirement.SelectToken("code") ?? "").Trim();
                        var insertRequirement =
                            $"INSERT INTO {Program.Prefix}requirement SET id_lot = @id_lot, name = @name, content = @content, code = @code";
                        var cmd18 = new MySqlCommand(insertRequirement, connect);
                        cmd18.Prepare();
                        cmd18.Parameters.AddWithValue("@id_lot", idLot);
                        cmd18.Parameters.AddWithValue("@name", requirementName);
                        cmd18.Parameters.AddWithValue("@content", requirementContent);
                        cmd18.Parameters.AddWithValue("@code", requirementCode);
                        cmd18.ExecuteNonQuery();
                    }

                    var restricts = GetElements(tender, "notificationInfo.restrictionsInfo.restrictionInfo");
                    foreach (var restrict in restricts)
                    {
                        var rInfo = ((string) restrict.SelectToken("preferenseRequirementInfo.name") ?? "").Trim();
                        var fInfo = ((string) restrict.SelectToken("content") ?? "").Trim();
                        var insertRestrict =
                            $"INSERT INTO {Program.Prefix}restricts SET id_lot = @id_lot, foreign_info = @foreign_info, info = @info";
                        var cmd19 = new MySqlCommand(insertRestrict, connect);
                        cmd19.Prepare();
                        cmd19.Parameters.AddWithValue("@id_lot", idLot);
                        cmd19.Parameters.AddWithValue("@foreign_info", fInfo);
                        cmd19.Parameters.AddWithValue("@info", rInfo);
                        cmd19.ExecuteNonQuery();
                    }

                    var purchaseobjects = GetElements(tender,
                        "notificationInfo.purchaseObjectsInfo.notDrugPurchaseObjectsInfo.purchaseObject");
                    purchaseobjects.AddRange(GetElements(tender,
                        "notificationInfo.purchaseObjectsInfo.purchaseObject"));
                    foreach (var purchaseobject in purchaseobjects)
                    {
                        var okpd2Code = ((string) purchaseobject.SelectToken("OKPD2.OKPDCode") ?? "").Trim();
                        if (string.IsNullOrEmpty(okpd2Code))
                        {
                            okpd2Code = ((string) purchaseobject.SelectToken("KTRU.code") ?? "").Trim().GetDateFromRegex(@"(.+?)-");
                        }
                            
                        var okpdCode = ((string) purchaseobject.SelectToken("OKPD.code") ?? "").Trim();
                        var okpdName = ((string) purchaseobject.SelectToken("OKPD2.OKPDName") ?? "").Trim();
                        if (string.IsNullOrEmpty(okpdName))
                            okpdName = ((string) purchaseobject.SelectToken("OKPD.name") ?? "").Trim();
                        if (string.IsNullOrEmpty(okpdName))
                            okpdName = ((string) purchaseobject.SelectToken("KTRU.name") ?? "").Trim();
                        var name = ((string) purchaseobject.SelectToken("name") ?? "").Trim();
                        if (!string.IsNullOrEmpty(name))
                            name = Regex.Replace(name, @"\s+", " ");
                        var quantityValue = ((string) purchaseobject.SelectToken("quantity.value") ?? "")
                            .Trim();
                        var price = ((string) purchaseobject.SelectToken("price") ?? "").Trim();
                        price = price.Replace(",", ".");
                        var okei = ((string) purchaseobject.SelectToken("OKEI.nationalCode") ?? "").Trim();
                        var sumP = ((string) purchaseobject.SelectToken("sum") ?? "").Trim();
                        sumP = sumP.Replace(",", ".");
                        var okpd2GroupCode = 0;
                        var okpd2GroupLevel1Code = "";
                        if (!string.IsNullOrEmpty(okpd2Code))
                        {
                            GetOkpd(okpd2Code, out okpd2GroupCode, out okpd2GroupLevel1Code);
                        }
                        var dop_info_pur_obj = purchaseobject.ToString();
                        var customerquantities =
                            GetElements(purchaseobject, "customerQuantities.customerQuantity");
                        foreach (var customerquantity in customerquantities)
                        {
                            var customerQuantityValue =
                                ((string) customerquantity.SelectToken("quantity") ?? "").Trim();
                            var custRegNum = ((string) customerquantity.SelectToken("customer.regNum") ?? "")
                                .Trim();
                            UpdateRegionId(custRegNum, idTender, connect);
                            var custFullName =
                                ((string) customerquantity.SelectToken("customer.fullName") ?? "").Trim();
                            var idCustomerQ = 0;
                            if (!string.IsNullOrEmpty(custRegNum))
                            {
                                var selectCustomerQ =
                                    $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                                var cmd20 = new MySqlCommand(selectCustomerQ, connect);
                                cmd20.Prepare();
                                cmd20.Parameters.AddWithValue("@reg_num", custRegNum);
                                var reader7 = cmd20.ExecuteReader();
                                if (reader7.HasRows)
                                {
                                    reader7.Read();
                                    idCustomerQ = reader7.GetInt32("id_customer");
                                    reader7.Close();
                                }
                                else
                                {
                                    reader7.Close();
                                    var insertCustomerQ =
                                        $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name";
                                    var cmd21 = new MySqlCommand(insertCustomerQ, connect);
                                    cmd21.Prepare();
                                    cmd21.Parameters.AddWithValue("@reg_num", custRegNum);
                                    cmd21.Parameters.AddWithValue("@full_name", custFullName);
                                    cmd21.ExecuteNonQuery();
                                    idCustomerQ = (int) cmd21.LastInsertedId;
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(custFullName))
                                {
                                    var selectCustNameQ =
                                        $"SELECT id_customer FROM {Program.Prefix}customer WHERE full_name = @full_name";
                                    var cmd22 = new MySqlCommand(selectCustNameQ, connect);
                                    cmd22.Prepare();
                                    cmd22.Parameters.AddWithValue("@full_name", custFullName);
                                    var reader8 = cmd22.ExecuteReader();
                                    if (reader8.HasRows)
                                    {
                                        reader8.Read();
                                        idCustomerQ = reader8.GetInt32("id_customer");
                                        Log.Logger("Получили id_customer_q по customer_full_name", FilePath);
                                    }

                                    reader8.Close();
                                }
                            }

                            var insertCustomerquantity =
                                $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, okpd2_group_code = @okpd2_group_code, okpd2_group_level1_code = @okpd2_group_level1_code, okpd_code = @okpd_code, okpd_name = @okpd_name, name = @name, quantity_value = @quantity_value, price = @price, okei = @okei, sum = @sum, customer_quantity_value = @customer_quantity_value, dop_info = @dop_info";
                            var cmd23 = new MySqlCommand(insertCustomerquantity, connect);
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
                            cmd23.Parameters.AddWithValue("@dop_info", dop_info_pur_obj);
                            cmd23.ExecuteNonQuery();
                            PoExist = true;
                            if (idCustomerQ == 0)
                                Log.Logger("Нет id_customer_q", FilePath);
                        }

                        if (customerquantities.Count == 0)
                        {
                            var insertCustomerquantity =
                                $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, okpd2_group_code = @okpd2_group_code, okpd2_group_level1_code = @okpd2_group_level1_code, okpd_code = @okpd_code, okpd_name = @okpd_name, name = @name, quantity_value = @quantity_value, price = @price, okei = @okei, sum = @sum, customer_quantity_value = @customer_quantity_value, dop_info = @dop_info";
                            var cmd24 = new MySqlCommand(insertCustomerquantity, connect);
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
                            cmd24.Parameters.AddWithValue("@dop_info", dop_info_pur_obj);
                            cmd24.ExecuteNonQuery();
                            PoExist = true;
                        }
                    }

                    var drugPurchaseObjectsInfo = GetElements(tender,
                        "notificationInfo.purchaseObjectsInfo.drugPurchaseObjectsInfo.drugPurchaseObjectInfo");
                    foreach (var drugPurchaseObjectInfo in drugPurchaseObjectsInfo)
                    {
                        pils = true;
                        var isZnvlp = ((string) drugPurchaseObjectInfo.SelectToken("isZNVLP") ?? "").Trim();
                        var drugQuantityCustomersInfo =
                            GetElements(drugPurchaseObjectInfo, "drugQuantityCustomersInfo.drugQuantityCustomerInfo");
                        foreach (var drugQuantityCustomerInfo in drugQuantityCustomersInfo)
                        {
                            var customerQuantityValue =
                                ((string) drugQuantityCustomerInfo.SelectToken("quantity") ?? "").Trim();
                            var custRegNum = ((string) drugQuantityCustomerInfo.SelectToken("customer.regNum") ?? "")
                                .Trim();
                            UpdateRegionId(custRegNum, idTender, connect);
                            var custFullName =
                                ((string) drugQuantityCustomerInfo.SelectToken("customer.fullName") ?? "").Trim();
                            var idCustomerQ = 0;
                            if (!string.IsNullOrEmpty(custRegNum))
                            {
                                var selectCustomerQ =
                                    $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                                var cmd20 = new MySqlCommand(selectCustomerQ, connect);
                                cmd20.Prepare();
                                cmd20.Parameters.AddWithValue("@reg_num", custRegNum);
                                var reader7 = cmd20.ExecuteReader();
                                if (reader7.HasRows)
                                {
                                    reader7.Read();
                                    idCustomerQ = reader7.GetInt32("id_customer");
                                    reader7.Close();
                                }
                                else
                                {
                                    reader7.Close();
                                    var insertCustomerQ =
                                        $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name";
                                    var cmd21 = new MySqlCommand(insertCustomerQ, connect);
                                    cmd21.Prepare();
                                    cmd21.Parameters.AddWithValue("@reg_num", custRegNum);
                                    cmd21.Parameters.AddWithValue("@full_name", custFullName);
                                    cmd21.ExecuteNonQuery();
                                    idCustomerQ = (int) cmd21.LastInsertedId;
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(custFullName))
                                {
                                    var selectCustNameQ =
                                        $"SELECT id_customer FROM {Program.Prefix}customer WHERE full_name = @full_name";
                                    var cmd22 = new MySqlCommand(selectCustNameQ, connect);
                                    cmd22.Prepare();
                                    cmd22.Parameters.AddWithValue("@full_name", custFullName);
                                    var reader8 = cmd22.ExecuteReader();
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
                                var okpd2Code =
                                    ((string) drugInfo.SelectToken("MNNInfo.MNNExternalCode") ?? "").Trim();
                                var name = ((string) drugInfo.SelectToken("MNNInfo.MNNName") ?? "").Trim();
                                var medicamentalFormName =
                                    ((string) drugInfo.SelectToken("medicamentalFormInfo.medicamentalFormName") ?? "")
                                    .Trim();
                                name = $"{name} | {medicamentalFormName}";

                                var dosageGrlsValue =
                                    ((string) drugInfo.SelectToken("dosageInfo.dosageGRLSValue") ?? "").Trim();
                                name = $"{name} | {dosageGrlsValue}";
                                name = $"{name} | {isZnvlp}";

                                if (!string.IsNullOrEmpty(name))
                                    name = Regex.Replace(name, @"\s+", " ");
                                var quantityValue = ((string) drugInfo.SelectToken("drugQuantity") ?? "")
                                    .Trim();
                                var okei = ((string) drugInfo.SelectToken("dosageInfo.dosageUserOKEI.name") ?? "")
                                    .Trim();
                                if (okei == "")
                                {
                                    okei = ((string) drugInfo.SelectToken("manualUserOKEI.name") ?? "").Trim();
                                }

                                var price =
                                    ((string) drugPurchaseObjectInfo.SelectToken("pricePerUnit") ?? "").Trim();
                                price = price.Replace(",", ".");
                                var sumP =
                                    ((string) drugPurchaseObjectInfo.SelectToken("..positionPrice") ?? "").Trim();
                                sumP = sumP.Replace(",", ".");
                                var insertCustomerquantity =
                                    $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, name = @name, quantity_value = @quantity_value, price = @price, okei = @okei, sum = @sum, customer_quantity_value = @customer_quantity_value";
                                var cmd23 = new MySqlCommand(insertCustomerquantity, connect);
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
                                PoExist = true;
                                if (idCustomerQ == 0)
                                    Log.Logger("Нет id_customer_q", FilePath);
                            }

                            var drugsInfoRef = GetElements(drugPurchaseObjectInfo,
                                "objectInfoUsingReferenceInfo.drugsInfo.drugInfo");
                            drugsInfoRef.AddRange(GetElements(drugPurchaseObjectInfo,
                                "objectInfoUsingReferenceInfo.drugsInfo.drugInterchangeInfo.drugInterchangeManualInfo.drugInfo"));
                            drugsInfoRef.AddRange(GetElements(drugPurchaseObjectInfo,
                                "objectInfoUsingReferenceInfo.drugsInfo.drugInterchangeInfo.drugInterchangeReferenceInfo.drugInfo"));
                            foreach (var drugInfo in drugsInfoRef)
                            {
                                var okpd2Code =
                                    ((string) drugInfo.SelectToken("..MNNInfo.MNNExternalCode") ?? "").Trim();
                                var name = ((string) drugInfo.SelectToken("..MNNInfo.MNNName") ?? "").Trim();
                                var medicamentalFormName =
                                    ((string) drugInfo.SelectToken("..medicamentalFormInfo.medicamentalFormName") ?? "")
                                    .Trim();
                                name = $"{name} | {medicamentalFormName}";

                                var dosageGrlsValue =
                                    ((string) drugInfo.SelectToken("..dosageInfo.dosageGRLSValue") ?? "").Trim();
                                name = $"{name} | {dosageGrlsValue}";
                                name = $"{name} | {isZnvlp}";

                                if (!string.IsNullOrEmpty(name))
                                    name = Regex.Replace(name, @"\s+", " ");
                                var quantityValue = ((string) drugInfo.SelectToken("..drugQuantity") ?? "")
                                    .Trim();
                                var okei = ((string) drugInfo.SelectToken("..dosageInfo.dosageUserOKEI.name") ?? "")
                                    .Trim();
                                if (okei == "")
                                {
                                    okei = ((string) drugInfo.SelectToken("..manualUserOKEI.name") ?? "").Trim();
                                }

                                var price =
                                    ((string) drugPurchaseObjectInfo.SelectToken("..pricePerUnit") ?? "").Trim();
                                price = price.Replace(",", ".");
                                var sumP =
                                    ((string) drugPurchaseObjectInfo.SelectToken("..positionPrice") ?? "").Trim();
                                sumP = sumP.Replace(",", ".");
                                var insertCustomerquantity =
                                    $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, name = @name, quantity_value = @quantity_value, price = @price, okei = @okei, sum = @sum, customer_quantity_value = @customer_quantity_value";
                                var cmd23 = new MySqlCommand(insertCustomerquantity, connect);
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
                                PoExist = true;
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
                                var okpd2Code =
                                    ((string) drugInfo.SelectToken("MNNInfo.MNNExternalCode") ?? "").Trim();
                                var name = ((string) drugInfo.SelectToken("MNNInfo.MNNName") ?? "").Trim();
                                var medicamentalFormName =
                                    ((string) drugInfo.SelectToken("medicamentalFormInfo.medicamentalFormName") ?? "")
                                    .Trim();
                                name = $"{name} | {medicamentalFormName}";

                                var dosageGrlsValue =
                                    ((string) drugInfo.SelectToken("dosageInfo.dosageGRLSValue") ?? "").Trim();
                                name = $"{name} | {dosageGrlsValue}";
                                name = $"{name} | {isZnvlp}";

                                if (!string.IsNullOrEmpty(name))
                                    name = Regex.Replace(name, @"\s+", " ");
                                var quantityValue = ((string) drugInfo.SelectToken("drugQuantity") ?? "")
                                    .Trim();
                                var okei = ((string) drugInfo.SelectToken("dosageInfo.dosageUserOKEI.name") ?? "")
                                    .Trim();
                                if (okei == "")
                                {
                                    okei = ((string) drugInfo.SelectToken("manualUserOKEI.name") ?? "").Trim();
                                }

                                var price =
                                    ((string) drugPurchaseObjectInfo.SelectToken("pricePerUnit") ?? "").Trim();
                                price = price.Replace(",", ".");
                                var sumP =
                                    ((string) drugPurchaseObjectInfo.SelectToken("positionPrice") ?? "").Trim();
                                sumP = sumP.Replace(",", ".");
                                var insertCustomerquantity =
                                    $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, name = @name, quantity_value = @quantity_value, price = @price, okei = @okei, sum = @sum, customer_quantity_value = @customer_quantity_value";
                                var cmd23 = new MySqlCommand(insertCustomerquantity, connect);
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
                                PoExist = true;
                                if (idCustomer == 0)
                                    Log.Logger("Нет id_customer", FilePath);
                            }

                            var drugsInfoText = GetElements(drugPurchaseObjectInfo,
                                "objectInfoUsingTextForm.drugsInfo.drugInfo");
                            foreach (var drugInfo in drugsInfoText)
                            {
                                var okpd2Code =
                                    ((string) drugInfo.SelectToken("MNNInfo.MNNExternalCode") ?? "").Trim();
                                var name = ((string) drugInfo.SelectToken("MNNInfo.MNNName") ?? "").Trim();
                                var medicamentalFormName =
                                    ((string) drugInfo.SelectToken("medicamentalFormInfo.medicamentalFormName") ?? "")
                                    .Trim();
                                name = $"{name} | {medicamentalFormName}";

                                var dosageGrlsValue =
                                    ((string) drugInfo.SelectToken("dosageInfo.dosageGRLSValue") ?? "").Trim();
                                name = $"{name} | {dosageGrlsValue}";
                                name = $"{name} | {isZnvlp}";

                                if (!string.IsNullOrEmpty(name))
                                    name = Regex.Replace(name, @"\s+", " ");
                                var quantityValue = ((string) drugInfo.SelectToken("drugQuantity") ?? "")
                                    .Trim();
                                var okei = ((string) drugInfo.SelectToken("dosageInfo.dosageUserOKEI.name") ?? "")
                                    .Trim();
                                if (okei == "")
                                {
                                    okei = ((string) drugInfo.SelectToken("manualUserOKEI.name") ?? "").Trim();
                                }

                                var price =
                                    ((string) drugPurchaseObjectInfo.SelectToken("pricePerUnit") ?? "").Trim();
                                price = price.Replace(",", ".");
                                var sumP =
                                    ((string) drugPurchaseObjectInfo.SelectToken("positionPrice") ?? "").Trim();
                                sumP = sumP.Replace(",", ".");
                                var insertCustomerquantity =
                                    $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, name = @name, quantity_value = @quantity_value, price = @price, okei = @okei, sum = @sum, customer_quantity_value = @customer_quantity_value";
                                var cmd23 = new MySqlCommand(insertCustomerquantity, connect);
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
                                PoExist = true;
                                if (idCustomer == 0)
                                    Log.Logger("Нет id_customer", FilePath);
                            }
                        }
                    }

                    if (!PoExist)
                    {
                        //Log.Logger("cannot find purchase objects in ", FilePath);
                    }
                    /*if(pils){
                        string updateTender =
                            $"UPDATE {Program.Prefix}tender SET is_medicine = @is_medicine WHERE id_tender = @id_tender";
                        MySqlCommand cmd5 = new MySqlCommand(updateTender, connect);
                        cmd5.Prepare();
                        cmd5.Parameters.AddWithValue("@id_tender", idTender);
                        cmd5.Parameters.AddWithValue("@is_medicine", 1);
                        cmd5.ExecuteNonQuery();
                    }*/
                    TenderKwords(connect, idTender, pils);
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
        
        private void UpdateRegionId(string cusRegNum, int idTender, MySqlConnection connect)
        {
            if (string.IsNullOrEmpty(cusRegNum))
                return;
            if(isRegionExsist)
                return;
            var selectCustomerRegion =
                $"SELECT region FROM nsi_eis_organizations_44 WHERE eis_regNumber = @eis_regNumber";
            var cmd = new MySqlCommand(selectCustomerRegion, connect);
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@eis_regNumber", cusRegNum);
            var reader = cmd.ExecuteReader();
            var idRegion = 0;
            if (reader.HasRows)
            {
                reader.Read();
                idRegion = reader.GetInt32("region");
                reader.Close();
            }
            else
            {
                reader.Close();
                return;
            }

            if (idRegion != 0)
            {
                var updateTender =
                    $"UPDATE tender SET id_region = @id_region WHERE id_tender = @id_tender";
                var cmd1 = new MySqlCommand(updateTender, connect);
                cmd1.Prepare();
                cmd1.Parameters.AddWithValue("@id_tender", idTender);
                cmd1.Parameters.AddWithValue("@id_region", idRegion);
                var resT = cmd1.ExecuteNonQuery();
                if (resT == 1)
                    isRegionExsist = true;
            }
            
        }
    }
}