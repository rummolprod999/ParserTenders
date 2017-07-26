using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ParserTenders
{
    public class Log
    {
        private static object locker = new object();
        public static void Logger(params object[] parametrs)
        {
            string s = "";
            s += DateTime.Now.ToString(CultureInfo.InvariantCulture);
            for (int i = 0; i < parametrs.Length; i++)
            {
                s = $"{s} {parametrs[i]}";
            }

            lock (locker)
            {
                using (StreamWriter sw = new StreamWriter(Program.FileLog, true, Encoding.Default))
                {
                    sw.WriteLine(s);
                }
            }
        }
    }
}