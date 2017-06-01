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

namespace ParserTenders
{
    public class ParserTend44 : Parser
    {
        protected DataTable DtRegion;

        private string[] file_xml44 = new[]
        {
            "ea44_", "ep44_", "ok44_", "okd44_", "oku44_", "po44_", "za44_", "zk44_",
            "zkb44_", "zkk44_", "zkkd44_", "zkku44_", "zp44_", "protocolzkbi_"
        };

        private string[] file_cancel = new[] {"notificationcancel_"};
        private string[] file_sign = new[] {"contractsign_"};
        private string[] file_cancelFailure = new[] {"cancelfailure_"};
        private string[] file_prolongation = new[] {"prolongation"};
        private string[] file_datechange = new[] {"datechange_"};
        private string[] file_orgchange = new[] {"orgchange_"};
        private string[] file_lotcancel = new[] {"lotcancel_"};

        public ParserTend44(TypeArguments arg) : base(arg)
        {
        }

        public override void Parsing()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                List<String> arch = new List<string>();
                string PathParse = "";
                string RegionPath = (string) row["path"];
                switch (Program.Periodparsing)
                {
                    case TypeArguments.Last44:
                        PathParse = $"/fcs_regions/{RegionPath}/notifications/";
                        arch = GetListArchLast(PathParse, RegionPath);
                        break;
                    case TypeArguments.Curr44:
                        PathParse = $"/fcs_regions/{RegionPath}/notifications/currMonth/";
                        arch = GetListArchCurr(PathParse, RegionPath);
                        break;
                    case TypeArguments.Prev44:
                        PathParse = $"/fcs_regions/{RegionPath}/notifications/prevMonth/";
                        arch = GetListArchPrev(PathParse, RegionPath);
                        break;
                }

                if (arch.Count == 0)
                {
                    Log.Logger("Получен пустой список архивов", row["path"]);
                    continue;
                }

                foreach (var v in arch)
                {
                    GetListFileArch(v, PathParse, (string) row["conf"], (int) row["id"]);
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

        public override void GetListFileArch(string Arch, string PathParse, string region, int region_id)
        {
            string filea = "";
            string path_unzip = "";
            filea = GetArch44(Arch, PathParse);
            if (!String.IsNullOrEmpty(filea))
            {
                path_unzip = Unzipped.Unzip(filea);
                if (path_unzip != "")
                {
                    if (Directory.Exists(path_unzip))
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(path_unzip);
                        FileInfo[] filelist = dirInfo.GetFiles();
                        List<FileInfo> array_xml44 = filelist
                            .Where(a => file_xml44.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> array_prolongation = filelist
                            .Where(a => file_prolongation.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> array_datechange = filelist
                            .Where(a => file_datechange.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> array_orgchange = filelist
                            .Where(a => file_orgchange.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> array_lotcancel = filelist
                            .Where(a => file_lotcancel.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> array_cancel = filelist
                            .Where(a => file_cancel.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> array_sign = filelist
                            .Where(a => file_sign.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> array_cancelFailure = filelist
                            .Where(a => file_cancelFailure.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();

                        foreach (var f in array_xml44)
                        {
                            Bolter(f, region, region_id, TypeFile44.TypeTen44);
                        }
                        foreach (var f in array_prolongation)
                        {
                            Bolter(f, region, region_id, TypeFile44.TypeProlongation);
                        }
                        foreach (var f in array_datechange)
                        {
                            Bolter(f, region, region_id, TypeFile44.TypeDatechange);
                        }
                        foreach (var f in array_orgchange)
                        {
                            Bolter(f, region, region_id, TypeFile44.TypeOrgchange);
                        }
                        foreach (var f in array_lotcancel)
                        {
                            Bolter(f, region, region_id, TypeFile44.TypeLotcancel);
                        }
                        foreach (var f in array_cancel)
                        {
                            Bolter(f, region, region_id, TypeFile44.TypeCancel);
                        }
                        foreach (var f in array_sign)
                        {
                            Bolter(f, region, region_id, TypeFile44.TypeSign);
                        }
                        foreach (var f in array_cancelFailure)
                        {
                            Bolter(f, region, region_id, TypeFile44.TypeCancelFailure);
                        }

                        dirInfo.Delete(true);
                    }
                }
            }
        }

        public override void Bolter(FileInfo f, string region, int region_id, TypeFile44 typefile)
        {
            if (!f.Name.EndsWith(".xml", StringComparison.Ordinal))
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

        public void ParsingXML(FileInfo f, string region, int region_id, TypeFile44 typefile)
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
                        TenderType44 a = new TenderType44(f, region, region_id, json);
                        a.Parsing();
                        break;
                    case TypeFile44.TypeProlongation:
                        TenderTypeProlongation b = new TenderTypeProlongation(f, region, region_id, json);
                        b.Parsing();
                        break;
                    case TypeFile44.TypeDatechange:
                        TenderTypeDateChange c = new TenderTypeDateChange(f, region, region_id, json);
                        c.Parsing();
                        break;
                    case TypeFile44.TypeOrgchange:
                        TenderTypeOrgChange d = new TenderTypeOrgChange(f, region, region_id, json);
                        d.Parsing();
                        break;
                    case TypeFile44.TypeLotcancel:
                        TenderTypeLotCancel e = new TenderTypeLotCancel(f, region, region_id, json);
                        e.Parsing();
                        break;
                    case TypeFile44.TypeCancel:
                        TenderTypeCancel g = new TenderTypeCancel(f, region, region_id, json);
                        g.Parsing();
                        break;
                    case TypeFile44.TypeCancelFailure:
                        TenderTypeCancelFailure h = new TenderTypeCancelFailure(f, region, region_id, json);
                        h.Parsing();
                        break;
                }
            }
        }

        public override List<String> GetListArchLast(string PathParse, string RegionPath)
        {
            WorkWithFtp ftp = ClientFtp44();
            try
            {
                ftp.ChangeWorkingDirectory(PathParse);
            }
            catch (Exception e)
            {
                Log.Logger("Не удалось найти путь ftp", PathParse, e);
                return new List<string>();
            }

            List<String> archtemp = ftp.ListDirectory();
            List<String> years_search = Program.Years.Select(y => $"notification_{RegionPath}{y}").ToList();
            return archtemp.Where(a => years_search.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)).ToList();
        }

        public override List<String> GetListArchCurr(string PathParse, string RegionPath)
        {
            List<String> arch = new List<string>();
            WorkWithFtp ftp = ClientFtp44();
            try
            {
                ftp.ChangeWorkingDirectory(PathParse);
            }
            catch (Exception e)
            {
                Log.Logger("Не удалось найти путь ftp", PathParse, e);
                return arch;
            }

            List<String> archtemp = ftp.ListDirectory();
            List<String> years_search = Program.Years.Select(y => $"notification_{RegionPath}{y}").ToList();
            foreach (var a in archtemp.Where(a => years_search.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)))
            {
                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    connect.Open();
                    string select_arch =
                        $"SELECT id FROM {Program.Prefix}arhiv_tenders WHERE arhiv = @archive AND region =  @region";
                    MySqlCommand cmd = new MySqlCommand(select_arch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", a);
                    cmd.Parameters.AddWithValue("@region", RegionPath);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    bool res_read = reader.HasRows;
                    reader.Close();
                    if (!res_read)
                    {
                        string add_arch =
                            $"INSERT INTO {Program.Prefix}arhiv_tenders SET arhiv = @archive, region =  @region";
                        MySqlCommand cmd1 = new MySqlCommand(add_arch, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@archive", a);
                        cmd1.Parameters.AddWithValue("@region", RegionPath);
                        cmd1.ExecuteNonQuery();
                        arch.Add(a);
                    }
                }
            }

            return arch;
        }

        public override List<String> GetListArchPrev(string PathParse, string RegionPath)
        {
            List<String> arch = new List<string>();
            WorkWithFtp ftp = ClientFtp44();
            try
            {
                ftp.ChangeWorkingDirectory(PathParse);
            }
            catch (Exception e)
            {
                Log.Logger("Не удалось найти путь ftp", PathParse, e);
                return arch;
            }

            List<String> archtemp = ftp.ListDirectory();
            string serachd = $"{Program.LocalDate:yyyyMMdd}";
            foreach (var a in archtemp.Where(a => a.IndexOf(serachd, StringComparison.Ordinal) != -1))
            {
                string prev_a = $"prev_{a}";
                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    connect.Open();
                    string select_arch =
                        $"SELECT id FROM {Program.Prefix}arhiv_tenders WHERE arhiv = @archive AND region =  @region";
                    MySqlCommand cmd = new MySqlCommand(select_arch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", prev_a);
                    cmd.Parameters.AddWithValue("@region", RegionPath);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    bool res_read = reader.HasRows;
                    reader.Close();
                    if (!res_read)
                    {
                        string add_arch =
                            $"INSERT INTO {Program.Prefix}arhiv_tenders SET arhiv = @archive, region =  @region";
                        MySqlCommand cmd1 = new MySqlCommand(add_arch, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@archive", prev_a);
                        cmd1.Parameters.AddWithValue("@region", RegionPath);
                        cmd1.ExecuteNonQuery();
                        arch.Add(a);
                    }
                }
            }

            return arch;
        }
    }
}