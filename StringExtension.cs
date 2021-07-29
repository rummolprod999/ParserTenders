using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ParserTenders
{
    public static class StringExtension
    {
        public static string Win1251ToUtf8(this string s)
        {
            var windows1251 = Encoding.GetEncoding("windows-1251");
            var utf8 = Encoding.UTF8;
            var originalBytes = windows1251.GetBytes(s);
            return utf8.GetString(originalBytes);
        }
        public static DateTime ParseDateUn(this string s, string form)
        {
            
            var d = DateTime.MinValue;
            if (!string.IsNullOrEmpty(s))
            {
                try
                {
                    d = DateTime.ParseExact(s, form, CultureInfo.InvariantCulture);
                }
                catch
                {
                    // ignored
                }
            }
            return d;
        }

        public static string GetDateFromRegex(this string s, string r)
        {
            var ret = "";
            try
            {
                var regex = new Regex(r, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var matches = regex.Matches(s);
                if (matches.Count > 0)
                {
                    ret = matches[0].Groups[1].Value.Trim();
                }
            }
            catch (Exception e)
            {
                Log.Logger(e, r);
            }
            return ret;
        }
    }
}