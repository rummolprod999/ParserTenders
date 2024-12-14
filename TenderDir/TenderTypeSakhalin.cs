#region

using System;
using System.Data;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using MySql.Data.MySqlClient;

#endregion

namespace ParserTenders.TenderDir
{
    public class TenderTypeSakhalin : TenderBase
    {
        public event Action<int, bool> AddTenderSakhalin;
        private readonly string PurNum;
        private readonly string Url;
        private readonly DateTime DatePub;
        private readonly DateTime DateUpd;
        private const int TypeFz = 20;
        private readonly string etpName = "Сахалин Энерджи Инвестмент Компани Лтд.";
        private readonly string _etpUrl = "http://www.sakhalinenergy.ru";

        public TenderTypeSakhalin(string purNum, string url)
        {
            PurNum = purNum ?? throw new ArgumentNullException(nameof(purNum));
            Url = url ?? throw new ArgumentNullException(nameof(url));
            DatePub = DateTime.Now;
            DateUpd = DatePub;
            AddTenderSakhalin += delegate(int d, bool b)
            {
                if (d > 0)
                {
                    if (b)
                    {
                        Program.UpSakhalin++;
                    }
                    else
                    {
                        Program.AddSakhalin++;
                    }
                }

                else
                {
                    Log.Logger("Не удалось добавить Sakhalin", Url);
                }
            };
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
            var dateEnd = DateTime.MinValue;
            var dateEndT = (document.QuerySelector("td:contains('Финальная дата') +  td")?.TextContent ?? "").Trim();
            if (!string.IsNullOrEmpty(dateEndT))
            {
                dateEnd = dateEndT.ParseDateUn("dd.MM.yyyy");
            }

            using (var connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                var selectTend =
                    $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND end_date = @end_date AND type_fz = @type_fz";
                var cmd = new MySqlCommand(selectTend, connect);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@purchase_number", PurNum);
                cmd.Parameters.AddWithValue("@end_date", dateEnd);
                cmd.Parameters.AddWithValue("@type_fz", TypeFz);
                var dt = new DataTable();
                var adapter = new MySqlDataAdapter { SelectCommand = cmd };
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
                var adapter2 = new MySqlDataAdapter { SelectCommand = cmd2 };
                var dt2 = new DataTable();
                adapter2.Fill(dt2);
                foreach (DataRow row in dt2.Rows)
                {
                    //DateTime dateNew = DateTime.Parse(pr.DatePublished);
                    update = true;
                    if (DateUpd >= (DateTime)row["date_version"])
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
                    new MySqlCommandBuilder(adapter2) { ConflictOption = ConflictOption.OverwriteChanges };
                //Console.WriteLine(commandBuilder.GetUpdateCommand().CommandText);
                adapter2.Update(dt2);
                var noticeVersion =
                    (document.QuerySelector("td:contains('Дополнительная информация') +  td")?.TextContent ?? "")
                    .Trim();
                noticeVersion = $"Дополнительная информация: {noticeVersion}";
                var printForm = Url;
                var customerId = 0;
                var organiserId = 0;
                var orgFullName = etpName;
                if (!string.IsNullOrEmpty(orgFullName))
                {
                    var selectOrg =
                        $"SELECT id_organizer FROM {Program.Prefix}organizer WHERE full_name = @full_name";
                    var cmd3 = new MySqlCommand(selectOrg, connect);
                    cmd3.Prepare();
                    cmd3.Parameters.AddWithValue("@full_name", orgFullName);
                    var dt3 = new DataTable();
                    var adapter3 = new MySqlDataAdapter { SelectCommand = cmd3 };
                    adapter3.Fill(dt3);
                    if (dt3.Rows.Count > 0)
                    {
                        organiserId = (int)dt3.Rows[0].ItemArray[0];
                    }
                    else
                    {
                        var (inn, kpp) = ("6500010551", "");
                        var (factAddr, contactP, email, phone) = (
                            "693020, Сахалинская область, г Южно-Сахалинск, УЛ ДЗЕРЖИНСКОГО, 35", "",
                            "ask@sakhalinenergy.ru", "+7 4242 66 2000");
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
                        organiserId = (int)cmd4.LastInsertedId;
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
                        customerId = (int)reader7["id_customer"];
                        reader7.Close();
                    }
                    else
                    {
                        reader7.Close();
                        var insertCustomer =
                            $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, is223=1, inn = @inn";
                        var cmd14 = new MySqlCommand(insertCustomer, connect);
                        cmd14.Prepare();
                        var customerRegNumber = Guid.NewGuid().ToString();
                        cmd14.Parameters.AddWithValue("@reg_num", customerRegNumber);
                        cmd14.Parameters.AddWithValue("@full_name", orgFullName);
                        cmd14.Parameters.AddWithValue("@inn", "6500010551");
                        cmd14.ExecuteNonQuery();
                        customerId = (int)cmd14.LastInsertedId;
                    }
                }

                var idPlacingWay = 0;
                GetEtp(connect, out var idEtp, etpName, _etpUrl);
                var purObjInfo = (document.QuerySelector("div.page-cols > h1")?.TextContent ?? "").Trim();
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
                cmd9.Parameters.AddWithValue("@end_date", dateEnd);
                cmd9.Parameters.AddWithValue("@scoring_date", DateTime.MinValue);
                cmd9.Parameters.AddWithValue("@bidding_date", DateTime.MinValue);
                cmd9.Parameters.AddWithValue("@cancel", cancelStatus);
                cmd9.Parameters.AddWithValue("@date_version", DateUpd);
                cmd9.Parameters.AddWithValue("@num_version", 1);
                cmd9.Parameters.AddWithValue("@notice_version", noticeVersion);
                cmd9.Parameters.AddWithValue("@xml", Url);
                cmd9.Parameters.AddWithValue("@print_form", printForm);
                var resInsertTender = cmd9.ExecuteNonQuery();
                var idTender = (int)cmd9.LastInsertedId;
                AddTenderSakhalin?.Invoke(resInsertTender, update);
                var docs = document.QuerySelectorAll(
                    "td:contains('Дополнительная информация') +  td a");
                GetDocs(docs, connect, idTender);
                var insertLot =
                    $"INSERT INTO {Program.Prefix}lot SET id_tender = @id_tender, lot_number = @lot_number";
                var cmd18 = new MySqlCommand(insertLot, connect);
                cmd18.Prepare();
                cmd18.Parameters.AddWithValue("@id_tender", idTender);
                cmd18.Parameters.AddWithValue("@lot_number", 1);
                cmd18.ExecuteNonQuery();
                var idLot = (int)cmd18.LastInsertedId;
                var recName = (document.QuerySelector("td:contains('Требования') +  td")
                    ?.TextContent ?? "").Trim();
                if (!string.IsNullOrEmpty(recName))
                {
                    var recA = recName.Split('•');
                    foreach (var rc in recA)
                    {
                        GetRec(rc.Trim(), connect, idLot);
                    }
                }

                var deliveryPlace = (document.QuerySelector("td:contains('Местоположение') +  td")
                    ?.TextContent ?? "").Trim();
                var startD = (document.QuerySelector("td:contains('Планируемый период') +  td")
                    ?.TextContent ?? "").Trim();
                var startS = (document.QuerySelector("td:contains('Планируемый срок') +  td")
                    ?.TextContent ?? "").Trim();
                if (!string.IsNullOrEmpty(deliveryPlace) || !string.IsNullOrEmpty(startD) ||
                    !string.IsNullOrEmpty(startS))
                {
                    var deliveryTerm =
                        $"Планируемый период присуждения договора: {startD}, Планируемый срок действия договора: {startS}";
                    var insertCustomerRequirement =
                        $"INSERT INTO {Program.Prefix}customer_requirement SET id_lot = @id_lot, id_customer = @id_customer, delivery_place = @delivery_place, delivery_term = @delivery_term";
                    var cmd16 = new MySqlCommand(insertCustomerRequirement, connect);
                    cmd16.Prepare();
                    cmd16.Parameters.AddWithValue("@id_lot", idLot);
                    cmd16.Parameters.AddWithValue("@id_customer", customerId);
                    cmd16.Parameters.AddWithValue("@delivery_place", deliveryPlace);
                    cmd16.Parameters.AddWithValue("@delivery_term", deliveryTerm);
                    cmd16.ExecuteNonQuery();
                }

                var purObj = (document.QuerySelector("td:contains('Объём работ') +  td")
                    ?.TextContent ?? "").Trim();
                if (string.IsNullOrEmpty(purObj))
                {
                    purObj = (document.QuerySelector("td:contains('Объект') +  td")
                        ?.TextContent ?? "").Trim();
                }

                if (!string.IsNullOrEmpty(purObj))
                {
                    var purObjA = purObj.Split('•');
                    foreach (var po in purObjA)
                    {
                        if (!string.IsNullOrEmpty(po))
                        {
                            var insertLotitem =
                                $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, name = @name";
                            var cmd19 = new MySqlCommand(insertLotitem, connect);
                            cmd19.Prepare();
                            cmd19.Parameters.AddWithValue("@id_lot", idLot);
                            cmd19.Parameters.AddWithValue("@id_customer", customerId);
                            cmd19.Parameters.AddWithValue("@name", po.Trim());
                            cmd19.ExecuteNonQuery();
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
                if (fName.Contains("@"))
                {
                    continue;
                }

                var urlAttT = (doc?.GetAttribute("href") ?? "").Trim();
                var urlAtt = $"http://www.sakhalinenergy.ru{urlAttT}";
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
    }
}