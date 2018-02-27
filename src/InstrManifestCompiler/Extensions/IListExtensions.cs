namespace InstrManifestCompiler.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class IListExtensions
    {
        public static IList<T> AddRange<T>(this IList<T> list, IEnumerable<T> range)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (range == null)
                return list;

            foreach (var item in range)
                list.Add(item);

            return list;
        }

        public static int FindIndex<T>(this IList<T> list, Predicate<T> match)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            return list.FindIndex(0, list.Count, match);
        }

        public static int FindIndex<T>(this IList<T> list, int startIndex, Predicate<T> match)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (startIndex > list.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, null);
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            return list.FindIndex(startIndex, list.Count - startIndex, match);
        }

        public static int FindIndex<T>(this IList<T> list, int startIndex, int count, Predicate<T> match)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (startIndex > list.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, null);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, null);
            if (count > list.Count - startIndex)
                throw new ArgumentException("Invalid range");
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            int end = startIndex + count;
            for (int i = startIndex; i < end; ++i) {
                if (match(list[i]))
                    return i;
            }

            return -1;
        }

        public static void RemoveAll<T>(this IList<T> list, Predicate<T> match)
        {
            if (list is List<T> listT) {
                listT.RemoveAll(match);
                return;
            }

            var matches = list.Where(new Func<T, bool>(match)).ToList();
            foreach (var item in matches)
                list.Remove(item);
        }
    }
}
