﻿using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
                    var task = Task.Run(() => (new TimedWebClient()).DownloadString(url));
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
                    var task = Task.Run(() => (new TimedWebClientUa()).DownloadString(url));
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
                    if (ex.Response is HttpWebResponse r) Log.Logger("Response code: ", r.StatusCode);
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
                    var task = Task.Run(() => (new TimedWebClientUaEis()).DownloadString(url));
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
                    if (ex.Response is HttpWebResponse r) Log.Logger("Response code: ", r.StatusCode);
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
                        var v = new TimedWebClient {Encoding = Encoding.GetEncoding("windows-1251")};
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
    }
}