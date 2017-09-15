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
                List<LotGpB> l = new List<LotGpB>();
                foreach (var lt in lots)
                {
                    int lotNum = (int?) proc.SelectToken("@number") ?? 0;
                    int status = (int?) proc.SelectToken("status") ?? 0;
                    
                }
            }
        }

        public override void ParsingProc()
        {
            
        }
    }
}