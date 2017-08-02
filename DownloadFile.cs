using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace ParserTenders
{
    public class DownloadFile
    {
        private static object locker = new object();
        private static object locker2 = new object();
        private bool DownCount = true;

        public void DownloadF(string sSourceURL, string sDestinationPath, string proxy, int port, string useragent)
        {
            long iFileSize = 0;
            int iBufferSize = 1024;
            iBufferSize *= 1000;
            long iExistLen = 0;
            FileStream saveFileStream;
            if (File.Exists(sDestinationPath))
            {
                FileInfo fINfo =
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
            hwRq = (HttpWebRequest) HttpWebRequest.Create(sSourceURL);
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
                byte[] downBuffer = new byte[iBufferSize];

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
                    DownCount = false;
                    Log.Logger("Ошибка скачивания", ex, sSourceURL);
                }
                saveFileStream.Dispose();
                saveFileStream.Close();
                saveFileStream = null;
            }
        }

        public string DownL(string url, int id_att, TypeFileAttach tp, List<string> proxies, List<string> useragents)
        {
            string patharch = "";
            switch (tp)
            {
                case TypeFileAttach.doc:
                    patharch = $"{Program.TempPath}{Path.DirectorySeparatorChar}{id_att}.doc";
                    break;
                case TypeFileAttach.docx:
                    patharch = $"{Program.TempPath}{Path.DirectorySeparatorChar}{id_att}.docx";
                    break;
            }

            int count = 0;
            while (DownCount)
            {
                string proxy = proxies[new Random().Next(proxies.Count)];
                string ip = proxy.Substring(0, proxy.IndexOf(":"));
                //ip = "67.205.191.44";
                string port_s = proxy.Substring(proxy.IndexOf(":") + 1);
                int port = Int32.Parse(port_s);
                //port = 8083;
                string useragent = useragents[new Random().Next(useragents.Count)];
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

        public string DownLOldTest(string url, int id_att, TypeFileAttach tp, List<string> proxies,
            List<string> proxies_auth, List<string> useragents)
        {
            //List<string> proxies_copy = proxies.ToList();
            //List<string> proxies_auth_copy = proxies_auth.ToList();
            string patharch = "";
            switch (tp)
            {
                case TypeFileAttach.doc:
                    patharch = $"{Program.TempPath}{Path.DirectorySeparatorChar}{id_att}.doc";
                    break;
                case TypeFileAttach.docx:
                    patharch = $"{Program.TempPath}{Path.DirectorySeparatorChar}{id_att}.docx";
                    break;
            }

            int count = 0;
            while (count <= Program.DownCount)
            {
                int rnd = 0;
                string proxy = "";
                int r = new Random().Next(2);
                lock (locker)
                {
                    rnd = new Random().Next(proxies.Count);
                    proxy = proxies[rnd];
                }
                if (r == 1)
                {
                    lock (locker2)
                    {
                        rnd = new Random().Next(proxies_auth.Count);
                        proxy = proxies_auth[rnd];
                    }
                }
                try
                {
                    string ip = proxy.Substring(0, proxy.IndexOf(":"));
                    //ip = "107.170.23.30";
                    //Console.WriteLine(ip);
                    string port_s = proxy.Substring(proxy.IndexOf(":") + 1);
                    int port = Int32.Parse(port_s);
                    //port = 88;
                    //Console.WriteLine(port);
                    string useragent = useragents[new Random().Next(useragents.Count)];
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
                        WebProxy wp = new WebProxy(ip, port);
                        if (r == 1)
                        {
                            wp.Credentials = new NetworkCredential("VIP233572", "YC2iFQFpOf");
                        }
                        client.Proxy = wp;

                        using (var stream = client.OpenRead(url))
                        {
                            Int64 bytes_total = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                            if (bytes_total > 5000000)
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
                    FileInfo FileD = new FileInfo(patharch);
                    if (FileD.Exists && FileD.Length == 0)
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
                            lock (locker)
                            {
                                proxies.Remove(proxy);
                            }
                            break;
                        case 1:
                            lock (locker2)
                            {
                                proxies_auth.Remove(proxy);
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
                WebClient wc = new WebClient();
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

        public string DownLOld(string url, int id_att, TypeFileAttach tp, List<string> proxies,
            List<string> proxies_auth, List<string> useragents)
        {
            List<string> proxies_copy = proxies.ToList();
            List<string> proxies_auth_copy = proxies_auth.ToList();
            string patharch = "";
            switch (tp)
            {
                case TypeFileAttach.doc:
                    patharch = $"{Program.TempPath}{Path.DirectorySeparatorChar}{id_att}.doc";
                    break;
                case TypeFileAttach.docx:
                    patharch = $"{Program.TempPath}{Path.DirectorySeparatorChar}{id_att}.docx";
                    break;
            }

            int count = 0;
            while (count <= Program.DownCount)
            {
                int r = new Random().Next(2);
                int rnd = new Random().Next(proxies_copy.Count);
                string proxy = proxies_copy[rnd];
                if (r == 1)
                {
                    rnd = new Random().Next(proxies_auth_copy.Count);
                    proxy = proxies_auth_copy[rnd];
                }
                try
                {
                    string ip = proxy.Substring(0, proxy.IndexOf(":"));
                    //ip = "107.170.23.30";
                    //Console.WriteLine(ip);
                    string port_s = proxy.Substring(proxy.IndexOf(":") + 1);
                    int port = Int32.Parse(port_s);
                    //port = 88;
                    //Console.WriteLine(port);
                    string useragent = useragents[new Random().Next(useragents.Count)];
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
                        WebProxy wp = new WebProxy(ip, port);
                        if (r == 1)
                        {
                            wp.Credentials = new NetworkCredential("VIP233572", "YC2iFQFpOf");
                        }
                        client.Proxy = wp;

                        using (var stream = client.OpenRead(url))
                        {
                            Int64 bytes_total = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                            if (bytes_total > 5000000)
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
                    FileInfo FileD = new FileInfo(patharch);
                    if (FileD.Exists && FileD.Length == 0)
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
                            proxies_copy.RemoveAt(rnd);
                            break;
                        case 1:
                            proxies_auth_copy.RemoveAt(rnd);
                            break;
                    }
                    //Console.WriteLine(e);
                }

                count++;
            }
            Log.Logger($"Не скачали файл за {count} попыток", url);
            try
            {
                WebClient wc = new WebClient();
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