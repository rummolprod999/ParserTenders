using System.Text;

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
    }
}