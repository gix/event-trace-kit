namespace EventManifestFramework.Internal.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal static class CollectionExtensions
    {
        public static TValue Coalesce<T, TValue>(
            this IEnumerable<T> enumerable, Func<T, TValue> selector)
            where TValue : class
        {
            return enumerable.Select(selector).FirstOrDefault(value => value != null);
        }

        public static int FindIndex<T>(this IReadOnlyList<T> list, Predicate<T> match)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            return list.FindIndex(0, list.Count, match);
        }

        public static int FindIndex<T>(this IReadOnlyList<T> list, int startIndex, Predicate<T> match)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (startIndex > list.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, null);
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            return list.FindIndex(startIndex, list.Count - startIndex, match);
        }

        public static int FindIndex<T>(this IReadOnlyList<T> list, int startIndex, int count, Predicate<T> match)
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
    }
}
