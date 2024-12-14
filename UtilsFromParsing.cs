#region

using System;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

#endregion

namespace ParserTenders
{
    public static class UtilsFromParsing
    {
        public static decimal ParsePrice(string s)
        {
            s = WebUtility.HtmlDecode(s);
            s = Regex.Replace(s, @"\s+", "");
            s = Regex.Replace(s, @"[A-Za-zА-Яа-я]", "");
            s = Regex.Replace(s, @"\(|\)|-", "");
            s = s.Replace(".", "");
            //Console.WriteLine(s);
            var d = 0.0m;
            try
            {
                IFormatProvider formatter = new NumberFormatInfo { NumberDecimalSeparator = "," };
                d = decimal.Parse(s, formatter);
            }
            catch (Exception e)
            {
                //WriteLine(e);
            }

            return d;
        }

        public static decimal ParsePriceMrsk(string s)
        {
            s = WebUtility.HtmlDecode(s);
            s = Regex.Replace(s, @"\s+", "");
            s = Regex.Replace(s, @"[A-Za-zА-Яа-я]", "");
            s = Regex.Replace(s, @"\(|\)|-", "");
            var d = 0.0m;
            try
            {
                IFormatProvider formatter = new NumberFormatInfo { NumberDecimalSeparator = "." };
                d = decimal.Parse(s, formatter);
            }
            catch (Exception)
            {
                //WriteLine(e);
            }

            return d;
        }

        public static decimal ParsePriceRosneft(string s)
        {
            s = WebUtility.HtmlDecode(s);
            s = Regex.Replace(s, @"\s+", "");
            s = s.GetDateFromRegex(@"(\d+[\.,]?\d+)");
            s = s.Replace('.', ',');
            var d = 0.0m;
            try
            {
                IFormatProvider formatter = new NumberFormatInfo { NumberDecimalSeparator = "," };
                d = decimal.Parse(s, formatter);
            }
            catch (Exception)
            {
                //ignore
            }

            return d;
        }

        public static DateTime ParseDateTend(string s)
        {
            var d = DateTime.MinValue;
            if (!string.IsNullOrEmpty(s))
            {
                try
                {
                    d = DateTime.ParseExact(s, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
                }
                catch
                {
                    // ignored
                }
            }

            return d;
        }

        public static DateTime ParseDateMrsk(string s)
        {
            var d = DateTime.MinValue;
            if (!string.IsNullOrEmpty(s))
            {
                try
                {
                    d = DateTime.ParseExact(s, "dd.MM.yyyy", CultureInfo.InvariantCulture);
                }
                catch
                {
                    // ignored
                }
            }

            return d;
        }

        public static int GetConformity(string conf)
        {
            var sLower = conf.ToLower();
            if (sLower.IndexOf("открыт", StringComparison.Ordinal) != -1)
            {
                return 5;
            }

            if (sLower.IndexOf("аукцион", StringComparison.Ordinal) != -1)
            {
                return 1;
            }

            if (sLower.IndexOf("котиров", StringComparison.Ordinal) != -1)
            {
                return 2;
            }

            if (sLower.IndexOf("предложен", StringComparison.Ordinal) != -1)
            {
                return 3;
            }

            if (sLower.IndexOf("единств", StringComparison.Ordinal) != -1)
            {
                return 4;
            }

            return 6;
        }
    }
}