using System;
using System.Data;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using ParserTenders.ParserDir;
using ParserTenders.TenderDir;

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
            var str = DownloadString.DownL1251(UrlTender);
            if (!string.IsNullOrEmpty(str))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(str);
                var eis = (htmlDoc.DocumentNode.SelectSingleNode("//td[@class = \"fname\"]")?.InnerText ?? "")
                    .Trim();
                //WriteLine(eis);
                if (eis == "Номер извещения в ЕИС:")
                {
                    var num =
                        (htmlDoc.DocumentNode.SelectSingleNode("//tr[@class = \"c1\"]/td/a[@href]")?.InnerText ?? "")
                        .Trim();
                    //Log.Logger("Tender exist on zakupki.gov", num);
                    return;
                }

                var _pNum = (htmlDoc.DocumentNode.SelectSingleNode("//tr[@class = \"thead\"]/td[@colspan = \"2\"]")?
                                    .InnerText ?? "").Trim();
                var pNum = "";
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
                    //Log.Logger("Not extract purchase number", _pNum);
                    return;
                }

                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    var selectTend =
                        $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND date_version = @date_version AND end_date = @end_date AND type_fz = 6";
                    var cmd = new MySqlCommand(selectTend, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@purchase_number", pNum);
                    cmd.Parameters.AddWithValue("@date_version", DatePub);
                    cmd.Parameters.AddWithValue("@end_date", DateOpen);
                    var dt = new DataTable();
                    var adapter = new MySqlDataAdapter {SelectCommand = cmd};
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        Log.Logger("This tender is exist in base", pNum);
                        return;
                    }

                    var cancelStatus = 0;
                    var update = false;
                    var selectDateT =
                        $"SELECT id_tender, date_version, cancel FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number AND type_fz = 6";
                    var cmd2 = new MySqlCommand(selectDateT, connect);
                    cmd2.Prepare();
                    cmd2.Parameters.AddWithValue("@purchase_number", pNum);
                    var adapter2 = new MySqlDataAdapter {SelectCommand = cmd2};
                    var dt2 = new DataTable();
                    adapter2.Fill(dt2);
                    //Console.WriteLine(dt2.Rows.Count);
                    foreach (DataRow row in dt2.Rows)
                    {
                        //DateTime dateNew = DateTime.Parse(pr.DatePublished);
                        update = true;
                        if (DatePub >= (DateTime) row["date_version"])
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
                    var printForm = UrlTender;
                    var customerId = 0;
                    var organiserId = 0;
                    var _UrlOrg =
                        (htmlDoc.DocumentNode
                             .SelectSingleNode("//td/a[@title = \"Просмотреть информационную карту участника\"]")
                             ?.Attributes["href"].Value ?? "").Trim();
                    UrlOrg = $"{ParserGntWeb._site}{_UrlOrg}";
                    //WriteLine(UrlOrg);
                    var orgS = DownloadString.DownL1251(UrlOrg);
                    if (!string.IsNullOrEmpty(orgS))
                    {
                        var htmlDocOrg = new HtmlDocument();
                        htmlDocOrg.LoadHtml(orgS);
                        var navigator = (HtmlNodeNavigator) htmlDocOrg.CreateNavigator();
                        var orgFullName =
                            (navigator?.SelectSingleNode(
                                 "//tr[td [position()=1]= \"Полное наименование организации:\"]/td[last()]")?.Value ??
                             "")
                            .Trim();
                        orgFullName = System.Net.WebUtility.HtmlDecode(orgFullName);
                        //WriteLine(orgFullName);
                        var orgInn =
                            (navigator?.SelectSingleNode("//tr[td [position()=1]= \"ИНН:\"]/td[last()]")?.Value ?? "")
                            .Trim();
                        var orgKpp =
                            (navigator?.SelectSingleNode("//tr[td [position()=1]= \"КПП:\"]/td[last()]")?.Value ?? "")
                            .Trim();
                        var orgPostAdr =
                            (navigator?.SelectSingleNode("//tr[td [position()=1]= \"Почтовый адрес:\"]/td[last()]")
                                 ?.Value ?? "").Trim();
                        var orgFactAdr =
                            (navigator?.SelectSingleNode("//tr[td [position()=1]= \"Юридический адрес:\"]/td[last()]")
                                 ?.Value ?? "").Trim();
                        var orgTel =
                            (navigator?.SelectSingleNode(
                                     "//tr[td [position()=1]= \"Телефоны и факсы организации\"]/following-sibling::tr [position()=1]/td[position()=2]")
                                 ?.Value ?? "").Trim();
                        orgTel = System.Net.WebUtility.HtmlDecode(orgTel);
                        //WriteLine(orgTel);
                        if (!string.IsNullOrEmpty(orgInn))
                        {
                            var selectOrg =
                                $"SELECT id_organizer FROM {Program.Prefix}organizer WHERE inn = @inn AND kpp = @kpp";
                            var cmd3 = new MySqlCommand(selectOrg, connect);
                            cmd3.Prepare();
                            cmd3.Parameters.AddWithValue("@inn", orgInn);
                            cmd3.Parameters.AddWithValue("@kpp", orgKpp);
                            var dt3 = new DataTable();
                            var adapter3 = new MySqlDataAdapter {SelectCommand = cmd3};
                            adapter3.Fill(dt3);
                            if (dt3.Rows.Count > 0)
                            {
                                organiserId = (int) dt3.Rows[0].ItemArray[0];
                            }
                            else
                            {
                                var addOrganizer =
                                    $"INSERT INTO {Program.Prefix}organizer SET full_name = @full_name, post_address = @post_address, fact_address = @fact_address, inn = @inn, kpp = @kpp, contact_phone = @contact_phone";
                                var cmd4 = new MySqlCommand(addOrganizer, connect);
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

                    var idPlacingWay = 0;
                    var pwName = "";
                    switch (TypeGnT.Type)
                    {
                        case GntType.ProposalRequest:
                            pwName = "Запрос предложений";
                            break;
                        case GntType.Tender:
                            pwName = "Конкурс";
                            break;
                        default:
                            pwName = "";
                            break;
                    }

                    var selectPlacingWay =
                        $"SELECT id_placing_way FROM {Program.Prefix}placing_way WHERE name = @name";
                    var cmd5 = new MySqlCommand(selectPlacingWay, connect);
                    cmd5.Prepare();
                    cmd5.Parameters.AddWithValue("@name", pwName);
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
                            $"INSERT INTO {Program.Prefix}placing_way SET name= @name";
                        var cmd6 = new MySqlCommand(insertPlacingWay, connect);
                        cmd6.Prepare();
                        cmd6.Parameters.AddWithValue("@name", pwName);
                        cmd6.ExecuteNonQuery();
                        idPlacingWay = (int) cmd6.LastInsertedId;
                    }

                    var idEtp = 0;
                    var etpName = "ГАЗНЕФТЕТОРГ.РУ";
                    var etpUrl = ParserGntWeb._site;
                    var selectEtp = $"SELECT id_etp FROM {Program.Prefix}etp WHERE name = @name AND url = @url";
                    var cmd7 = new MySqlCommand(selectEtp, connect);
                    cmd7.Prepare();
                    cmd7.Parameters.AddWithValue("@name", etpName);
                    cmd7.Parameters.AddWithValue("@url", etpUrl);
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
                        cmd8.Parameters.AddWithValue("@url", etpUrl);
                        cmd8.ExecuteNonQuery();
                        idEtp = (int) cmd8.LastInsertedId;
                    }

                    var insertTender =
                        $"INSERT INTO {Program.Prefix}tender SET id_region = @id_region, id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, purchase_object_info = @purchase_object_info, type_fz = @type_fz, id_organizer = @id_organizer, id_placing_way = @id_placing_way, id_etp = @id_etp, end_date = @end_date, scoring_date = @scoring_date, bidding_date = @bidding_date, cancel = @cancel, date_version = @date_version, num_version = @num_version, notice_version = @notice_version, xml = @xml, print_form = @print_form";
                    var cmd9 = new MySqlCommand(insertTender, connect);
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
                    var resInsertTender = cmd9.ExecuteNonQuery();
                    var idTender = (int) cmd9.LastInsertedId;
                    if (update)
                    {
                        Program.UpGntWeb++;
                    }
                    else
                    {
                        Program.AddGntWeb++;
                    }

                    var navT = (HtmlNodeNavigator) htmlDoc.CreateNavigator();
                    var urlAtt = (navT?.SelectSingleNode("//td[a=\"Документация по торгам\"]/a/@href")?.Value ?? "")
                        .Trim();
                    if (!string.IsNullOrEmpty(urlAtt))
                    {
                        urlAtt = $"{ParserGntWeb._site}{urlAtt}";
                        var strAtt = DownloadString.DownL1251(urlAtt);
                        if (!string.IsNullOrEmpty(strAtt))
                        {
                            var htmlAtt = new HtmlDocument();
                            htmlAtt.LoadHtml(strAtt);
                            var attach = htmlAtt.DocumentNode.SelectNodes("//tr[@class = \"file\"]") ??
                                         new HtmlNodeCollection(null);
                            foreach (var att in attach)
                            {
                                var fName = (att.SelectSingleNode("td[2]/a")?.InnerText ?? "").Trim();
                                var urlF = (att.SelectSingleNode("td[2]/a[@href]")?.Attributes["href"]?.Value ?? "")
                                    .Trim();
                                if (urlF.StartsWith("/files"))
                                {
                                    urlF = $"{ParserGntWeb._site}{urlF}";
                                }

                                var Desc = (att.SelectSingleNode("td[3]")?.InnerText ?? "").Trim();
                                Desc = System.Net.WebUtility.HtmlDecode(Desc);
                                if (!string.IsNullOrEmpty(fName))
                                {
                                    var insertAttach =
                                        $"INSERT INTO {Program.Prefix}attachment SET id_tender = @id_tender, file_name = @file_name, url = @url, description = @description";
                                    var cmd10 = new MySqlCommand(insertAttach, connect);
                                    cmd10.Prepare();
                                    cmd10.Parameters.AddWithValue("@id_tender", idTender);
                                    cmd10.Parameters.AddWithValue("@file_name", fName);
                                    cmd10.Parameters.AddWithValue("@url", urlF);
                                    cmd10.Parameters.AddWithValue("@description", Desc);
                                    cmd10.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    var finSource =
                        (navT?.SelectSingleNode("//tr[td [position()=1]= \"Источник финансирования:\"]/td[last()]")
                             ?.Value ?? "").Trim();
                    var lotNum = 1;
                    var lots = htmlDoc.DocumentNode.SelectNodes("//div[@class = \"lot_info\"]") ??
                               new HtmlNodeCollection(null);
                    foreach (var lt in lots)
                    {
                        var navL = (HtmlNodeNavigator) lt.CreateNavigator();
                        var prc =
                            (navL?.SelectSingleNode(
                                     "table/tbody/tr[td [position()=1]= \"Начальная (максимальная) цена договора:\"]/td[last()]")
                                 ?.Value ??
                             "").Trim();
                        prc = System.Net.WebUtility.HtmlDecode(prc);
                        var maxP = UtilsFromParsing.ParsePrice(prc);
                        //WriteLine(maxP);
                        var _price =
                            (navL?.SelectSingleNode(
                                     "table/tbody/tr[td [position()=1]= \"Цена за единицу продукции:\"]/td[last()]")
                                 ?.Value ??
                             "").Trim();
                        _price = System.Net.WebUtility.HtmlDecode(_price);
                        var price = UtilsFromParsing.ParsePrice(_price);
                        var currency = "";
                        if (!string.IsNullOrEmpty(prc))
                        {
                            if (prc.Contains("руб"))
                            {
                                currency = "руб.";
                            }
                        }

                        var insertLot =
                            $"INSERT INTO {Program.Prefix}lot SET id_tender = @id_tender, lot_number = @lot_number, max_price = @max_price, currency = @currency, finance_source = @finance_source";
                        var cmd18 = new MySqlCommand(insertLot, connect);
                        cmd18.Prepare();
                        cmd18.Parameters.AddWithValue("@id_tender", idTender);
                        cmd18.Parameters.AddWithValue("@lot_number", lotNum);
                        cmd18.Parameters.AddWithValue("@max_price", maxP);
                        cmd18.Parameters.AddWithValue("@currency", currency);
                        cmd18.Parameters.AddWithValue("@finance_source", finSource);
                        cmd18.ExecuteNonQuery();
                        var idLot = (int) cmd18.LastInsertedId;
                        var prefName =
                            (navL?.SelectSingleNode(
                                     "table/tbody/tr[td [position()=1]= \"Сведения о предоставлении преференций:\"]/td[last()]")
                                 ?.Value ?? "").Trim();
                        if (!string.IsNullOrEmpty(prefName))
                        {
                            var insertPreference =
                                $"INSERT INTO {Program.Prefix}preferense SET id_lot = @id_lot, name = @name";
                            var cmd17 = new MySqlCommand(insertPreference, connect);
                            cmd17.Prepare();
                            cmd17.Parameters.AddWithValue("@id_lot", idLot);
                            cmd17.Parameters.AddWithValue("@name", prefName);
                            cmd17.ExecuteNonQuery();
                        }

                        var recName =
                            (navL?.SelectSingleNode(
                                 "table/tbody/tr[starts-with(td[position()=1], \"Требования\")]/td[last()]")?.Value ??
                             "").Trim();
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

                        var restName =
                            (navL?.SelectSingleNode(
                                     "table/tbody/tr[starts-with(td[position()=1], \"Участниками закупки могут\")]/td[last()]")
                                 ?.Value ?? "").Trim();
                        if (restName == "Да")
                        {
                            var restrictInfo =
                                "Участниками закупки могут быть только субъекты малого и среднего предпринимательства";
                            var insertRestrict =
                                $"INSERT INTO {Program.Prefix}restricts SET id_lot = @id_lot, info = @info";
                            var cmd19 = new MySqlCommand(insertRestrict, connect);
                            cmd19.Prepare();
                            cmd19.Parameters.AddWithValue("@id_lot", idLot);
                            cmd19.Parameters.AddWithValue("@info", restrictInfo);
                            cmd19.ExecuteNonQuery();
                        }

                        lotNum++;
                        var _urlPurObj =
                            (navL?.SelectSingleNode("table/tbody/tr/td/p/a[. = \"Просмотр позиций по лоту\"]/@href")
                                 ?.Value ?? "").Trim();
                        if (!string.IsNullOrEmpty(_urlPurObj))
                        {
                            var urlObj = $"{ParserGntWeb._site}{_urlPurObj}";
                            var strObj = DownloadString.DownL1251(urlObj);
                            if (!string.IsNullOrEmpty(strObj))
                            {
                                var htmlObj = new HtmlDocument();
                                htmlObj.LoadHtml(strObj);
                                var obj =
                                    htmlObj.DocumentNode.SelectNodes("//tr[@class = \"c1\" or @class = \"c2\"]") ??
                                    new HtmlNodeCollection(null);
                                if (obj.Count > 0)
                                {
                                    foreach (var o in obj)
                                    {
                                        var pName = (o.SelectSingleNode("td[2]")?.InnerText ?? "").Trim();
                                        pName = System.Net.WebUtility.HtmlDecode(pName);
                                        var okpd2Name = (o.SelectSingleNode("td[3]")?.InnerText ?? "").Trim();
                                        var _prPo = (o.SelectSingleNode("td[5]")?.InnerText ?? "").Trim();
                                        _prPo = System.Net.WebUtility.HtmlDecode(_prPo);
                                        var sum = UtilsFromParsing.ParsePrice(_prPo);
                                        var quantity = (o.SelectSingleNode("td[6]")?.InnerText ?? "").Trim();
                                        quantity = System.Net.WebUtility.HtmlDecode(quantity);
                                        var insertLotitem =
                                            $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd_name = @okpd_name, name = @name, quantity_value = @quantity_value, sum = @sum, customer_quantity_value = @customer_quantity_value";
                                        var cmd19 = new MySqlCommand(insertLotitem, connect);
                                        cmd19.Prepare();
                                        cmd19.Parameters.AddWithValue("@id_lot", idLot);
                                        cmd19.Parameters.AddWithValue("@id_customer", customerId);
                                        cmd19.Parameters.AddWithValue("@okpd_name", okpd2Name);
                                        cmd19.Parameters.AddWithValue("@name", pName);
                                        cmd19.Parameters.AddWithValue("@quantity_value", quantity);
                                        cmd19.Parameters.AddWithValue("@sum", sum);
                                        cmd19.Parameters.AddWithValue("@customer_quantity_value", quantity);
                                        cmd19.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    var pName =
                                        (navL?.SelectSingleNode(
                                                 "table/tbody/tr[td [position()=1]/text()[1]  = \"Предмет договора\"]/td[last()]")
                                             ?.Value ?? "").Trim();
                                    pName = System.Net.WebUtility.HtmlDecode(pName);
                                    var okpd2Name =
                                        (navL?.SelectSingleNode(
                                                 "table/tbody/tr[td [position()=1]= \"Категория продукции (ОКПД2):\"]/td[last()]")
                                             ?.Value ?? "").Trim();
                                    var quantity =
                                        (navL?.SelectSingleNode(
                                                 "table/tbody/tr[td [position()=1]= \"Количество поставляемого товара, объем выполняемых работ, оказываемых услуг:\"]/td[last()]")
                                             ?.Value ?? "").Trim();
                                    quantity = System.Net.WebUtility.HtmlDecode(quantity);
                                    var insertLotitem =
                                        $"INSERT INTO {Program.Prefix}purchase_object SET id_lot = @id_lot, id_customer = @id_customer, okpd_name = @okpd_name, name = @name, quantity_value = @quantity_value, price = @price, sum = @sum, customer_quantity_value = @customer_quantity_value";
                                    var cmd19 = new MySqlCommand(insertLotitem, connect);
                                    cmd19.Prepare();
                                    cmd19.Parameters.AddWithValue("@id_lot", idLot);
                                    cmd19.Parameters.AddWithValue("@id_customer", customerId);
                                    cmd19.Parameters.AddWithValue("@okpd_name", okpd2Name);
                                    cmd19.Parameters.AddWithValue("@name", pName);
                                    cmd19.Parameters.AddWithValue("@quantity_value", quantity);
                                    cmd19.Parameters.AddWithValue("@price", price);
                                    cmd19.Parameters.AddWithValue("@sum", maxP);
                                    cmd19.Parameters.AddWithValue("@customer_quantity_value", quantity);
                                    cmd19.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    Tender.TenderKwords(connect, idTender);
                    AddVerNumber(connect, pNum);
                }
            }
        }

        private void AddVerNumber(MySqlConnection connect, string purchaseNumber)
        {
            var verNum = 1;
            var selectTenders =
                $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchaseNumber AND type_fz = 6 ORDER BY id_tender ASC";
            var cmd1 = new MySqlCommand(selectTenders, connect);
            cmd1.Prepare();
            cmd1.Parameters.AddWithValue("@purchaseNumber", purchaseNumber);
            var dt1 = new DataTable();
            var adapter1 = new MySqlDataAdapter {SelectCommand = cmd1};
            adapter1.Fill(dt1);
            if (dt1.Rows.Count > 0)
            {
                var updateTender =
                    $"UPDATE {Program.Prefix}tender SET num_version = @num_version WHERE id_tender = @id_tender";
                foreach (DataRow ten in dt1.Rows)
                {
                    var idTender = (int) ten["id_tender"];
                    var cmd2 = new MySqlCommand(updateTender, connect);
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