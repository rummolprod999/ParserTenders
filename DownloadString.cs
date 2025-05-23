﻿#region

using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace ParserTenders
{
    public static class DownloadString
    {
        public static int MaxDownload;

        static DownloadString()
        {
            MaxDownload = 0;
        }

        public static string DownL(string url)
        {
            var tmp = "";
            var count = 0;
            while (true)
            {
                try
                {
                    var task = Task.Run(() => new TimedWebClient().DownloadString(url));
                    if (task.Wait(TimeSpan.FromSeconds(30)))
                    {
                        tmp = task.Result;
                        break;
                    }

                    throw new TimeoutException();
                    //tmp = new TimedWebClient().DownloadString(url);
                }

                catch (Exception e)
                {
                    if (count >= 2)
                    {
                        Log.Logger($"Не удалось скачать xml за {count} попыток", url);
                        break;
                    }

                    switch (e)
                    {
                        case AggregateException a
                            when a.InnerException != null && a.InnerException.Message.Contains("(404) Not Found"):
                            Log.Logger("404 Exception", a.InnerException.Message, url);
                            return tmp;
                        case AggregateException a
                            when a.InnerException != null && a.InnerException.Message.Contains("(403) Forbidden"):
                            Log.Logger("403 Exception", a.InnerException.Message, url);
                            return tmp;
                        case AggregateException a when a.InnerException != null &&
                                                       a.InnerException.Message.Contains(
                                                           "The remote server returned an error: (434)"):
                            Log.Logger("434 Exception", a.InnerException.Message, url);
                            return tmp;
                        case TimeoutException a:
                            Log.Logger("Timeout exception");
                            return tmp;
                    }

                    Log.Logger("Не удалось получить строку xml", e, url);
                    count++;
                    Thread.Sleep(5000);
                }
            }

            return tmp;
        }

        public static string DownLUserAgent(string url)
        {
            MaxDownload++;
            var tmp = "";
            var count = 0;
            while (true)
            {
                try
                {
                    var task = Task.Run(() => new TimedWebClientUa().DownloadString(url));
                    if (task.Wait(TimeSpan.FromSeconds(30)))
                    {
                        tmp = task.Result;
                        break;
                    }

                    throw new TimeoutException();
                    //tmp = new TimedWebClient().DownloadString(url);
                }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse r)
                    {
                        Log.Logger("Response code: ", r.StatusCode);
                    }

                    if (ex.Response is HttpWebResponse errorResponse &&
                        errorResponse.StatusCode == HttpStatusCode.Forbidden)
                    {
                        Log.Logger("Error 403 or 434");
                        return tmp;
                    }

                    if (count >= 2)
                    {
                        Log.Logger($"Не удалось скачать xml за {count} попыток", url);
                        break;
                    }

                    Log.Logger("Не удалось получить строку xml", ex.Message, url);
                    count++;
                    Thread.Sleep(5000);
                }
                catch (Exception e)
                {
                    if (count >= 2)
                    {
                        Log.Logger($"Не удалось скачать xml за {count} попыток", url);
                        break;
                    }

                    switch (e)
                    {
                        case AggregateException a
                            when a.InnerException != null && a.InnerException.Message.Contains("(404) Not Found"):
                            Log.Logger("404 Exception", a.InnerException.Message, url);
                            goto Finish;
                        case AggregateException a
                            when a.InnerException != null && a.InnerException.Message.Contains("(403) Forbidden"):
                            Log.Logger("403 Exception", a.InnerException.Message, url);
                            goto Finish;
                        case AggregateException a when a.InnerException != null &&
                                                       a.InnerException.Message.Contains(
                                                           "The remote server returned an error: (434)"):
                            Log.Logger("434 Exception", a.InnerException.Message, url);
                            goto Finish;
                        case TimeoutException a:
                            Log.Logger("Timeout exception");
                            goto Finish;
                    }

                    Log.Logger("Не удалось получить строку xml", e, url);
                    count++;
                    Thread.Sleep(5000);
                }
            }

            Finish:
            return tmp;
        }

        public static string DownLUserAgentEis(string url)
        {
            MaxDownload++;
            var tmp = "";
            var count = 0;
            while (true)
            {
                try
                {
                    var task = Task.Run(() => new TimedWebClientUaEis().DownloadString(url));
                    if (task.Wait(TimeSpan.FromSeconds(99)))
                    {
                        tmp = task.Result;
                        break;
                    }

                    throw new TimeoutException();
                    //tmp = new TimedWebClient().DownloadString(url);
                }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse r)
                    {
                        Log.Logger("Response code: ", r.StatusCode);
                    }

                    if (ex.Response is HttpWebResponse errorResponse &&
                        errorResponse.StatusCode == HttpStatusCode.Forbidden)
                    {
                        Log.Logger("Error 403 or 434");
                        return tmp;
                    }

                    if (count >= 2)
                    {
                        Log.Logger($"Не удалось скачать xml за {count} попыток", url);
                        break;
                    }

                    Log.Logger("Не удалось получить строку xml", ex.Message, url);
                    count++;
                    Thread.Sleep(5000);
                }
                catch (Exception e)
                {
                    if (count >= 2)
                    {
                        Log.Logger($"Не удалось скачать xml за {count} попыток", url);
                        break;
                    }

                    switch (e)
                    {
                        case AggregateException a
                            when a.InnerException != null && a.InnerException.Message.Contains("(404) Not Found"):
                            Log.Logger("404 Exception", a.InnerException.Message, url);
                            goto Finish;
                        case AggregateException a
                            when a.InnerException != null && a.InnerException.Message.Contains("(403) Forbidden"):
                            Log.Logger("403 Exception", a.InnerException.Message, url);
                            goto Finish;
                        case AggregateException a when a.InnerException != null &&
                                                       a.InnerException.Message.Contains(
                                                           "The remote server returned an error: (434)"):
                            Log.Logger("434 Exception", a.InnerException.Message, url);
                            goto Finish;
                        /*case TimeoutException a:
                            Log.Logger("Timeout exception");
                            goto Finish;*/
                    }

                    Log.Logger("Не удалось получить строку xml", e, url);
                    count++;
                    Thread.Sleep(5000);
                }
            }

            Finish:
            return tmp;
        }

        public static string DownL1251(string url)
        {
            var tmp = "";
            var count = 0;
            while (true)
            {
                try
                {
                    var task = Task.Run(() =>
                    {
                        var v = new TimedWebClient { Encoding = Encoding.GetEncoding("windows-1251") };
                        return v.DownloadString(url);
                    });
                    if (task.Wait(TimeSpan.FromSeconds(30)))
                    {
                        tmp = task.Result;
                        break;
                    }

                    throw new TimeoutException();
                    //tmp = new TimedWebClient().DownloadString(url);
                }
                catch (Exception e)
                {
                    if (count >= 2)
                    {
                        Log.Logger($"Не удалось скачать xml за {count} попыток", url);
                        break;
                    }

                    Log.Logger("Не удалось получить строку xml", e, url);
                    count++;
                    Thread.Sleep(5000);
                }
            }

            return tmp;
        }

        public static string soap44(string regionKladr, string type, int i, string subsystem = "PRIZ")
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
                    var oldRequest = $"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ws=\"http://zakupki.gov.ru/fz44/get-docs-ip/ws\">\n<soapenv:Header>\n<individualPerson_token>{Program._token}</individualPerson_token>\n</soapenv:Header>\n<soapenv:Body>\n    <ws:getDocsByOrgRegionRequest>\n    <index>\n    <id>{guid}</id>\n    <createDateTime>{currDate}</createDateTime>\n    <mode>PROD</mode>\n    </index>\n    <selectionParams>\n    <orgRegion>{regionKladr}</orgRegion>\n    <subsystemType>{subsystem}</subsystemType>\n    <documentType44>{type}</documentType44>\n    <periodInfo>\n    <exactDate>{prevday}</exactDate>\n    </periodInfo>\n    </selectionParams>\n    </ws:getDocsByOrgRegionRequest>\n    </soapenv:Body>\n    </soapenv:Envelope>";
                    var newRequest =
                        $"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ws=\"http://zakupki.gov.ru/fz44/get-docs-ip/ws\">\n<soapenv:Header>\n<individualPerson_token>{Program._token}</individualPerson_token>\n</soapenv:Header>\n<soapenv:Body>\n<ws:getDocsByOrgRegionRequest>\n<index>\n<id>{guid}</id>\n<createDateTime>{currDate}</createDateTime>\n<mode>PROD</mode>\n</index>\n<selectionParams>\n<orgRegion>{regionKladr}</orgRegion>\n<subsystemType>PRIZ</subsystemType>\n<documentType44>{type}</documentType44>\n<periodInfo><exactDate>{prevday}</exactDate></periodInfo>\n</selectionParams>\n</ws:getDocsByOrgRegionRequest>\n</soapenv:Body>\n</soapenv:Envelope>";
                    var request = Program._newApi ? newRequest : oldRequest;
                    var url = Program._newApi
                        ? "https://int44.zakupki.gov.ru/eis-integration/services/getDocsIP"
                        : "https://int44.zakupki.gov.ru/eis-integration/services/getDocsLE2";
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
        
        public static string soap44PriceReq(string regionKladr, string type, int i)
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
                        $"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ws=\"http://zakupki.gov.ru/fz44/get-docs-ip/ws\">\n<soapenv:Header>\n<individualPerson_token>{Program._token}</individualPerson_token>\n</soapenv:Header>\n<soapenv:Body>\n<ws:getDocsByOrgRegionRequest>\n<index>\n<id>{guid}</id>\n<createDateTime>{currDate}</createDateTime>\n<mode>PROD</mode>\n</index>\n<selectionParams>\n<orgRegion>{regionKladr}</orgRegion>\n<subsystemType>ZC</subsystemType>\n<documentType44>{type}</documentType44>\n<periodInfo><exactDate>{prevday}</exactDate></periodInfo>\n</selectionParams>\n</ws:getDocsByOrgRegionRequest>\n</soapenv:Body>\n</soapenv:Envelope>";
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
                    var oldRequest =
                        $"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ws=\"http://zakupki.gov.ru/fz44/get-docs-ip/ws\">\n<soapenv:Header>\n<individualPerson_token>{Program._token}</individualPerson_token>\n</soapenv:Header>\n<soapenv:Body>\n    <ws:getDocsByOrgRegionRequest>\n    <index>\n    <id>{guid}</id>\n    <createDateTime>{currDate}</createDateTime>\n    <mode>PROD</mode>\n    </index>\n    <selectionParams>\n    <orgRegion>{regionKladr}</orgRegion>\n    <subsystemType>RI223</subsystemType>\n    <documentType223>{type}</documentType223>\n    <periodInfo>\n    <exactDate>{prevday}</exactDate>\n    </periodInfo>\n    </selectionParams>\n    </ws:getDocsByOrgRegionRequest>\n    </soapenv:Body>\n    </soapenv:Envelope>";
                    var newRequest =
                        $"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ws=\"http://zakupki.gov.ru/fz44/get-docs-ip/ws\">\n<soapenv:Header>\n<individualPerson_token>{Program._token}</individualPerson_token>\n</soapenv:Header>\n<soapenv:Body>\n<ws:getDocsByOrgRegionRequest>\n<index>\n<id>{guid}</id>\n<createDateTime>{currDate}</createDateTime>\n<mode>PROD</mode>\n</index>\n<selectionParams>\n<orgRegion>{regionKladr}</orgRegion>\n<subsystemType>RI223</subsystemType>\n<documentType223>{type}</documentType223>\n<periodInfo>\n<exactDate>{prevday}</exactDate>\n</periodInfo>  </selectionParams>\n</ws:getDocsByOrgRegionRequest>\n</soapenv:Body>\n</soapenv:Envelope>";
                    var request = Program._newApi ? newRequest : oldRequest;
                    var url = Program._newApi
                        ? "https://int44.zakupki.gov.ru/eis-integration/services/getDocsIP"
                        : "https://int44.zakupki.gov.ru/eis-integration/services/getDocsLE2";
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
    }
}