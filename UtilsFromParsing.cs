using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ParserTenders
{
    public static class UtilsFromParsing
    {
        static UtilsFromParsing()
        {
        }

        public static decimal ParsePrice(string s)
        {
            s = System.Net.WebUtility.HtmlDecode(s);
            s = Regex.Replace(s, @"\s+", "");
            s = s.Replace("руб.", "");
            decimal d = 0.0m;
            try
            {
                IFormatProvider formatter = new NumberFormatInfo {NumberDecimalSeparator = ","};
                d = Decimal.Parse(s, formatter);
            }
            catch (Exception e)
            {
                //WriteLine(e);
            }

            return d;
        }

        public static DateTime ParseDateTend(string s)
        {
            DateTime d = DateTime.MinValue;
            if (!String.IsNullOrEmpty(s))
            {
                try
                {
                    d = DateTime.Parse(s);
                }
                catch
                {
                    // ignored
                }
            }
            return d;
        }
    }
}