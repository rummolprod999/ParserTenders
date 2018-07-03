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

namespace ParserTenders.TenderDir
{
    public class TenderType223Web : TenderWeb
    {
        public event Action<int> AddTender223;
        private bool Up = default ;
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
            string xml = GetXml();
            JProperty firstOrDefault = T.Properties()
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
                string idT = ((string) tender.SelectToken("guid") ?? "").Trim();
                if (String.IsNullOrEmpty(idT))
                {
                    Log.Logger("У тендера нет id", FilePath);
                    return;
                }

                string purchaseNumber = ((string) tender.SelectToken("registrationNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("У тендера нет purchaseNumber", FilePath);
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
                    (JsonConvert.SerializeObject(tender.SelectToken("publicationDateTime") ?? "") ??
                     "").Trim('"');
                    string dateVersion = (JsonConvert.SerializeObject(tender.SelectToken("modificationDate") ?? "") ??
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
                    int cancelStatus = 0;
                    if (!String.IsNullOrEmpty(dateVersion))
                    {
                        string selectDateT =
                            $"SELECT id_tender, date_version FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number";
                        MySqlCommand cmd2 = new MySqlCommand(selectDateT, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        DataTable dt = new DataTable();
                        MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd2};
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            Up = true;
                            foreach (DataRow row in dt.Rows)
                            {
                                DateTime dateNew = DateTime.Parse(dateVersion);
                                DateTime dateOld = (DateTime) row["date_version"];
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

                    string href = ((string) tender.SelectToken("urlVSRZ") ?? "").Trim();
                    string purchaseObjectInfo = ((string) tender.SelectToken("name") ?? "").Trim();

                    string numVersion = ((string) tender.SelectToken("version") ?? "").Trim();
                    string noticeVersion = ((string) tender.SelectToken("modificationDescription") ?? "").Trim();
                    string printform = ((string) tender.SelectToken("urlOOS") ?? "").Trim();
                    if (!String.IsNullOrEmpty(printform) && printform.IndexOf("CDATA") != -1)
                        printform = printform.Substring(9, printform.Length - 12);
                    string organizerFullName = ((string) tender.SelectToken("placer.mainInfo.fullName") ?? "").Trim();
                    string organizerPostAddress = ((string) tender.SelectToken("placer.mainInfo.postalAddress") ?? "")
                        .Trim();
                    string organizerFactAddress = ((string) tender.SelectToken("placer.mainInfo.legalAddress") ?? "")
                        .Trim();
                    var addr = (organizerPostAddress != "") ? organizerPostAddress : organizerFactAddress;
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
                    string organizerInn = ((string) tender.SelectToken("placer.mainInfo.inn") ?? "").Trim();
                    string organizerKpp = ((string) tender.SelectToken("placer.mainInfo.kpp") ?? "").Trim();
                    string organizerEmail = ((string) tender.SelectToken("placer.mainInfo.email") ?? "").Trim();
                    string organizerPhone = ((string) tender.SelectToken("placer.mainInfo.phone") ?? "").Trim();
                    string organizerFax = ((string) tender.SelectToken("placer.mainInfo.fax") ?? "").Trim();
                    int idOrganizer = 0;
                    if (!String.IsNullOrEmpty(organizerInn))
                    {
                        string selectOrg =
                            $"SELECT id_organizer FROM {Program.Prefix}organizer WHERE inn = @inn AND kpp = @kpp";
                        MySqlCommand cmd2 = new MySqlCommand(selectOrg, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@inn", organizerInn);
                        cmd2.Parameters.AddWithValue("@kpp", organizerKpp);
                        MySqlDataReader reader1 = cmd2.ExecuteReader();
                        if (reader1.HasRows)
                        {
                            reader1.Read();
                            idOrganizer = reader1.GetInt32("id_organizer");
                            reader1.Close();
                        }
                        else
                        {
                            reader1.Close();
                            string addOrganizer =
                                $"INSERT INTO {Program.Prefix}organizer SET full_name = @full_name, post_address = @post_address, fact_address = @fact_address, inn = @inn, kpp = @kpp, contact_email = @contact_email, contact_phone = @contact_phone, contact_fax = @contact_fax";
                            MySqlCommand cmd3 = new MySqlCommand(addOrganizer, connect);
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

                    int idPlacingWay = 0;
                    string placingWayCode = ((string) tender.SelectToken("purchaseMethodCode") ?? "").Trim();
                    string placingWayName = ((string) tender.SelectToken("purchaseCodeName") ?? "").Trim();
                    int conformity = GetConformity(placingWayName);
                    if (!String.IsNullOrEmpty(placingWayCode))
                    {
                        string selectPlacingWay =
                            $"SELECT id_placing_way FROM {Program.Prefix}placing_way WHERE code = @code";
                        MySqlCommand cmd4 = new MySqlCommand(selectPlacingWay, connect);
                        cmd4.Prepare();
                        cmd4.Parameters.AddWithValue("@code", placingWayCode);
                        MySqlDataReader reader2 = cmd4.ExecuteReader();
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            idPlacingWay = reader2.GetInt32("id_placing_way");
                            reader2.Close();
                        }
                        else
                        {
                            reader2.Close();
                            string insertPlacingWay =
                                $"INSERT INTO {Program.Prefix}placing_way SET code= @code, name= @name, conformity = @conformity";
                            MySqlCommand cmd5 = new MySqlCommand(insertPlacingWay, connect);
                            cmd5.Prepare();
                            cmd5.Parameters.AddWithValue("@code", placingWayCode);
                            cmd5.Parameters.AddWithValue("@name", placingWayName);
                            cmd5.Parameters.AddWithValue("@conformity", conformity);
                            cmd5.ExecuteNonQuery();
                            idPlacingWay = (int) cmd5.LastInsertedId;
                        }
                    }

                    int idEtp = 0;
                    string etpCode =
                        ((string) tender.SelectToken("electronicPlaceInfo.electronicPlaceId") ?? "").Trim();
                    string etpName = ((string) tender.SelectToken("electronicPlaceInfo.name") ?? "").Trim();
                    string etpUrl = ((string) tender.SelectToken("electronicPlaceInfo.url") ?? "").Trim();
                    if (!String.IsNullOrEmpty(etpCode))
                    {
                        string selectEtp = $"SELECT id_etp FROM {Program.Prefix}etp WHERE code = @code";
                        MySqlCommand cmd6 = new MySqlCommand(selectEtp, connect);
                        cmd6.Prepare();
                        cmd6.Parameters.AddWithValue("@code", etpCode);
                        MySqlDataReader reader3 = cmd6.ExecuteReader();
                        if (reader3.HasRows)
                        {
                            reader3.Read();
                            idEtp = reader3.GetInt32("id_etp");
                            reader3.Close();
                        }
                        else
                        {
                            reader3.Close();
                            string insertEtp =
                                $"INSERT INTO {Program.Prefix}etp SET code= @code, name= @name, url= @url, conf=0";
                            MySqlCommand cmd7 = new MySqlCommand(insertEtp, connect);
                            cmd7.Prepare();
                            cmd7.Parameters.AddWithValue("@code", etpCode);
                            cmd7.Parameters.AddWithValue("@name", etpName);
                            cmd7.Parameters.AddWithValue("@url", etpUrl);
                            cmd7.ExecuteNonQuery();
                            idEtp = (int) cmd7.LastInsertedId;
                        }
                    }

                    string endDate =
                    (JsonConvert.SerializeObject(tender.SelectToken("submissionCloseDateTime") ?? "") ??
                     "").Trim('"');
                    string scoringDate = GetScogingDate(tender);
                    string biddingDate = GetBiddingDate(tender);
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

                    string insertTender =
                        $"INSERT INTO {Program.Prefix}tender SET id_region = @id_region, id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, purchase_object_info = @purchase_object_info, type_fz = @type_fz, id_organizer = @id_organizer, id_placing_way = @id_placing_way, id_etp = @id_etp, end_date = @end_date, scoring_date = @scoring_date, bidding_date = @bidding_date, cancel = @cancel, date_version = @date_version, num_version = @num_version, notice_version = @notice_version, xml = @xml, print_form = @print_form, extend_scoring_date = @extend_scoring_date, extend_bidding_date = @extend_bidding_date";
                    MySqlCommand cmd8 = new MySqlCommand(insertTender, connect);
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
                    int resInsertTender = cmd8.ExecuteNonQuery();
                    int idTender = (int) cmd8.LastInsertedId;
                    AddTender223?.Invoke(resInsertTender);
                    List<JToken> attachments = GetElements(tender, "attachments.document");
                    foreach (var att in attachments)
                    {
                        string attachName = ((string) att.SelectToken("fileName") ?? "").Trim();
                        string attachDescription = ((string) att.SelectToken("description") ?? "").Trim();
                        string attachUrl = ((string) att.SelectToken("url") ?? "").Trim();
                        string insertAttach =
                            $"INSERT INTO {Program.Prefix}attachment SET id_tender = @id_tender, file_name = @file_name, url = @url, description = @description";
                        MySqlCommand cmd9 = new MySqlCommand(insertAttach, connect);
                        cmd9.Prepare();
                        cmd9.Parameters.AddWithValue("@id_tender", idTender);
                        cmd9.Parameters.AddWithValue("@file_name", attachName);
                        cmd9.Parameters.AddWithValue("@url", attachUrl);
                        cmd9.Parameters.AddWithValue("@description", attachDescription);
                        cmd9.ExecuteNonQuery();
                    }

                    string customerInn = ((string) tender.SelectToken("customer.mainInfo.inn") ?? "").Trim();
                    string customerFullName = ((string) tender.SelectToken("customer.mainInfo.fullName") ?? "")
                        .Trim();
                    string customerKpp = ((string) tender.SelectToken("customer.mainInfo.kpp") ?? "").Trim();
                    string customerOgrn = ((string) tender.SelectToken("customer.mainInfo.ogrn") ?? "").Trim();
                    string customerPostAddress =
                        ((string) tender.SelectToken("customer.mainInfo.postalAddress") ?? "").Trim();
                    string customerPhone = ((string) tender.SelectToken("customer.mainInfo.phone") ?? "").Trim();
                    string customerFax = ((string) tender.SelectToken("customer.mainInfo.fax") ?? "").Trim();
                    string customerEmail = ((string) tender.SelectToken("customer.mainInfo.email") ?? "").Trim();
                    string cusLn = ((string) tender.SelectToken("contact.lastName") ?? "").Trim();
                    string cusFn = ((string) tender.SelectToken("contact.firstName") ?? "").Trim();
                    string cusMn = ((string) tender.SelectToken("contact.middleName") ?? "").Trim();
                    string cusContact = $"{cusLn} {cusFn} {cusMn}".Trim();
                    int idCustomer = 0;
                    string customerRegNumber = "";
                    if (!String.IsNullOrEmpty(customerInn))
                    {
                        string selectOdCustomer =
                            $"SELECT regNumber FROM od_customer WHERE inn = @inn AND kpp = @kpp";
                        MySqlCommand cmd10 = new MySqlCommand(selectOdCustomer, connect);
                        cmd10.Prepare();
                        cmd10.Parameters.AddWithValue("@inn", customerInn);
                        cmd10.Parameters.AddWithValue("@kpp", customerKpp);
                        MySqlDataReader reader4 = cmd10.ExecuteReader();
                        if (reader4.HasRows)
                        {
                            reader4.Read();
                            customerRegNumber = (string) reader4["regNumber"];
                        }

                        reader4.Close();
                        if (String.IsNullOrEmpty(customerRegNumber))
                        {
                            string selectOdCustomerFromFtp =
                                $"SELECT regNumber FROM od_customer_from_ftp WHERE inn = @inn AND kpp = @kpp";
                            MySqlCommand cmd11 = new MySqlCommand(selectOdCustomerFromFtp, connect);
                            cmd11.Prepare();
                            cmd11.Parameters.AddWithValue("@inn", customerInn);
                            cmd11.Parameters.AddWithValue("@kpp", customerKpp);
                            MySqlDataReader reader5 = cmd11.ExecuteReader();
                            if (reader5.HasRows)
                            {
                                reader5.Read();
                                customerRegNumber = (string) reader5["regNumber"];
                            }

                            reader5.Close();
                        }

                        if (String.IsNullOrEmpty(customerRegNumber))
                        {
                            string selectOdCustomerFromFtp223 =
                                $"SELECT regNumber FROM od_customer_from_ftp223 WHERE inn = @inn AND kpp = @kpp";
                            MySqlCommand cmd12 = new MySqlCommand(selectOdCustomerFromFtp223, connect);
                            cmd12.Prepare();
                            cmd12.Parameters.AddWithValue("@inn", customerInn);
                            cmd12.Parameters.AddWithValue("@kpp", customerKpp);
                            MySqlDataReader reader6 = cmd12.ExecuteReader();
                            if (reader6.HasRows)
                            {
                                reader6.Read();
                                customerRegNumber = (string) reader6["regNumber"];
                            }

                            reader6.Close();
                        }

                        if (!String.IsNullOrEmpty(customerRegNumber))
                        {
                            string selectCustomer =
                                $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                            MySqlCommand cmd13 = new MySqlCommand(selectCustomer, connect);
                            cmd13.Prepare();
                            cmd13.Parameters.AddWithValue("@reg_num", customerRegNumber);
                            MySqlDataReader reader7 = cmd13.ExecuteReader();
                            if (reader7.HasRows)
                            {
                                reader7.Read();
                                idCustomer = (int) reader7["id_customer"];
                                reader7.Close();
                            }
                            else
                            {
                                reader7.Close();
                                string insertCustomer =
                                    $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, inn = @inn, is223=1";
                                MySqlCommand cmd14 = new MySqlCommand(insertCustomer, connect);
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
                            string selectCustomerInn =
                                $"SELECT id_customer FROM {Program.Prefix}customer WHERE inn = @inn";
                            MySqlCommand cmd15 = new MySqlCommand(selectCustomerInn, connect);
                            cmd15.Prepare();
                            cmd15.Parameters.AddWithValue("@inn", customerInn);
                            MySqlDataReader reader8 = cmd15.ExecuteReader();
                            if (reader8.HasRows)
                            {
                                reader8.Read();
                                idCustomer = (int) reader8["id_customer"];
                                reader8.Close();
                            }
                            else
                            {
                                reader8.Close();
                                string regNum223 = $"00000223{customerInn}";
                                string insertCustomer =
                                    $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, inn = @inn, is223=1";
                                MySqlCommand cmd16 = new MySqlCommand(insertCustomer, connect);
                                cmd16.Prepare();
                                cmd16.Parameters.AddWithValue("@reg_num", regNum223);
                                cmd16.Parameters.AddWithValue("@full_name", customerFullName);
                                cmd16.Parameters.AddWithValue("@inn", customerInn);
                                cmd16.ExecuteNonQuery();
                                idCustomer = (int) cmd16.LastInsertedId;
                                string insertCustomer223 =
                                    $"INSERT INTO {Program.Prefix}customer223 SET inn = @inn, full_name = @full_name, contact = @contact, kpp = @kpp, ogrn = @ogrn, post_address = @post_address, phone = @phone, fax = @fax, email = @email";
                                MySqlCommand cmd17 = new MySqlCommand(insertCustomer223, connect);
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

                    int lotNumber = 1;
                    List<JToken> lots = GetElements(tender, "lots.lot");
                    if (lots.Count == 0)
                        lots = GetElements(tender, "lot");
                    foreach (var lot in lots)
                    {
                        string lotMaxPrice = ((string) lot.SelectToken("lotData.initialSum") ?? "").Trim();
                        string lotCurrency = ((string) lot.SelectToken("lotData.currency.name") ?? "").Trim();
                        string lotSubj = ((string) lot.SelectToken("lotData.subject") ?? "").Trim();
                        string insertLot =
                            $"INSERT INTO {Program.Prefix}lot SET id_tender = @id_tender, lot_number = @lot_number, max_price = @max_price, currency = @currency";
                        MySqlCommand cmd18 = new MySqlCommand(insertLot, connect);
                        cmd18.Prepare();
                        cmd18.Parameters.AddWithValue("@id_tender", idTender);
                        cmd18.Parameters.AddWithValue("@lot_number", lotNumber);
                        cmd18.Parameters.AddWithValue("@max_price", lotMaxPrice);
                        cmd18.Parameters.AddWithValue("@currency", lotCurrency);
                        cmd18.ExecuteNonQuery();
                        int idLot = (int) cmd18.LastInsertedId;
                        lotNumber++;
                        List<JToken> lotitems = GetElements(lot, "lotData.lotItems.lotItem");
                        foreach (var lotitem in lotitems)
                        {
                            string okpd2Code = ((string) lotitem.SelectToken("okpd2.code") ?? "").Trim();
                            string okpdName = ((string) lotitem.SelectToken("okpd2.name") ?? "").Trim();
                            string additionalInfo = ((string) lotitem.SelectToken("additionalInfo") ?? "").Trim();
                            string name = "";
                            if (!String.IsNullOrEmpty(lotSubj))
                            {
                                name = $"{lotSubj} {additionalInfo}".Trim();
                            }
                            else
                            {
                                name = $"{additionalInfo} {okpdName}".Trim();
                            }
                            string quantityValue = ((string) lotitem.SelectToken("qty") ?? "")
                                .Trim();
                            string okei = ((string) lotitem.SelectToken("okei.name") ?? "").Trim();
                            int okpd2GroupCode = 0;
                            string okpd2GroupLevel1Code = "";
                            if (!String.IsNullOrEmpty(okpd2Code))
                            {
                                GetOkpd(okpd2Code, out okpd2GroupCode, out okpd2GroupLevel1Code);
                            }

                            string insertLotitem =
                                $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd2_code = @okpd2_code, okpd2_group_code = @okpd2_group_code, okpd2_group_level1_code = @okpd2_group_level1_code, okpd_name = @okpd_name, name = @name, quantity_value = @quantity_value, okei = @okei, customer_quantity_value = @customer_quantity_value";
                            MySqlCommand cmd19 = new MySqlCommand(insertLotitem, connect);
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
                            cmd19.Parameters.AddWithValue("@customer_quantity_value", quantityValue);
                            cmd19.ExecuteNonQuery();
                        }
                    }

                    TenderKwords(connect, idTender);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег Tender223", FilePath);
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
            string scoringDate = "";
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
            string biddingDate = "";
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
            string d = "";
            string pattern = @"((\d{2})\.(\d{2})\.(\d{4}))";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            Match match = regex.Match(date);
            if (match.Success)
            {
                try
                {
                    DateTime i = DateTime.ParseExact(match.Value, "dd.MM.yyyy", CultureInfo.InvariantCulture);
                    d = i.ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch (Exception e)
                {
                    Log.Logger("Не получилось пропарсить дату", match.Value, FilePath, e);
                }
            }

            return d;
        }
    }
}