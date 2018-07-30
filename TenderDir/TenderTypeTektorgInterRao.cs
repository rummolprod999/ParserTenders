using System;
using System.Data;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using MySql.Data.MySqlClient;

namespace ParserTenders.TenderDir
{
    public class TenderTypeTektorgInterRao : TenderBase, ITenderWeb
    {
        public TenderTypeTektorgInterRao(string etpName, string etpUrl, int typeFz, string urlTender)
        {
            EtpName = etpName ?? throw new ArgumentNullException(nameof(etpName));
            EtpUrl = etpUrl ?? throw new ArgumentNullException(nameof(etpUrl));
            TypeFz = typeFz;
            UrlTender = urlTender;
            AddTender += delegate(int d)
            {
                if (d > 0)
                    Program.AddTektorgInterRao++;
                else
                    Log.Logger($"Не удалось добавить {GetType().Name}", UrlTender);
            };
            UpdateTender += delegate(int d)
            {
                if (d > 0)
                    Program.UpTektorgGazprom++;
                else
                    Log.Logger("Не удалось обновить TektorgGazprom", UrlTender);
            };
        }

        public string UrlTender { get; }
        public string EtpName { get; set; }
        public string EtpUrl { get; set; }
        public int TypeFz { get; set; }
        public event Action<int> AddTender;
        public event Action<int> UpdateTender;

        public void Parsing()
        {
            string s = DownloadString.DownL(UrlTender);
            if (String.IsNullOrEmpty(s))
            {
                Log.Logger($"Empty string in {GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name}",
                    UrlTender);
            }

            var parser = new HtmlParser();
            var document = parser.Parse(s);
            var datePubT = (document.QuerySelector("td:contains('Дата публикации:') + td")?.TextContent ?? "").Trim();
            var datePub = datePubT.ParseDateUn("dd.MM.yyyy HH:mm 'GMT'z");
            var dateEndT = (document.QuerySelector("td:contains('Дата окончания приема заявок') + td")?.TextContent ??
                            "").Trim();
            var dateEnd = dateEndT.ParseDateUn("dd.MM.yyyy HH:mm 'GMT'z");
            if (datePub == DateTime.MinValue || dateEnd == DateTime.MinValue)
            {
                Log.Logger($"Empty dates in {GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name}",
                    UrlTender, datePubT, dateEndT);
                return;
            }

            var purNumT = (document.QuerySelector("h1.section-procurement__title")?.TextContent ?? "").Trim();
            var purNum = purNumT.GetDateFromRegex("Извещение о процедуре (.+)");
            if (string.IsNullOrEmpty(purNum))
            {
                Log.Logger($"Empty purNum in {GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name}",
                    UrlTender, purNumT);
                return;
            }

            string noticeVersion =
                (document.QuerySelector("td:contains('Текущая стадия:') +  td")?.TextContent ?? "")
                .Trim();
            using (MySqlConnection connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                string selectTend =
                    $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND end_date = @end_date AND type_fz = @type_fz AND notice_version = @notice_version";
                MySqlCommand cmd = new MySqlCommand(selectTend, connect);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@purchase_number", purNum);
                cmd.Parameters.AddWithValue("@end_date", dateEnd);
                cmd.Parameters.AddWithValue("@type_fz", TypeFz);
                cmd.Parameters.AddWithValue("@notice_version", noticeVersion);
                DataTable dt = new DataTable();
                MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd};
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    //Log.Logger("This tender is exist in base", PurNum);
                    return;
                }

                var dateUpd = DateTime.Now;
                int cancelStatus = 0;
                var update = false;
                string selectDateT =
                    $"SELECT id_tender, date_version, cancel FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND type_fz = @type_fz";
                MySqlCommand cmd2 = new MySqlCommand(selectDateT, connect);
                cmd2.Prepare();
                cmd2.Parameters.AddWithValue("@purchase_number", purNum);
                cmd2.Parameters.AddWithValue("@type_fz", TypeFz);
                MySqlDataAdapter adapter2 = new MySqlDataAdapter {SelectCommand = cmd2};
                DataTable dt2 = new DataTable();
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

                MySqlCommandBuilder commandBuilder =
                    new MySqlCommandBuilder(adapter2) {ConflictOption = ConflictOption.OverwriteChanges};
                //Console.WriteLine(commandBuilder.GetUpdateCommand().CommandText);
                adapter2.Update(dt2);
                string printForm = UrlTender;
                int customerId = 0;
                int organiserId = 0;
                var orgFullName =
                    (document.QuerySelector("td:contains('Наименование организатора:') +  td")?.TextContent ?? "")
                    .Trim();
                if (!string.IsNullOrEmpty(orgFullName))
                {
                    string selectOrg =
                        $"SELECT id_organizer FROM {Program.Prefix}organizer WHERE full_name = @full_name";
                    MySqlCommand cmd3 = new MySqlCommand(selectOrg, connect);
                    cmd3.Prepare();
                    cmd3.Parameters.AddWithValue("@full_name", orgFullName);
                    DataTable dt3 = new DataTable();
                    MySqlDataAdapter adapter3 = new MySqlDataAdapter {SelectCommand = cmd3};
                    adapter3.Fill(dt3);
                    if (dt3.Rows.Count > 0)
                    {
                        organiserId = (int) dt3.Rows[0].ItemArray[0];
                    }
                    else
                    {
                        var phone = (document.QuerySelector("td:contains('Контактный телефон:') +  td")?.TextContent ??
                                     "")
                            .Trim();
                        var email = (document.QuerySelector("td:contains('Адрес электронной почты:') +  td")
                                         ?.TextContent ?? "")
                            .Trim();
                        var contactPerson =
                            (document.QuerySelector("td:contains('ФИО контактного лица:') +  td")?.TextContent ?? "")
                            .Trim();
                        string addOrganizer =
                            $"INSERT INTO {Program.Prefix}organizer SET full_name = @full_name, contact_phone = @contact_phone, contact_person = @contact_person, contact_email = @contact_email";
                        MySqlCommand cmd4 = new MySqlCommand(addOrganizer, connect);
                        cmd4.Prepare();
                        cmd4.Parameters.AddWithValue("@full_name", orgFullName);
                        cmd4.Parameters.AddWithValue("@contact_phone", phone);
                        cmd4.Parameters.AddWithValue("@contact_person", contactPerson);
                        cmd4.Parameters.AddWithValue("@contact_email", email);
                        cmd4.ExecuteNonQuery();
                        organiserId = (int) cmd4.LastInsertedId;
                    }
                }

                PlacingWay = (document.QuerySelector("td:contains('Способ закупки:') +  td")?.TextContent ?? "")
                    .Trim();
                GetPlacingWay(connect, out int idPlacingWay);
                GetEtp(connect, out int idEtp, EtpName, EtpUrl);
                var purObjInfo =
                    (document.QuerySelector("span:contains('Наименование закупки:') +  span")?.TextContent ?? "")
                    .Trim();
                string insertTender =
                    $"INSERT INTO {Program.Prefix}tender SET id_region = @id_region, id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, purchase_object_info = @purchase_object_info, type_fz = @type_fz, id_organizer = @id_organizer, id_placing_way = @id_placing_way, id_etp = @id_etp, end_date = @end_date, scoring_date = @scoring_date, bidding_date = @bidding_date, cancel = @cancel, date_version = @date_version, num_version = @num_version, notice_version = @notice_version, xml = @xml, print_form = @print_form";
                MySqlCommand cmd9 = new MySqlCommand(insertTender, connect);
                cmd9.Prepare();
                cmd9.Parameters.AddWithValue("@id_region", 0);
                cmd9.Parameters.AddWithValue("@id_xml", purNum);
                cmd9.Parameters.AddWithValue("@purchase_number", purNum);
                cmd9.Parameters.AddWithValue("@doc_publish_date", datePub);
                cmd9.Parameters.AddWithValue("@href", UrlTender);
                cmd9.Parameters.AddWithValue("@purchase_object_info", purObjInfo);
                cmd9.Parameters.AddWithValue("@type_fz", TypeFz);
                cmd9.Parameters.AddWithValue("@id_organizer", organiserId);
                cmd9.Parameters.AddWithValue("@id_placing_way", idPlacingWay);
                cmd9.Parameters.AddWithValue("@id_etp", idEtp);
                cmd9.Parameters.AddWithValue("@end_date", dateEnd);
                cmd9.Parameters.AddWithValue("@scoring_date", DateTime.MinValue);
                cmd9.Parameters.AddWithValue("@bidding_date", DateTime.MinValue);
                cmd9.Parameters.AddWithValue("@cancel", cancelStatus);
                cmd9.Parameters.AddWithValue("@date_version", dateUpd);
                cmd9.Parameters.AddWithValue("@num_version", 1);
                cmd9.Parameters.AddWithValue("@notice_version", noticeVersion);
                cmd9.Parameters.AddWithValue("@xml", UrlTender);
                cmd9.Parameters.AddWithValue("@print_form", printForm);
                int resInsertTender = cmd9.ExecuteNonQuery();
                int idTender = (int) cmd9.LastInsertedId;
                if (update)
                {
                    UpdateTender?.Invoke(resInsertTender);
                }
                else
                {
                    AddTender?.Invoke(resInsertTender);
                }

                var docs = document.QuerySelectorAll(
                    "#documentation > a");
                GetDocs(docs, connect, idTender);
                var lots = document.QuerySelectorAll(
                    "div.procedure__lots > div.procedure__lot");
                foreach (var lot in lots)
                {
                    var lotNumT = (lot.QuerySelector("div.procedure__lot-header span")?.TextContent ?? "").Trim();
                    lotNumT = lotNumT.GetDateFromRegex(@"Лот (\d+)");
                    int.TryParse(lotNumT, out int lotNum);
                    if (lotNum == 0) lotNum = 1;
                    var currency = (lot.QuerySelector("td:contains('Валюта:') +  td")?.TextContent ?? "").Trim();
                    var nmckT = (lot.QuerySelector("td:contains('Начальная цена:') +  td")?.TextContent ?? "0.0")
                        .Trim();
                    var nmck = UtilsFromParsing.ParsePriceRosneft(nmckT);
                    string insertLot =
                        $"INSERT INTO {Program.Prefix}lot SET id_tender = @id_tender, lot_number = @lot_number, max_price = @max_price, currency = @currency";
                    MySqlCommand cmd18 = new MySqlCommand(insertLot, connect);
                    cmd18.Prepare();
                    cmd18.Parameters.AddWithValue("@id_tender", idTender);
                    cmd18.Parameters.AddWithValue("@lot_number", lotNum);
                    cmd18.Parameters.AddWithValue("@max_price", nmck);
                    cmd18.Parameters.AddWithValue("@currency", currency);
                    cmd18.ExecuteNonQuery();
                    int idLot = (int) cmd18.LastInsertedId;
                    var customerFullName =
                        (lot.QuerySelector("td:contains('Заказчик:') +  td")?.TextContent ?? "0.0").Trim();
                    if (!string.IsNullOrEmpty(customerFullName))
                    {
                        string selectCustomer =
                            $"SELECT id_customer FROM {Program.Prefix}customer WHERE full_name = @full_name";
                        MySqlCommand cmd13 = new MySqlCommand(selectCustomer, connect);
                        cmd13.Prepare();
                        cmd13.Parameters.AddWithValue("@full_name", customerFullName);
                        MySqlDataReader reader7 = cmd13.ExecuteReader();
                        if (reader7.HasRows)
                        {
                            reader7.Read();
                            customerId = (int) reader7["id_customer"];
                            reader7.Close();
                        }
                        else
                        {
                            reader7.Close();
                            string insertCustomer =
                                $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, is223=1";
                            MySqlCommand cmd14 = new MySqlCommand(insertCustomer, connect);
                            cmd14.Prepare();
                            var customerRegNumber = Guid.NewGuid().ToString();
                            cmd14.Parameters.AddWithValue("@reg_num", customerRegNumber);
                            cmd14.Parameters.AddWithValue("@full_name", customerFullName);
                            cmd14.ExecuteNonQuery();
                            customerId = (int) cmd14.LastInsertedId;
                        }
                    }

                    var purName =
                        (lot.QuerySelector("td:contains('Предмет договора:') +  td")?.TextContent ?? "").Trim();
                    var okpd2Temp =
                        (lot.QuerySelector("td:contains('Код классификатора ОКДП/ОКПД2') +  td")?.TextContent ?? "")
                        .Trim();
                    var okpd2Code = okpd2Temp.GetDateFromRegex(@"^(\d[\.|\d]*\d)");
                    var okpd2GroupCode = 0;
                    var okpd2GroupLevel1Code = "";
                    if (!String.IsNullOrEmpty(okpd2Code))
                    {
                        Tender.GetOkpd(okpd2Code, out okpd2GroupCode, out okpd2GroupLevel1Code);
                    }

                    var okpdName = okpd2Temp.GetDateFromRegex(@"^\d[\.|\d]*\d (.*)$");
                    if (!string.IsNullOrEmpty(purName))
                    {
                        string insertLotitem =
                            $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, name = @name, sum = @sum, okpd2_code = @okpd2_code, okpd2_group_code = @okpd2_group_code, okpd2_group_level1_code = @okpd2_group_level1_code, okpd_name = @okpd_name";
                        MySqlCommand cmd19 = new MySqlCommand(insertLotitem, connect);
                        cmd19.Prepare();
                        cmd19.Parameters.AddWithValue("@id_lot", idLot);
                        cmd19.Parameters.AddWithValue("@id_customer", customerId);
                        cmd19.Parameters.AddWithValue("@name", purName);
                        cmd19.Parameters.AddWithValue("@sum", nmck);
                        cmd19.Parameters.AddWithValue("@okpd2_code", okpd2Code);
                        cmd19.Parameters.AddWithValue("@okpd2_group_code", okpd2GroupCode);
                        cmd19.Parameters.AddWithValue("@okpd2_group_level1_code", okpd2GroupLevel1Code);
                        cmd19.Parameters.AddWithValue("@okpd_name", okpdName);
                        cmd19.ExecuteNonQuery();
                    }

                    var appGuarAT = (lot.QuerySelector("td:contains('Обеспечение заявки:') +  td")?.TextContent ?? "")
                        .Trim();
                    var appGuarA = UtilsFromParsing.ParsePriceRosneft(appGuarAT);
                    if (appGuarA != 0.0m)
                    {
                        string insertCustomerRequirement =
                            $"INSERT INTO {Program.Prefix}customer_requirement SET id_lot = @id_lot, id_customer = @id_customer, application_guarantee_amount = @application_guarantee_amount, max_price = @max_price";
                        MySqlCommand cmd16 = new MySqlCommand(insertCustomerRequirement, connect);
                        cmd16.Prepare();
                        cmd16.Parameters.AddWithValue("@id_lot", idLot);
                        cmd16.Parameters.AddWithValue("@id_customer", customerId);
                        cmd16.Parameters.AddWithValue("@application_guarantee_amount", appGuarA);
                        cmd16.Parameters.AddWithValue("@max_price", nmck);
                        cmd16.ExecuteNonQuery();
                    }
                }

                Tender.TenderKwords(connect, idTender);
                AddVerNumber(connect, purNum, TypeFz);
            }
        }

        private void GetDocs(IHtmlCollection<IElement> docs, MySqlConnection connect, int idTender)
        {
            foreach (var doc in docs)
            {
                var fName = (doc?.TextContent ?? "").Trim();
                var urlAttT = (doc?.GetAttribute("href") ?? "").Trim();
                var urlAtt = $"https://www.tektorg.ru{urlAttT}";
                if (!string.IsNullOrEmpty(fName))
                {
                    string insertAttach =
                        $"INSERT INTO {Program.Prefix}attachment SET id_tender = @id_tender, file_name = @file_name, url = @url";
                    MySqlCommand cmd10 = new MySqlCommand(insertAttach, connect);
                    cmd10.Prepare();
                    cmd10.Parameters.AddWithValue("@id_tender", idTender);
                    cmd10.Parameters.AddWithValue("@file_name", fName);
                    cmd10.Parameters.AddWithValue("@url", urlAtt);
                    cmd10.ExecuteNonQuery();
                }
            }
        }
    }
}