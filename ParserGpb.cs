using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class ParserGpb : ParserWeb
    {
        private string UrlListTenders;
        private string UrlTender;
        private string UrlCustomerId;
        private string UrlCustomerInnKpp;

        public ParserGpb(TypeArguments a) : base(a)
        {
            UrlListTenders = "https://etp.gpb.ru/api/procedures.php?late=0";
            UrlTender = "https://etp.gpb.ru/api/procedures.php?regid=";
            UrlCustomerId = "https://etp.gpb.ru/api/company.php?id=";
            UrlCustomerInnKpp = "https://etp.gpb.ru/api/company.php?inn={inn}&kpp={kpp}";
        }

        public override void Parsing()
        {
            string xml = DownloadString.DownL(UrlListTenders);
            using (StreamReader sr = new StreamReader("/home/alex/Рабочий стол/parser/procedures.xml",
                Encoding.Default))
            {
                xml = sr.ReadToEnd();
            }
            if (xml.Length < 100)
            {
                Log.Logger("Получили пустую строку со списком торгов", UrlListTenders);
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
            pr.Xml = $"{UrlTender}{pr.RegistryNumber}";
            string xml = DownloadString.DownL(pr.Xml);
            using (StreamReader sr = new StreamReader("/home/alex/Рабочий стол/parser/procedure.xml",
                Encoding.Default))
            {
                xml = sr.ReadToEnd();
            }
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
                pr.dateVersion = (DateTime?) proc.SelectToken("date_last_update") ?? DateTime.MinValue;
                //Console.WriteLine(pr.dateVersion);
                if (pr.dateVersion == DateTime.MinValue)
                {
                    pr.dateVersion = pr.DatePublished;
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
                    cmd.Parameters.AddWithValue("@date_version", pr.dateVersion);
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
                            $"SELECT id_tender, doc_publish_date, cancel FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number";
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

                            if (pr.DatePublished > (DateTime) row["doc_publish_date"])
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
                    pr.purchaseObjectInfo = ((string) proc.SelectToken("title") ?? "").Trim();
                    pr.noticeVersion = "";
                    pr.printform = pr.Xml;
                    var org = new OrganizerGpB();
                    org.OrganiserCustomerId = ((string) proc.SelectToken("organizer_customer_id") ?? "").Trim();
                    string UrlCus = $"{UrlCustomerId}{org.OrganiserCustomerId}";
                    string xmlCus = DownloadString.DownL(UrlCus);
                    if (xmlCus.Length < 100)
                    {
                        Log.Logger("Получили пустую строку с заказчиком", UrlCus);
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
                        Log.Logger("Нет organizer_inn", UrlCus);
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
                    string EtpName = "ЭТП ГПБ";
                    string EtpUrl = "https://etp.gpb.ru/";
                    string selectEtp = $"SELECT id_etp FROM {Program.Prefix}etp WHERE name = @name AND url = @url";
                    MySqlCommand cmd6 = new MySqlCommand(selectEtp, connect);
                    cmd6.Prepare();
                    cmd6.Parameters.AddWithValue("@name", EtpName);
                    cmd6.Parameters.AddWithValue("@url", EtpUrl);
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
                        cmd7.Parameters.AddWithValue("@name", EtpName);
                        cmd7.Parameters.AddWithValue("@url", EtpUrl);
                        cmd7.ExecuteNonQuery();
                        pr.IdEtp = (int) cmd7.LastInsertedId;
                    }
                    pr.BiddingDate = DateTime.MinValue;
                    pr.EndDate = DateTime.MinValue;
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
                    }
                    string insertTender =
                        $"INSERT INTO {Program.Prefix}tender SET id_region = @id_region, id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, purchase_object_info = @purchase_object_info, type_fz = @type_fz, id_organizer = @id_organizer, id_placing_way = @id_placing_way, id_etp = @id_etp, end_date = @end_date, scoring_date = @scoring_date, bidding_date = @bidding_date, cancel = @cancel, date_version = @date_version, num_version = @num_version, notice_version = @notice_version, xml = @xml, print_form = @print_form";
                    MySqlCommand cmd8 = new MySqlCommand(insertTender, connect);
                    cmd8.Prepare();
                    cmd8.Parameters.AddWithValue("@id_region", 0);
                    cmd8.Parameters.AddWithValue("@id_xml", pr.IdXml);
                    cmd8.Parameters.AddWithValue("@purchase_number", pr.RegistryNumber);
                    cmd8.Parameters.AddWithValue("@doc_publish_date", pr.DatePublished);
                    cmd8.Parameters.AddWithValue("@href", pr.Href);
                    cmd8.Parameters.AddWithValue("@purchase_object_info", pr.purchaseObjectInfo);
                    cmd8.Parameters.AddWithValue("@type_fz", 1);
                    cmd8.Parameters.AddWithValue("@id_organizer", pr.IdOrg);
                    cmd8.Parameters.AddWithValue("@id_placing_way", pr.IdPlacingWay);
                    cmd8.Parameters.AddWithValue("@id_etp", pr.IdEtp);
                    cmd8.Parameters.AddWithValue("@end_date", pr.EndDate);
                    cmd8.Parameters.AddWithValue("@scoring_date", pr.ScoringDate);
                    cmd8.Parameters.AddWithValue("@bidding_date", pr.BiddingDate);
                    cmd8.Parameters.AddWithValue("@cancel", cancelStatus);
                    cmd8.Parameters.AddWithValue("@date_version", pr.dateVersion);
                    cmd8.Parameters.AddWithValue("@num_version", pr.Version);
                    cmd8.Parameters.AddWithValue("@notice_version", pr.noticeVersion);
                    cmd8.Parameters.AddWithValue("@xml", pr.Xml);
                    cmd8.Parameters.AddWithValue("@print_form", pr.printform);
                    int resInsertTender = cmd8.ExecuteNonQuery();
                    int idTender = (int) cmd8.LastInsertedId;
                    Program.AddGazprom++;
                    List<JToken> attachments = GetElements(proc, "docs.doc");
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
                        lt.Currency = "";
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

                    }

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