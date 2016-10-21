namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;

    public static class CollectionExtensions
    {
        public static int IndexOf<TSource, TValue>(
            this IEnumerable<TSource> source, TValue value, Func<TSource, TValue, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            int index = 0;
            foreach (var item in source) {
                if (predicate(item, value))
                    return index;
                ++index;
            }

            return -1;
        }

        public static int BinarySearch<T, TValue>(
            this List<T> list, TValue value, Func<T, TValue, int> comparer)
        {
            int lo = 0;
            int hi = list.Count - 1;
            while (lo <= hi) {
                int mid = lo + ((hi - lo) >> 1);

                int cmp = comparer(list[mid], value);
                if (cmp == 0)
                    return mid;

                if (cmp < 0)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            return ~lo;
        }
    }
}
