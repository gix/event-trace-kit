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
        public static void AddRange<T>(
            this ICollection<T> collection, IEnumerable<T> newItems)
        {
            TryAddCapacity(collection, newItems);
            foreach (T local in newItems)
                collection.Add(local);
        }

        public static void RemoveRange<T>(
            this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (T item in items)
                collection.Remove(item);
        }

        public static void EnsureCapacity<T>(this List<T> list, int newCapacity)
        {
            if (list.Capacity < newCapacity)
                list.Capacity = newCapacity;
        }

        private static bool TryAddCapacity<T>(
            ICollection<T> collection, IEnumerable<T> newItems)
        {
            if (collection is List<T> list && newItems is ICollection<T> newItemsCollection) {
                list.EnsureCapacity(list.Count + newItemsCollection.Count);
                return true;
            }

            return false;
        }

        public static IEnumerable<T> OrderBySelf<T>(this IEnumerable<T> source)
            where T : IComparable<T>
        {
            return source.OrderBy(x => x);
        }

        public static int SequenceCompare<T>(
            this IEnumerable<T> first, IEnumerable<T> second)
        {
            return first.SequenceCompare(second, null);
        }

        public static int SequenceCompare<T>(
            this IEnumerable<T> first, IEnumerable<T> second, IComparer<T> comparer)
        {
            if (first == null)
                throw new ArgumentNullException(nameof(first));
            if (second == null)
                throw new ArgumentNullException(nameof(second));

            if (comparer == null)
                comparer = Comparer<T>.Default;

            int cmp;
            using (IEnumerator<T> e1 = first.GetEnumerator())
            using (IEnumerator<T> e2 = second.GetEnumerator()) {
                do {
                    bool atEnd1 = !e1.MoveNext();
                    bool atEnd2 = !e2.MoveNext();

                    if (atEnd1 && atEnd2)
                        return 0;
                    if (atEnd1)
                        return -1;
                    if (atEnd2)
                        return 1;

                    cmp = comparer.Compare(e1.Current, e2.Current);
                } while (cmp == 0);
            }

            return cmp;
        }

        public static bool Any(this IEnumerable source)
        {
            IEnumerator e = source.GetEnumerator();
            return e.MoveNext();
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> collection)
        {
            return new HashSet<T>(collection);
        }

        public static HashSet<T> ToHashSet<T>(
            this IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(collection, comparer);
        }

        public static int IndexOf<TSource>(
            this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            int index = 0;
            foreach (var item in source) {
                if (predicate(item))
                    return index;
                ++index;
            }

            return -1;
        }

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
