#region

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParserTenders.TenderDir;

#endregion

namespace ParserTenders.ParserDir
{
    public class ParserTend223Api : Parser
    {
        private string[] _purchaseDir =
        {
            "purchaseNotice", "purchaseNoticeAE", "purchaseNoticeAE94", "purchaseNoticeEP", "purchaseNoticeIS",
            "purchaseNoticeOA", "purchaseNoticeOK", "purchaseNoticeZK", "lotCancellation", "purchaseRejection",
            "purchaseNoticeZPESMBO", "purchaseNoticeZKESMBO", "purchaseNoticeKESMBO", "purchaseNoticeAESMBO"
        };

        protected DataTable DtRegion;

        private readonly string[] types =
        {
            "purchaseNotice",
            "purchaseNoticeAE",
            "purchaseNoticeAE94",
            "purchaseNoticeAESMBO",
            "purchaseNoticeEP",
            "purchaseNoticeKESMBO",
            "purchaseNoticeOK",
            "purchaseNoticeZKESMBO",
            "purchaseNoticeZK",
            "purchaseNoticeZPESMBO"
        };

        public ParserTend223Api(TypeArguments arg) : base(arg)
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
                                case TypeArguments.Daily223:
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
                                    GetListFileArch(v, (string)row["conf"], (int)row["id"], type);
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


        public void GetListFileArch(string arch, string region, int regionId, string type)
        {
            var filea = "";
            var pathUnzip = "";
            filea = downloadArchive(arch);
            if (string.IsNullOrEmpty(filea))
            {
                return;
            }

            pathUnzip = Unzipped.Unzip(filea);
            if (pathUnzip != "")
            {
                if (Directory.Exists(pathUnzip))
                {
                    var dirInfo = new DirectoryInfo(pathUnzip);
                    var filelist = dirInfo.GetFiles();
                    foreach (var f in filelist)
                    {
                        switch (type)
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


        public List<string> GetListArchCurr(string regionKladr, string type, int i)
        {
            var arch = new List<string>();
            var resp = DownloadString.soap223(regionKladr, type, i);
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

        private string downloadArchive(string url)
        {
            var count = 5;
            var sleep = 5000;
            var dest = $"{Program.TempPath}{Path.DirectorySeparatorChar}array.zip";
            while (true)
            {
                try
                {
                    using (var client = new TimedWebClient())
                    {
                        client.DownloadFile(url, dest);
                    }

                    break;
                }
                catch (Exception e)
                {
                    if (count <= 0)
                    {
                        Log.Logger($"Не удалось скачать {url}");
                        break;
                    }

                    count--;
                    Thread.Sleep(sleep);
                    sleep *= 2;
                }
            }

            return dest;
        }
    }
}