using System;
using System.Data;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;

namespace ParserTenders.TenderDir
{
    public class TenderTypeMrsk
    {
        private TypeMrsk _tend;
        private string _etpUrl => "http://www.mrsksevzap.ru";
        private const int TypeFz = 9;

        public TenderTypeMrsk(TypeMrsk t)
        {
            _tend = t;
        }

        public void Parsing()
        {
            using (MySqlConnection connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                DateTime datePub = UtilsFromParsing.ParseDateMrsk(_tend.DatePub);
                DateTime dateUpd = UtilsFromParsing.ParseDateMrsk(_tend.DateUpd);
                if (dateUpd == DateTime.MinValue)
                {
                    dateUpd = datePub;
                }

                string selectTend =
                    $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND date_version = @date_version AND type_fz = @type_fz";
                MySqlCommand cmd = new MySqlCommand(selectTend, connect);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@purchase_number", _tend.IdTender);
                cmd.Parameters.AddWithValue("@date_version", dateUpd);
                cmd.Parameters.AddWithValue("@type_fz", TypeFz);
                DataTable dt = new DataTable();
                MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd};
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    //Log.Logger("This tender is exist in base", _tend.IdTender);
                    return;
                }

                string s = DownloadString.DownL(_tend.Href);
                if (String.IsNullOrEmpty(s))
                {
                    Log.Logger("Empty string in Parsing()", _tend.Href);
                    return;
                }

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(s);
                int cancelStatus = 0;
                string selectDateT =
                    $"SELECT id_tender, date_version, cancel FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND type_fz = @type_fz";
                MySqlCommand cmd2 = new MySqlCommand(selectDateT, connect);
                cmd2.Prepare();
                cmd2.Parameters.AddWithValue("@purchase_number", _tend.IdTender);
                cmd2.Parameters.AddWithValue("@type_fz", TypeFz);
                MySqlDataAdapter adapter2 = new MySqlDataAdapter {SelectCommand = cmd2};
                DataTable dt2 = new DataTable();
                adapter2.Fill(dt2);
                //Console.WriteLine(dt2.Rows.Count);
                foreach (DataRow row in dt2.Rows)
                {
                    //DateTime dateNew = DateTime.Parse(pr.DatePublished);

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
                string noticeVersion = "";
                string printForm = _tend.Href;
                int customerId = 0;
                int organiserId = 0;
                var navT = (HtmlNodeNavigator) htmlDoc.CreateNavigator();
                int idPlacingWay = 0;
                string pwName =
                    (navT?.SelectSingleNode(
                         "//table/tbody/tr[td[position()=1]/b= 'Способ размещения заказа']/td[last()]")?.Value ?? "")
                    .Trim();
                if (!String.IsNullOrEmpty(pwName))
                {
                    string selectPlacingWay =
                        $"SELECT id_placing_way FROM {Program.Prefix}placing_way WHERE name = @name";
                    MySqlCommand cmd5 = new MySqlCommand(selectPlacingWay, connect);
                    cmd5.Prepare();
                    cmd5.Parameters.AddWithValue("@name", pwName);
                    DataTable dt4 = new DataTable();
                    MySqlDataAdapter adapter4 = new MySqlDataAdapter {SelectCommand = cmd5};
                    adapter4.Fill(dt4);
                    if (dt4.Rows.Count > 0)
                    {
                        idPlacingWay = (int) dt4.Rows[0].ItemArray[0];
                    }
                    else
                    {
                        string insertPlacingWay =
                            $"INSERT INTO {Program.Prefix}placing_way SET name= @name";
                        MySqlCommand cmd6 = new MySqlCommand(insertPlacingWay, connect);
                        cmd6.Prepare();
                        cmd6.Parameters.AddWithValue("@name", pwName);
                        cmd6.ExecuteNonQuery();
                        idPlacingWay = (int) cmd6.LastInsertedId;
                    }
                }

                int idEtp = 0;
                string etpName = "МРСКСЗ";
                /*_etpUrl = "http://www.mrsksevzap.ru";*/
                string selectEtp = $"SELECT id_etp FROM {Program.Prefix}etp WHERE name = @name AND url = @url";
                MySqlCommand cmd7 = new MySqlCommand(selectEtp, connect);
                cmd7.Prepare();
                cmd7.Parameters.AddWithValue("@name", etpName);
                cmd7.Parameters.AddWithValue("@url", _etpUrl);
                DataTable dt5 = new DataTable();
                MySqlDataAdapter adapter5 = new MySqlDataAdapter {SelectCommand = cmd7};
                adapter5.Fill(dt5);
                if (dt5.Rows.Count > 0)
                {
                    idEtp = (int) dt5.Rows[0].ItemArray[0];
                }
                else
                {
                    string insertEtp =
                        $"INSERT INTO {Program.Prefix}etp SET name = @name, url = @url, conf=0";
                    MySqlCommand cmd8 = new MySqlCommand(insertEtp, connect);
                    cmd8.Prepare();
                    cmd8.Parameters.AddWithValue("@name", etpName);
                    cmd8.Parameters.AddWithValue("@url", _etpUrl);
                    cmd8.ExecuteNonQuery();
                    idEtp = (int) cmd8.LastInsertedId;
                }

                string _dateEnd =
                (navT?.SelectSingleNode("//table/tbody/tr[td[position()=1]/b= 'Дата завершения']/td[last()]")
                     ?.Value ?? "").Trim();
                DateTime dateEnd = UtilsFromParsing.ParseDateMrsk(_dateEnd);
                string purObjInfo =
                (navT?.SelectSingleNode("//table/tbody/tr[td[position()=1]/b= 'Наименование заказа']/td[last()]")
                     ?.Value ?? "").Trim();
                purObjInfo = System.Net.WebUtility.HtmlDecode(purObjInfo);
                string insertTender =
                    $"INSERT INTO {Program.Prefix}tender SET id_region = @id_region, id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, purchase_object_info = @purchase_object_info, type_fz = @type_fz, id_organizer = @id_organizer, id_placing_way = @id_placing_way, id_etp = @id_etp, end_date = @end_date, scoring_date = @scoring_date, bidding_date = @bidding_date, cancel = @cancel, date_version = @date_version, num_version = @num_version, notice_version = @notice_version, xml = @xml, print_form = @print_form";
                MySqlCommand cmd9 = new MySqlCommand(insertTender, connect);
                cmd9.Prepare();
                cmd9.Parameters.AddWithValue("@id_region", 0);
                cmd9.Parameters.AddWithValue("@id_xml", _tend.IdTender);
                cmd9.Parameters.AddWithValue("@purchase_number", _tend.IdTender);
                cmd9.Parameters.AddWithValue("@doc_publish_date", datePub);
                cmd9.Parameters.AddWithValue("@href", _tend.Href);
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
                cmd9.Parameters.AddWithValue("@xml", _tend.Href);
                cmd9.Parameters.AddWithValue("@print_form", printForm);
                int resInsertTender = cmd9.ExecuteNonQuery();
                int idTender = (int) cmd9.LastInsertedId;
                Program.AddMrsk++;
                var attach =
                    htmlDoc.DocumentNode.SelectNodes(
                        "//td[@class = 'b-cell b-cell_content_file']/div[@class = 'b-file']") ??
                    new HtmlNodeCollection(null);
                foreach (var att in attach)
                {
                    string fName = (att.SelectSingleNode(".//div[@class = 'b-file__main']/a/span[@class = 'b-text']")
                                        ?.InnerText ?? "").Trim();
                    string urlF = (att.SelectSingleNode(".//div[@class = 'b-file__main']/a[@href]")?.Attributes["href"]
                                       ?.Value ?? "")
                        .Trim();
                    urlF = $"{_etpUrl}{urlF}";
                    string Desc = (att.SelectSingleNode(".//div[@class = 'b-file__size']")?.InnerText ?? "").Trim();
                    Desc = System.Net.WebUtility.HtmlDecode(Desc);
                    if (!string.IsNullOrEmpty(fName))
                    {
                        string insertAttach =
                            $"INSERT INTO {Program.Prefix}attachment SET id_tender = @id_tender, file_name = @file_name, url = @url, description = @description";
                        MySqlCommand cmd10 = new MySqlCommand(insertAttach, connect);
                        cmd10.Prepare();
                        cmd10.Parameters.AddWithValue("@id_tender", idTender);
                        cmd10.Parameters.AddWithValue("@file_name", fName);
                        cmd10.Parameters.AddWithValue("@url", urlF);
                        cmd10.Parameters.AddWithValue("@description", Desc);
                        cmd10.ExecuteNonQuery();
                    }
                }

                int lotNum = 1;
                string prc =
                (navT?.SelectSingleNode(
                         "//table/tbody/tr[td[position()=1]/b= 'Сумма']/td[last()]")
                     ?.Value ??
                 "").Trim();
                prc = System.Net.WebUtility.HtmlDecode(prc);
                decimal maxP = UtilsFromParsing.ParsePriceMrsk(prc);
                string currency = "";
                if (!string.IsNullOrEmpty(prc))
                {
                    if (prc.Contains("руб"))
                    {
                        currency = "руб.";
                    }
                }

                string insertLot =
                    $"INSERT INTO {Program.Prefix}lot SET id_tender = @id_tender, lot_number = @lot_number, max_price = @max_price, currency = @currency, finance_source = @finance_source";
                MySqlCommand cmd18 = new MySqlCommand(insertLot, connect);
                cmd18.Prepare();
                cmd18.Parameters.AddWithValue("@id_tender", idTender);
                cmd18.Parameters.AddWithValue("@lot_number", lotNum);
                cmd18.Parameters.AddWithValue("@max_price", maxP);
                cmd18.Parameters.AddWithValue("@currency", currency);
                cmd18.Parameters.AddWithValue("@finance_source", "");
                cmd18.ExecuteNonQuery();
                int idLot = (int) cmd18.LastInsertedId;
                Tender.TenderKwords(connect, idTender);
                AddVerNumber(connect, _tend.IdTender);
            }
        }

        private void AddVerNumber(MySqlConnection connect, string purchaseNumber)
        {
            int verNum = 1;
            string selectTenders =
                $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchaseNumber AND type_fz = @type_fz ORDER BY id_tender ASC";
            MySqlCommand cmd1 = new MySqlCommand(selectTenders, connect);
            cmd1.Prepare();
            cmd1.Parameters.AddWithValue("@purchaseNumber", purchaseNumber);
            cmd1.Parameters.AddWithValue("@type_fz", TypeFz);
            DataTable dt1 = new DataTable();
            MySqlDataAdapter adapter1 = new MySqlDataAdapter {SelectCommand = cmd1};
            adapter1.Fill(dt1);
            if (dt1.Rows.Count > 0)
            {
                string updateTender =
                    $"UPDATE {Program.Prefix}tender SET num_version = @num_version WHERE id_tender = @id_tender";
                foreach (DataRow ten in dt1.Rows)
                {
                    int idTender = (int) ten["id_tender"];
                    MySqlCommand cmd2 = new MySqlCommand(updateTender, connect);
                    cmd2.Prepare();
                    cmd2.Parameters.AddWithValue("@id_tender", idTender);
                    cmd2.Parameters.AddWithValue("@num_version", verNum);
                    cmd2.ExecuteNonQuery();
                    verNum++;
                }
            }
        }
    }
}