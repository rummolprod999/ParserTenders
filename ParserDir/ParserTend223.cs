using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParserTenders.TenderDir;

namespace ParserTenders.ParserDir
{
    public class ParserTend223 : Parser
    {
        private string[] _purchaseDir = new[]
        {
            "purchaseNotice", "purchaseNoticeAE", "purchaseNoticeAE94", "purchaseNoticeEP", "purchaseNoticeIS",
            "purchaseNoticeOA", "purchaseNoticeOK", "purchaseNoticeZK", "lotCancellation", "purchaseRejection",
            "purchaseNoticeZPESMBO", "purchaseNoticeZKESMBO", "purchaseNoticeKESMBO", "purchaseNoticeAESMBO"
        };

        protected DataTable DtRegion;

        public ParserTend223(TypeArguments arg) : base(arg)
        {
        }

        public override void Parsing()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                foreach (string purchase in _purchaseDir)
                {
                    List<String> arch = new List<string>();
                    string pathParse = "";
                    string regionPath = (string) row["path223"];
                    switch (Program.Periodparsing)
                    {
                        case (TypeArguments.Last223):
                            pathParse = $"/out/published/{regionPath}/{purchase}/";
                            arch = GetListArchLast(pathParse, regionPath, purchase);
                            break;
                        case (TypeArguments.Daily223):
                            pathParse = $"/out/published/{regionPath}/{purchase}/daily/";
                            arch = GetListArchDaily(pathParse, regionPath, purchase);
                            break;
                    }

                    if (arch.Count == 0)
                    {
                        Log.Logger("Получен пустой список архивов", pathParse);
                        continue;
                    }

                    foreach (var v in arch)
                    {
                        GetListFileArch(v, pathParse, (string) row["conf"], (int) row["id"], purchase);
                    }
                }
            }

            try
            {
                CheckInn();
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при обновлении инн", e);
            }
        }

        public override void GetListFileArch(string arch, string pathParse, string region, int regionId,
            string purchase)
        {
            string filea = "";
            string pathUnzip = "";
            filea = GetArch223(arch, pathParse);
            if (!String.IsNullOrEmpty(filea))
            {
                pathUnzip = Unzipped.Unzip(filea);
                if (pathUnzip != "")
                {
                    if (Directory.Exists(pathUnzip))
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(pathUnzip);
                        FileInfo[] filelist = dirInfo.GetFiles();
                        foreach (var f in filelist)
                        {
                            switch (purchase)
                            {
                                case "purchaseNotice":
                                    Bolter(f, region, regionId, TypeFile223.PurchaseNotice);
                                    break;
                                case "purchaseNoticeAE":
                                    Bolter(f, region, regionId, TypeFile223.PurchaseNoticeAe);
                                    break;
                                case "purchaseNoticeAE94":
                                    Bolter(f, region, regionId, TypeFile223.PurchaseNoticeAe94);
                                    break;
                                case "purchaseNoticeEP":
                                    Bolter(f, region, regionId, TypeFile223.PurchaseNoticeEp);
                                    break;
                                case "purchaseNoticeIS":
                                    Bolter(f, region, regionId, TypeFile223.PurchaseNoticeIs);
                                    break;
                                case "purchaseNoticeOA":
                                    Bolter(f, region, regionId, TypeFile223.PurchaseNoticeOa);
                                    break;
                                case "purchaseNoticeOK":
                                    Bolter(f, region, regionId, TypeFile223.PurchaseNoticeOk);
                                    break;
                                case "purchaseNoticeZK":
                                    Bolter(f, region, regionId, TypeFile223.PurchaseNoticeZk);
                                    break;
                                case "lotCancellation":
                                    Bolter(f, region, regionId, TypeFile223.PurchaseLotCancellation);
                                    break;
                                case "purchaseRejection":
                                    Bolter(f, region, regionId, TypeFile223.PurchaseRejection);
                                    break;
                                case "purchaseNoticeZPESMBO":
                                    Bolter(f, region, regionId, TypeFile223.PurchaseNoticeZpesmbo);
                                    break;
                                case "purchaseNoticeZKESMBO":
                                    Bolter(f, region, regionId, TypeFile223.PurchaseNoticeZkesmbo);
                                    break;
                                case "purchaseNoticeKESMBO":
                                    Bolter(f, region, regionId, TypeFile223.PurchaseNoticeKesmbo);
                                    break;
                                case "purchaseNoticeAESMBO":
                                    Bolter(f, region, regionId, TypeFile223.PurchaseNoticeAesmbo);
                                    break;
                            }
                        }

                        dirInfo.Delete(true);
                    }
                }
                else
                {
                    Log.Logger("pathUnzip does not exist", filea);
                }
            }
        }

        public override void Bolter(FileInfo f, string region, int regionId, TypeFile223 typefile)
        {
            if (!f.Name.ToLower().EndsWith(".xml", StringComparison.Ordinal))
            {
                return;
            }

            /*f.Refresh();*/
            if (f.Length == 0)
            {
                //Log.Logger("!!!file size = 0", f);
                return;
            }

            try
            {
                ParsingXml(f, region, regionId, typefile);
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при парсинге xml", e, f);
                Log.Logger(e.Source);
                Log.Logger(e.StackTrace);
            }
        }

        public void ParsingXml(FileInfo f, string region, int regionId, TypeFile223 typefile)
        {
            using (StreamReader sr = new StreamReader(f.ToString(), Encoding.Default))
            {
                var ftext = sr.ReadToEnd();
                ftext = ClearText.ClearString(ftext);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(ftext);
                string jsons = JsonConvert.SerializeXmlNode(doc);
                JObject json = JObject.Parse(jsons);
                switch (typefile)
                {
                    case TypeFile223.PurchaseLotCancellation:
                        TenderTypeLotCancel223 k = new TenderTypeLotCancel223(f, region, regionId, json);
                        k.Parsing();
                        break;
                    case TypeFile223.PurchaseRejection:
                        TenderTypeCancel223 r = new TenderTypeCancel223(f, region, regionId, json);
                        r.Parsing();
                        break;
                    default:
                        TenderType223 a = new TenderType223(f, region, regionId, json, typefile);
                        a.Parsing();
                        break;
                }
            }
        }

        public override List<String> GetListArchLast(string pathParse, string regionPath, string purchase)
        {
            List<string> archtemp = new List<string>();
            archtemp = GetListFtp223(pathParse);
            List<String> yearsSearch = Program.Years.Select(y => $"{purchase}_{regionPath}{y}").ToList();
            return archtemp.Where(a => yearsSearch.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)).ToList();
        }

        public override List<String> GetListArchDaily(string pathParse, string regionPath, string purchase)
        {
            List<String> arch = new List<string>();
            //List<string> archtemp = new List<string>();
            //archtemp = GetListFtp223(pathParse);
            var newLs = GetListFtp223New(pathParse);
            List<String> yearsSearch = Program.Years.Select(y => $"{purchase}_{regionPath}{y}").ToList();
            foreach (var a in newLs
                .Where(a => yearsSearch.Any(t => a.Item1.IndexOf(t, StringComparison.Ordinal) != -1)))
            {
                if (a.Item2 == 0)
                {
                    Log.Logger("!!!archive size = 0", a.Item1);
                    continue;
                }

                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();

                    string selectArch =
                        $"SELECT id FROM {Program.Prefix}arhiv_tenders WHERE arhiv = @archive AND size_archive IN(0, @size_archive)";

                    MySqlCommand cmd = new MySqlCommand(selectArch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", a.Item1);
                    cmd.Parameters.AddWithValue("@size_archive", a.Item2);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    bool resRead = reader.HasRows;
                    reader.Close();
                    if (resRead) continue;
                    string addArch =
                        $"INSERT INTO {Program.Prefix}arhiv_tenders SET arhiv = @archive, size_archive = @size_archive";
                    MySqlCommand cmd1 = new MySqlCommand(addArch, connect);
                    cmd1.Prepare();
                    cmd1.Parameters.AddWithValue("@archive", a.Item1);
                    cmd1.Parameters.AddWithValue("@size_archive", a.Item2);
                    cmd1.ExecuteNonQuery();
                    arch.Add(a.Item1);
                }
            }

            return arch;
        }
    }
}