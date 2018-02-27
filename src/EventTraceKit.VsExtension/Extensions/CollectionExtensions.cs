namespace EventTraceKit.VsExtension.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Serialization;

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

        public static int BinarySearch<T, TValue>(
            this IReadOnlyList<T> list, TValue value, Func<T, TValue, int> comparer)
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

        private static T[] ConvertToArray<T>(IList list)
        {
            return list.Cast<T>().ToArray();
        }

        public static object[] ConvertToTypedArray(this IList list)
        {
            Type itemType = DetermineItemType(list);
            var convertMethod = typeof(CollectionExtensions).GetMethod(
                nameof(ConvertToArray), BindingFlags.Static | BindingFlags.NonPublic);

            var genericMethod = convertMethod.MakeGenericMethod(itemType);
            return (object[])genericMethod.Invoke(null, new object[] { list });
        }

        private static Type DetermineItemType(IList list)
        {
            if (TypeHelper.TryGetGenericListItemType(list.GetType(), out var itemType) &&
                itemType != typeof(object))
                return itemType;

            if (list.Count > 0) {
                itemType = null;
                foreach (var item in list) {
                    if (item == null)
                        continue;
                    if (itemType == null)
                        itemType = item.GetType();
                    else if (itemType != item.GetType()) {
                        itemType = null;
                        break;
                    }
                }

                if (itemType != null)
                    return itemType;
            }

            return typeof(object);
        }
    }
}
