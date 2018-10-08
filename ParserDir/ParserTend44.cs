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
    public class ParserTend44 : Parser
    {
        protected DataTable DtRegion;

        private string[] _fileXml44 = new[]
        {
            "ea44_", "ep44_", "ok44_", "okd44_", "oku44_", "po44_", "za44_", "zk44_",
            "zkb44_", "zkk44_", "zkkd44_", "zkku44_", "zp44_", "protocolzkbi_", "inm111_"
        };

        private string[] _fileCancel = new[] {"notificationcancel_"};
        private string[] _fileSign = new[] {"contractsign_"};
        private string[] _fileCancelFailure = new[] {"cancelfailure_"};
        private string[] _fileProlongation = new[] {"prolongation"};
        private string[] _fileDatechange = new[] {"datechange_"};
        private string[] _fileOrgchange = new[] {"orgchange_"};
        private string[] _fileLotcancel = new[] {"lotcancel_"};
        private string[] _fileClarification = new[] {"clarification_", "epclarificationdoc_"};
        private string[] _fileXml504 = new[]
        {
            "zk504_", "zp504_", "ok504_", "okd504_", "okou504_", "oku504_"
        };

        readonly List<string> summList;
        public ParserTend44(TypeArguments arg) : base(arg)
        {
            summList = new List<string>().AddRangeAndReturnList(_fileCancel.ToList()).AddRangeAndReturnList(_fileCancelFailure.ToList()).AddRangeAndReturnList(_fileClarification.ToList()).AddRangeAndReturnList(_fileDatechange.ToList()).AddRangeAndReturnList(_fileLotcancel.ToList()).AddRangeAndReturnList(_fileOrgchange.ToList()).AddRangeAndReturnList(_fileProlongation.ToList()).AddRangeAndReturnList(_fileSign.ToList()).AddRangeAndReturnList(_fileXml44.ToList()).AddRangeAndReturnList(_fileXml504.ToList());
        }

        public override void Parsing()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                List<String> arch = new List<string>();
                string pathParse = "";
                string regionPath = (string) row["path"];
                switch (Program.Periodparsing)
                {
                    case TypeArguments.Last44:
                        pathParse = $"/fcs_regions/{regionPath}/notifications/";
                        arch = GetListArchLast(pathParse, regionPath);
                        break;
                    case TypeArguments.Curr44:
                        pathParse = $"/fcs_regions/{regionPath}/notifications/currMonth/";
                        arch = GetListArchCurr(pathParse, regionPath);
                        break;
                    case TypeArguments.Prev44:
                        pathParse = $"/fcs_regions/{regionPath}/notifications/prevMonth/";
                        arch = GetListArchPrev(pathParse, regionPath);
                        break;
                }

                if (arch.Count == 0)
                {
                    Log.Logger("Получен пустой список архивов", pathParse);
                    continue;
                }

                foreach (var v in arch)
                {
                    GetListFileArch(v, pathParse, (string) row["conf"], (int) row["id"]);
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

        public override void GetListFileArch(string arch, string pathParse, string region, int regionId)
        {
            string filea = "";
            string pathUnzip = "";
            filea = GetArch44(arch, pathParse);
            if (!String.IsNullOrEmpty(filea))
            {
                pathUnzip = Unzipped.Unzip(filea);
                if (pathUnzip != "")
                {
                    if (Directory.Exists(pathUnzip))
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(pathUnzip);
                        FileInfo[] filelist = dirInfo.GetFiles();
                        List<FileInfo> arrayXml44 = filelist
                            .Where(a => _fileXml44.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> arrayProlongation = filelist
                            .Where(a => _fileProlongation.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> arrayDatechange = filelist
                            .Where(a => _fileDatechange.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> arrayOrgchange = filelist
                            .Where(a => _fileOrgchange.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> arrayLotcancel = filelist
                            .Where(a => _fileLotcancel.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> arrayCancel = filelist
                            .Where(a => _fileCancel.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> arraySign = filelist
                            .Where(a => _fileSign.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> arrayCancelFailure = filelist
                            .Where(a => _fileCancelFailure.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> arrayClarification = filelist
                            .Where(a => _fileClarification.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> arrayXml504 = filelist
                            .Where(a => _fileXml504.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> arrayNull = filelist
                            .Where(a => summList.All(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) == -1))
                            .ToList();

                        foreach (var f in arrayXml44)
                        {
                            Bolter(f, region, regionId, TypeFile44.TypeTen44);
                        }
                        foreach (var f in arrayProlongation)
                        {
                            Bolter(f, region, regionId, TypeFile44.TypeProlongation);
                        }
                        foreach (var f in arrayDatechange)
                        {
                            Bolter(f, region, regionId, TypeFile44.TypeDateChange);
                        }
                        foreach (var f in arrayOrgchange)
                        {
                            Bolter(f, region, regionId, TypeFile44.TypeOrgChange);
                        }
                        foreach (var f in arrayLotcancel)
                        {
                            Bolter(f, region, regionId, TypeFile44.TypeLotCancel);
                        }
                        foreach (var f in arrayCancel)
                        {
                            Bolter(f, region, regionId, TypeFile44.TypeCancel);
                        }
                        foreach (var f in arraySign)
                        {
                            Bolter(f, region, regionId, TypeFile44.TypeSign);
                        } 
                        foreach (var f in arrayCancelFailure)
                        {
                            Bolter(f, region, regionId, TypeFile44.TypeCancelFailure);
                        }

                        foreach (var f in arrayClarification)
                        {
                            Bolter(f, region, regionId, TypeFile44.TypeClarification);
                        }
                        foreach (var f in arrayXml504)
                        {
                            Bolter(f, region, regionId, TypeFile44.TypeTen504);
                        }
                        foreach (var f in arrayNull)
                        {
                            Bolter(f, region, regionId, TypeFile44.TypeNull);
                        }
                        dirInfo.Delete(true);
                    }
                }
            }
        }

        public override void Bolter(FileInfo f, string region, int regionId, TypeFile44 typefile)
        {
            if (!f.Name.ToLower().EndsWith(".xml", StringComparison.Ordinal))
            {
                return;
            }

            if (typefile == TypeFile44.TypeNull)
            {
                if (!f.Name.Contains("PlacementResult_") && !f.Name.Contains("NotificationEA615_") && !f.Name.Contains("NotificationPO615_"))
                {
                    Log.Logger("!!!Can not parse this file", f.FullName, region);
                    Log.Logger("\n");
                }
               
                return;
            }
            try
            {
                ParsingXml(f, region, regionId, typefile);
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при парсинге xml", e, f);
            }
        }

        public void ParsingXml(FileInfo f, string region, int regionId, TypeFile44 typefile)
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
                    case TypeFile44.TypeTen44:
                        TenderType44 a = new TenderType44(f, region, regionId, json);
                        a.Parsing();
                        break;
                    case TypeFile44.TypeProlongation:
                        TenderTypeProlongation b = new TenderTypeProlongation(f, region, regionId, json);
                        b.Parsing();
                        break;
                    case TypeFile44.TypeDateChange:
                        TenderTypeDateChange c = new TenderTypeDateChange(f, region, regionId, json);
                        c.Parsing();
                        break;
                    case TypeFile44.TypeOrgChange:
                        TenderTypeOrgChange d = new TenderTypeOrgChange(f, region, regionId, json);
                        d.Parsing();
                        break;
                    case TypeFile44.TypeLotCancel:
                        TenderTypeLotCancel e = new TenderTypeLotCancel(f, region, regionId, json);
                        e.Parsing();
                        break;
                    case TypeFile44.TypeCancel:
                        TenderTypeCancel g = new TenderTypeCancel(f, region, regionId, json);
                        g.Parsing();
                        break;
                    case TypeFile44.TypeCancelFailure:
                        TenderTypeCancelFailure h = new TenderTypeCancelFailure(f, region, regionId, json);
                        h.Parsing();
                        break;
                    case TypeFile44.TypeSign:
                        TenderTypeSign n = new TenderTypeSign(f, region, regionId, json);
                        n.Parsing();
                        break;
                    case TypeFile44.TypeClarification:
                        TenderTypeClarification m = new TenderTypeClarification(f, region, regionId, json);
                        m.Parsing();
                        break;
                    case TypeFile44.TypeTen504:
                        TenderType504 o = new TenderType504(f, region, regionId, json);
                        o.Parsing();
                        break;
                }
            }
        }

        public override List<String> GetListArchLast(string pathParse, string regionPath)
        {
            List<string> archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            archtemp = GetListFtp44(pathParse);
            List<String> yearsSearch = Program.Years.Select(y => $"notification_{regionPath}{y}").ToList();
            yearsSearch.AddRange(Program.Years.Select(y => $"notification{y}").ToList());
            return archtemp.Where(a => yearsSearch.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)).ToList();
        }

        public override List<String> GetListArchCurr(string pathParse, string regionPath)
        {
            List<String> arch = new List<string>();
            List<string> archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            archtemp = GetListFtp44(pathParse);
            List<String> yearsSearch = Program.Years.Select(y => $"notification_{regionPath}{y}").ToList();
            yearsSearch.AddRange(Program.Years.Select(y => $"notification{y}").ToList());
            foreach (var a in archtemp.Where(a => yearsSearch.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)))
            {
                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    string selectArch =
                        $"SELECT id FROM {Program.Prefix}arhiv_tenders WHERE arhiv = @archive";
                    MySqlCommand cmd = new MySqlCommand(selectArch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", a);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    bool resRead = reader.HasRows;
                    reader.Close();
                    if (!resRead)
                    {
                        string addArch =
                            $"INSERT INTO {Program.Prefix}arhiv_tenders SET arhiv = @archive";
                        MySqlCommand cmd1 = new MySqlCommand(addArch, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@archive", a);
                        cmd1.ExecuteNonQuery();
                        arch.Add(a);
                    }
                }
            }

            return arch;
        }

        public override List<String> GetListArchPrev(string pathParse, string regionPath)
        {
            List<String> arch = new List<string>();
            List<string> archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            archtemp = GetListFtp44(pathParse);
            string serachd = $"{Program.LocalDate:yyyyMMdd}";
            foreach (var a in archtemp.Where(a => a.IndexOf(serachd, StringComparison.Ordinal) != -1))
            {
                string prevA = $"prev_{a}";
                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    string selectArch =
                        $"SELECT id FROM {Program.Prefix}arhiv_tenders WHERE arhiv = @archive";
                    MySqlCommand cmd = new MySqlCommand(selectArch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", prevA);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    bool resRead = reader.HasRows;
                    reader.Close();
                    if (!resRead)
                    {
                        string addArch =
                            $"INSERT INTO {Program.Prefix}arhiv_tenders SET arhiv = @archive";
                        MySqlCommand cmd1 = new MySqlCommand(addArch, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@archive", prevA);
                        cmd1.ExecuteNonQuery();
                        arch.Add(a);
                    }
                }
            }

            return arch;
        }
    }
}