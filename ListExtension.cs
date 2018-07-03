using System.Collections.Generic;

namespace ParserTenders
{
    public static class ListExtensions
    {
        public static List<T> AddRangeAndReturnList<T>(this List<T> l, List<T> addList)
        {
            l.AddRange(addList);
            return l;
        }
    }
}