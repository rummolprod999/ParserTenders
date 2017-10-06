﻿using System;
using System.Threading;

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
                    tmp = new TimedWebClient().DownloadString(url);
                    break;
                }
                catch (Exception e)
                {
                    if (count >= 100)
                    {
                        Log.Logger($"Не удалось скачать xml за {count} попыток", url);
                        break;
                    }
                    Log.Logger("Не удалось получить строку xml", e , url);
                    count++;
                    Thread.Sleep(5000);
                }
            }
            return tmp;
        }
    }
}