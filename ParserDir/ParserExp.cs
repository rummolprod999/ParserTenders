#region

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;
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

        private readonly string[] types =
        {
            "explanation",
            "explanationRequest"
        };

        public ParserExp(TypeArguments arg) : base(arg)
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
                            var regionKladr = (string)row["conf"];
                            switch (Program.Periodparsing)
                            {
                                case TypeArguments.DailyExp223:
                                    arch = GetListArchDaily(regionKladr, type, i);
                                    break;
                            }

                            if (arch.Count == 0)
                            {
                                Log.Logger($"Получен пустой список архивов регион {regionKladr} тип {type}");
                                continue;
                            }

                            foreach (var v in arch)
                            {
                                GetListFileArch(v, (string)row["conf"], (int)row["id"], type);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Logger("Ошибка ", e);
                        }
                    }
                }
            }
        }


        public List<string> GetListArchDaily(string regionKladr, string type, int i)
        {
            var arch = new List<string>();
            var resp = soap223(regionKladr, type, i);
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

        public static string soap223(string regionKladr, string type, int i)
        {
            var count = 5;
            var sleep = 2000;
            while (true)
            {
                try
                {
                    var guid = Guid.NewGuid();
                    var currDate = DateTime.Now.ToString("s");
                    var prevday = DateTime.Now.AddDays(-1 * i).ToString("yyyy-MM-dd");
                    var request =
                        $"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ws=\"http://zakupki.gov.ru/fz44/get-docs-ip/ws\">\n<soapenv:Header>\n<individualPerson_token>{Program._token}</individualPerson_token>\n</soapenv:Header>\n<soapenv:Body>\n<ws:getDocsByOrgRegionRequest>\n<index>\n<id>{guid}</id>\n<createDateTime>{currDate}</createDateTime>\n<mode>PROD</mode>\n</index>\n<selectionParams>\n<orgRegion>{regionKladr}</orgRegion>\n<subsystemType>RI223</subsystemType>\n<documentType223>{type}</documentType223>\n<periodInfo>\n<exactDate>{prevday}</exactDate>\n</periodInfo>  </selectionParams>\n</ws:getDocsByOrgRegionRequest>\n</soapenv:Body>\n</soapenv:Envelope>";
                    var url = "https://int44.zakupki.gov.ru/eis-integration/services/getDocsIP";
                    var response = "";
                    using (WebClient wc = new TimedWebClient())
                    {
                        wc.Headers[HttpRequestHeader.ContentType] = "text/xml; charset=utf-8";
                        response = wc.UploadString(url,
                            request);
                    }

                    //Console.WriteLine(response);
                    return response;
                }
                catch (Exception e)
                {
                    if (count <= 0)
                    {
                        throw;
                    }

                    count--;
                    Thread.Sleep(sleep);
                    sleep *= 2;
                }
            }
        }

        public void GetListFileArch(string arch, string region, int regionId, string type)
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