using System;
using System.Data;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using static System.Console;

namespace ParserTenders
{
    public class GntWebTender
    {
        public string UrlTender;
        public DateTime DatePub;
        public DateTime DateOpen;
        public DateTime DateRes;
        public DateTime DateEnd;
        public string Entity;
        public string UrlOrg;
        public decimal MaxPrice;
        public TypeGnt TypeGnT;

        public GntWebTender()
        {
        }

        public void Parse()
        {
            string str = DownloadString.DownL1251(UrlTender);
            if (!string.IsNullOrEmpty(str))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(str);
                string eis = (htmlDoc.DocumentNode.SelectSingleNode("//td[@class = \"fname\"]").InnerText ?? "").Trim();
                //WriteLine(eis);
                if (eis == "Номер извещения в ЕИС:")
                {
                    string num =
                        (htmlDoc.DocumentNode.SelectSingleNode("//tr[@class = \"c1\"]/td/a[@href]").InnerText ?? "")
                        .Trim();
                    Log.Logger("Tender exist on zakupki.gov", num);
                    return;
                }

                string _pNum = (htmlDoc.DocumentNode.SelectSingleNode("//tr[@class = \"thead\"]/td[@colspan = \"2\"]")
                                    .InnerText ?? "").Trim();
                string pNum = "";
                try
                {
                    pNum = Regex.Match(_pNum, @"\d+").Value;
                }
                catch
                {
                    //ignore
                }

                //WriteLine(pNum);
                if (string.IsNullOrEmpty(pNum))
                {
                    Log.Logger("Not extract purchase number", _pNum);
                    return;
                }

                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    string selectTend =
                        $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND date_version = @date_version AND type_fz = 6";
                    MySqlCommand cmd = new MySqlCommand(selectTend, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@purchase_number", pNum);
                    cmd.Parameters.AddWithValue("@date_version", DatePub);
                    DataTable dt = new DataTable();
                    MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd};
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        Log.Logger("This tender is exist in base", pNum);
                        //return;
                        //TODO clear this comments
                    }

                    int cancelStatus = 0;
                    string selectDateT =
                        $"SELECT id_tender, date_version, cancel FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND type_fz = 6";
                    MySqlCommand cmd2 = new MySqlCommand(selectDateT, connect);
                    cmd2.Prepare();
                    cmd2.Parameters.AddWithValue("@purchase_number", pNum);
                    MySqlDataAdapter adapter2 = new MySqlDataAdapter {SelectCommand = cmd2};
                    DataTable dt2 = new DataTable();
                    adapter2.Fill(dt2);
                    //Console.WriteLine(dt2.Rows.Count);
                    foreach (DataRow row in dt2.Rows)
                    {
                        //DateTime dateNew = DateTime.Parse(pr.DatePublished);

                        if (DateOpen >= (DateTime) row["date_version"])
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
                    string printForm = UrlTender;
                    int customerId = 0;
                    int organiserId = 0;
                    string _UrlOrg =
                    (htmlDoc.DocumentNode
                         .SelectSingleNode("//td/a[@title = \"Просмотреть информационную карту участника\"]")
                         ?.Attributes["href"].Value ?? "").Trim();
                    UrlOrg = $"{ParserGntWeb._site}{_UrlOrg}";
                    //WriteLine(UrlOrg);
                    string orgS = DownloadString.DownL1251(UrlOrg);
                    if (!string.IsNullOrEmpty(orgS))
                    {
                        var htmlDocOrg = new HtmlDocument();
                        htmlDocOrg.LoadHtml(orgS);
                        var navigator = (HtmlNodeNavigator) htmlDocOrg.CreateNavigator();
                        string orgFullName =
                            (navigator?.SelectSingleNode(
                                 "//tr[td [position()=1]= \"Полное наименование организации:\"]/td[last()]")?.Value ??
                             "")
                            .Trim();
                        orgFullName = System.Net.WebUtility.HtmlDecode(orgFullName);
                        //WriteLine(orgFullName);
                        string orgInn =
                            (navigator?.SelectSingleNode("//tr[td [position()=1]= \"ИНН:\"]/td[last()]")?.Value ?? "")
                            .Trim();
                        string orgKpp =
                            (navigator?.SelectSingleNode("//tr[td [position()=1]= \"КПП:\"]/td[last()]")?.Value ?? "")
                            .Trim();
                        string orgPostAdr =
                        (navigator?.SelectSingleNode("//tr[td [position()=1]= \"Почтовый адрес:\"]/td[last()]")
                             ?.Value ?? "").Trim();
                        string orgFactAdr =
                        (navigator?.SelectSingleNode("//tr[td [position()=1]= \"Юридический адрес:\"]/td[last()]")
                             ?.Value ?? "").Trim();
                        string orgTel =
                        (navigator?.SelectSingleNode(
                                 "//tr[td [position()=1]= \"Телефоны и факсы организации\"]/following-sibling::tr [position()=1]/td[position()=2]")
                             ?.Value ?? "").Trim();
                        orgTel = System.Net.WebUtility.HtmlDecode(orgTel);
                        //WriteLine(orgTel);
                        if (!String.IsNullOrEmpty(orgInn))
                        {
                            string selectOrg =
                                $"SELECT id_organizer FROM {Program.Prefix}organizer WHERE inn = @inn AND kpp = @kpp";
                            MySqlCommand cmd3 = new MySqlCommand(selectOrg, connect);
                            cmd3.Prepare();
                            cmd3.Parameters.AddWithValue("@inn", orgInn);
                            cmd3.Parameters.AddWithValue("@kpp", orgKpp);
                            DataTable dt3 = new DataTable();
                            MySqlDataAdapter adapter3 = new MySqlDataAdapter {SelectCommand = cmd3};
                            adapter3.Fill(dt3);
                            if (dt3.Rows.Count > 0)
                            {
                                organiserId = (int) dt3.Rows[0].ItemArray[0];
                            }
                            else
                            {
                                string addOrganizer =
                                    $"INSERT INTO {Program.Prefix}organizer SET full_name = @full_name, post_address = @post_address, fact_address = @fact_address, inn = @inn, kpp = @kpp, contact_phone = @contact_phone";
                                MySqlCommand cmd4 = new MySqlCommand(addOrganizer, connect);
                                cmd4.Prepare();
                                cmd4.Parameters.AddWithValue("@full_name", orgFullName);
                                cmd4.Parameters.AddWithValue("@post_address", orgPostAdr);
                                cmd4.Parameters.AddWithValue("@fact_address", orgFactAdr);
                                cmd4.Parameters.AddWithValue("@inn", orgInn);
                                cmd4.Parameters.AddWithValue("@kpp", orgKpp);
                                cmd4.Parameters.AddWithValue("@contact_phone", orgTel);
                                cmd4.ExecuteNonQuery();
                                organiserId = (int) cmd4.LastInsertedId;
                            }
                        }
                    }

                    int idPlacingWay = 0;
                    string pwName = "";
                    switch (TypeGnT.Type)
                    {
                        case GntType.ProposalRequest:
                            pwName = "Запрос предложений";
                            break;
                        default:
                            pwName = "";
                            break;
                    }

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

                    int idEtp = 0;
                    string etpName = "ГАЗНЕФТЕТОРГ.РУ";
                    string etpUrl = ParserGntWeb._site;
                    string selectEtp = $"SELECT id_etp FROM {Program.Prefix}etp WHERE name = @name AND url = @url";
                    MySqlCommand cmd7 = new MySqlCommand(selectEtp, connect);
                    cmd7.Prepare();
                    cmd7.Parameters.AddWithValue("@name", etpName);
                    cmd7.Parameters.AddWithValue("@url", etpUrl);
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
                        cmd8.Parameters.AddWithValue("@url", etpUrl);
                        cmd8.ExecuteNonQuery();
                        idEtp = (int) cmd8.LastInsertedId;
                    }

                    string insertTender =
                        $"INSERT INTO {Program.Prefix}tender SET id_region = @id_region, id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, purchase_object_info = @purchase_object_info, type_fz = @type_fz, id_organizer = @id_organizer, id_placing_way = @id_placing_way, id_etp = @id_etp, end_date = @end_date, scoring_date = @scoring_date, bidding_date = @bidding_date, cancel = @cancel, date_version = @date_version, num_version = @num_version, notice_version = @notice_version, xml = @xml, print_form = @print_form";
                    MySqlCommand cmd9 = new MySqlCommand(insertTender, connect);
                    cmd9.Prepare();
                    cmd9.Parameters.AddWithValue("@id_region", 0);
                    cmd9.Parameters.AddWithValue("@id_xml", pNum);
                    cmd9.Parameters.AddWithValue("@purchase_number", pNum);
                    cmd9.Parameters.AddWithValue("@doc_publish_date", DatePub);
                    cmd9.Parameters.AddWithValue("@href", UrlTender);
                    cmd9.Parameters.AddWithValue("@purchase_object_info", Entity);
                    cmd9.Parameters.AddWithValue("@type_fz", 6);
                    cmd9.Parameters.AddWithValue("@id_organizer", organiserId);
                    cmd9.Parameters.AddWithValue("@id_placing_way", idPlacingWay);
                    cmd9.Parameters.AddWithValue("@id_etp", idEtp);
                    cmd9.Parameters.AddWithValue("@end_date", DateOpen);
                    cmd9.Parameters.AddWithValue("@scoring_date", DateRes);
                    cmd9.Parameters.AddWithValue("@bidding_date", DateTime.MinValue);
                    cmd9.Parameters.AddWithValue("@cancel", cancelStatus);
                    cmd9.Parameters.AddWithValue("@date_version", DatePub);
                    cmd9.Parameters.AddWithValue("@num_version", 1);
                    cmd9.Parameters.AddWithValue("@notice_version", noticeVersion);
                    cmd9.Parameters.AddWithValue("@xml", UrlTender);
                    cmd9.Parameters.AddWithValue("@print_form", printForm);
                    int resInsertTender = cmd9.ExecuteNonQuery();
                    int idTender = (int) cmd9.LastInsertedId;
                    Program.AddGntWeb++;
                    var navT = (HtmlNodeNavigator) htmlDoc.CreateNavigator();
                    string urlAtt = (navT?.SelectSingleNode("//td[a=\"Документация по торгам\"]/a/@href")?.Value ?? "")
                        .Trim();
                    if (!string.IsNullOrEmpty(urlAtt))
                    {
                        urlAtt = $"{ParserGntWeb._site}{urlAtt}";
                        string strAtt = DownloadString.DownL1251(urlAtt);
                        if (!string.IsNullOrEmpty(strAtt))
                        {
                            var htmlAtt = new HtmlDocument();
                            htmlAtt.LoadHtml(strAtt);
                            var attach = htmlAtt.DocumentNode.SelectNodes("//tr[@class = \"file\"]") ??
                                         new HtmlNodeCollection(null);
                            foreach (var att in attach)
                            {
                                string fName = (att.SelectSingleNode("td[2]/a").InnerText ?? "").Trim();
                                string urlF = (att.SelectSingleNode("td[2]/a[@href]")?.Attributes["href"].Value ?? "")
                                    .Trim();
                                string Desc = (att.SelectSingleNode("td[3]").InnerText ?? "").Trim();
                                Desc = System.Net.WebUtility.HtmlDecode(Desc);
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
                    }
                    string finSource = (navT?.SelectSingleNode("//tr[td [position()=1]= \"Источник финансирования:\"]/td[last()]")?.Value ?? "").Trim();
                    int lotNum = 1;
                    var lots = htmlDoc.DocumentNode.SelectNodes("//div[@class = \"lot_info\"]") ??
                                 new HtmlNodeCollection(null);
                    foreach (var l in lots)
                    {
                        var navL = (HtmlNodeNavigator) l.CreateNavigator();
                        string prc =
                        (navL?.SelectSingleNode(
                             "//tr[td [position()=1]= \"Начальная (максимальная) цена договора:\"]/td[last()]")?.Value ??
                         "").Trim();
                        prc = System.Net.WebUtility.HtmlDecode(prc);
                        decimal maxP = UtilsFromParsing.ParsePrice(prc);
                        string currency = "";
                        if (!string.IsNullOrEmpty(prc))
                        {
                            string[] arrCur = prc.Split(new string[]{"&nbsp;"}, StringSplitOptions.RemoveEmptyEntries);
                            if (arrCur.Length > 0)
                            {
                                currency = arrCur[arrCur.Length - 2];
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
                        cmd18.Parameters.AddWithValue("@finance_source", finSource);
                        cmd18.ExecuteNonQuery();
                        int idLot = (int) cmd18.LastInsertedId;
                        lotNum++;
                    }
                    
                }
            }
        }
    }
}