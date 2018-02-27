namespace InstrManifestCompiler.Extensions
{
    using System;
    using System.Collections.Generic;
    using InstrManifestCompiler.Collections;

    public static class SortExtensions
    {
        public static List<T> SortBy<T, TResult>(
            this List<T> list, Func<T, TResult> selector) where TResult : IComparable<TResult>
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            list.Sort((x, y) => selector(x).CompareTo(selector(y)));
            return list;
        }

        public static IList<T> StableSortBy<T, TResult>(
            this IList<T> list, Func<T, TResult> selector) where TResult : IComparable<TResult>
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            return list.StableSort(new SelectorComparer<T, TResult>(selector));
        }

        public static IList<T> StableSort<T>(this IList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            return UncheckedMergeSort(list, 0, list.Count, Comparer<T>.Default);
        }

        public static IList<T> StableSort<T>(this IList<T> list, Comparison<T> comparison)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison));
            return UncheckedMergeSort(list, 0, list.Count, new FunctorComparer<T>(comparison));
        }

        public static IList<T> StableSort<T>(this IList<T> list, IComparer<T> comparer)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
            return UncheckedMergeSort(list, 0, list.Count, comparer);
        }

        public static IList<T> StableSort<T>(
            this IList<T> list, int index, int count, Comparison<T> comparison)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison));
            return UncheckedMergeSort(list, index, count, new FunctorComparer<T>(comparison));
        }

        public static IList<T> StableSort<T>(
            this IList<T> list, int index, int count, IComparer<T> comparer)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
            return UncheckedMergeSort(list, index, count, comparer);
        }

        public static IList<T> MergeSort<T>(this IList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            return UncheckedMergeSort(list, 0, list.Count, Comparer<T>.Default);
        }

        public static IList<T> MergeSort<T>(this IList<T> list, Comparison<T> comparison)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison));
            return UncheckedMergeSort(list, 0, list.Count, new FunctorComparer<T>(comparison));
        }

        public static IList<T> MergeSort<T>(this IList<T> list, IComparer<T> comparer)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
            return UncheckedMergeSort(list, 0, list.Count, comparer);
        }

        public static IList<T> MergeSort<T>(
            this IList<T> list, int index, int count, Comparison<T> comparison)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison));
            return UncheckedMergeSort(list, index, count, new FunctorComparer<T>(comparison));
        }

        public static IList<T> MergeSort<T>(
            this IList<T> list, int index, int count, IComparer<T> comparer)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, null);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, null);
            if (list.Count - index >= count)
                throw new ArgumentException("Invalid range");
            return UncheckedMergeSort(list, index, count, comparer);
        }

        private static IList<T> UncheckedMergeSort<T>(
            this IList<T> list, int index, int count, IComparer<T> comparer)
        {
            if (count == 0)
                return list;

            List<T> sorted = Collections.MergeSort.Sort(list, index, index + count, comparer);
            for (int i = 0; i < sorted.Count; ++i)
                list[i] = sorted[i];
            return list;
        }
    }
}
