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
    public class ParserTend44Api : Parser
    {
        private readonly List<string> summList;

        private readonly string[] _fileCancel = { "notificationcancel_" };
        private readonly string[] _fileCancelFailure = { "cancelfailure_" };
        private readonly string[] _fileClarification = { "clarification_", "epclarificationdoc_" };
        private readonly string[] _fileClarificationResult = { "epclarificationresult_" };
        private readonly string[] _fileDatechange = { "datechange_" };
        private readonly string[] _fileLotcancel = { "lotcancel_" };
        private readonly string[] _fileOrgchange = { "orgchange_" };
        private readonly string[] _fileProlongation = { "prolongation" };
        private readonly string[] _fileSign = { "contractsign_" };

        private readonly string[] _fileXml44 =
        {
            "ea44_", "ep44_", "ok44_", "okd44_", "oku44_", "po44_", "za44_", "zk44_",
            "zkb44_", "zkk44_", "zkkd44_", "zkku44_", "zp44_", "protocolzkbi_", "inm111_"
        };

        private readonly string[] _fileXml504 =
        {
            "zk504_", "zp504_", "ok504_", "okd504_", "okou504_", "oku504_", "ezk2020_", "ezt2020_", "ef2020_",
            "eok2020_"
        };

        private readonly Dictionary<string, string> types = new Dictionary<string, string>
        {
            {"epNotificationEZK2020", "PRIZ"},
            {"epNotificationEF2020", "PRIZ"},
            {"epNotificationEZT2020", "PRIZ"},
            { "epNotificationEOK2020", "PRIZ"},
            {"epClarificationDoc", "PRIZ"},
            {"fcsClarification", "PRIZ"},
            {"epClarificationDocRequest", "PRIZ"},
            {"pprf615ClarificationRequest", "PPRF615"},
            {"pprf615Clarification", "PPRF615"}
        };

        protected DataTable DtRegion;

        public ParserTend44Api(TypeArguments arg) : base(arg)
        {
            summList = new List<string>().AddRangeAndReturnList(_fileCancel.ToList())
                .AddRangeAndReturnList(_fileCancelFailure.ToList()).AddRangeAndReturnList(_fileClarification.ToList())
                .AddRangeAndReturnList(_fileDatechange.ToList()).AddRangeAndReturnList(_fileLotcancel.ToList())
                .AddRangeAndReturnList(_fileOrgchange.ToList()).AddRangeAndReturnList(_fileProlongation.ToList())
                .AddRangeAndReturnList(_fileSign.ToList()).AddRangeAndReturnList(_fileXml44.ToList())
                .AddRangeAndReturnList(_fileXml504.ToList()).AddRangeAndReturnList(_fileClarificationResult.ToList());
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
                                case TypeArguments.Curr44:
                                    arch = GetListArchCurr(regionKladr, type, i);
                                    break;
                            }

                            if (arch.Count == 0)
                            {
                                Log.Logger($"Получен пустой список архивов регион {regionKladr} тип {type.Key}");
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
                        var arrayXml44 = filelist
                            .Where(a => _fileXml44.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        var arrayProlongation = filelist
                            .Where(a => _fileProlongation.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        var arrayDatechange = filelist
                            .Where(a => _fileDatechange.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        var arrayOrgchange = filelist
                            .Where(a => _fileOrgchange.Any(
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
                        var arraySign = filelist
                            .Where(a => _fileSign.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        var arrayCancelFailure = filelist
                            .Where(a => _fileCancelFailure.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        var arrayClarification = filelist
                            .Where(a => _fileClarification.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        var arrayClarificationResult = filelist
                            .Where(a => _fileClarificationResult.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        var arrayXml504 = filelist
                            .Where(a => _fileXml504.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        var arrayNull = filelist
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

                        foreach (var f in arrayClarificationResult)
                        {
                            Bolter(f, region, regionId, TypeFile44.TypeClarificationResult);
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
                if (!f.Name.Contains("PlacementResult_") && !f.Name.Contains("NotificationEA615_") &&
                    !f.Name.Contains("NotificationPO615_"))
                {
                    Log.Logger("!!!cannot parse this file", f.FullName, region);
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
                    case TypeFile44.TypeTen44:
                        var a = new TenderType44(f, region, regionId, json);
                        a.Parsing();
                        break;
                    case TypeFile44.TypeProlongation:
                        var b = new TenderTypeProlongation(f, region, regionId, json);
                        b.Parsing();
                        break;
                    case TypeFile44.TypeDateChange:
                        var c = new TenderTypeDateChange(f, region, regionId, json);
                        c.Parsing();
                        break;
                    case TypeFile44.TypeOrgChange:
                        var d = new TenderTypeOrgChange(f, region, regionId, json);
                        d.Parsing();
                        break;
                    case TypeFile44.TypeLotCancel:
                        var e = new TenderTypeLotCancel(f, region, regionId, json);
                        e.Parsing();
                        break;
                    case TypeFile44.TypeCancel:
                        var g = new TenderTypeCancel(f, region, regionId, json);
                        g.Parsing();
                        break;
                    case TypeFile44.TypeCancelFailure:
                        var h = new TenderTypeCancelFailure(f, region, regionId, json);
                        h.Parsing();
                        break;
                    case TypeFile44.TypeSign:
                        var n = new TenderTypeSign(f, region, regionId, json);
                        n.Parsing();
                        break;
                    case TypeFile44.TypeClarification:
                        var m = new TenderTypeClarification(f, region, regionId, json);
                        m.Parsing();
                        break;
                    case TypeFile44.TypeClarificationResult:
                        var r = new TenderTypeClarificationResult(f, region, regionId, json);
                        r.Parsing();
                        break;
                    case TypeFile44.TypeTen504:
                        var o = new TenderType504(f, region, regionId, json);
                        o.Parsing();
                        break;
                }
            }
        }


        public List<string> GetListArchCurr(string regionKladr, KeyValuePair<string, string> type, int i)
        {
            var arch = new List<string>();
            var resp = DownloadString.soap44(regionKladr, type.Key, i, type.Value);
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