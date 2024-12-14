#region

using System;
using System.Globalization;
using System.IO;
using System.Text;

#endregion

namespace ParserTenders
{
    public class Log
    {
        private static readonly object _locker = new object();

        public static void Logger(params object[] parametrs)
        {
            var s = "";
            s += DateTime.Now.ToString(CultureInfo.InvariantCulture);
            for (var i = 0; i < parametrs.Length; i++)
            {
                s = $"{s} {parametrs[i]}";
            }

            lock (_locker)
            {
                using (var sw = new StreamWriter(Program.FileLog, true, Encoding.Default))
                {
                    sw.WriteLine(s);
                }
            }
        }
    }
}