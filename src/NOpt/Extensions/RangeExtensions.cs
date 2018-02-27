namespace NOpt.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    internal static class RangeExtensions
    {
        public static IList<T> AddRange<T>(this IList<T> list, IEnumerable<T> range)
        {
            Contract.Requires<ArgumentNullException>(list != null);

            if (range == null)
                return list;

            foreach (var item in range)
                list.Add(item);

            return list;
        }

        public static ISet<T> AddRange<T>(this ISet<T> set, IEnumerable<T> range)
        {
            Contract.Requires<ArgumentNullException>(set != null);

            if (range == null)
                return set;

            foreach (var item in range)
                set.Add(item);

            return set;
        }
    }
}
