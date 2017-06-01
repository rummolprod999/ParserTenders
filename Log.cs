using System;
using System.Globalization;
using System.IO;

namespace ParserTenders
{
    public class Log
    {
        public static void Logger(params object[] parametrs)
        {
            string s = "";
            s += DateTime.Now.ToString(CultureInfo.InvariantCulture);
            for (int i = 0; i < parametrs.Length; i++)
            {
                s = $"{s} {parametrs[i]}";
            }

            using (StreamWriter sw = new StreamWriter(Program.FileLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine(s);
            }
        }
    }
}