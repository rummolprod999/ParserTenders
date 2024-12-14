#region

using System;
using System.Collections.Generic;

#endregion

namespace ParserTenders
{
    public static class ListExtensions
    {
        private static readonly Random rng = new Random();

        public static List<T> AddRangeAndReturnList<T>(this List<T> l, List<T> addList)
        {
            l.AddRange(addList);
            return l;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }
}