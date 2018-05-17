using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderType615 : Tender
    {
        public event Action<int> AddTender615;

        public TenderType615(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddTender615 += delegate(int d)
            {
                if (d > 0)
                    Program.AddTender615++;
                else
                    Log.Logger("Не удалось добавить Tender615", FilePath);
            };
        }

        public override void Parsing()
        {
            string xml = GetXml(File.ToString());
            JObject root = (JObject) T.SelectToken("export");
            JProperty firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("pprf615"));
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
                        $"SELECT id_tender FROM {Program.Prefix}tender WHERE id_xml = @id_xml AND id_region = @id_region AND purchase_number = @purchase_number";
                    MySqlCommand cmd = new MySqlCommand(selectTender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_xml", idT);
                    cmd.Parameters.AddWithValue("@id_region", RegionId);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Close();
                        return;
                    }

                    reader.Close();
                    string docPublishDate = (JsonConvert.SerializeObject(tender.SelectToken("commonInfo.docPublishDate") ?? "") ??
                                             "").Trim('"');
                    string dateVersion = docPublishDate;
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
                    string purchaseObjectInfo = ((string) tender.SelectToken("commonInfo.purchaseObjectInfo") ?? "").Trim();
                    string organizerRegNum =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.regNum") ?? "").Trim();
                    string organizerFullName =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.fullName") ?? "").Trim();
                    string organizerPostAddress =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.postAddress") ?? "").Trim();
                    string organizerFactAddress =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.factAddress") ?? "").Trim();
                    string organizerInn = ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.INN") ?? "")
                        .Trim();
                    string organizerKpp = ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleOrgInfo.KPP") ?? "")
                        .Trim();
                    string organizerResponsibleRole =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.publisherRole") ?? "").Trim();
                    string organizerLastName =
                    ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleInfo.contactPerson.lastName") ??
                     "").Trim();
                    string organizerFirstName =
                    ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleInfo.contactPerson.firstName") ??
                     "").Trim();
                    string organizerMiddleName =
                    ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleInfo.contactPerson.middleName") ??
                     "").Trim();
                    string organizerContact = $"{organizerLastName} {organizerFirstName} {organizerMiddleName}"
                        .Trim();
                    string organizerEmail =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleInfo.contactEMail") ?? "").Trim();
                    string organizerFax =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleInfo.contactFax") ?? "").Trim();
                    string organizerPhone =
                        ((string) tender.SelectToken("purchaseResponsibleInfo.responsibleInfo.contactPhone") ?? "").Trim();
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
                    string placingWayCode = ((string) tender.SelectToken("placingWayInfo.code") ?? "").Trim();
                    string placingWayName = ((string) tender.SelectToken("placingWayInfo.name") ?? "").Trim();
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
                    string etpCode = ((string) tender.SelectToken("notificationInfo.ETPInfo.code") ?? "").Trim();
                    string etpName = ((string) tender.SelectToken("notificationInfo.ETPInfo.name") ?? "").Trim();
                    string etpUrl = ((string) tender.SelectToken("notificationInfo.ETPInfo.url") ?? "").Trim();
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
                        (JsonConvert.SerializeObject(tender.SelectToken("notificationInfo.procedureInfo.collectingEndDate") ?? "") ??
                         "").Trim('"');
                    string scoringDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("notificationInfo.procedureInfo.scoringDate") ?? "") ??
                         "").Trim('"');
                    scoringDate = scoringDate.Replace("+", "T00:00:00+");
                    string biddingDate =
                        (JsonConvert.SerializeObject(tender.SelectToken("notificationInfo.procedureInfo.biddingDate") ?? "") ??
                         "").Trim('"');
                    biddingDate = biddingDate.Replace("+", "T00:00:00+");
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
                    cmd9.Parameters.AddWithValue("@type_fz", 615);
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
                    int resInsertTender = cmd9.ExecuteNonQuery();
                    int idTender = (int) cmd9.LastInsertedId;
                    AddTender615?.Invoke(resInsertTender);
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
                    List<JToken> lots = GetElements(tender, "purchaseSubjectInfo");
                    foreach (var lot in lots)
                    {
                        string lotMaxPrice = ((string) tender.SelectToken("notificationInfo.contractCondition.maxPriceInfo.maxPrice") ?? "").Trim();
                        string lotCurrency = "";
                        string lotFinanceSource = "";
                        string lotName = ((string) lot.SelectToken("name") ?? "").Trim();
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
                        
                    }
                }
            }
            else
            {
                Log.Logger("Не могу найти тег Tender615", FilePath);
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