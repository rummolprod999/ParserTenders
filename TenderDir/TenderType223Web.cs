using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderType223Web : TenderWeb
    {
        public event Action<int> AddTender223;
        private bool Up = default;
        private TypeFile223 _purchase;
        private string _extendScoringDate = "";
        private string _extendBiddingDate = "";

        public TenderType223Web(string url, JObject json, TypeFile223 p)
            : base(json, url)
        {
            _purchase = p;
            AddTender223 += delegate(int d)
            {
                if (d > 0 && !Up)
                    Program.AddTender223++;
                else if (d > 0 && Up)
                    Program.UpdateTender223++;
                else
                    Log.Logger("Не удалось добавить Tender223", FilePath);
            };
        }

        public override void Parsing()
        {
            JProperty tend = null;
            var xml = GetXml();
            var firstOrDefault = T.Properties()
                .FirstOrDefault(p => p.Name.StartsWith("purchase", StringComparison.Ordinal));
            var firstOrDefault2 = ((JObject) firstOrDefault?.Value)?.Properties()
                .FirstOrDefault(p => p.Name.StartsWith("body", StringComparison.Ordinal));
            var firstOrDefault3 = ((JObject) firstOrDefault2?.Value)?.Properties()
                .FirstOrDefault(p => p.Name.StartsWith("item", StringComparison.Ordinal));
            if (firstOrDefault3 != null)
            {
                tend = ((JObject) firstOrDefault3.Value).Properties()
                    .FirstOrDefault(p => p.Name.StartsWith("purchase", StringComparison.Ordinal));
            }

            if (tend != null)
            {
                var tender = tend.Value;
                var idT = ((string) tender.SelectToken("guid") ?? "").Trim();
                if (String.IsNullOrEmpty(idT))
                {
                    Log.Logger("У тендера нет id", FilePath);
                    return;
                }

                var purchaseNumber = ((string) tender.SelectToken("registrationNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("У тендера нет purchaseNumber", FilePath);
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
                        (JsonConvert.SerializeObject(tender.SelectToken("publicationDateTime") ?? "") ??
                         "").Trim('"');
                    var dateVersion = (JsonConvert.SerializeObject(tender.SelectToken("modificationDate") ?? "") ??
                                       "").Trim('"');
                    if (String.IsNullOrEmpty(dateVersion))
                    {
                        dateVersion = docPublishDate;
                    }

                    /*string utc_offset = "";
                    try
                    {
                        utc_offset = docPublishDate.Substring(23);
                    }
                    catch (Exception e)
                    {
                        Log.Logger("Ошибка при получении часового пояса", e, docPublishDate);
                    }*/
                    var cancelStatus = 0;
                    if (!String.IsNullOrEmpty(dateVersion))
                    {
                        var selectDateT =
                            $"SELECT id_tender, date_version FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number";
                        var cmd2 = new MySqlCommand(selectDateT, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        var dt = new DataTable();
                        var adapter = new MySqlDataAdapter {SelectCommand = cmd2};
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            Up = true;
                            foreach (DataRow row in dt.Rows)
                            {
                                var dateNew = DateTime.Parse(dateVersion);
                                var dateOld = (DateTime) row["date_version"];
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

                    var href = ((string) tender.SelectToken("urlVSRZ") ?? "").Trim();
                    if (string.IsNullOrEmpty(href))
                    {
                        href =
                            $"https://zakupki.gov.ru/223/purchase/public/purchase/info/common-info.html?regNumber={purchaseNumber}";
                    }

                    var purchaseObjectInfo = ((string) tender.SelectToken("name") ?? "").Trim();

                    var numVersion = ((string) tender.SelectToken("version") ?? "").Trim();
                    var noticeVersion = ((string) tender.SelectToken("modificationDescription") ?? "").Trim();
                    var printform = ((string) tender.SelectToken("urlOOS") ?? "").Trim();
                    if (!String.IsNullOrEmpty(printform) && printform.IndexOf("CDATA") != -1)
                        printform = printform.Substring(9, printform.Length - 12);
                    if (!href.Contains("zakupki.gov.ru") && string.IsNullOrEmpty(printform))
                    {
                        printform =
                            $"https://zakupki.gov.ru/223/purchase/public/purchase/info/common-info.html?regNumber={purchaseNumber}";
                    }
                    if (String.IsNullOrEmpty(printform))
                    {
                        printform = xml;

                    }
                    var organizerFullName = ((string) tender.SelectToken("placer.mainInfo.fullName") ?? "").Trim();
                    var organizerPostAddress = ((string) tender.SelectToken("placer.mainInfo.postalAddress") ?? "")
                        .Trim();
                    var organizerFactAddress = ((string) tender.SelectToken("placer.mainInfo.legalAddress") ?? "")
                        .Trim();
                    var customerPostAddress =
                        ((string) tender.SelectToken("customer.mainInfo.postalAddress") ?? "").Trim();
                    var customerLegalAddress =
                        ((string) tender.SelectToken("customer.mainInfo.legalAddress") ?? "").Trim();
                    var addr = GetRegionString(customerLegalAddress) != "" ? customerLegalAddress :
                        GetRegionString(customerPostAddress) != "" ? customerPostAddress :
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

                    var organizerInn = ((string) tender.SelectToken("placer.mainInfo.inn") ?? "").Trim();
                    var organizerKpp = ((string) tender.SelectToken("placer.mainInfo.kpp") ?? "").Trim();
                    var organizerEmail = ((string) tender.SelectToken("placer.mainInfo.email") ?? "").Trim();
                    var organizerPhone = ((string) tender.SelectToken("placer.mainInfo.phone") ?? "").Trim();
                    var organizerFax = ((string) tender.SelectToken("placer.mainInfo.fax") ?? "").Trim();
                    var idOrganizer = 0;
                    if (!String.IsNullOrEmpty(organizerInn))
                    {
                        var selectOrg =
                            $"SELECT id_organizer FROM {Program.Prefix}organizer WHERE inn = @inn AND kpp = @kpp";
                        var cmd2 = new MySqlCommand(selectOrg, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@inn", organizerInn);
                        cmd2.Parameters.AddWithValue("@kpp", organizerKpp);
                        var reader1 = cmd2.ExecuteReader();
                        if (reader1.HasRows)
                        {
                            reader1.Read();
                            idOrganizer = reader1.GetInt32("id_organizer");
                            reader1.Close();
                        }
                        else
                        {
                            reader1.Close();
                            var addOrganizer =
                                $"INSERT INTO {Program.Prefix}organizer SET full_name = @full_name, post_address = @post_address, fact_address = @fact_address, inn = @inn, kpp = @kpp, contact_email = @contact_email, contact_phone = @contact_phone, contact_fax = @contact_fax";
                            var cmd3 = new MySqlCommand(addOrganizer, connect);
                            cmd3.Prepare();
                            cmd3.Parameters.AddWithValue("@full_name", organizerFullName);
                            cmd3.Parameters.AddWithValue("@post_address", organizerPostAddress);
                            cmd3.Parameters.AddWithValue("@fact_address", organizerFactAddress);
                            cmd3.Parameters.AddWithValue("@inn", organizerInn);
                            cmd3.Parameters.AddWithValue("@kpp", organizerKpp);
                            cmd3.Parameters.AddWithValue("@contact_email", organizerEmail);
                            cmd3.Parameters.AddWithValue("@contact_phone", organizerPhone);
                            cmd3.Parameters.AddWithValue("@contact_fax", organizerFax);
                            cmd3.ExecuteNonQuery();
                            idOrganizer = (int) cmd3.LastInsertedId;
                        }
                    }
                    else
                    {
                        Log.Logger("Нет organizer_inn", FilePath);
                    }

                    var idPlacingWay = 0;
                    var placingWayCode = ((string) tender.SelectToken("purchaseMethodCode") ?? "").Trim();
                    var placingWayName = ((string) tender.SelectToken("purchaseCodeName") ?? "").Trim();
                    var conformity = GetConformity(placingWayName);
                    if (!String.IsNullOrEmpty(placingWayCode))
                    {
                        var selectPlacingWay =
                            $"SELECT id_placing_way FROM {Program.Prefix}placing_way WHERE code = @code";
                        var cmd4 = new MySqlCommand(selectPlacingWay, connect);
                        cmd4.Prepare();
                        cmd4.Parameters.AddWithValue("@code", placingWayCode);
                        var reader2 = cmd4.ExecuteReader();
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            idPlacingWay = reader2.GetInt32("id_placing_way");
                            reader2.Close();
                        }
                        else
                        {
                            reader2.Close();
                            var insertPlacingWay =
                                $"INSERT INTO {Program.Prefix}placing_way SET code= @code, name= @name, conformity = @conformity";
                            var cmd5 = new MySqlCommand(insertPlacingWay, connect);
                            cmd5.Prepare();
                            cmd5.Parameters.AddWithValue("@code", placingWayCode);
                            cmd5.Parameters.AddWithValue("@name", placingWayName);
                            cmd5.Parameters.AddWithValue("@conformity", conformity);
                            cmd5.ExecuteNonQuery();
                            idPlacingWay = (int) cmd5.LastInsertedId;
                        }
                    }

                    var idEtp = 0;
                    var etpCode =
                        ((string) tender.SelectToken("electronicPlaceInfo.electronicPlaceId") ?? "").Trim();
                    var etpName = ((string) tender.SelectToken("electronicPlaceInfo.name") ?? "").Trim();
                    var etpUrl = ((string) tender.SelectToken("electronicPlaceInfo.url") ?? "").Trim();
                    if (!String.IsNullOrEmpty(etpCode))
                    {
                        var selectEtp = $"SELECT id_etp FROM {Program.Prefix}etp WHERE code = @code";
                        var cmd6 = new MySqlCommand(selectEtp, connect);
                        cmd6.Prepare();
                        cmd6.Parameters.AddWithValue("@code", etpCode);
                        var reader3 = cmd6.ExecuteReader();
                        if (reader3.HasRows)
                        {
                            reader3.Read();
                            idEtp = reader3.GetInt32("id_etp");
                            reader3.Close();
                        }
                        else
                        {
                            reader3.Close();
                            var insertEtp =
                                $"INSERT INTO {Program.Prefix}etp SET code= @code, name= @name, url= @url, conf=0";
                            var cmd7 = new MySqlCommand(insertEtp, connect);
                            cmd7.Prepare();
                            cmd7.Parameters.AddWithValue("@code", etpCode);
                            cmd7.Parameters.AddWithValue("@name", etpName);
                            cmd7.Parameters.AddWithValue("@url", etpUrl);
                            cmd7.ExecuteNonQuery();
                            idEtp = (int) cmd7.LastInsertedId;
                        }
                    }

                    var endDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("submissionCloseDateTime") ?? "") ??
                         "").Trim('"');
                    var scoringDateNew = (JsonConvert.SerializeObject(tender.SelectToken("$..extendField[?(@.description == 'Дата и время окончания срока подачи ценовых предложений')].value.dateTime") ?? "") ??
                                          "").Trim('"');
                    var scoringDate = GetScogingDate(tender);
                    var biddingDate = GetBiddingDate(tender);
                    if (_purchase == TypeFile223.PurchaseNotice)
                    {
                        if (String.IsNullOrEmpty(biddingDate) && !String.IsNullOrEmpty(scoringDate))
                        {
                            biddingDate = scoringDate;
                        }
                    }
                    else if (_purchase == TypeFile223.PurchaseNoticeOk)
                    {
                        scoringDate = (JsonConvert.SerializeObject(tender.SelectToken("envelopeOpeningTime") ?? "") ??
                                       "").Trim('"');
                        biddingDate = (JsonConvert.SerializeObject(tender.SelectToken("examinationDateTime") ?? "") ??
                                       "").Trim('"');
                    }
                    else if (_purchase == TypeFile223.PurchaseNoticeZk)
                    {
                        scoringDate = biddingDate =
                            (JsonConvert.SerializeObject(tender.SelectToken("quotationExaminationTime") ?? "") ??
                             "").Trim('"');
                    }
                    if (!String.IsNullOrEmpty(scoringDateNew))
                    {
                        scoringDate = scoringDateNew;
                    }
                    _extendScoringDate = tender.SelectToken("extendFields")?.ToString() ?? "";
                    _extendScoringDate = Regex.Replace(_extendScoringDate, @"\s+", "").Trim();
                    var insertTender =
                        $"INSERT INTO {Program.Prefix}tender SET id_region = @id_region, id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, purchase_object_info = @purchase_object_info, type_fz = @type_fz, id_organizer = @id_organizer, id_placing_way = @id_placing_way, id_etp = @id_etp, end_date = @end_date, scoring_date = @scoring_date, bidding_date = @bidding_date, cancel = @cancel, date_version = @date_version, num_version = @num_version, notice_version = @notice_version, xml = @xml, print_form = @print_form, extend_scoring_date = @extend_scoring_date, extend_bidding_date = @extend_bidding_date";
                    var cmd8 = new MySqlCommand(insertTender, connect);
                    cmd8.Prepare();
                    cmd8.Parameters.AddWithValue("@id_region", RegionId);
                    cmd8.Parameters.AddWithValue("@id_xml", idT);
                    cmd8.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    cmd8.Parameters.AddWithValue("@doc_publish_date", docPublishDate);
                    cmd8.Parameters.AddWithValue("@href", href);
                    cmd8.Parameters.AddWithValue("@purchase_object_info", purchaseObjectInfo);
                    cmd8.Parameters.AddWithValue("@type_fz", 223);
                    cmd8.Parameters.AddWithValue("@id_organizer", idOrganizer);
                    cmd8.Parameters.AddWithValue("@id_placing_way", idPlacingWay);
                    cmd8.Parameters.AddWithValue("@id_etp", idEtp);
                    cmd8.Parameters.AddWithValue("@end_date", endDate);
                    cmd8.Parameters.AddWithValue("@scoring_date", scoringDate);
                    cmd8.Parameters.AddWithValue("@bidding_date", biddingDate);
                    cmd8.Parameters.AddWithValue("@cancel", cancelStatus);
                    cmd8.Parameters.AddWithValue("@date_version", dateVersion);
                    cmd8.Parameters.AddWithValue("@num_version", numVersion);
                    cmd8.Parameters.AddWithValue("@notice_version", noticeVersion);
                    cmd8.Parameters.AddWithValue("@xml", xml);
                    cmd8.Parameters.AddWithValue("@print_form", printform);
                    cmd8.Parameters.AddWithValue("@extend_scoring_date", _extendScoringDate);
                    cmd8.Parameters.AddWithValue("@extend_bidding_date", _extendBiddingDate);
                    var resInsertTender = cmd8.ExecuteNonQuery();
                    var idTender = (int) cmd8.LastInsertedId;
                    AddTender223?.Invoke(resInsertTender);
                    var attachments = GetElements(tender, "attachments.document");
                    foreach (var att in attachments)
                    {
                        var attachName = ((string) att.SelectToken("fileName") ?? "").Trim();
                        var attachDescription = ((string) att.SelectToken("description") ?? "").Trim();
                        var attachUrl = ((string) att.SelectToken("url") ?? "").Trim();
                        var insertAttach =
                            $"INSERT INTO {Program.Prefix}attachment SET id_tender = @id_tender, file_name = @file_name, url = @url, description = @description";
                        var cmd9 = new MySqlCommand(insertAttach, connect);
                        cmd9.Prepare();
                        cmd9.Parameters.AddWithValue("@id_tender", idTender);
                        cmd9.Parameters.AddWithValue("@file_name", attachName);
                        cmd9.Parameters.AddWithValue("@url", attachUrl);
                        cmd9.Parameters.AddWithValue("@description", attachDescription);
                        cmd9.ExecuteNonQuery();
                    }

                    var customerInn = ((string) tender.SelectToken("customer.mainInfo.inn") ?? "").Trim();
                    UpdateRegionId(customerInn, idTender, connect);
                    var customerFullName = ((string) tender.SelectToken("customer.mainInfo.fullName") ?? "")
                        .Trim();
                    var customerKpp = ((string) tender.SelectToken("customer.mainInfo.kpp") ?? "").Trim();
                    var customerOgrn = ((string) tender.SelectToken("customer.mainInfo.ogrn") ?? "").Trim();
                    var customerPhone = ((string) tender.SelectToken("customer.mainInfo.phone") ?? "").Trim();
                    var customerFax = ((string) tender.SelectToken("customer.mainInfo.fax") ?? "").Trim();
                    var customerEmail = ((string) tender.SelectToken("customer.mainInfo.email") ?? "").Trim();
                    var cusLn = ((string) tender.SelectToken("contact.lastName") ?? "").Trim();
                    var cusFn = ((string) tender.SelectToken("contact.firstName") ?? "").Trim();
                    var cusMn = ((string) tender.SelectToken("contact.middleName") ?? "").Trim();
                    var cusContact = $"{cusLn} {cusFn} {cusMn}".Trim();
                    var idCustomer = 0;
                    var customerRegNumber = "";
                    if (!String.IsNullOrEmpty(customerInn))
                    {
                        var selectOdCustomer =
                            $"SELECT regNumber FROM od_customer WHERE inn = @inn AND kpp = @kpp AND regNumber IS NOT NULL";
                        var cmd10 = new MySqlCommand(selectOdCustomer, connect);
                        cmd10.Prepare();
                        cmd10.Parameters.AddWithValue("@inn", customerInn);
                        cmd10.Parameters.AddWithValue("@kpp", customerKpp);
                        var reader4 = cmd10.ExecuteReader();
                        if (reader4.HasRows)
                        {
                            reader4.Read();
                            customerRegNumber = (string) reader4["regNumber"];
                        }

                        reader4.Close();
                        if (String.IsNullOrEmpty(customerRegNumber))
                        {
                            var selectOdCustomerFromFtp =
                                $"SELECT regNumber FROM od_customer_from_ftp WHERE inn = @inn AND kpp = @kpp AND regNumber IS NOT NULL";
                            var cmd11 = new MySqlCommand(selectOdCustomerFromFtp, connect);
                            cmd11.Prepare();
                            cmd11.Parameters.AddWithValue("@inn", customerInn);
                            cmd11.Parameters.AddWithValue("@kpp", customerKpp);
                            var reader5 = cmd11.ExecuteReader();
                            if (reader5.HasRows)
                            {
                                reader5.Read();
                                customerRegNumber = (string) reader5["regNumber"];
                            }

                            reader5.Close();
                        }

                        if (String.IsNullOrEmpty(customerRegNumber))
                        {
                            var selectOdCustomerFromFtp223 =
                                $"SELECT regNumber FROM od_customer_from_ftp223 WHERE inn = @inn AND kpp = @kpp AND regNumber IS NOT NULL";
                            var cmd12 = new MySqlCommand(selectOdCustomerFromFtp223, connect);
                            cmd12.Prepare();
                            cmd12.Parameters.AddWithValue("@inn", customerInn);
                            cmd12.Parameters.AddWithValue("@kpp", customerKpp);
                            var reader6 = cmd12.ExecuteReader();
                            if (reader6.HasRows)
                            {
                                reader6.Read();
                                customerRegNumber = (string) reader6["regNumber"];
                            }

                            reader6.Close();
                        }

                        if (!String.IsNullOrEmpty(customerRegNumber))
                        {
                            var selectCustomer =
                                $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                            var cmd13 = new MySqlCommand(selectCustomer, connect);
                            cmd13.Prepare();
                            cmd13.Parameters.AddWithValue("@reg_num", customerRegNumber);
                            var reader7 = cmd13.ExecuteReader();
                            if (reader7.HasRows)
                            {
                                reader7.Read();
                                idCustomer = (int) reader7["id_customer"];
                                reader7.Close();
                            }
                            else
                            {
                                reader7.Close();
                                var insertCustomer =
                                    $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, inn = @inn, is223=1";
                                var cmd14 = new MySqlCommand(insertCustomer, connect);
                                cmd14.Prepare();
                                cmd14.Parameters.AddWithValue("@reg_num", customerRegNumber);
                                cmd14.Parameters.AddWithValue("@full_name", customerFullName);
                                cmd14.Parameters.AddWithValue("@inn", customerInn);
                                cmd14.ExecuteNonQuery();
                                idCustomer = (int) cmd14.LastInsertedId;
                            }
                        }
                        else
                        {
                            var selectCustomerInn =
                                $"SELECT id_customer FROM {Program.Prefix}customer WHERE inn = @inn";
                            var cmd15 = new MySqlCommand(selectCustomerInn, connect);
                            cmd15.Prepare();
                            cmd15.Parameters.AddWithValue("@inn", customerInn);
                            var reader8 = cmd15.ExecuteReader();
                            if (reader8.HasRows)
                            {
                                reader8.Read();
                                idCustomer = (int) reader8["id_customer"];
                                reader8.Close();
                            }
                            else
                            {
                                reader8.Close();
                                var regNum223 = $"00000223{customerInn}";
                                var insertCustomer =
                                    $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, inn = @inn, is223=1";
                                var cmd16 = new MySqlCommand(insertCustomer, connect);
                                cmd16.Prepare();
                                cmd16.Parameters.AddWithValue("@reg_num", regNum223);
                                cmd16.Parameters.AddWithValue("@full_name", customerFullName);
                                cmd16.Parameters.AddWithValue("@inn", customerInn);
                                cmd16.ExecuteNonQuery();
                                idCustomer = (int) cmd16.LastInsertedId;
                                var insertCustomer223 =
                                    $"INSERT INTO {Program.Prefix}customer223 SET inn = @inn, full_name = @full_name, contact = @contact, kpp = @kpp, ogrn = @ogrn, post_address = @post_address, phone = @phone, fax = @fax, email = @email";
                                var cmd17 = new MySqlCommand(insertCustomer223, connect);
                                cmd17.Prepare();
                                cmd17.Parameters.AddWithValue("@full_name", customerFullName);
                                cmd17.Parameters.AddWithValue("@inn", customerInn);
                                cmd17.Parameters.AddWithValue("@contact", cusContact);
                                cmd17.Parameters.AddWithValue("@kpp", customerKpp);
                                cmd17.Parameters.AddWithValue("@ogrn", customerOgrn);
                                cmd17.Parameters.AddWithValue("@post_address", customerPostAddress);
                                cmd17.Parameters.AddWithValue("@phone", customerPhone);
                                cmd17.Parameters.AddWithValue("@fax", customerFax);
                                cmd17.Parameters.AddWithValue("@email", customerEmail);
                                cmd17.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {
                        Log.Logger("У customer нет inn", FilePath);
                    }

                    var lotNumber = 1;
                    var lots = GetElements(tender, "lots.lot");
                    if (lots.Count == 0)
                        lots = GetElements(tender, "lot");
                    foreach (var lot in lots)
                    {
                        var lotMaxPrice = ((string) lot.SelectToken("lotData.initialSum") ?? "").Trim();
                        var lotCurrency = ((string) lot.SelectToken("lotData.currency.name") ?? "").Trim();
                        var lotSubj = ((string) lot.SelectToken("lotData.subject") ?? "").Trim();
                        var purchaseDescription =
                            ((string) lot.SelectToken("lotData.purchaseDescription") ?? "").Trim();
                        if (purchaseDescription != "")
                        {
                            lotSubj = $"{lotSubj}. {purchaseDescription}";
                        }

                        var deliveryPlaceLot =
                            ((string) lot.SelectToken("lotData.deliveryPlace.address") ?? "")
                            .Trim();
                        var planNumber =
                            ((string) lot.SelectToken("lotPlanInfo.planRegistrationNumber") ?? "").Trim();
                        var positionNumber =
                            ((string) lot.SelectToken("lotPlanInfo.positionNumber") ?? "").Trim();
                        var applicationSupplySumm = ((string) lot.SelectToken("lotData.applicationSupplySumm") ?? "").Trim();
                        var insertLot =
                            $"INSERT INTO {Program.Prefix}lot SET id_tender = @id_tender, lot_number = @lot_number, max_price = @max_price, currency = @currency, lot_name = @lot_name";
                        var cmd18 = new MySqlCommand(insertLot, connect);
                        cmd18.Prepare();
                        cmd18.Parameters.AddWithValue("@id_tender", idTender);
                        cmd18.Parameters.AddWithValue("@lot_number", lotNumber);
                        cmd18.Parameters.AddWithValue("@max_price", lotMaxPrice);
                        cmd18.Parameters.AddWithValue("@currency", lotCurrency);
                        cmd18.Parameters.AddWithValue("@lot_name", lotSubj);
                        cmd18.ExecuteNonQuery();
                        var idLot = (int) cmd18.LastInsertedId;
                        lotNumber++;
                        var lotitems = GetElements(lot, "lotData.lotItems.lotItem");
                        foreach (var lotitem in lotitems)
                        {
                            var okpd2Code = ((string) lotitem.SelectToken("okpd2.code") ?? "").Trim();
                            var okpdName = ((string) lotitem.SelectToken("okpd2.name") ?? "").Trim();
                            var commodityItemPrice = ((string) lotitem.SelectToken("commodityItemPrice") ?? "")
                                .Trim();
                            var additionalInfo = ((string) lotitem.SelectToken("additionalInfo") ?? "").Trim();
                            var name = $"{additionalInfo} {okpdName}".Trim();
                            var quantityValue = ((string) lotitem.SelectToken("qty") ?? "")
                                .Trim();
                            var okei = ((string) lotitem.SelectToken("okei.name") ?? "").Trim();
                            var okpd2GroupCode = 0;
                            var okpd2GroupLevel1Code = "";
                            if (!String.IsNullOrEmpty(okpd2Code))
                            {
                                GetOkpd(okpd2Code, out okpd2GroupCode, out okpd2GroupLevel1Code);
                            }

                            var insertLotitem =
                                $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, okpd2_group_code = @okpd2_group_code, okpd2_group_level1_code = @okpd2_group_level1_code, okpd_name = @okpd_name, name = @name, quantity_value = @quantity_value, okei = @okei, customer_quantity_value = @customer_quantity_value, price = @price, sum = @sum";
                            var cmd19 = new MySqlCommand(insertLotitem, connect);
                            cmd19.Prepare();
                            cmd19.Parameters.AddWithValue("@id_lot", idLot);
                            cmd19.Parameters.AddWithValue("@id_customer", idCustomer);
                            cmd19.Parameters.AddWithValue("@okpd2_code", okpd2Code);
                            cmd19.Parameters.AddWithValue("@okpd2_group_code", okpd2GroupCode);
                            cmd19.Parameters.AddWithValue("@okpd2_group_level1_code", okpd2GroupLevel1Code);
                            cmd19.Parameters.AddWithValue("@okpd_name", okpdName);
                            cmd19.Parameters.AddWithValue("@name", name);
                            cmd19.Parameters.AddWithValue("@quantity_value", quantityValue);
                            cmd19.Parameters.AddWithValue("@okei", okei);
                            cmd19.Parameters.AddWithValue("@price", commodityItemPrice);
                            cmd19.Parameters.AddWithValue("@sum", "");
                            cmd19.Parameters.AddWithValue("@customer_quantity_value", quantityValue);
                            cmd19.ExecuteNonQuery();
                            var deliveryPlace =
                                ((string) lotitem.SelectToken("deliveryPlace.address") ?? "")
                                .Trim();
                            if (String.IsNullOrEmpty(deliveryPlace))
                                deliveryPlace = deliveryPlaceLot;
                            if (!String.IsNullOrEmpty(deliveryPlace) || !string.IsNullOrEmpty(planNumber) ||
                                !string.IsNullOrEmpty(positionNumber))
                            {
                                var insertCustomerRequirement =
                                    $"INSERT INTO {Program.Prefix}customer_requirement SET id_lot = @id_lot, id_customer = @id_customer, kladr_place = @kladr_place, delivery_place = @delivery_place, delivery_term = @delivery_term, plan_number = @plan_number, position_number = @position_number, application_guarantee_amount = @application_guarantee_amount, max_price = @max_price";
                                var cmd16 = new MySqlCommand(insertCustomerRequirement, connect);
                                cmd16.Prepare();
                                cmd16.Parameters.AddWithValue("@id_lot", idLot);
                                cmd16.Parameters.AddWithValue("@id_customer", idCustomer);
                                cmd16.Parameters.AddWithValue("@kladr_place", "");
                                cmd16.Parameters.AddWithValue("@delivery_place", deliveryPlace);
                                cmd16.Parameters.AddWithValue("@delivery_term", "");
                                cmd16.Parameters.AddWithValue("@plan_number", planNumber);
                                cmd16.Parameters.AddWithValue("@position_number", positionNumber);
                                cmd16.Parameters.AddWithValue("@application_guarantee_amount", applicationSupplySumm);
                                cmd16.Parameters.AddWithValue("@max_price", lotMaxPrice);
                                cmd16.ExecuteNonQuery();
                            }
                        }

                        var lotitemsJoin = GetElements(lot, "jointLotData.lotCustomers.lotCustomer");
                        foreach (var lotitem in lotitemsJoin)
                        {
                            var customerInnJoin =
                                ((string) lotitem.SelectToken("customerInfo.inn") ?? "").Trim();
                            UpdateRegionId(customerInnJoin, idTender, connect);
                            var customerFullNameJoin =
                                ((string) lotitem.SelectToken("customerInfo.fullName") ?? "")
                                .Trim();
                            var customerKppJoin =
                                ((string) lotitem.SelectToken("customerInfo.kpp") ?? "").Trim();
                            var idCustomerJoin = 0;
                            var customerRegNumberJoin = "";
                            if (!String.IsNullOrEmpty(customerInnJoin))
                            {
                                var selectOdCustomer =
                                    $"SELECT regNumber FROM od_customer WHERE inn = @inn AND kpp = @kpp AND regNumber IS NOT NULL";
                                var cmd10 = new MySqlCommand(selectOdCustomer, connect);
                                cmd10.Prepare();
                                cmd10.Parameters.AddWithValue("@inn", customerInnJoin);
                                cmd10.Parameters.AddWithValue("@kpp", customerKppJoin);
                                var reader4 = cmd10.ExecuteReader();
                                if (reader4.HasRows)
                                {
                                    reader4.Read();
                                    customerRegNumberJoin = (string) reader4["regNumber"];
                                }

                                reader4.Close();
                                if (String.IsNullOrEmpty(customerRegNumberJoin))
                                {
                                    var selectOdCustomerFromFtp =
                                        $"SELECT regNumber FROM od_customer_from_ftp WHERE inn = @inn AND kpp = @kpp AND regNumber IS NOT NULL";
                                    var cmd11 = new MySqlCommand(selectOdCustomerFromFtp, connect);
                                    cmd11.Prepare();
                                    cmd11.Parameters.AddWithValue("@inn", customerInnJoin);
                                    cmd11.Parameters.AddWithValue("@kpp", customerKppJoin);
                                    var reader5 = cmd11.ExecuteReader();
                                    if (reader5.HasRows)
                                    {
                                        reader5.Read();
                                        customerRegNumberJoin = (string) reader5["regNumber"];
                                    }

                                    reader5.Close();
                                }

                                if (String.IsNullOrEmpty(customerRegNumberJoin))
                                {
                                    var selectOdCustomerFromFtp223 =
                                        $"SELECT regNumber FROM od_customer_from_ftp223 WHERE inn = @inn AND kpp = @kpp AND regNumber IS NOT NULL";
                                    var cmd12 = new MySqlCommand(selectOdCustomerFromFtp223, connect);
                                    cmd12.Prepare();
                                    cmd12.Parameters.AddWithValue("@inn", customerInnJoin);
                                    cmd12.Parameters.AddWithValue("@kpp", customerKppJoin);
                                    var reader6 = cmd12.ExecuteReader();
                                    if (reader6.HasRows)
                                    {
                                        reader6.Read();
                                        customerRegNumberJoin = (string) reader6["regNumber"];
                                    }

                                    reader6.Close();
                                }

                                if (!String.IsNullOrEmpty(customerRegNumberJoin))
                                {
                                    var selectCustomer =
                                        $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                                    var cmd13 = new MySqlCommand(selectCustomer, connect);
                                    cmd13.Prepare();
                                    cmd13.Parameters.AddWithValue("@reg_num", customerRegNumberJoin);
                                    var reader7 = cmd13.ExecuteReader();
                                    if (reader7.HasRows)
                                    {
                                        reader7.Read();
                                        idCustomerJoin = (int) reader7["id_customer"];
                                        reader7.Close();
                                    }
                                    else
                                    {
                                        reader7.Close();
                                        var insertCustomer =
                                            $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, inn = @inn, is223=1";
                                        var cmd14 = new MySqlCommand(insertCustomer, connect);
                                        cmd14.Prepare();
                                        cmd14.Parameters.AddWithValue("@reg_num", customerRegNumberJoin);
                                        cmd14.Parameters.AddWithValue("@full_name", customerFullNameJoin);
                                        cmd14.Parameters.AddWithValue("@inn", customerInnJoin);
                                        cmd14.ExecuteNonQuery();
                                        idCustomerJoin = (int) cmd14.LastInsertedId;
                                    }
                                }
                                else
                                {
                                    var selectCustomerInn =
                                        $"SELECT id_customer FROM {Program.Prefix}customer WHERE inn = @inn";
                                    var cmd15 = new MySqlCommand(selectCustomerInn, connect);
                                    cmd15.Prepare();
                                    cmd15.Parameters.AddWithValue("@inn", customerInnJoin);
                                    var reader8 = cmd15.ExecuteReader();
                                    if (reader8.HasRows)
                                    {
                                        reader8.Read();
                                        idCustomerJoin = (int) reader8["id_customer"];
                                        reader8.Close();
                                    }
                                    else
                                    {
                                        reader8.Close();
                                        var regNum223 = $"00000223{customerInnJoin}";
                                        var insertCustomer =
                                            $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, inn = @inn, is223=1";
                                        var cmd16 = new MySqlCommand(insertCustomer, connect);
                                        cmd16.Prepare();
                                        cmd16.Parameters.AddWithValue("@reg_num", regNum223);
                                        cmd16.Parameters.AddWithValue("@full_name", customerFullNameJoin);
                                        cmd16.Parameters.AddWithValue("@inn", customerInnJoin);
                                        cmd16.ExecuteNonQuery();
                                        idCustomerJoin = (int) cmd16.LastInsertedId;
                                    }
                                }
                            }
                            else
                            {
                                Log.Logger("У customer нет inn", FilePath);
                            }

                            if (idCustomerJoin == 0)
                            {
                                idCustomerJoin = idCustomer;
                            }

                            var planNumberJoin =
                                ((string) lotitem.SelectToken("lotPlanInfo.planRegistrationNumber") ?? "").Trim();
                            var positionNumberJoin =
                                ((string) lotitem.SelectToken("lotPlanInfo.positionNumber") ?? "").Trim();
                            var deliveryPlaceJoin =
                                ((string) lotitem.SelectToken("lotCustomerData.deliveryPlace.address") ?? "")
                                .Trim();
                            if (String.IsNullOrEmpty(deliveryPlaceJoin))
                                deliveryPlaceJoin = deliveryPlaceLot;
                            var sumJoin = ((string) lotitem.SelectToken("lotCustomerData.initialSum") ?? "")
                                .Trim();
                            if (!String.IsNullOrEmpty(deliveryPlaceJoin) || !string.IsNullOrEmpty(planNumberJoin) ||
                                !string.IsNullOrEmpty(positionNumberJoin))
                            {
                                var insertCustomerRequirement =
                                    $"INSERT INTO {Program.Prefix}customer_requirement SET id_lot = @id_lot, id_customer = @id_customer, kladr_place = @kladr_place, delivery_place = @delivery_place, delivery_term = @delivery_term, plan_number = @plan_number, position_number = @position_number, max_price = @max_price";
                                var cmd16 = new MySqlCommand(insertCustomerRequirement, connect);
                                cmd16.Prepare();
                                cmd16.Parameters.AddWithValue("@id_lot", idLot);
                                cmd16.Parameters.AddWithValue("@id_customer", idCustomerJoin);
                                cmd16.Parameters.AddWithValue("@kladr_place", "");
                                cmd16.Parameters.AddWithValue("@delivery_place", deliveryPlaceJoin);
                                cmd16.Parameters.AddWithValue("@delivery_term", "");
                                cmd16.Parameters.AddWithValue("@plan_number", planNumberJoin);
                                cmd16.Parameters.AddWithValue("@position_number", positionNumberJoin);
                                cmd16.Parameters.AddWithValue("@max_price", sumJoin);
                                cmd16.ExecuteNonQuery();
                            }

                            var items = GetElements(lotitem, "lotCustomerData.lotItems.lotItem");
                            foreach (var item in items)
                            {
                                var okpd2Code = ((string) item.SelectToken("okpd2.code") ?? "").Trim();
                                var okpdName = ((string) item.SelectToken("okpd2.name") ?? "").Trim();
                                var additionalInfo = ((string) item.SelectToken("additionalInfo") ?? "").Trim();
                                var name = $"{additionalInfo} {okpdName}".Trim();
                                var quantityValue = ((string) item.SelectToken("qty") ?? "")
                                    .Trim();
                                var okei = ((string) item.SelectToken("okei.name") ?? "").Trim();
                                var okpd2GroupCode = 0;
                                var okpd2GroupLevel1Code = "";
                                if (!String.IsNullOrEmpty(okpd2Code))
                                {
                                    GetOkpd(okpd2Code, out okpd2GroupCode, out okpd2GroupLevel1Code);
                                }

                                var insertLotitem =
                                    $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, okpd2_group_code = @okpd2_group_code, okpd2_group_level1_code = @okpd2_group_level1_code, okpd_name = @okpd_name, name = @name, quantity_value = @quantity_value, okei = @okei, customer_quantity_value = @customer_quantity_value, sum = @sum";
                                var cmd19 = new MySqlCommand(insertLotitem, connect);
                                cmd19.Prepare();
                                cmd19.Parameters.AddWithValue("@id_lot", idLot);
                                cmd19.Parameters.AddWithValue("@id_customer", idCustomerJoin);
                                cmd19.Parameters.AddWithValue("@okpd2_code", okpd2Code);
                                cmd19.Parameters.AddWithValue("@okpd2_group_code", okpd2GroupCode);
                                cmd19.Parameters.AddWithValue("@okpd2_group_level1_code", okpd2GroupLevel1Code);
                                cmd19.Parameters.AddWithValue("@okpd_name", okpdName);
                                cmd19.Parameters.AddWithValue("@name", name);
                                cmd19.Parameters.AddWithValue("@quantity_value", quantityValue);
                                cmd19.Parameters.AddWithValue("@okei", okei);
                                cmd19.Parameters.AddWithValue("@customer_quantity_value", quantityValue);
                                cmd19.Parameters.AddWithValue("@sum", sumJoin);
                                cmd19.ExecuteNonQuery();
                            }
                        }
                    }

                    try
                    {
                        TenderKwords(connect, idTender);
                    }
                    catch (Exception e)
                    {
                        Log.Logger("error add tender_kwords", idTender, FilePath);
                        Log.Logger(e);
                    }
                }
            }
            else
            {
                Log.Logger("Не могу найти тег Tender223", FilePath);
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

        private string GetScogingDate(JToken ten)
        {
            var scoringDate = "";
            scoringDate =
                (JsonConvert.SerializeObject(ten.SelectToken("placingProcedure.examinationDateTime") ?? "") ??
                 "").Trim('"');
            if (String.IsNullOrEmpty(scoringDate))
            {
                scoringDate = (JsonConvert.SerializeObject(ten.SelectToken("applExamPeriodTime") ?? "") ??
                               "").Trim('"');
            }

            if (String.IsNullOrEmpty(scoringDate))
            {
                scoringDate = (JsonConvert.SerializeObject(ten.SelectToken("examinationDateTime") ?? "") ??
                               "").Trim('"');
            }

            if (String.IsNullOrEmpty(scoringDate))
            {
                scoringDate = ParsingScoringDate(ten);
            }

            return scoringDate;
        }

        private string GetBiddingDate(JToken ten)
        {
            var biddingDate = "";
            biddingDate =
                (JsonConvert.SerializeObject(ten.SelectToken("auctionTime") ?? "") ??
                 "").Trim('"');
            if (String.IsNullOrEmpty(biddingDate))
            {
                biddingDate =
                    (JsonConvert.SerializeObject(ten.SelectToken("placingProcedure.summingupDateTime") ?? "") ??
                     "").Trim('"');
            }

            if (String.IsNullOrEmpty(biddingDate))
            {
                biddingDate = ParsingBiddingDate(ten);
            }

            return biddingDate;
        }

        private string ParsingScoringDate(JToken tn)
        {
            var date = "";
            var noticeExtendField = GetElements(tn, "extendFields.noticeExtendField");
            foreach (var n in noticeExtendField)
            {
                var extendField = GetElements(n, "extendField");
                foreach (var b in extendField)
                {
                    var desc = ((string) b.SelectToken("description") ?? "").Trim();

                    if (desc.ToLower().IndexOf("дата", StringComparison.Ordinal) != -1 &&
                        desc.ToLower().IndexOf("рассмотр", StringComparison.Ordinal) != -1)
                    {
                        var dm = ((string) b.SelectToken("value.text") ?? "").Trim();
                        if (String.IsNullOrEmpty(dm))
                        {
                            return (JsonConvert.SerializeObject(b.SelectToken("value.dateTime") ?? "") ??
                                    "").Trim('"');
                        }

                        date = FindDate(dm);
                        if (String.IsNullOrEmpty(date))
                        {
                            _extendScoringDate = dm;
                        }

                        return date;
                    }
                }
            }

            return date;
        }

        private string ParsingBiddingDate(JToken tn)
        {
            var date = "";
            var noticeExtendField = GetElements(tn, "extendFields.noticeExtendField");
            foreach (var n in noticeExtendField)
            {
                var extendField = GetElements(n, "extendField");
                foreach (var b in extendField)
                {
                    var desc = ((string) b.SelectToken("description") ?? "").Trim();

                    if (desc.ToLower().IndexOf("дата", StringComparison.Ordinal) != -1 &&
                        desc.ToLower().IndexOf("подвед", StringComparison.Ordinal) != -1)
                    {
                        var dm = ((string) b.SelectToken("value.text") ?? "").Trim();
                        if (String.IsNullOrEmpty(dm))
                        {
                            return (JsonConvert.SerializeObject(b.SelectToken("value.dateTime") ?? "") ??
                                    "").Trim('"');
                        }

                        date = FindDate(dm);
                        if (String.IsNullOrEmpty(date))
                        {
                            _extendBiddingDate = dm;
                        }

                        return date;
                    }
                }
            }

            return date;
        }

        private string FindDate(string date)
        {
            var d = "";
            var pattern = @"((\d{2})\.(\d{2})\.(\d{4}))";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var match = regex.Match(date);
            if (match.Success)
            {
                try
                {
                    var i = DateTime.ParseExact(match.Value, "dd.MM.yyyy", CultureInfo.InvariantCulture);
                    d = i.ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch (Exception e)
                {
                    Log.Logger("Не получилось пропарсить дату", match.Value, FilePath, e);
                }
            }

            return d;
        }
        
        private void UpdateRegionId(string cusInn, int idTender, MySqlConnection connect)
        {
            if (string.IsNullOrEmpty(cusInn))
                return;
            if(isRegionExsist)
                return;
            var selectCustomerRegion =
                $"SELECT region FROM nsi_eis_organizations_223 WHERE inn = @inn";
            var cmd = new MySqlCommand(selectCustomerRegion, connect);
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@inn", cusInn);
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