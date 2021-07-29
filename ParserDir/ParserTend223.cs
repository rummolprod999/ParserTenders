using System;
using System.Collections;
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
        private string[] _purchaseDir = {
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
                foreach (var purchase in _purchaseDir)
                {
                    var arch = new List<string>();
                    var pathParse = "";
                    var regionPath = (string) row["path223"];
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
        
        public void ParsingAst()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                foreach (var purchase in _purchaseDir)
                {
                    var arch = new List<string>();
                    var pathParse = "";
                    var regionPath = (string) row["path223"];
                    switch (Program.Periodparsing)
                    {
                        case (TypeArguments.Last223):
                            pathParse = $"/out/published/ast/{regionPath}/{purchase}/";
                            arch = GetListArchLast(pathParse, regionPath, purchase);
                            break;
                        case (TypeArguments.Daily223):
                            pathParse = $"/out/published/ast/{regionPath}/{purchase}/daily/";
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

        public void ParserLostTens()
        {
            var rootPath = "/out";
            var archtemp = GetListFtp223(rootPath);
            foreach (var a in archtemp)
            {
                if (!a.Contains("0000")) continue;
                try
                {
                    GetElementsFromFtp($"/{a}/");
                }
                catch (Exception e)
                {
                    Log.Logger(e);
                }
            }
        }

        private void GetElementsFromFtp(string s)
        {
            var arr = new ListArchDailyLost(this, s);
            foreach (var s1 in arr)
            {
                if (s1.Contains("purchaseNoticeAESMBO"))
                {
                    GetListFileArch(s1, s, "", 0, "purchaseNoticeAESMBO");
                }
                else if (s1.Contains("purchaseNoticeKESMBO"))
                {
                    GetListFileArch(s1, s, "", 0, "purchaseNoticeKESMBO");
                }
                else if (s1.Contains("purchaseNoticeZKESMBO"))
                {
                    GetListFileArch(s1, s, "", 0, "purchaseNoticeZKESMBO");
                }
                else if (s1.Contains("purchaseNoticeZPESMBO"))
                {
                    GetListFileArch(s1, s, "", 0, "purchaseNoticeZPESMBO");
                }
                else if (s1.Contains("purchaseRejection"))
                {
                    GetListFileArch(s1, s, "", 0, "purchaseRejection");
                }
                else if (s1.Contains("lotCancellation"))
                {
                    GetListFileArch(s1, s, "", 0, "lotCancellation");
                }
                else if (s1.Contains("purchaseNoticeZK"))
                {
                    GetListFileArch(s1, s, "", 0, "purchaseNoticeZK");
                }
                else if (s1.Contains("purchaseNoticeOK"))
                {
                    GetListFileArch(s1, s, "", 0, "purchaseNoticeOK");
                }
                else if (s1.Contains("purchaseNoticeOA"))
                {
                    GetListFileArch(s1, s, "", 0, "purchaseNoticeOA");
                }
                else if (s1.Contains("purchaseNoticeIS"))
                {
                    GetListFileArch(s1, s, "", 0, "purchaseNoticeIS");
                }
                else if (s1.Contains("purchaseNoticeEP"))
                {
                    GetListFileArch(s1, s, "", 0, "purchaseNoticeEP");
                }
                else if (s1.Contains("purchaseNoticeAE94"))
                {
                    GetListFileArch(s1, s, "", 0, "purchaseNoticeAE94");
                }
                else if (s1.Contains("purchaseNoticeAE"))
                {
                    GetListFileArch(s1, s, "", 0, "purchaseNoticeAE");
                }
                else if (s1.Contains("purchaseNotice"))
                {
                    GetListFileArch(s1, s, "", 0, "purchaseNotice");
                }
            }
        }

        public override void GetListFileArch(string arch, string pathParse, string region, int regionId,
            string purchase)
        {
            var filea = "";
            var pathUnzip = "";
            filea = GetArch223(arch, pathParse);
            if (string.IsNullOrEmpty(filea)) return;
            pathUnzip = Unzipped.Unzip(filea);
            if (pathUnzip != "")
            {
                if (Directory.Exists(pathUnzip))
                {
                    var dirInfo = new DirectoryInfo(pathUnzip);
                    var filelist = dirInfo.GetFiles();
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
            using (var sr = new StreamReader(f.ToString(), Encoding.Default))
            {
                var ftext = sr.ReadToEnd();
                ftext = ClearText.ClearString(ftext);
                var doc = new XmlDocument();
                doc.LoadXml(ftext);
                var jsons = JsonConvert.SerializeXmlNode(doc);
                var json = JObject.Parse(jsons);
                switch (typefile)
                {
                    case TypeFile223.PurchaseLotCancellation:
                        var k = new TenderTypeLotCancel223(f, region, regionId, json);
                        k.Parsing();
                        break;
                    case TypeFile223.PurchaseRejection:
                        var r = new TenderTypeCancel223(f, region, regionId, json);
                        r.Parsing();
                        break;
                    default:
                        var a = new TenderType223(f, region, regionId, json, typefile);
                        a.Parsing();
                        break;
                }
            }
        }

        public override List<string> GetListArchLast(string pathParse, string regionPath, string purchase)
        {
            var archtemp = new List<string>();
            archtemp = GetListFtp223(pathParse);
            var yearsSearch = Program.Years.Select(y => $"{purchase}_{regionPath}{y}").ToList();
            return archtemp.Where(a => yearsSearch.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)).ToList();
        }

        public override List<string> GetListArchDaily(string pathParse, string regionPath, string purchase)
        {
            var arch = new List<string>();
            //List<string> archtemp = new List<string>();
            //archtemp = GetListFtp223(pathParse);
            var newLs = GetListFtp223New(pathParse);
            var yearsSearch = Program.Years.Select(y => $"{purchase}_{regionPath}{y}").ToList();
            foreach (var a in newLs
                .Where(a => yearsSearch.Any(t => a.Item1.IndexOf(t, StringComparison.Ordinal) != -1)))
            {
                if (a.Item2 == 0)
                {
                    Log.Logger("!!!archive size = 0", a.Item1);
                    continue;
                }

                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();

                    var selectArch =
                        $"SELECT id FROM {Program.Prefix}arhiv_tenders WHERE arhiv = @archive AND size_archive IN(0, @size_archive)";

                    var cmd = new MySqlCommand(selectArch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", a.Item1);
                    cmd.Parameters.AddWithValue("@size_archive", a.Item2);
                    var reader = cmd.ExecuteReader();
                    var resRead = reader.HasRows;
                    reader.Close();
                    if (resRead) continue;
                    var addArch =
                        $"INSERT INTO {Program.Prefix}arhiv_tenders SET arhiv = @archive, size_archive = @size_archive";
                    var cmd1 = new MySqlCommand(addArch, connect);
                    cmd1.Prepare();
                    cmd1.Parameters.AddWithValue("@archive", a.Item1);
                    cmd1.Parameters.AddWithValue("@size_archive", a.Item2);
                    cmd1.ExecuteNonQuery();
                    arch.Add(a.Item1);
                }
            }

            return arch;
        }


        class ListArchDailyLost: IEnumerable<string>
        {
            private ParserTend223 tnd;
            private string pathParse;

            public ListArchDailyLost(ParserTend223 t, string pathParse)
            {
                tnd = t;
                this.pathParse = pathParse;
            }

            public IEnumerator<string> GetEnumerator()
            {
                var arch = new List<string>();
                var newLs = tnd.GetListFtp223New(pathParse);
                var yearsSearch = Program.Years.Select(y => $"{y}").ToList();
                foreach (var a in newLs
                    .Where(a => yearsSearch.Any(t => a.Item1.IndexOf(t, StringComparison.Ordinal) != -1))
                    .Where(a => tnd._purchaseDir.Any(t => a.Item1.ToLower().Contains(t.ToLower()))).Reverse())
                {
                    if (a.Item2 == 0)
                    {
                        Log.Logger("!!!archive size = 0", a.Item1);
                        continue;
                    }

                    using (var connect = ConnectToDb.GetDbConnection())
                    {
                        connect.Open();

                        var selectArch =
                            $"SELECT id FROM {Program.Prefix}arhiv_tenders WHERE arhiv = @archive AND size_archive IN(0, @size_archive)";

                        var cmd = new MySqlCommand(selectArch, connect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@archive", a.Item1);
                        cmd.Parameters.AddWithValue("@size_archive", a.Item2);
                        var reader = cmd.ExecuteReader();
                        var resRead = reader.HasRows;
                        reader.Close();
                        if (resRead) continue;
                        var addArch =
                            $"INSERT INTO {Program.Prefix}arhiv_tenders SET arhiv = @archive, size_archive = @size_archive";
                        var cmd1 = new MySqlCommand(addArch, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@archive", a.Item1);
                        cmd1.Parameters.AddWithValue("@size_archive", a.Item2);
                        cmd1.ExecuteNonQuery();
                        arch.Add(a.Item1);
                    }

                    yield return a.Item1;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}