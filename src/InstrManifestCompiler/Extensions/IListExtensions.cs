namespace InstrManifestCompiler.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    public static class IListExtensions
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

        public static int FindIndex<T>(this IList<T> list, Predicate<T> match)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(match != null);
            return list.FindIndex(0, list.Count, match);
        }

        public static int FindIndex<T>(this IList<T> list, int startIndex, Predicate<T> match)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentOutOfRangeException>(startIndex <= list.Count);
            Contract.Requires<ArgumentNullException>(match != null);
            return list.FindIndex(startIndex, list.Count - startIndex, match);
        }

        public static int FindIndex<T>(this IList<T> list, int startIndex, int count, Predicate<T> match)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentOutOfRangeException>(startIndex <= list.Count);
            Contract.Requires<ArgumentOutOfRangeException>(count >= 0 && startIndex <= (list.Count - count));
            Contract.Requires<ArgumentNullException>(match != null);

            int end = startIndex + count;
            for (int i = startIndex; i < end; ++i) {
                if (match(list[i]))
                    return i;
            }

            return -1;
        }

    }
}
