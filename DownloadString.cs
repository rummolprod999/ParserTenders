using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ParserTenders
{
    public static class DownloadString
    {
        public static string DownL(string url)
        {
            string tmp = "";
            int count = 0;
            while (true)
            {
                try
                {
                    var task = Task.Run(() => (new TimedWebClient()).DownloadString(url));
                    if (task.Wait(TimeSpan.FromSeconds(650)))
                    {
                        tmp = task.Result;
                        break;
                    }
                    throw new TimeoutException();
                    //tmp = new TimedWebClient().DownloadString(url);
                }
                catch (Exception e)
                {
                    if (count >= 3)
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
        
        public static string DownLUserAgent(string url)
        {
            string tmp = "";
            int count = 0;
            while (true)
            {
                try
                {
                    var task = Task.Run(() => (new TimedWebClientUa()).DownloadString(url));
                    if (task.Wait(TimeSpan.FromSeconds(100)))
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
                    if (ex.Response is HttpWebResponse errorResponse && errorResponse.StatusCode == HttpStatusCode.Forbidden)
                    {
                        Log.Logger("Error 403");
                        return tmp;
                    }
                    if (count >= 5)
                    {
                        Log.Logger($"Не удалось скачать xml за {count} попыток", url);
                        break;
                    }
                    Log.Logger("Не удалось получить строку xml", ex, url);
                    count++;
                    Thread.Sleep(5000);

                }
                catch (Exception e)
                {
                    if (count >= 5)
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
        
        public static string DownL1251(string url)
        {
            string tmp = "";
            int count = 0;
            while (true)
            {
                try
                {
                    var task = Task.Run(() =>
                    {
                        var v = new TimedWebClient {Encoding = Encoding.GetEncoding("windows-1251")};
                        return v.DownloadString(url);
                    });
                    if (task.Wait(TimeSpan.FromSeconds(650)))
                    {
                        tmp = task.Result;
                        break;
                    }
                    throw new TimeoutException();
                    //tmp = new TimedWebClient().DownloadString(url);
                }
                catch (Exception e)
                {
                    if (count >= 3)
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