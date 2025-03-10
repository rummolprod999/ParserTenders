#region

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParserTenders.TenderDir;

#endregion

namespace ParserTenders.ParserDir

{
    public class ParserTend615Api : Parser
    {
        private readonly string[] _fileCancel = { "notificationcancel_" };
        private readonly string[] _filecontract = { "contract_" };
        private readonly string[] _fileDatechange = { "datechange_", "timeef_" };
        private readonly string[] _fileLotcancel = { "lotcancel_" };
        private readonly string[] _fileXml615 = { "notificationef_", "notificationpo_" };
        protected DataTable DtRegion;

        private readonly string[] types =
        {
            "pprf615NotificationEF",
            "pprf615NotificationPO"
        };

        public ParserTend615Api(TypeArguments arg) : base(arg)
        {
        }

        public override void Parsing()
        {
            DtRegion = GetRegions();
            for (var i = Program._days; i >= 0; i--)
            {
                foreach (DataRow row in DtRegion.Rows)
                {
                    foreach (var type in types)
                    {
                        try
                        {
                            var arch = new List<string>();
                            var pathParse = "";
                            var regionKladr = (string)row["conf"];
                            switch (Program.Periodparsing)
                            {
                                case TypeArguments.Curr615:
                                    arch = GetListArchCurr(regionKladr, type, i);
                                    break;
                            }

                            if (arch.Count == 0)
                            {
                                Log.Logger($"Получен пустой список архивов регион {regionKladr} тип {type}");
                                continue;
                            }

                            foreach (var v in arch)
                            {
                                try
                                {
                                    GetListFileArch(v, (string)row["conf"], (int)row["id"]);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                    Log.Logger(v, e);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Logger("Ошибка ", e);
                        }
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


        public void GetListFileArch(string arch, string region, int regionId)
        {
            var filea = "";
            var pathUnzip = "";
            filea = downloadArchive(arch);
            if (!string.IsNullOrEmpty(filea))
            {
                pathUnzip = Unzipped.Unzip(filea);
                if (pathUnzip != "")
                {
                    if (Directory.Exists(pathUnzip))
                    {
                        var dirInfo = new DirectoryInfo(pathUnzip);
                        var filelist = dirInfo.GetFiles();
                        var arrayXml615 = filelist
                            .Where(a => _fileXml615.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        var arrayLotcancel = filelist
                            .Where(a => _fileLotcancel.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        var arrayCancel = filelist
                            .Where(a => _fileCancel.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        var arrayDatechange = filelist
                            .Where(a => _fileDatechange.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        var arrayContract = filelist
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

        public List<string> GetListArchCurr(string regionKladr, string type, int i)
        {
            var arch = new List<string>();
            var resp = DownloadString.soap44(regionKladr, type, i);
            var xDoc = new XmlDocument();
            xDoc.LoadXml(resp);
            var nodeList = xDoc.SelectNodes("//dataInfo/archiveUrl");
            foreach (XmlNode node in nodeList)
            {
                var nodeValue = node.InnerText;
                arch.Add(nodeValue);
            }

            return arch;
        }
    }
}