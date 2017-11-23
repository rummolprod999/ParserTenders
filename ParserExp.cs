﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class ParserExp : Parser
    {
        protected DataTable DtRegion;

        private string[] _fileExp223 = new[]
            {"explanation_"};

        public ParserExp(TypeArguments arg) : base(arg)
        {
        }

        public override void Parsing()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                List<String> arch = new List<string>();
                string pathParse = "";
                string regionPath = (string) row["path223"];
                switch (Program.Periodparsing)
                {
                    case (TypeArguments.LastExp223):
                        pathParse = $"/out/published/{regionPath}/explanation/";
                        arch = GetListArchLast(pathParse, regionPath);
                        break;
                    case (TypeArguments.DailyExp223):
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
                    GetListFileArch(v, pathParse, (string) row["conf"], (int) row["id"]);
                }
            }
        }
        
        public override List<String> GetListArchLast(string pathParse, string regionPath)
        {
            /*FtpClient ftp = ClientFtp44();*/
            var archtemp = GetListFtp223(pathParse);
            List<String> yearsSearch = Program.Years.Select(y => $"explanation_{regionPath}{y}").ToList();
            return archtemp.Where(a => yearsSearch.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)).ToList();
        }
        
        public override List<String> GetListArchDaily(string pathParse, string regionPath)
        {
            List<String> arch = new List<string>();
            List<string> archtemp = GetListFtp223(pathParse);
            foreach (var a in archtemp
                .Where(a => Program.Years.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)))
            {
                using (ArchiveExp223Context db = new ArchiveExp223Context())
                {
                    var archives = db.ArchiveExp223Results.Where(p => p.Archive == a).ToList();

                    if (archives.Count == 0)
                    {
                        ArchiveExp223 ar = new ArchiveExp223 {Archive = a, Region = regionPath};
                        db.ArchiveExp223Results.Add(ar);
                        arch.Add(a);
                        db.SaveChanges();
                    }
                }
            }
            return arch;
        }
        
        public override void GetListFileArch(string arch, string pathParse, string region, int regionId)
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
                        List<FileInfo> arraySign223 = filelist
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
            using (StreamReader sr = new StreamReader(f.ToString(), Encoding.Default))
            {
                var ftext = sr.ReadToEnd();
                ftext = ClearText.ClearString(ftext);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(ftext);
                string jsons = JsonConvert.SerializeXmlNode(doc);
                JObject json = JObject.Parse(jsons);
                TenderTypeSign223 a = new TenderTypeSign223(f, region, regionId, json);
                a.Parsing();
            }
        }
    }
}