using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace ParserTenders
{
    public class DownloadFile
    {
        private static object _locker = new object();
        private static object _locker2 = new object();
        private bool _downCount = true;

        public void DownloadF(string sSourceUrl, string sDestinationPath, string proxy, int port, string useragent)
        {
            long iFileSize = 0;
            var iBufferSize = 1024;
            iBufferSize *= 1000;
            long iExistLen = 0;
            FileStream saveFileStream;
            if (File.Exists(sDestinationPath))
            {
                var fINfo =
                    new FileInfo(sDestinationPath);
                iExistLen = fINfo.Length;
            }
            if (iExistLen > 0)
                saveFileStream = new FileStream(sDestinationPath,
                    FileMode.Append, FileAccess.Write,
                    FileShare.ReadWrite);
            else
                saveFileStream = new FileStream(sDestinationPath,
                    FileMode.Create, FileAccess.Write,
                    FileShare.ReadWrite);

            HttpWebRequest hwRq;
            HttpWebResponse hwRes;
            hwRq = (HttpWebRequest) HttpWebRequest.Create(sSourceUrl);
            //hwRq.Proxy = new WebProxy(proxy, port);
            //hwRq.UserAgent = useragent;
            hwRq.AddRange((int) iExistLen);
            try
            {
                Stream smRespStream;
                hwRes = (HttpWebResponse) hwRq.GetResponse();
                smRespStream = hwRes.GetResponseStream();
                iFileSize = hwRes.ContentLength;
                int iByteSize;
                var downBuffer = new byte[iBufferSize];

                while ((iByteSize = smRespStream.Read(downBuffer, 0, downBuffer.Length)) > 0)
                {
                    saveFileStream.Write(downBuffer, 0, iByteSize);
                }
                saveFileStream.Dispose();
                saveFileStream.Close();
                saveFileStream = null;
            }
            catch (WebException ex)
            {
                hwRes = (HttpWebResponse) ex.Response;
                if (hwRes != null && (hwRes.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable ||
                                      hwRes.StatusCode == HttpStatusCode.NotFound))
                {
                    _downCount = false;
                    Log.Logger("Ошибка скачивания", ex, sSourceUrl);
                }
                saveFileStream.Dispose();
                saveFileStream.Close();
                saveFileStream = null;
            }
        }

        public string DownL(string url, int idAtt, TypeFileAttach tp, List<string> proxies, List<string> useragents)
        {
            var patharch = "";
            switch (tp)
            {
                case TypeFileAttach.Doc:
                    patharch = $"{Program.TempPath}{Path.DirectorySeparatorChar}{idAtt}.doc";
                    break;
                case TypeFileAttach.Docx:
                    patharch = $"{Program.TempPath}{Path.DirectorySeparatorChar}{idAtt}.docx";
                    break;
            }

            var count = 0;
            while (_downCount)
            {
                var proxy = proxies[new Random().Next(proxies.Count)];
                var ip = proxy.Substring(0, proxy.IndexOf(":"));
                //ip = "67.205.191.44";
                var portS = proxy.Substring(proxy.IndexOf(":") + 1);
                var port = Int32.Parse(portS);
                //port = 8083;
                var useragent = useragents[new Random().Next(useragents.Count)];
                try
                {
                    DownloadF(url, patharch, ip, port, useragent);
                }
                catch (Exception e)
                {
                    count++;
                    if (count > Program.DownCount)
                    {
                        Log.Logger($"Не удалось скачать файл за {count} попыток", e);
                        break;
                    }
                }
            }

            return patharch;
        }

        public string DownLOldTest(string url, int idAtt, TypeFileAttach tp, List<string> proxies,
            List<string> proxiesAuth, List<string> useragents)
        {
            //List<string> proxies_copy = proxies.ToList();
            //List<string> proxies_auth_copy = proxies_auth.ToList();
            var patharch = "";
            switch (tp)
            {
                case TypeFileAttach.Doc:
                    patharch = $"{Program.TempPath}{Path.DirectorySeparatorChar}{idAtt}.doc";
                    break;
                case TypeFileAttach.Docx:
                    patharch = $"{Program.TempPath}{Path.DirectorySeparatorChar}{idAtt}.docx";
                    break;
            }

            var count = 0;
            while (count <= Program.DownCount)
            {
                var rnd = 0;
                var proxy = "";
                var r = new Random().Next(2);
                lock (_locker)
                {
                    rnd = new Random().Next(proxies.Count);
                    proxy = proxies[rnd];
                }
                if (r == 1)
                {
                    lock (_locker2)
                    {
                        rnd = new Random().Next(proxiesAuth.Count);
                        proxy = proxiesAuth[rnd];
                    }
                }
                try
                {
                    var ip = proxy.Substring(0, proxy.IndexOf(":"));
                    //ip = "107.170.23.30";
                    //Console.WriteLine(ip);
                    var portS = proxy.Substring(proxy.IndexOf(":") + 1);
                    var port = Int32.Parse(portS);
                    //port = 88;
                    //Console.WriteLine(port);
                    var useragent = useragents[new Random().Next(useragents.Count)];
                    /*WebClient wc = new WebClient();
                    wc.Headers.Add("user-agent", useragent);
                    WebProxy wp = new WebProxy(ip, port);
                    if (r == 1)
                    {
                        wp.Credentials = new NetworkCredential("VIP233572", "YC2iFQFpOf");
                    }
                    wc.Proxy = wp;
                    wc.DownloadFile(url, patharch);*/
                    using (var client = new WebDownload())
                    {
                        client.Headers.Add("user-agent", useragent);
                        var wp = new WebProxy(ip, port);
                        if (r == 1)
                        {
                            wp.Credentials = new NetworkCredential("VIP233572", "YC2iFQFpOf");
                        }
                        client.Proxy = wp;

                        using (var stream = client.OpenRead(url))
                        {
                            var bytesTotal = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                            if (bytesTotal > 5000000)
                            {
                                Log.Logger("Too many file", url);
                                return patharch;
                            }
                            using (var file = File.Create(patharch))
                            {
                                var buffer = new byte[4096];
                                int bytesReceived;
                                while ((bytesReceived = stream.Read(buffer, 0, buffer.Length)) != 0)
                                {
                                    file.Write(buffer, 0, bytesReceived);
                                }
                            }
                        }
                    }
                    var fileD = new FileInfo(patharch);
                    if (fileD.Exists && fileD.Length == 0)
                    {
                        continue;
                    }
                    return patharch;
                }
                catch (Exception)
                {
                    switch (r)
                    {
                        case 0:
                            lock (_locker)
                            {
                                proxies.Remove(proxy);
                            }
                            break;
                        case 1:
                            lock (_locker2)
                            {
                                proxiesAuth.Remove(proxy);
                            }
                            break;
                    }
                    //Console.WriteLine(e);
                }

                count++;
            }
            Log.Logger($"Не скачали файл за {count} попыток", url);
            try
            {
                var wc = new WebClient();
                wc.Headers.Add("user-agent",
                    "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:55.0) Gecko/20100101 Firefox/55.0");
                wc.DownloadFile(url, patharch);
                Log.Logger("Скачали файл без прокси", url);
                return patharch;
            }

            catch (Exception e)
            {
                Log.Logger("Не удалось скачать файл без прокси", url, e);
            }
            return patharch;
        }

        public string DownLOld(string url, int idAtt, TypeFileAttach tp, List<string> proxies,
            List<string> proxiesAuth, List<string> useragents)
        {
            var proxiesCopy = proxies.ToList();
            var proxiesAuthCopy = proxiesAuth.ToList();
            var patharch = "";
            switch (tp)
            {
                case TypeFileAttach.Doc:
                    patharch = $"{Program.TempPath}{Path.DirectorySeparatorChar}{idAtt}.doc";
                    break;
                case TypeFileAttach.Docx:
                    patharch = $"{Program.TempPath}{Path.DirectorySeparatorChar}{idAtt}.docx";
                    break;
            }

            var count = 0;
            while (count <= Program.DownCount)
            {
                var r = new Random().Next(2);
                var rnd = new Random().Next(proxiesCopy.Count);
                var proxy = proxiesCopy[rnd];
                if (r == 1)
                {
                    rnd = new Random().Next(proxiesAuthCopy.Count);
                    proxy = proxiesAuthCopy[rnd];
                }
                try
                {
                    var ip = proxy.Substring(0, proxy.IndexOf(":"));
                    //ip = "107.170.23.30";
                    //Console.WriteLine(ip);
                    var portS = proxy.Substring(proxy.IndexOf(":") + 1);
                    var port = Int32.Parse(portS);
                    //port = 88;
                    //Console.WriteLine(port);
                    var useragent = useragents[new Random().Next(useragents.Count)];
                    /*WebClient wc = new WebClient();
                                        wc.Headers.Add("user-agent", useragent);
                                        WebProxy wp = new WebProxy(ip, port);
                                        if (r == 1)
                                        {
                                            wp.Credentials = new NetworkCredential("VIP233572", "YC2iFQFpOf");
                                        }
                                        wc.Proxy = wp;
                                        wc.DownloadFile(url, patharch);*/
                    using (var client = new WebDownload())
                    {
                        client.Headers.Add("user-agent", useragent);
                        var wp = new WebProxy(ip, port);
                        if (r == 1)
                        {
                            wp.Credentials = new NetworkCredential("VIP233572", "YC2iFQFpOf");
                        }
                        client.Proxy = wp;

                        using (var stream = client.OpenRead(url))
                        {
                            var bytesTotal = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                            if (bytesTotal > 5000000)
                            {
                                Log.Logger("Too lagre file!!!!", url);
                                return patharch;
                            }
                            using (var file = File.Create(patharch))
                            {
                                var buffer = new byte[4096];
                                int bytesReceived;
                                while ((bytesReceived = stream.Read(buffer, 0, buffer.Length)) != 0)
                                {
                                    file.Write(buffer, 0, bytesReceived);
                                }
                            }
                        }
                    }
                    var fileD = new FileInfo(patharch);
                    if (fileD.Exists && fileD.Length == 0)
                    {
                        continue;
                    }
                    return patharch;
                }
                catch (Exception e)
                {
                    switch (r)
                    {
                        case 0:
                            proxiesCopy.RemoveAt(rnd);
                            break;
                        case 1:
                            proxiesAuthCopy.RemoveAt(rnd);
                            break;
                    }
                    //Console.WriteLine(e);
                }

                count++;
            }
            Log.Logger($"Не скачали файл за {count} попыток", url);
            try
            {
                var wc = new WebClient();
                wc.Headers.Add("user-agent",
                    "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:55.0) Gecko/20100101 Firefox/55.0");
                wc.DownloadFile(url, patharch);
                Log.Logger("Скачали файл без прокси", url);
                return patharch;
            }

            catch (Exception e)
            {
                Log.Logger("Не удалось скачать файл без прокси", url, e);
            }
            return patharch;
        }
    }
}