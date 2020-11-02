using System;
using System.Data;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using ikvm.extensions;
using MySql.Data.MySqlClient;

namespace ParserTenders.TenderDir
{
    public class TenderTypeRosneft : TenderBase
    {
        private readonly string PurNum;
        private readonly string Url;
        private readonly string PlacingWay;
        private readonly DateTime DatePub;
        private readonly DateTime DateEnd;
        private const int TypeFz = 19;

        public TenderTypeRosneft(string purNum, string url, string placingWay, DateTime datePub, DateTime dateEnd)
        {
            PurNum = purNum ?? throw new ArgumentNullException(nameof(purNum));
            Url = url ?? throw new ArgumentNullException(nameof(url));
            PlacingWay = placingWay ?? throw new ArgumentNullException(nameof(placingWay));
            DatePub = datePub;
            DateEnd = dateEnd;
        }

        public void Parsing()
        {
            var s = DownloadString.DownL(Url);
            if (string.IsNullOrEmpty(s))
            {
                Log.Logger("Empty string in Parsing()", Url);
                return;
            }

            var parser = new HtmlParser();
            var document = parser.Parse(s);
            var dateUpd = DatePub;
            var dateUpdT =
                document.All.FirstOrDefault(m => m.TextContent.Contains("последние изменения") && m.TagName == "STRONG")
                    ?.TextContent ?? "";
            if (dateUpdT != "")
            {
                var dateUpdTt = dateUpdT.GetDateFromRegex(@"последние изменения от(.*)\)");
                if (dateUpdTt != "") dateUpd = dateUpdTt.ParseDateUn("dd.MM.yyyy - HH:mm");
            }

            using (var connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                var selectTend =
                    $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND date_version = @date_version AND type_fz = @type_fz";
                var cmd = new MySqlCommand(selectTend, connect);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@purchase_number", PurNum);
                cmd.Parameters.AddWithValue("@date_version", dateUpd);
                cmd.Parameters.AddWithValue("@type_fz", TypeFz);
                var dt = new DataTable();
                var adapter = new MySqlDataAdapter {SelectCommand = cmd};
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    //Log.Logger("This tender is exist in base", PurNum);
                    return;
                }

                var cancelStatus = 0;
                var update = false;
                var selectDateT =
                    $"SELECT id_tender, date_version, cancel FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND type_fz = @type_fz";
                var cmd2 = new MySqlCommand(selectDateT, connect);
                cmd2.Prepare();
                cmd2.Parameters.AddWithValue("@purchase_number", PurNum);
                cmd2.Parameters.AddWithValue("@type_fz", TypeFz);
                var adapter2 = new MySqlDataAdapter {SelectCommand = cmd2};
                var dt2 = new DataTable();
                adapter2.Fill(dt2);
                foreach (DataRow row in dt2.Rows)
                {
                    //DateTime dateNew = DateTime.Parse(pr.DatePublished);
                    update = true;
                    if (dateUpd >= (DateTime) row["date_version"])
                    {
                        row["cancel"] = 1;
                        //row.AcceptChanges();
                        //row.SetModified();
                    }
                    else
                    {
                        cancelStatus = 1;
                    }
                }

                var commandBuilder =
                    new MySqlCommandBuilder(adapter2) {ConflictOption = ConflictOption.OverwriteChanges};
                //Console.WriteLine(commandBuilder.GetUpdateCommand().CommandText);
                adapter2.Update(dt2);
                var noticeVersion = "";
                var printForm = Url;
                var customerId = 0;
                var organiserId = 0;
                var orgFullName = (document.QuerySelector("td:contains('Организатор') +  td")?.QuerySelector("strong")
                                       ?.TextContent ?? "").Trim();
                if (!string.IsNullOrEmpty(orgFullName))
                {
                    var selectOrg =
                        $"SELECT id_organizer FROM {Program.Prefix}organizer WHERE full_name = @full_name";
                    var cmd3 = new MySqlCommand(selectOrg, connect);
                    cmd3.Prepare();
                    cmd3.Parameters.AddWithValue("@full_name", orgFullName);
                    var dt3 = new DataTable();
                    var adapter3 = new MySqlDataAdapter {SelectCommand = cmd3};
                    adapter3.Fill(dt3);
                    if (dt3.Rows.Count > 0)
                    {
                        organiserId = (int) dt3.Rows[0].ItemArray[0];
                    }
                    else
                    {
                        var (inn, kpp, ogrn) = ("", "", "");
                        var tmp = document.QuerySelector("td:contains('Организатор') +  td")?.TextContent ?? "";
                        inn = tmp.GetDateFromRegex(@"Инн: (\d+)");
                        kpp = tmp.GetDateFromRegex(@"КПП: (\d+)");
                        //ogrn = tmp.GetDateFromRegex(@"ОГРН: (\d+)");
                        var cont = document.QuerySelector("h2:contains('Контактная информация') +  table");
                        var (factAddr, contactP, email, phone) = ("", "", "", "");
                        if (cont != null)
                        {
                            contactP = (cont.QuerySelector("td.contact-left")?.TextContent ?? "").trim();
                            factAddr = (cont.QuerySelector("td > div.contact-adress > span")?.TextContent ?? "").trim();
                            email = (cont.QuerySelector("td > div.contact-email > span")?.TextContent ?? "").trim();
                            phone = (cont.QuerySelector("td > div.contact-tel > span")?.TextContent ?? "").trim();
                        }

                        var addOrganizer =
                            $"INSERT INTO {Program.Prefix}organizer SET full_name = @full_name, post_address = @post_address, fact_address = @fact_address, inn = @inn, kpp = @kpp, contact_phone = @contact_phone, contact_person = @contact_person, contact_email = @contact_email";
                        var cmd4 = new MySqlCommand(addOrganizer, connect);
                        cmd4.Prepare();
                        cmd4.Parameters.AddWithValue("@full_name", orgFullName);
                        cmd4.Parameters.AddWithValue("@post_address", factAddr);
                        cmd4.Parameters.AddWithValue("@fact_address", factAddr);
                        cmd4.Parameters.AddWithValue("@inn", inn);
                        cmd4.Parameters.AddWithValue("@kpp", kpp);
                        cmd4.Parameters.AddWithValue("@contact_phone", phone);
                        cmd4.Parameters.AddWithValue("@contact_person", contactP);
                        cmd4.Parameters.AddWithValue("@contact_email", email);
                        cmd4.ExecuteNonQuery();
                        organiserId = (int) cmd4.LastInsertedId;
                    }
                }

                if (!string.IsNullOrEmpty(orgFullName))
                {
                    var selectCustomer =
                        $"SELECT id_customer FROM {Program.Prefix}customer WHERE full_name = @full_name";
                    var cmd13 = new MySqlCommand(selectCustomer, connect);
                    cmd13.Prepare();
                    cmd13.Parameters.AddWithValue("@full_name", orgFullName);
                    var reader7 = cmd13.ExecuteReader();
                    if (reader7.HasRows)
                    {
                        reader7.Read();
                        customerId = (int) reader7["id_customer"];
                        reader7.Close();
                    }
                    else
                    {
                        reader7.Close();
                        var insertCustomer =
                            $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, is223=1";
                        var cmd14 = new MySqlCommand(insertCustomer, connect);
                        cmd14.Prepare();
                        var customerRegNumber = Guid.NewGuid().ToString();
                        cmd14.Parameters.AddWithValue("@reg_num", customerRegNumber);
                        cmd14.Parameters.AddWithValue("@full_name", orgFullName);
                        cmd14.ExecuteNonQuery();
                        customerId = (int) cmd14.LastInsertedId;
                    }
                }

                //int idPlacingWay = 0;
                GetPlacingWay(connect, out var idPlacingWay);
                //int idEtp = 0;
                GetEtp(connect, out var idEtp);
                var purObjInfo = (document.QuerySelector("h1.title")?.TextContent ?? "").Trim();
                var insertTender =
                    $"INSERT INTO {Program.Prefix}tender SET id_region = @id_region, id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, purchase_object_info = @purchase_object_info, type_fz = @type_fz, id_organizer = @id_organizer, id_placing_way = @id_placing_way, id_etp = @id_etp, end_date = @end_date, scoring_date = @scoring_date, bidding_date = @bidding_date, cancel = @cancel, date_version = @date_version, num_version = @num_version, notice_version = @notice_version, xml = @xml, print_form = @print_form";
                var cmd9 = new MySqlCommand(insertTender, connect);
                cmd9.Prepare();
                cmd9.Parameters.AddWithValue("@id_region", 0);
                cmd9.Parameters.AddWithValue("@id_xml", PurNum);
                cmd9.Parameters.AddWithValue("@purchase_number", PurNum);
                cmd9.Parameters.AddWithValue("@doc_publish_date", DatePub);
                cmd9.Parameters.AddWithValue("@href", Url);
                cmd9.Parameters.AddWithValue("@purchase_object_info", purObjInfo);
                cmd9.Parameters.AddWithValue("@type_fz", TypeFz);
                cmd9.Parameters.AddWithValue("@id_organizer", organiserId);
                cmd9.Parameters.AddWithValue("@id_placing_way", idPlacingWay);
                cmd9.Parameters.AddWithValue("@id_etp", idEtp);
                cmd9.Parameters.AddWithValue("@end_date", DateEnd);
                cmd9.Parameters.AddWithValue("@scoring_date", DateTime.MinValue);
                cmd9.Parameters.AddWithValue("@bidding_date", DateTime.MinValue);
                cmd9.Parameters.AddWithValue("@cancel", cancelStatus);
                cmd9.Parameters.AddWithValue("@date_version", dateUpd);
                cmd9.Parameters.AddWithValue("@num_version", 1);
                cmd9.Parameters.AddWithValue("@notice_version", noticeVersion);
                cmd9.Parameters.AddWithValue("@xml", Url);
                cmd9.Parameters.AddWithValue("@print_form", printForm);
                var resInsertTender = cmd9.ExecuteNonQuery();
                var idTender = (int) cmd9.LastInsertedId;
                if (update)
                {
                    Program.UpRosneft++;
                }
                else
                {
                    Program.AddRosneft++;
                }

                var docs = document.QuerySelectorAll(
                    "h2:contains('Пакет документов') + table td.cont-right > div.info > a");
                GetDocs(docs, connect, idTender);
                var lots = document.QuerySelectorAll("h2:contains('Лоты') + div table tbody tr");
                if (lots.Length == 0)
                {
                    var nmckT = (document.QuerySelector("td:contains('Сведения о начальной') +  td")
                                     ?.TextContent ?? "").Trim();
                    var nmck = UtilsFromParsing.ParsePriceRosneft(nmckT);
                    var currency = "";
                    if (nmckT.Contains("руб"))
                    {
                        currency = "руб.";
                    }

                    var insertLot =
                        $"INSERT INTO {Program.Prefix}lot SET id_tender = @id_tender, lot_number = @lot_number, max_price = @max_price, currency = @currency";
                    var cmd18 = new MySqlCommand(insertLot, connect);
                    cmd18.Prepare();
                    cmd18.Parameters.AddWithValue("@id_tender", idTender);
                    cmd18.Parameters.AddWithValue("@lot_number", 1);
                    cmd18.Parameters.AddWithValue("@max_price", nmck);
                    cmd18.Parameters.AddWithValue("@currency", currency);
                    cmd18.ExecuteNonQuery();
                    var idLot = (int) cmd18.LastInsertedId;
                    var recName = (document.QuerySelector("td:contains('Требования к участникам') +  td")
                                          ?.TextContent ?? "").Trim();
                    GetRec(recName, connect, idLot);
                    if (!string.IsNullOrEmpty(purObjInfo))
                    {
                        var insertLotitem =
                            $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, name = @name, sum = @sum";
                        var cmd19 = new MySqlCommand(insertLotitem, connect);
                        cmd19.Prepare();
                        cmd19.Parameters.AddWithValue("@id_lot", idLot);
                        cmd19.Parameters.AddWithValue("@id_customer", customerId);
                        cmd19.Parameters.AddWithValue("@name", purObjInfo);
                        cmd19.Parameters.AddWithValue("@sum", nmck);
                        cmd19.ExecuteNonQuery();
                    }
                }
                else
                {
                    foreach (var l in lots)
                    {
                        var lotNum = 1;
                        var lotNumT = (l.QuerySelector("td.views-field-counter")?.TextContent ?? "").Trim();
                        if (!string.IsNullOrEmpty(lotNumT))
                        {
                            lotNumT = lotNumT.replace("Лот №", "");
                            lotNum = int.TryParse(lotNumT, out lotNum) ? int.Parse(lotNumT) : 1;
                        }

                        var urlLot = (l.QuerySelector("td.views-field-title > a")?.GetAttribute("href") ?? "").Trim();
                        if (!string.IsNullOrEmpty(urlLot))
                        {
                            var sl = DownloadString.DownL(urlLot);
                            if (string.IsNullOrEmpty(sl))
                            {
                                Log.Logger("Empty string in Parsing()", urlLot);
                                continue;
                            }

                            var parserL = new HtmlParser();
                            var docl = parserL.Parse(sl);
                            var nmckT = (docl.QuerySelector("td:contains('Сведения о начальной') +  td")
                                             ?.TextContent ?? "").Trim();
                            var nmck = UtilsFromParsing.ParsePriceRosneft(nmckT);
                            var currency = "";
                            if (nmckT.Contains("руб"))
                            {
                                currency = "руб.";
                            }

                            var insertLot =
                                $"INSERT INTO {Program.Prefix}lot SET id_tender = @id_tender, lot_number = @lot_number, max_price = @max_price, currency = @currency";
                            var cmd18 = new MySqlCommand(insertLot, connect);
                            cmd18.Prepare();
                            cmd18.Parameters.AddWithValue("@id_tender", idTender);
                            cmd18.Parameters.AddWithValue("@lot_number", lotNum);
                            cmd18.Parameters.AddWithValue("@max_price", nmck);
                            cmd18.Parameters.AddWithValue("@currency", currency);
                            cmd18.ExecuteNonQuery();
                            var idLot = (int) cmd18.LastInsertedId;
                            var recName = (document.QuerySelector("td:contains('Требования к участникам') +  td")
                                                  ?.TextContent ?? "").Trim();
                            GetRec(recName, connect, idLot);
                            var namePo = (docl.QuerySelector("td:contains('товар/работа') +  td")
                                              ?.TextContent ?? "").Trim();
                            if (!string.IsNullOrEmpty(namePo))
                            {
                                var insertLotitem =
                                    $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, name = @name, sum = @sum";
                                var cmd19 = new MySqlCommand(insertLotitem, connect);
                                cmd19.Prepare();
                                cmd19.Parameters.AddWithValue("@id_lot", idLot);
                                cmd19.Parameters.AddWithValue("@id_customer", customerId);
                                cmd19.Parameters.AddWithValue("@name", namePo);
                                cmd19.Parameters.AddWithValue("@sum", nmck);
                                cmd19.ExecuteNonQuery();
                            }

                            var deliveryPlace = (docl.QuerySelector("td:contains('Место поставки товара') +  td")
                                                     ?.TextContent ?? "").Trim();
                            if (!string.IsNullOrEmpty(deliveryPlace))
                            {
                                var insertCustomerRequirement =
                                    $"INSERT INTO {Program.Prefix}customer_requirement SET id_lot = @id_lot, id_customer = @id_customer, delivery_place = @delivery_place, max_price = @max_price";
                                var cmd16 = new MySqlCommand(insertCustomerRequirement, connect);
                                cmd16.Prepare();
                                cmd16.Parameters.AddWithValue("@id_lot", idLot);
                                cmd16.Parameters.AddWithValue("@id_customer", customerId);
                                cmd16.Parameters.AddWithValue("@delivery_place", deliveryPlace);
                                cmd16.Parameters.AddWithValue("@max_price", nmck);
                                cmd16.ExecuteNonQuery();
                            }
                        }
                    }
                }

                Tender.TenderKwords(connect, idTender);
                AddVerNumber(connect, PurNum, TypeFz);
            }
        }

        private void GetRec(string recName, MySqlConnection connect, int idLot)
        {
            if (!string.IsNullOrEmpty(recName))
            {
                var insertRequirement =
                    $"INSERT INTO {Program.Prefix}requirement SET id_lot = @id_lot, content = @content";
                var cmd30 = new MySqlCommand(insertRequirement, connect);
                cmd30.Prepare();
                cmd30.Parameters.AddWithValue("@id_lot", idLot);
                cmd30.Parameters.AddWithValue("@content", recName);
                cmd30.ExecuteNonQuery();
            }
        }

        private void GetDocs(IHtmlCollection<IElement> docs, MySqlConnection connect, int idTender)
        {
            foreach (var doc in docs)
            {
                var fName = (doc?.TextContent ?? "").Trim();
                var urlAttT = (doc?.GetAttribute("href") ?? "").Trim();
                var urlAtt = $"http://zakupki.rosneft.ru{urlAttT}";
                if (!string.IsNullOrEmpty(fName))
                {
                    var insertAttach =
                        $"INSERT INTO {Program.Prefix}attachment SET id_tender = @id_tender, file_name = @file_name, url = @url";
                    var cmd10 = new MySqlCommand(insertAttach, connect);
                    cmd10.Prepare();
                    cmd10.Parameters.AddWithValue("@id_tender", idTender);
                    cmd10.Parameters.AddWithValue("@file_name", fName);
                    cmd10.Parameters.AddWithValue("@url", urlAtt);
                    cmd10.ExecuteNonQuery();
                }
            }
        }

        private void GetEtp(MySqlConnection connect, out int idEtp)
        {
            var etpName = "ПАО \"НК \"Роснефть\"";
            var _etpUrl = "http://zakupki.rosneft.ru";
            var selectEtp = $"SELECT id_etp FROM {Program.Prefix}etp WHERE name = @name AND url = @url";
            var cmd7 = new MySqlCommand(selectEtp, connect);
            cmd7.Prepare();
            cmd7.Parameters.AddWithValue("@name", etpName);
            cmd7.Parameters.AddWithValue("@url", _etpUrl);
            var dt5 = new DataTable();
            var adapter5 = new MySqlDataAdapter {SelectCommand = cmd7};
            adapter5.Fill(dt5);
            if (dt5.Rows.Count > 0)
            {
                idEtp = (int) dt5.Rows[0].ItemArray[0];
            }
            else
            {
                var insertEtp =
                    $"INSERT INTO {Program.Prefix}etp SET name = @name, url = @url, conf=0";
                var cmd8 = new MySqlCommand(insertEtp, connect);
                cmd8.Prepare();
                cmd8.Parameters.AddWithValue("@name", etpName);
                cmd8.Parameters.AddWithValue("@url", _etpUrl);
                cmd8.ExecuteNonQuery();
                idEtp = (int) cmd8.LastInsertedId;
            }
        }

        private void GetPlacingWay(MySqlConnection connect, out int idPlacingWay)
        {
            if (!string.IsNullOrEmpty(PlacingWay))
            {
                var selectPlacingWay =
                    $"SELECT id_placing_way FROM {Program.Prefix}placing_way WHERE name = @name";
                var cmd5 = new MySqlCommand(selectPlacingWay, connect);
                cmd5.Prepare();
                cmd5.Parameters.AddWithValue("@name", PlacingWay);
                var dt4 = new DataTable();
                var adapter4 = new MySqlDataAdapter {SelectCommand = cmd5};
                adapter4.Fill(dt4);
                if (dt4.Rows.Count > 0)
                {
                    idPlacingWay = (int) dt4.Rows[0].ItemArray[0];
                }
                else
                {
                    var insertPlacingWay =
                        $"INSERT INTO {Program.Prefix}placing_way SET name= @name, conformity = @conformity";
                    var cmd6 = new MySqlCommand(insertPlacingWay, connect);
                    cmd6.Prepare();
                    var conformity = UtilsFromParsing.GetConformity(PlacingWay);
                    cmd6.Parameters.AddWithValue("@name", PlacingWay);
                    cmd6.Parameters.AddWithValue("@conformity", conformity);
                    cmd6.ExecuteNonQuery();
                    idPlacingWay = (int) cmd6.LastInsertedId;
                }
            }
            else
            {
                idPlacingWay = 0;
            }
        }
    }
}