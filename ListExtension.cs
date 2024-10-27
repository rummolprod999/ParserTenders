using System;
using System.Collections.Generic;

namespace ParserTenders
{
    public static class ListExtensions
    {
        private static Random rng = new Random();

        public static List<T> AddRangeAndReturnList<T>(this List<T> l, List<T> addList)
        {
            l.AddRange(addList);
            return l;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }
}