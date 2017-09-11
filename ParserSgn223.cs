using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class ParserSgn223 : Parser
    {
        protected DataTable DtRegion;

        private string[] file_sign223 = new[]
            {"contract_"};

        public ParserSgn223(TypeArguments arg) : base(arg)
        {
        }

        public override void Parsing()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                List<String> arch = new List<string>();
                string PathParse = "";
                string RegionPath = (string) row["path223"];
                switch (Program.Periodparsing)
                {
                    case (TypeArguments.Last223):
                        PathParse = $"/out/published/{RegionPath}/contract/";
                        arch = GetListArchLast(PathParse, RegionPath);
                        break;
                    case (TypeArguments.Daily223):
                        PathParse = $"/out/published/{RegionPath}/contract/daily/";
                        arch = GetListArchDaily(PathParse, RegionPath);
                        break;
                }

                if (arch.Count == 0)
                {
                    Log.Logger("Получен пустой список архивов", PathParse);
                    continue;
                }

                foreach (var v in arch)
                {
                    GetListFileArch(v, PathParse, (string) row["conf"], (int) row["id"]);
                }
            }
        }
        
        public void ParsingXML(FileInfo f, string region, int region_id)
        {
            using (StreamReader sr = new StreamReader(f.ToString(), Encoding.Default))
            {
                var ftext = sr.ReadToEnd();
                ftext = ClearText.ClearString(ftext);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(ftext);
                string jsons = JsonConvert.SerializeXmlNode(doc);
                JObject json = JObject.Parse(jsons);
                TenderTypeSign223 a = new TenderTypeSign223(f, region, region_id, json);
                a.Parsing();
            }
        }


        public override void GetListFileArch(string Arch, string PathParse, string region, int region_id)
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
                        List<FileInfo> array_sign223 = filelist
                            .Where(a => file_sign223.Any(
                                            t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1) &&
                                        a.Length != 0).ToList();
                        foreach (var f in array_sign223)
                        {
                            try
                            {
                                Bolter(f, region, region_id);
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
        
        public override void Bolter(FileInfo f, string region, int region_id)
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
                ParsingXML(f, region, region_id);
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при парсинге xml", e, f);
            }
        }

        public override List<String> GetListArchLast(string PathParse, string RegionPath)
        {
            List<string> archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            archtemp = GetListFtp223(PathParse);
            List<String> years_search = Program.Years.Select(y => $"contract_{RegionPath}{y}").ToList();
            return archtemp.Where(a => years_search.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)).ToList();
        }

        public override List<String> GetListArchDaily(string pathParse, string regionPath)
        {
            List<String> arch = new List<string>();
            List<string> archtemp = GetListFtp223(pathParse);
            foreach (var a in archtemp
                .Where(a => Program.Years.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)))
            {
                using (ArchiveSign223Context db = new ArchiveSign223Context())
                {
                    var Archives = db.ArchiveSign223Results.Where(p => p.Archive == a).ToList();

                    if (Archives.Count == 0)
                    {
                        ArchiveSign223 ar = new ArchiveSign223 {Archive = a, Region = regionPath};
                        db.ArchiveSign223Results.Add(ar);
                        arch.Add(a);
                        db.SaveChanges();
                    }
                }
            }
            return arch;
        }
    }
}