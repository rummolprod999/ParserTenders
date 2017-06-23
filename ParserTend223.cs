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

namespace ParserTenders
{
    public class ParserTend223 : Parser
    {
        protected DataTable DtRegion;

        private string[] purchase_dir = new[]
        {
            "purchaseNotice", "purchaseNoticeAE", "purchaseNoticeAE94", "purchaseNoticeEP", "purchaseNoticeIS",
            "purchaseNoticeOA", "purchaseNoticeOK", "purchaseNoticeZK"
        };

        public ParserTend223(TypeArguments arg) : base(arg)
        {
        }

        public override void Parsing()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                foreach (string purchase in purchase_dir)
                {
                    List<String> arch = new List<string>();
                    string PathParse = "";
                    string RegionPath = (string) row["path223"];
                    switch (Program.Periodparsing)
                    {
                        case (TypeArguments.Last223):
                            PathParse = $"/out/published/{RegionPath}/{purchase}";
                            arch = GetListArchLast(PathParse, RegionPath, purchase);
                            break;
                        case (TypeArguments.Daily223):
                            PathParse = $"/out/published/{RegionPath}/{purchase}/daily";
                            arch = GetListArchDaily(PathParse, RegionPath, purchase);
                            break;
                    }

                    if (arch.Count == 0)
                    {
                        Log.Logger("Получен пустой список архивов", PathParse);
                        continue;
                    }

                    foreach (var v in arch)
                    {
                        GetListFileArch(v, PathParse, (string) row["conf"], (int) row["id"], purchase);
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

        public override void GetListFileArch(string Arch, string PathParse, string region, int region_id,
            string purchase)
        {
            string filea = "";
            string path_unzip = "";
            filea = GetArch223(Arch, PathParse);
            if (!String.IsNullOrEmpty(filea))
            {
                path_unzip = Unzipped.Unzip(filea);
                if (path_unzip != "")
                {
                    if (Directory.Exists(path_unzip))
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(path_unzip);
                        FileInfo[] filelist = dirInfo.GetFiles();
                        foreach (var f in filelist)
                        {
                            switch (purchase)
                            {
                                case "purchaseNotice":
                                    Bolter(f, region, region_id, TypeFile223.purchaseNotice);
                                    break;
                                case "purchaseNoticeAE":
                                    Bolter(f, region, region_id, TypeFile223.purchaseNoticeAE);
                                    break;
                                case "purchaseNoticeAE94":
                                    Bolter(f, region, region_id, TypeFile223.purchaseNoticeAE94);
                                    break;
                                case "purchaseNoticeEP":
                                    Bolter(f, region, region_id, TypeFile223.purchaseNoticeEP);
                                    break;
                                case "purchaseNoticeIS":
                                    Bolter(f, region, region_id, TypeFile223.purchaseNoticeIS);
                                    break;
                                case "purchaseNoticeOA":
                                    Bolter(f, region, region_id, TypeFile223.purchaseNoticeOA);
                                    break;
                                case "purchaseNoticeOK":
                                    Bolter(f, region, region_id, TypeFile223.purchaseNoticeOK);
                                    break;
                                case "purchaseNoticeZK":
                                    Bolter(f, region, region_id, TypeFile223.purchaseNoticeZK);
                                    break;
                            }
                        }

                        dirInfo.Delete(true);
                    }
                }
            }
        }

        public override void Bolter(FileInfo f, string region, int region_id, TypeFile223 typefile)
        {
            if (!f.Name.ToLower().EndsWith(".xml", StringComparison.Ordinal))
            {
                return;
            }

            /*f.Refresh();*/
            if (f.Length == 0)
            {
                return;
            }

            try
            {
                ParsingXML(f, region, region_id, typefile);
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при парсинге xml", e, f);
            }
        }

        public void ParsingXML(FileInfo f, string region, int region_id, TypeFile223 typefile)
        {
            using (StreamReader sr = new StreamReader(f.ToString(), Encoding.Default))
            {
                var ftext = sr.ReadToEnd();
                ftext = ClearText.ClearString(ftext);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(ftext);
                string jsons = JsonConvert.SerializeXmlNode(doc);
                JObject json = JObject.Parse(jsons);
                TenderType223 a = new TenderType223(f, region, region_id, json, typefile);
                a.Parsing();
            }
        }


        public override List<String> GetListArchLast(string PathParse, string RegionPath, string purchase)
        {
            List<string> archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            try
            {
                WorkWithFtp ftp = ClientFtp44_old();
                ftp.ChangeWorkingDirectory(PathParse);
                archtemp = ftp.ListDirectory();
            }
            catch (Exception e)
            {
                Log.Logger("Не могу найти директорию", PathParse);
            }
            List<String> years_search = Program.Years.Select(y => $"{purchase}_{RegionPath}{y}").ToList();
            return archtemp.Where(a => years_search.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)).ToList();
        }

        public override List<String> GetListArchDaily(string PathParse, string RegionPath, string purchase)
        {
            List<String> arch = new List<string>();
            List<string> archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            try
            {
                WorkWithFtp ftp = ClientFtp44_old();
                ftp.ChangeWorkingDirectory(PathParse);
                archtemp = ftp.ListDirectory();
            }
            catch (Exception e)
            {
                Log.Logger("Не могу найти директорию", PathParse);
            }
            List<String> years_search = Program.Years.Select(y => $"{purchase}_{RegionPath}{y}").ToList();
            foreach (var a in archtemp
                .Where(a => years_search.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)))
            {
                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    connect.Open();

                    string select_arch =
                        $"SELECT id FROM {Program.Prefix}arhiv_tenders223 WHERE arhiv = @archive";

                    MySqlCommand cmd = new MySqlCommand(select_arch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", a);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    bool res_read = reader.HasRows;
                    reader.Close();
                    if (!res_read)
                    {
                        string add_arch =
                            $"INSERT INTO {Program.Prefix}arhiv_tenders SET arhiv = @archive";
                        MySqlCommand cmd1 = new MySqlCommand(add_arch, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@archive", a);
                        cmd1.ExecuteNonQuery();
                        arch.Add(a);
                    }
                }
            }

            return arch;
        }
    }
}