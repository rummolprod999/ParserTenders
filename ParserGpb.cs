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
                
                ProcedureGpB pr = new ProcedureGpB {RegistryNumber = registryNumber, Lots = l};
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
                pr.Version = ((string) proc.SelectToken("version") ?? "").Trim();
                pr.DatePublished = (JsonConvert.SerializeObject(proc.SelectToken("date_published") ?? "") ??
                                    "").Trim('"');
                //Console.WriteLine(pr.DatePublished);
                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    string selectTend = $"SELECT id_tender FROM {Program.Prefix}tender WHERE id_xml = @id_xml AND num_version = @num_version AND purchase_number = @purchase_number AND doc_publish_date = @doc_publish_date";
                    MySqlCommand cmd = new MySqlCommand(selectTend, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_xml", pr.IdXml);
                    cmd.Parameters.AddWithValue("@num_version", pr.Version);
                    cmd.Parameters.AddWithValue("@purchase_number", pr.RegistryNumber);
                    cmd.Parameters.AddWithValue("@doc_publish_date", pr.DatePublished);
                    DataTable dt = new DataTable();
                    MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd};
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        return;
                    }
                    int cancelStatus = 0;
                    if (!String.IsNullOrEmpty(pr.DatePublished) && !String.IsNullOrEmpty(pr.RegistryNumber))
                    {
                        string selectDateT =
                            $"SELECT id_tender, doc_publish_date, cancel FROM {Program.Prefix}tender WHERE purchase_number = @purchase_number";
                        MySqlCommand cmd2 = new MySqlCommand(selectDateT, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@purchase_number", pr.RegistryNumber);
                        MySqlDataAdapter adapter2 = new MySqlDataAdapter {SelectCommand = cmd2};
                        DataTable dt2 = new DataTable();
                        adapter2.Fill(dt2);
                        Console.WriteLine(dt2.Rows.Count);
                        foreach (DataRow row in dt2.Rows)
                        {
                            DateTime dateNew = DateTime.Parse(pr.DatePublished);
                            
                            if (dateNew > (DateTime) row["doc_publish_date"])
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
                        Console.WriteLine(commandBuilder.GetUpdateCommand().CommandText);
                        adapter2.Update(dt2);
                        
                    }
                }
            }


        }
    }
}