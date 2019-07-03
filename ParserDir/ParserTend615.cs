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
    public class ParserTend615 : Parser
    {
        private string[] _fileCancel = {"notificationcancel_"};
        private string[] _filecontract = {"contract_"};
        private string[] _fileDatechange = {"datechange_", "timeef_"};
        private string[] _fileLotcancel = {"lotcancel_"};
        private string[] _fileXml615 = {"notificationef_", "notificationpo_"};
        protected DataTable DtRegion;

        public ParserTend615(TypeArguments arg) : base(arg)
        {
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
                    case TypeArguments.Last615:
                        pathParse = $"/fcs_regions/{regionPath}/pprf615docs/notifications/";
                        arch = GetListArchLast(pathParse, regionPath);
                        break;
                    case TypeArguments.Curr615:
                        pathParse = $"/fcs_regions/{regionPath}/pprf615docs/notifications/currMonth/";
                        arch = GetListArchCurr(pathParse, regionPath);
                        break;
                    case TypeArguments.Prev615:
                        pathParse = $"/fcs_regions/{regionPath}/pprf615docs/notifications/prevMonth/";
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

        public void ParsingContractS()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                List<String> arch = new List<string>();
                string pathParse = "";
                string regionPath = (string) row["path"];
                switch (Program.Periodparsing)
                {
                    case TypeArguments.Last615:
                        pathParse = $"/fcs_regions/{regionPath}/pprf615docs/contracts/";
                        arch = GetListArchLast(pathParse, regionPath);
                        break;
                    case TypeArguments.Curr615:
                        pathParse = $"/fcs_regions/{regionPath}/pprf615docs/contracts/currMonth/";
                        arch = GetListArchCurr(pathParse, regionPath);
                        break;
                    case TypeArguments.Prev615:
                        pathParse = $"/fcs_regions/{regionPath}/pprf615docs/contracts/prevMonth/";
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
                        List<FileInfo> arrayXml615 = filelist
                            .Where(a => _fileXml615.Any(
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
                        List<FileInfo> arrayDatechange = filelist
                            .Where(a => _fileDatechange.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> arrayContract = filelist
                            .Where(a => _filecontract.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();

                        foreach (var f in arrayXml615)
                        {
                            Bolter(f, region, regionId, TypeFile615.TypeTen615);
                        }

                        foreach (var f in arrayLotcancel)
                        {
                            Bolter(f, region, regionId, TypeFile615.TypeLotCancel);
                        }

                        foreach (var f in arrayCancel)
                        {
                            Bolter(f, region, regionId, TypeFile615.TypeCancel);
                        }

                        foreach (var f in arrayDatechange)
                        {
                            Bolter(f, region, regionId, TypeFile615.TypeDateChange);
                        }

                        foreach (var f in arrayContract)
                        {
                            Bolter(f, region, regionId, TypeFile615.TypeContract);
                        }

                        dirInfo.Delete(true);
                    }
                }
            }
        }

        public override void Bolter(FileInfo f, string region, int regionId, TypeFile615 typefile)
        {
            if (!f.Name.ToLower().EndsWith(".xml", StringComparison.Ordinal))
            {
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

        public void ParsingXml(FileInfo f, string region, int regionId, TypeFile615 typefile)
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
                    case TypeFile615.TypeTen615:
                        var a = new TenderType615(f, region, regionId, json);
                        a.Parsing();
                        break;
                    case TypeFile615.TypeLotCancel:
                        var e = new TenderTypeLotCancel615(f, region, regionId, json);
                        e.Parsing();
                        break;
                    case TypeFile615.TypeCancel:
                        var g = new TenderTypeCancel615(f, region, regionId, json);
                        g.Parsing();
                        break;
                    case TypeFile615.TypeDateChange:
                        var c = new TenderTypeDateChange615(f, region, regionId, json);
                        c.Parsing();
                        break;
                    case TypeFile615.TypeContract:
                        var d = new TenderTypeSign615(f, region, regionId, json);
                        d.Parsing();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(typefile), typefile, null);
                }
            }
        }

        public override List<String> GetListArchLast(string pathParse, string regionPath)
        {
            List<string> archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            archtemp = GetListFtp44(pathParse);
            List<String> yearsSearch = Program.Years.Select(y => $"notification_{regionPath}{y}").ToList();
            return archtemp.Where(a => yearsSearch.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)).ToList();
        }

        public override List<String> GetListArchCurr(string pathParse, string regionPath)
        {
            List<String> arch = new List<string>();
            //List<string> archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            //archtemp = GetListFtp44(pathParse);
            var newLs = GetListFtp44New(pathParse);
            List<String> yearsSearch = Program.Years.Select(y => $"notification_{regionPath}{y}").ToList();
            foreach (var a in newLs.Where(a =>
                yearsSearch.Any(t => a.Item1.IndexOf(t, StringComparison.Ordinal) != -1)))
            {
                if (a.Item2 == 0)
                {
                    Log.Logger("!!!archive size = 0", a.Item1);
                    continue;
                }

                var b = $"pprf615_{a.Item1}";
                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    string selectArch =
                        $"SELECT id FROM {Program.Prefix}arhiv_tenders WHERE arhiv = @archive AND size_archive IN(0, @size_archive)";
                    MySqlCommand cmd = new MySqlCommand(selectArch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", b);
                    cmd.Parameters.AddWithValue("@size_archive", a.Item2);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    bool resRead = reader.HasRows;
                    reader.Close();
                    if (!resRead)
                    {
                        string addArch =
                            $"INSERT INTO {Program.Prefix}arhiv_tenders SET arhiv = @archive, size_archive = @size_archive";
                        MySqlCommand cmd1 = new MySqlCommand(addArch, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@archive", b);
                        cmd1.Parameters.AddWithValue("@size_archive", a.Item2);
                        cmd1.ExecuteNonQuery();
                        arch.Add(a.Item1);
                    }
                }
            }

            return arch;
        }

        public override List<String> GetListArchPrev(string pathParse, string regionPath)
        {
            List<String> arch = new List<string>();
            //List<string> archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            //archtemp = GetListFtp44(pathParse);
            var newLs = GetListFtp44New(pathParse);
            //string serachd = $"{Program.LocalDate:yyyyMMdd}";
            foreach (var a in newLs)
            {
                string prevA = $"prev_pprf615_{a.Item1}";
                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    string selectArch =
                        $"SELECT id FROM {Program.Prefix}arhiv_tenders WHERE arhiv = @archive AND size_archive IN(0, @size_archive)";
                    MySqlCommand cmd = new MySqlCommand(selectArch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", prevA);
                    cmd.Parameters.AddWithValue("@size_archive", a.Item2);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    bool resRead = reader.HasRows;
                    reader.Close();
                    if (!resRead)
                    {
                        string addArch =
                            $"INSERT INTO {Program.Prefix}arhiv_tenders SET arhiv = @archive, size_archive = @size_archive";
                        MySqlCommand cmd1 = new MySqlCommand(addArch, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@archive", prevA);
                        cmd1.Parameters.AddWithValue("@size_archive", a.Item2);
                        cmd1.ExecuteNonQuery();
                        arch.Add(a.Item1);
                    }
                }
            }

            return arch;
        }
    }
}