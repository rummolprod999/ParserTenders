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
    public class ParserExp : Parser
    {
        protected DataTable DtRegion;

        private readonly string[] _fileExp223 = { "explanation_" };

        public ParserExp(TypeArguments arg) : base(arg)
        {
        }

        public override void Parsing()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                var arch = new List<string>();
                var pathParse = "";
                var regionPath = (string)row["path223"];
                switch (Program.Periodparsing)
                {
                    case TypeArguments.LastExp223:
                        pathParse = $"/out/published/{regionPath}/explanation/";
                        arch = GetListArchLast(pathParse, regionPath);
                        break;
                    case TypeArguments.DailyExp223:
                        pathParse = $"/out/published/{regionPath}/explanation/daily/";
                        arch = GetListArchDaily(pathParse, regionPath);
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

        public override List<string> GetListArchLast(string pathParse, string regionPath)
        {
            /*FtpClient ftp = ClientFtp44();*/
            var archtemp = GetListFtp223(pathParse);
            var yearsSearch = Program.Years.Select(y => $"explanation_{regionPath}{y}").ToList();
            return archtemp.Where(a => yearsSearch.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)).ToList();
        }

        public override List<string> GetListArchDaily(string pathParse, string regionPath)
        {
            var arch = new List<string>();
            var archtemp = GetListFtp223(pathParse);
            foreach (var a in archtemp
                         .Where(a => Program.Years.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)))
            {
                /*using (ArchiveExp223Context db = new ArchiveExp223Context())
                {
                    var archives = db.ArchiveExp223Results.Where(p => p.Archive == a).ToList();

                    if (archives.Count == 0)
                    {
                        ArchiveExp223 ar = new ArchiveExp223 {Archive = a, Region = regionPath};
                        db.ArchiveExp223Results.Add(ar);
                        arch.Add(a);
                        db.SaveChanges();
                    }
                }*/
                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    var selectArch =
                        $"SELECT id FROM {Program.Prefix}arhiv_explanation223 WHERE arhiv = @archive";
                    var cmd = new MySqlCommand(selectArch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", a);
                    var reader = cmd.ExecuteReader();
                    var resRead = reader.HasRows;
                    reader.Close();
                    if (!resRead)
                    {
                        var addArch =
                            $"INSERT INTO {Program.Prefix}arhiv_explanation223 SET arhiv = @archive, region = @region";
                        var cmd1 = new MySqlCommand(addArch, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@archive", a);
                        cmd1.Parameters.AddWithValue("@region", regionPath);
                        cmd1.ExecuteNonQuery();
                        arch.Add(a);
                    }
                }
            }

            return arch;
        }

        public override void GetListFileArch(string arch, string pathParse, string region, int regionId)
        {
            var filea = "";
            var pathUnzip = "";
            filea = GetArch223(arch, pathParse);
            if (!string.IsNullOrEmpty(filea))
            {
                pathUnzip = Unzipped.Unzip(filea);
                if (pathUnzip != "")
                {
                    if (Directory.Exists(pathUnzip))
                    {
                        var dirInfo = new DirectoryInfo(pathUnzip);
                        var filelist = dirInfo.GetFiles();
                        var arraySign223 = filelist
                            .Where(a => _fileExp223.Any(
                                            t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1) &&
                                        a.Length != 0).ToList();
                        foreach (var f in arraySign223)
                        {
                            try
                            {
                                Bolter(f, region, regionId);
                            }
                            catch (Exception e)
                            {
                                Log.Logger("Не удалось обработать файл", f, filea);
                            }
                        }

                        dirInfo.Delete(true);
                    }
                }
            }
        }

        public override void Bolter(FileInfo f, string region, int regionId)
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
                var a = new TenderTypeExp223(f, region, regionId, json);
                a.Parsing();
            }
        }
    }
}