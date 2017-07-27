﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace ParserTenders
{
    public class DownloadFile
    {
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

        public string DownLOld(string url, int id_att, TypeFileAttach tp, List<string> proxies, List<string> useragents)
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
            while (count <= Program.DownCount)
            {
                try
                {
                    string proxy = proxies[new Random().Next(proxies.Count)];
                    string ip = proxy.Substring(0, proxy.IndexOf(":"));
                    //ip = "107.170.23.30";
                    //Console.WriteLine(ip);
                    string port_s = proxy.Substring(proxy.IndexOf(":") + 1);
                    int port = Int32.Parse(port_s);
                    //port = 88;
                    //Console.WriteLine(port);
                    string useragent = useragents[new Random().Next(useragents.Count)];
                    WebClient wc = new WebClient();
                    wc.Headers.Add ("user-agent", useragent);
                    wc.Proxy = new WebProxy(ip, port);
                    wc.DownloadFile(url, patharch);
                    FileInfo FileD = new FileInfo(patharch);
                    if (FileD.Exists && FileD.Length == 0)
                    {
                        continue;
                    }
                    return patharch;
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e);
                }

                count++;
            }
            Log.Logger($"Не скачали файл за {count} попыток", url);
            return patharch;
        }
    }
}