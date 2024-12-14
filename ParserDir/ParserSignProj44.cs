#region

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

#endregion

namespace ParserTenders.ParserDir
{
    public class ParserSignProj44 : Parser
    {
        private readonly string[] _fileSign = { "cpcontractsign_" };

        protected DataTable DtRegion;

        public ParserSignProj44(TypeArguments arg) : base(arg)
        {
        }

        public override void Parsing()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                var arch = new List<string>();
                var pathParse = "";
                var regionPath = (string)row["path"];
                switch (Program.Periodparsing)
                {
                    case TypeArguments.LastSignProj44:
                        pathParse = $"/fcs_regions/{regionPath}/contractprojects/";
                        arch = GetListArchLast(pathParse, regionPath);
                        break;
                    case TypeArguments.CurrSignProj44:
                        pathParse = $"/fcs_regions/{regionPath}/contractprojects/currMonth/";
                        arch = GetListArchCurr(pathParse, regionPath);
                        break;
                    case TypeArguments.PrevSignProj44:
                        pathParse = $"/fcs_regions/{regionPath}/contractprojects/prevMonth/";
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
                    GetListFileArch(v, pathParse, (string)row["conf"], (int)row["id"]);
                }
            }
        }

        public override void GetListFileArch(string arch, string pathParse, string region, int regionId)
        {
            var filea = GetArch44(arch, pathParse);
            if (string.IsNullOrEmpty(filea))
            {
                return;
            }

            var pathUnzip = Unzipped.Unzip(filea);
            if (pathUnzip == "")
            {
                return;
            }

            if (!Directory.Exists(pathUnzip))
            {
                return;
            }

            var dirInfo = new DirectoryInfo(pathUnzip);
            var filelist = dirInfo.GetFiles();
            var arrayXmlSignProj44 = filelist
                .Where(a => _fileSign.Any(
                    t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                .ToList();


            foreach (var f in arrayXmlSignProj44)
            {
                Bolter(f, region, regionId);
            }

            dirInfo.Delete(true);
        }

        public override void Bolter(FileInfo f, string region, int regionId)
        {
            if (!f.Name.ToLower().EndsWith(".xml", StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                ParsingXml(f, region, regionId);
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при парсинге xml", e, f);
            }
        }

        public void ParsingXml(FileInfo f, string region, int regionId)
        {
            using (var sr = new StreamReader(f.ToString(), Encoding.Default))
            {
                var ftext = sr.ReadToEnd();
                ftext = ClearText.ClearString(ftext);
                var doc = new XmlDocument();
                doc.LoadXml(ftext);
                var jsons = JsonConvert.SerializeXmlNode(doc);
                var json = JObject.Parse(jsons);
                var a = new TenderSignProj44(f, region, regionId, json);
                a.Parsing();
            }
        }

        public override List<string> GetListArchLast(string pathParse, string regionPath)
        {
            var archtemp = GetListFtp44(pathParse);
            var yearsSearch = Program.Years.Select(y => $"contractproject_{regionPath}{y}").ToList();
            yearsSearch.AddRange(Program.Years.Select(y => $"contractproject{y}").ToList());
            return archtemp.Where(a => yearsSearch.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)).ToList();
        }

        public override List<string> GetListArchCurr(string pathParse, string regionPath)
        {
            var arch = new List<string>();
            var newLs = GetListFtp44New(pathParse);
            var yearsSearch = Program.Years.Select(y => $"contractproject_{regionPath}{y}").ToList();
            yearsSearch.AddRange(Program.Years.Select(y => $"contractproject{y}").ToList());
            foreach (var a in newLs.Where(a =>
                         yearsSearch.Any(t => a.Item1.IndexOf(t, StringComparison.Ordinal) != -1)))
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
                        $"SELECT id FROM {Program.Prefix}arhiv_sign_proj44 WHERE arhiv = @archive AND size_archive IN(0, @size_archive)";
                    var cmd = new MySqlCommand(selectArch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", a.Item1);
                    cmd.Parameters.AddWithValue("@size_archive", a.Item2);
                    var reader = cmd.ExecuteReader();
                    var resRead = reader.HasRows;
                    reader.Close();
                    if (!resRead)
                    {
                        var addArch =
                            $"INSERT INTO {Program.Prefix}arhiv_sign_proj44 SET arhiv = @archive, size_archive = @size_archive";
                        var cmd1 = new MySqlCommand(addArch, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@archive", a.Item1);
                        cmd1.Parameters.AddWithValue("@size_archive", a.Item2);
                        cmd1.ExecuteNonQuery();
                        arch.Add(a.Item1);
                    }
                }
            }

            return arch;
        }

        public override List<string> GetListArchPrev(string pathParse, string regionPath)
        {
            var arch = new List<string>();
            //List<string> archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            //archtemp = GetListFtp44(pathParse);
            var newLs = GetListFtp44New(pathParse);
            //var serachd = $"{Program.LocalDate:yyyyMMdd}";
            foreach (var a in newLs)
            {
                var prevA = $"prev_{a}";
                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    var selectArch =
                        $"SELECT id FROM {Program.Prefix}arhiv_sign_proj44 WHERE arhiv = @archive AND size_archive IN(0, @size_archive)";
                    var cmd = new MySqlCommand(selectArch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", prevA);
                    cmd.Parameters.AddWithValue("@size_archive", a.Item2);
                    var reader = cmd.ExecuteReader();
                    var resRead = reader.HasRows;
                    reader.Close();
                    if (!resRead)
                    {
                        var addArch =
                            $"INSERT INTO {Program.Prefix}arhiv_sign_proj44 SET arhiv = @archive, size_archive = @size_archive";
                        var cmd1 = new MySqlCommand(addArch, connect);
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