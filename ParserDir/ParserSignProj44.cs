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

        private readonly string[] types =
        {
            "cpContractSign"
        };

        protected DataTable DtRegion;

        public ParserSignProj44(TypeArguments arg) : base(arg)
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
                        var arch = new List<string>();
                        var pathParse = "";
                        var regionKladr = (string)row["conf"];
                        switch (Program.Periodparsing)
                        {
                            case TypeArguments.CurrSignProj44:
                                pathParse = $"";
                                arch = GetListArchCurr(regionKladr, type, i);
                                break;
                        }

                        if (arch.Count == 0)
                        {
                            Log.Logger("Получен пустой список архивов", pathParse);
                            continue;
                        }

                        foreach (var v in arch)
                        {
                            GetListFileArch(v, (string)row["conf"], (int)row["id"]);
                        }
                    }
                }
            }
        }

        public void GetListFileArch(string arch, string region, int regionId)
        {
            var filea = downloadArchive(arch);
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
                        client.Headers.Add("individualPerson_token", Program._token);
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

        public List<string> GetListArchCurr(string regionKladr, string type, int i)
        {
            var arch = new List<string>();
            var resp = soap44(regionKladr, type, i);
            var xDoc = new XmlDocument();
            try
            {
                xDoc.LoadXml(resp);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            var nodeList = xDoc.SelectNodes("//dataInfo/archiveUrl");
            foreach (XmlNode node in nodeList)
            {
                var nodeValue = node.InnerText;
                arch.Add(nodeValue);
            }

            return arch;
        }

        public static string soap44(string regionKladr, string type, int i)
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
                        $"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ws=\"http://zakupki.gov.ru/fz44/get-docs-ip/ws\">\n<soapenv:Header>\n<individualPerson_token>{Program._token}</individualPerson_token>\n</soapenv:Header>\n<soapenv:Body>\n<ws:getDocsByOrgRegionRequest>\n<index>\n<id>{guid}</id>\n<createDateTime>{currDate}</createDateTime>\n<mode>PROD</mode>\n</index>\n<selectionParams>\n<orgRegion>{regionKladr}</orgRegion>\n<subsystemType>RPEC</subsystemType>\n<documentType44>{type}</documentType44>\n<periodInfo><exactDate>{prevday}</exactDate></periodInfo>\n</selectionParams>\n</ws:getDocsByOrgRegionRequest>\n</soapenv:Body>\n</soapenv:Envelope>";
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