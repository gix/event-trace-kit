﻿namespace EventTraceKit.VsExtension.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public static class Extensions
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
    }
}
