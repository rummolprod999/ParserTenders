using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class ParserGpb : ParserWeb
    {
        protected string _urlListTenders;
        protected string _urlTender;
        protected string _urlCustomerId;
        protected string _urlCustomerInnKpp;
        protected string _etpUrl;

        public ParserGpb(TypeArguments a) : base(a)
        {
            _urlListTenders = "https://etp.gpb.ru/api/procedures.php?late=0";
            _urlTender = "https://etp.gpb.ru/api/procedures.php?regid=";
            _urlCustomerId = "https://etp.gpb.ru/api/company.php?id=";
            _urlCustomerInnKpp = "https://etp.gpb.ru/api/company.php?inn={inn}&kpp={kpp}";
            _etpUrl = "https://etp.gpb.ru/";
        }

        public override void Parsing()
        {
            string xml = DownloadString.DownL(_urlListTenders);
            /*using (StreamReader sr = new StreamReader("/home/alex/Рабочий стол/parser/procedures.xml",
                Encoding.Default))
            {
                xml = sr.ReadToEnd();
            }*/
            if (xml.Length < 100)
            {
                Log.Logger("Получили пустую строку со списком торгов", _urlListTenders);
                return;
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            string jsons = JsonConvert.SerializeXmlNode(doc);
            JObject json = JObject.Parse(jsons);
            var procedures = GetElements(json, "procedures.procedure");
            foreach (var proc in procedures)
            {
                //Console.WriteLine(proc);
                string registryNumber = ((string) proc.SelectToken("@registry_number") ?? "").Trim();
                var lots = GetElements(proc, "lot");
                //List<LotGpB> l = new List<LotGpB>();
                Dictionary<int, int> l = new Dictionary<int, int>();
                foreach (var lt in lots)
                {
                    string lotNumSt = ((string) lt.SelectToken("@number") ?? "").Trim();
                    //Console.WriteLine(lotNumSt);
                    int lotNum = 0;
                    lotNum = Int32.TryParse(lotNumSt, out lotNum) ? Int32.Parse(lotNumSt) : 0;
                    int status = (int?) lt.SelectToken("status") ?? 0;
                    try
                    {
                        l.Add(lotNum, status);
                    }
                    catch (Exception e)
                    {
                        Log.Logger("Ошибка при создании словаря ГПБ", e, registryNumber);
                    }
                }

                ProcedureGpB pr = new ProcedureGpB
                {
                    RegistryNumber = registryNumber,
                    Lots = l,
                    ScoringDate = DateTime.MinValue,
                    BiddingDate = DateTime.MinValue,
                    EndDate = DateTime.MinValue
                };
                //Console.WriteLine(pr.RegistryNumber);
                //Console.WriteLine(registryNumber);
                try
                {
                    ParsingProc(pr);
                }
                catch (Exception e)
                {
                    Log.Logger("Ошибка при парсинге процедуры ГПБ", e, registryNumber);
                }
            }
        }

        public override void ParsingProc(ProcedureGpB pr)
        {
            //Console.WriteLine(pr.RegistryNumber);
            //Console.WriteLine(pr.BiddingDate);
            //Console.WriteLine(pr.EndDate);
            pr.Xml = $"{_urlTender}{pr.RegistryNumber}";
            string xml = DownloadString.DownL(pr.Xml);
            /*using (StreamReader sr = new StreamReader("/home/alex/Рабочий стол/parser/procedure.xml",
                Encoding.Default))
            {
                xml = sr.ReadToEnd();
            }*/
            if (xml.Length < 100)
            {
                Log.Logger("Получили пустую строку со списком торгов", pr.Xml);
                return;
            }
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            string jsons = JsonConvert.SerializeXmlNode(doc);
            JObject json = JObject.Parse(jsons);
            var procedures = GetElements(json, "procedures.procedure");
            foreach (var proc in procedures)
            {
                pr.IdXml = ((string) proc.SelectToken("id") ?? "").Trim();
                pr.Version = (int?) proc.SelectToken("version") ?? 0;
                pr.DatePublished = (DateTime?) proc.SelectToken("date_published") ?? DateTime.MinValue;
                pr.DateVersion = (DateTime?) proc.SelectToken("date_last_update") ?? DateTime.MinValue;
                //Console.WriteLine(pr.dateVersion);
                if (pr.DateVersion == DateTime.MinValue)
                {
                    pr.DateVersion = pr.DatePublished;
                }
                /*DateTime firstDateVer = (DateTime?) proc.SelectToken("first_date_published") ?? DateTime.MinValue;
                Console.WriteLine(firstDateVer);*/
                /* if (String.IsNullOrEmpty(pr.dateVersion))
                 {
                     pr.dateVersion = pr.DatePublished;
                 }
                 pr.DatePublished = Fdate(pr.DatePublished);
                 pr.dateVersion = Fdate(pr.dateVersion);*/
                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    string selectTend =
                        $"SELECT id_tender FROM {Program.Prefix}tender WHERE id_xml = @id_xml AND num_version = @num_version AND purchase_number = @purchase_number AND date_version = @date_version";
                    MySqlCommand cmd = new MySqlCommand(selectTend, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_xml", pr.IdXml);
                    cmd.Parameters.AddWithValue("@num_version", pr.Version);
                    cmd.Parameters.AddWithValue("@purchase_number", pr.RegistryNumber);
                    cmd.Parameters.AddWithValue("@date_version", pr.DateVersion);
                    DataTable dt = new DataTable();
                    MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd};
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        return;
                    }
                    int cancelStatus = 0;
                    if (!String.IsNullOrEmpty(pr.RegistryNumber))
                    {
                        string selectDateT =
                            $"SELECT id_tender, date_version, cancel FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number";
                        MySqlCommand cmd2 = new MySqlCommand(selectDateT, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@purchase_number", pr.RegistryNumber);
                        MySqlDataAdapter adapter2 = new MySqlDataAdapter {SelectCommand = cmd2};
                        DataTable dt2 = new DataTable();
                        adapter2.Fill(dt2);
                        //Console.WriteLine(dt2.Rows.Count);
                        foreach (DataRow row in dt2.Rows)
                        {
                            //DateTime dateNew = DateTime.Parse(pr.DatePublished);

                            if (pr.DateVersion >= (DateTime) row["date_version"])
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
                    }
                    pr.Href = ((string) proc.SelectToken("procedure_url") ?? "").Trim();
                    pr.PurchaseObjectInfo = ((string) proc.SelectToken("title") ?? "").Trim();
                    pr.NoticeVersion = "";
                    pr.Printform = pr.Xml;
                    var org = new OrganizerGpB();
                    org.OrganiserCustomerId = ((string) proc.SelectToken("organizer_customer_id") ?? "").Trim();
                    string urlCus = $"{_urlCustomerId}{org.OrganiserCustomerId}";
                    string xmlCus = DownloadString.DownL(urlCus);
                    if (xmlCus.Length < 100)
                    {
                        Log.Logger("Получили пустую строку с заказчиком", urlCus);
                    }
                    XmlDocument docCus = new XmlDocument();
                    docCus.LoadXml(xmlCus);
                    string jsonsCus = JsonConvert.SerializeXmlNode(docCus);
                    JObject jsonCus = JObject.Parse(jsonsCus);
                    var customers = GetElements(jsonCus, "companies.company");
                    if (customers.Count > 0)
                    {
                        //Console.WriteLine(customers[0]);
                        org.Inn = ((string) customers[0].SelectToken("inn") ?? "").Trim();
                        org.Kpp = ((string) customers[0].SelectToken("kpp") ?? "").Trim();
                        org.FullName = ((string) customers[0].SelectToken("full_name.#cdata-section") ?? "").Trim();
                        if (!String.IsNullOrEmpty(org.FullName) && org.FullName.IndexOf("CDATA") != -1)
                            org.FullName = org.FullName.Substring(9, org.FullName.Length - 12);
                        org.PostAddress = ((string) customers[0].SelectToken("addr_post") ?? "").Trim();
                        org.FactAddress = ((string) customers[0].SelectToken("addr_main") ?? "").Trim();
                        org.ResponsibleRole = "";
                        org.ContactPerson = "";
                        org.ContactEmail = ((string) customers[0].SelectToken("email") ?? "").Trim();
                        org.ContactPhone = ((string) customers[0].SelectToken("phone") ?? "").Trim();
                        org.ContactFax = ((string) customers[0].SelectToken("fax") ?? "").Trim();
                    }
                    pr.IdOrg = 0;
                    if (!String.IsNullOrEmpty(org.Inn))
                    {
                        string selectOrg =
                            $"SELECT id_organizer FROM {Program.Prefix}organizer WHERE inn = @inn AND kpp = @kpp";
                        MySqlCommand cmd3 = new MySqlCommand(selectOrg, connect);
                        cmd3.Prepare();
                        cmd3.Parameters.AddWithValue("@inn", org.Inn);
                        cmd3.Parameters.AddWithValue("@kpp", org.Kpp);
                        DataTable dt3 = new DataTable();
                        MySqlDataAdapter adapter3 = new MySqlDataAdapter {SelectCommand = cmd3};
                        adapter3.Fill(dt3);
                        if (dt3.Rows.Count > 0)
                        {
                            pr.IdOrg = (int) dt3.Rows[0].ItemArray[0];
                        }
                        else
                        {
                            string addOrganizer =
                                $"INSERT INTO {Program.Prefix}organizer SET full_name = @full_name, post_address = @post_address, fact_address = @fact_address, inn = @inn, kpp = @kpp, contact_email = @contact_email, contact_phone = @contact_phone, contact_fax = @contact_fax";
                            MySqlCommand cmd4 = new MySqlCommand(addOrganizer, connect);
                            cmd4.Prepare();
                            cmd4.Parameters.AddWithValue("@full_name", org.FullName);
                            cmd4.Parameters.AddWithValue("@post_address", org.PostAddress);
                            cmd4.Parameters.AddWithValue("@fact_address", org.FactAddress);
                            cmd4.Parameters.AddWithValue("@inn", org.Inn);
                            cmd4.Parameters.AddWithValue("@kpp", org.Kpp);
                            cmd4.Parameters.AddWithValue("@contact_email", org.ContactEmail);
                            cmd4.Parameters.AddWithValue("@contact_phone", org.ContactPhone);
                            cmd4.Parameters.AddWithValue("@contact_fax", org.ContactFax);
                            cmd4.ExecuteNonQuery();
                            pr.IdOrg = (int) cmd4.LastInsertedId;
                        }
                        //Console.WriteLine(pr.IdOrg);
                    }
                    else
                    {
                        Log.Logger("Нет organizer_inn", urlCus);
                    }
                    pr.IdPlacingWay = 0;
                    var pw = new PlacingWayGpB {Code = "", Name = ""};
                    pw.Code = ((string) proc.SelectToken("procedure_type") ?? "").Trim();
                    pw.Name = ((string) proc.SelectToken("procedure_type_name") ?? "").Trim();
                    if (!String.IsNullOrEmpty(pw.Code) && !String.IsNullOrEmpty(pw.Name))
                    {
                        string selectPlacingWay =
                            $"SELECT id_placing_way FROM {Program.Prefix}placing_way WHERE code = @code AND name = @name";
                        MySqlCommand cmd4 = new MySqlCommand(selectPlacingWay, connect);
                        cmd4.Prepare();
                        cmd4.Parameters.AddWithValue("@code", pw.Code);
                        cmd4.Parameters.AddWithValue("@name", pw.Name);
                        DataTable dt3 = new DataTable();
                        MySqlDataAdapter adapter3 = new MySqlDataAdapter {SelectCommand = cmd4};
                        adapter3.Fill(dt3);
                        if (dt3.Rows.Count > 0)
                        {
                            pr.IdPlacingWay = (int) dt3.Rows[0].ItemArray[0];
                        }
                        else
                        {
                            string insertPlacingWay =
                                $"INSERT INTO {Program.Prefix}placing_way SET code= @code, name= @name";
                            MySqlCommand cmd5 = new MySqlCommand(insertPlacingWay, connect);
                            cmd5.Prepare();
                            cmd5.Parameters.AddWithValue("@code", pw.Code);
                            cmd5.Parameters.AddWithValue("@name", pw.Name);
                            cmd5.ExecuteNonQuery();
                            pr.IdPlacingWay = (int) cmd5.LastInsertedId;
                        }
                    }
                    //Console.WriteLine(pr.IdPlacingWay);
                    pr.IdEtp = 0;
                    string etpName = "ЭТП ГПБ";
                    string etpUrl = _etpUrl;
                    string selectEtp = $"SELECT id_etp FROM {Program.Prefix}etp WHERE name = @name AND url = @url";
                    MySqlCommand cmd6 = new MySqlCommand(selectEtp, connect);
                    cmd6.Prepare();
                    cmd6.Parameters.AddWithValue("@name", etpName);
                    cmd6.Parameters.AddWithValue("@url", etpUrl);
                    DataTable dt4 = new DataTable();
                    MySqlDataAdapter adapter4 = new MySqlDataAdapter {SelectCommand = cmd6};
                    adapter4.Fill(dt4);
                    if (dt4.Rows.Count > 0)
                    {
                        pr.IdEtp = (int) dt4.Rows[0].ItemArray[0];
                    }
                    else
                    {
                        string insertEtp =
                            $"INSERT INTO {Program.Prefix}etp SET name = @name, url = @url, conf=0";
                        MySqlCommand cmd7 = new MySqlCommand(insertEtp, connect);
                        cmd7.Prepare();
                        cmd7.Parameters.AddWithValue("@name", etpName);
                        cmd7.Parameters.AddWithValue("@url", etpUrl);
                        cmd7.ExecuteNonQuery();
                        pr.IdEtp = (int) cmd7.LastInsertedId;
                    }
                    pr.BiddingDate = DateTime.MinValue;
                    pr.EndDate = DateTime.MinValue;
                    pr.ScoringDate = DateTime.MinValue;
                    List<JToken> lots = GetElements(proc, "lots.lot");
                    if (lots.Count > 0)
                    {
                        pr.EndDate = (DateTime?) lots[0].SelectToken("date_end_registration") ?? DateTime.MinValue;
                        pr.BiddingDate = (DateTime?) lots[0].SelectToken("date_applic_opened") ?? DateTime.MinValue;
                        if (pr.BiddingDate == DateTime.MinValue)
                        {
                            pr.BiddingDate =
                                (DateTime?) lots[0].SelectToken("date_begin_auction") ?? DateTime.MinValue;
                        }
                        pr.ScoringDate = (DateTime?) lots[0].SelectToken("date_end_first_parts_review") ?? DateTime.MinValue;
                    }
                    int typeFz = (int?) proc.SelectToken("FZ223") ?? 0;
                    typeFz = typeFz == 0 ? 1 : 223;
                    //Console.WriteLine(typeFz);
                    string insertTender =
                        $"INSERT INTO {Program.Prefix}tender SET id_region = @id_region, id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, purchase_object_info = @purchase_object_info, type_fz = @type_fz, id_organizer = @id_organizer, id_placing_way = @id_placing_way, id_etp = @id_etp, end_date = @end_date, scoring_date = @scoring_date, bidding_date = @bidding_date, cancel = @cancel, date_version = @date_version, num_version = @num_version, notice_version = @notice_version, xml = @xml, print_form = @print_form";
                    MySqlCommand cmd8 = new MySqlCommand(insertTender, connect);
                    cmd8.Prepare();
                    cmd8.Parameters.AddWithValue("@id_region", 0);
                    cmd8.Parameters.AddWithValue("@id_xml", pr.IdXml);
                    cmd8.Parameters.AddWithValue("@purchase_number", pr.RegistryNumber);
                    cmd8.Parameters.AddWithValue("@doc_publish_date", pr.DatePublished);
                    cmd8.Parameters.AddWithValue("@href", pr.Href);
                    cmd8.Parameters.AddWithValue("@purchase_object_info", pr.PurchaseObjectInfo);
                    cmd8.Parameters.AddWithValue("@type_fz", typeFz);
                    cmd8.Parameters.AddWithValue("@id_organizer", pr.IdOrg);
                    cmd8.Parameters.AddWithValue("@id_placing_way", pr.IdPlacingWay);
                    cmd8.Parameters.AddWithValue("@id_etp", pr.IdEtp);
                    cmd8.Parameters.AddWithValue("@end_date", pr.EndDate);
                    cmd8.Parameters.AddWithValue("@scoring_date", pr.ScoringDate);
                    cmd8.Parameters.AddWithValue("@bidding_date", pr.BiddingDate);
                    cmd8.Parameters.AddWithValue("@cancel", cancelStatus);
                    cmd8.Parameters.AddWithValue("@date_version", pr.DateVersion);
                    cmd8.Parameters.AddWithValue("@num_version", pr.Version);
                    cmd8.Parameters.AddWithValue("@notice_version", pr.NoticeVersion);
                    cmd8.Parameters.AddWithValue("@xml", pr.Xml);
                    cmd8.Parameters.AddWithValue("@print_form", pr.Printform);
                    int resInsertTender = cmd8.ExecuteNonQuery();
                    int idTender = (int) cmd8.LastInsertedId;
                    Program.AddGazprom++;
                    List<JToken> attachments = GetElements(proc, "docs.doc");
                    attachments.AddRange(GetElements(proc, "procedure_common_docs.doc"));
                    foreach (var att in attachments)
                    {
                        string attachName = ((string) att.SelectToken("@file_name") ?? "").Trim();
                        string attachDescription = ((string) att.SelectToken("@title") ?? "").Trim();
                        string attachUrl = ((string) att.SelectToken("@url") ?? "").Trim();
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
                    foreach (var lot in lots)
                    {
                        var lt = new LotGpB();
                        lt.LotNumber = (int?) lot.SelectToken("number") ?? 0;
                        lt.IdTender = idTender;
                        lt.MaxPrice = (decimal?) lot.SelectToken("start_price") ?? 0.0m;
                        lt.Subject = ((string) lot.SelectToken("subject") ?? "").Trim();
                        lt.Currency = ((string) proc.SelectToken("currency_name") ?? "").Trim();
                        string insertLot =
                            $"INSERT INTO {Program.Prefix}lot SET id_tender = @id_tender, lot_number = @lot_number, max_price = @max_price, currency = @currency";
                        MySqlCommand cmd18 = new MySqlCommand(insertLot, connect);
                        cmd18.Prepare();
                        cmd18.Parameters.AddWithValue("@id_tender", lt.IdTender);
                        cmd18.Parameters.AddWithValue("@lot_number", lt.LotNumber);
                        cmd18.Parameters.AddWithValue("@max_price", lt.MaxPrice);
                        cmd18.Parameters.AddWithValue("@currency", lt.Currency);
                        cmd18.ExecuteNonQuery();
                        int idLot = (int) cmd18.LastInsertedId;
                        int idCustomer = 0;
                        //string customerRegNumber = "";
                        var cust = new CustomerGpB();
                        var customerslot = GetElements(lot, "customers.customer");
                        //Console.WriteLine(customerslot[0]);
                        if (customerslot.Count > 0)
                        {
                            cust.OrganiserCustomerId = ((string) customerslot[0].SelectToken("@id") ?? "").Trim();
                            cust.Inn = ((string) customerslot[0].SelectToken("inn") ?? "").Trim();
                            cust.Kpp = ((string) customerslot[0].SelectToken("kpp") ?? "").Trim();
                            cust.FullName = ((string) customerslot[0].SelectToken("full_name") ?? "").Trim();
                            if (!String.IsNullOrEmpty(cust.Inn))
                            {
                                //Console.WriteLine(cust.Inn);
                                string selectOdCustomer =
                                    $"SELECT regNumber FROM od_customer WHERE inn = @inn AND kpp = @kpp";
                                MySqlCommand cmd10 = new MySqlCommand(selectOdCustomer, connect);
                                cmd10.Prepare();
                                cmd10.Parameters.AddWithValue("@inn", cust.Inn);
                                cmd10.Parameters.AddWithValue("@kpp", cust.Kpp);
                                MySqlDataReader reader4 = cmd10.ExecuteReader();
                                if (reader4.HasRows)
                                {
                                    reader4.Read();
                                    cust.CustomerRegNumber = (string) reader4["regNumber"];
                                }
                                reader4.Close();
                                if (String.IsNullOrEmpty(cust.CustomerRegNumber))
                                {
                                    string selectOdCustomerFromFtp =
                                        $"SELECT regNumber FROM od_customer_from_ftp WHERE inn = @inn AND kpp = @kpp";
                                    MySqlCommand cmd11 = new MySqlCommand(selectOdCustomerFromFtp, connect);
                                    cmd11.Prepare();
                                    cmd11.Parameters.AddWithValue("@inn", cust.Inn);
                                    cmd11.Parameters.AddWithValue("@kpp", cust.Kpp);
                                    MySqlDataReader reader5 = cmd11.ExecuteReader();
                                    if (reader5.HasRows)
                                    {
                                        reader5.Read();
                                        cust.CustomerRegNumber = (string) reader5["regNumber"];
                                    }
                                    reader5.Close();
                                }
                                if (String.IsNullOrEmpty(cust.CustomerRegNumber))
                                {
                                    string selectOdCustomerFromFtp223 =
                                        $"SELECT regNumber FROM od_customer_from_ftp223 WHERE inn = @inn AND kpp = @kpp";
                                    MySqlCommand cmd12 = new MySqlCommand(selectOdCustomerFromFtp223, connect);
                                    cmd12.Prepare();
                                    cmd12.Parameters.AddWithValue("@inn", cust.Inn);
                                    cmd12.Parameters.AddWithValue("@kpp", cust.Kpp);
                                    MySqlDataReader reader6 = cmd12.ExecuteReader();
                                    if (reader6.HasRows)
                                    {
                                        reader6.Read();
                                        cust.CustomerRegNumber = (string) reader6["regNumber"];
                                    }
                                    reader6.Close();
                                }
                                if (!String.IsNullOrEmpty(cust.CustomerRegNumber))
                                {
                                    string selectCustomer =
                                        $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                                    MySqlCommand cmd13 = new MySqlCommand(selectCustomer, connect);
                                    cmd13.Prepare();
                                    cmd13.Parameters.AddWithValue("@reg_num", cust.CustomerRegNumber);
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
                                        cmd14.Parameters.AddWithValue("@reg_num", cust.CustomerRegNumber);
                                        cmd14.Parameters.AddWithValue("@full_name", cust.Inn);
                                        cmd14.Parameters.AddWithValue("@inn", cust.Kpp);
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
                                    cmd15.Parameters.AddWithValue("@inn", cust.Inn);
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
                                        string urlCusLot = $"{_urlCustomerId}{cust.OrganiserCustomerId}";
                                        string xmlCusLot = DownloadString.DownL(urlCusLot);
                                        if (xmlCusLot.Length < 100)
                                        {
                                            Log.Logger("Получили пустую строку с customer", urlCusLot);
                                        }
                                        XmlDocument docCusLot = new XmlDocument();
                                        docCusLot.LoadXml(xmlCusLot);
                                        string jsonsCusLot = JsonConvert.SerializeXmlNode(docCusLot);
                                        JObject jsonCusLot = JObject.Parse(jsonsCusLot);
                                        var cst = GetElements(jsonCusLot, "companies.company");
                                        if (cst.Count > 0)
                                        {
                                            cust.FullName =
                                                ((string) cst[0].SelectToken("full_name.#cdata-section") ?? "").Trim();
                                            if (!String.IsNullOrEmpty(cust.FullName) &&
                                                cust.FullName.IndexOf("CDATA") != -1)
                                                cust.FullName = cust.FullName.Substring(9, cust.FullName.Length - 12);
                                            cust.PostAddress = ((string) cst[0].SelectToken("addr_post") ?? "").Trim();
                                            cust.FactAddress = ((string) cst[0].SelectToken("addr_main") ?? "").Trim();
                                            cust.ResponsibleRole = "";
                                            cust.ContactPerson = "";
                                            cust.ContactEmail = ((string) cst[0].SelectToken("email") ?? "").Trim();
                                            cust.ContactPhone = ((string) cst[0].SelectToken("phone") ?? "").Trim();
                                            cust.ContactFax = ((string) cst[0].SelectToken("fax") ?? "").Trim();
                                            cust.Ogrn = ((string) cst[0].SelectToken("ogrn") ?? "").Trim();
                                        }
                                        string regNum223 = $"00000223{cust.Inn}";
                                        string insertCustomer =
                                            $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, inn = @inn, is223=1";
                                        MySqlCommand cmd16 = new MySqlCommand(insertCustomer, connect);
                                        cmd16.Prepare();
                                        cmd16.Parameters.AddWithValue("@reg_num", regNum223);
                                        cmd16.Parameters.AddWithValue("@full_name", cust.FullName);
                                        cmd16.Parameters.AddWithValue("@inn", cust.Inn);
                                        cmd16.ExecuteNonQuery();
                                        idCustomer = (int) cmd16.LastInsertedId;
                                        string insertCustomer223 =
                                            $"INSERT INTO {Program.Prefix}customer223 SET inn = @inn, full_name = @full_name, contact = @contact, kpp = @kpp, ogrn = @ogrn, post_address = @post_address, phone = @phone, fax = @fax, email = @email";
                                        MySqlCommand cmd17 = new MySqlCommand(insertCustomer223, connect);
                                        cmd17.Prepare();
                                        cmd17.Parameters.AddWithValue("@full_name", cust.FullName);
                                        cmd17.Parameters.AddWithValue("@inn", cust.Inn);
                                        cmd17.Parameters.AddWithValue("@contact", cust.ContactPerson);
                                        cmd17.Parameters.AddWithValue("@kpp", cust.Kpp);
                                        cmd17.Parameters.AddWithValue("@ogrn", cust.Ogrn);
                                        cmd17.Parameters.AddWithValue("@post_address", cust.PostAddress);
                                        cmd17.Parameters.AddWithValue("@phone", cust.ContactPhone);
                                        cmd17.Parameters.AddWithValue("@fax", cust.ContactFax);
                                        cmd17.Parameters.AddWithValue("@email", cust.ContactEmail);
                                        cmd17.ExecuteNonQuery();
                                    }
                                }
                            }
                            else
                            {
                                Log.Logger("У customer нет inn", pr.Xml);
                            }
                        }
                        string deliveryPlace = ((string) lot.SelectToken("delivery_places.place") ?? "").Trim();
                        string deliveryTerm = ((string) lot.SelectToken("delivery_places.term") ?? "").Trim();
                        string applicationGuaranteeAmount =
                            ((string) lot.SelectToken("guarantee_application") ?? "").Trim();
                        string contractGuaranteeAmount = ((string) lot.SelectToken("guarantee_contract") ?? "").Trim();
                        string insertCustomerRequirement =
                            $"INSERT INTO {Program.Prefix}customer_requirement SET id_lot = @id_lot, id_customer = @id_customer, kladr_place = @kladr_place, delivery_place = @delivery_place, delivery_term = @delivery_term, application_guarantee_amount = @application_guarantee_amount, application_settlement_account = @application_settlement_account, application_personal_account = @application_personal_account, application_bik = @application_bik, contract_guarantee_amount = @contract_guarantee_amount, contract_settlement_account = @contract_settlement_account, contract_personal_account = @contract_personal_account, contract_bik = @contract_bik, max_price = @max_price";
                        MySqlCommand cmd22 = new MySqlCommand(insertCustomerRequirement, connect);
                        cmd22.Prepare();
                        cmd22.Parameters.AddWithValue("@id_lot", idLot);
                        cmd22.Parameters.AddWithValue("@id_customer", idCustomer);
                        cmd22.Parameters.AddWithValue("@kladr_place", "");
                        cmd22.Parameters.AddWithValue("@delivery_place", deliveryPlace);
                        cmd22.Parameters.AddWithValue("@delivery_term", deliveryTerm);
                        cmd22.Parameters.AddWithValue("@application_guarantee_amount",
                            applicationGuaranteeAmount);
                        cmd22.Parameters.AddWithValue("@application_settlement_account",
                            "");
                        cmd22.Parameters.AddWithValue("@application_personal_account",
                            "");
                        cmd22.Parameters.AddWithValue("@application_bik", "");
                        cmd22.Parameters.AddWithValue("@contract_guarantee_amount", contractGuaranteeAmount);
                        cmd22.Parameters.AddWithValue("@contract_settlement_account", "");
                        cmd22.Parameters.AddWithValue("@contract_personal_account", "");
                        cmd22.Parameters.AddWithValue("@contract_bik", "");
                        cmd22.Parameters.AddWithValue("@max_price", "");
                        cmd22.ExecuteNonQuery();
                        List<JToken> lotitems = GetElements(lot, "lot_units.unit");
                        foreach (var lotitem in lotitems)
                        {
                            string okpd2Code = ((string) lotitem.SelectToken("okdp_code") ?? "").Trim();
                            string okpdName = ((string) lotitem.SelectToken("okdp_name") ?? "").Trim();
                            string name = ((string) lotitem.SelectToken("name") ?? "").Trim();
                            name = $"{name} {lt.Subject}".Trim();
                            string quantityValue = ((string) lotitem.SelectToken("quantity") ?? "")
                                .Trim();
                            string okei = ((string) lotitem.SelectToken("okei_name") ?? "").Trim();
                            int okpd2GroupCode = 0;
                            string okpd2GroupLevel1Code = "";
                            if (!String.IsNullOrEmpty(okpd2Code))
                            {
                                Tender.GetOkpd(okpd2Code, out okpd2GroupCode, out okpd2GroupLevel1Code);
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

                    Tender.TenderKwords(connect, idTender);
                }
            }
        }

        public string Fdate(string s)
        {
            if (s.Length > 19)
            {
                s = s.Substring(0, 19);
            }

            return s;
        }
    }
}