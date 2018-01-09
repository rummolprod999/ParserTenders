using System;
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
                    if (count >= 100)
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
                    if (count >= 100)
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
                    if (count >= 100)
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