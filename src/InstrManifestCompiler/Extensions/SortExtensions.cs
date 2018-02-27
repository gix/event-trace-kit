namespace InstrManifestCompiler.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using InstrManifestCompiler.Collections;

    public static class SortExtensions
    {
        public static List<T> SortBy<T, TResult>(
            this List<T> list, Func<T, TResult> selector) where TResult : IComparable<TResult>
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(selector != null);
            list.Sort((x, y) => selector(x).CompareTo(selector(y)));
            return list;
        }

        public static IList<T> StableSortBy<T, TResult>(
            this IList<T> list, Func<T, TResult> selector) where TResult : IComparable<TResult>
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(selector != null);
            return list.StableSort(new SelectorComparer<T, TResult>(selector));
        }

        public static IList<T> StableSort<T>(this IList<T> list)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            return UncheckedMergeSort(list, 0, list.Count, Comparer<T>.Default);
        }

        public static IList<T> StableSort<T>(this IList<T> list, Comparison<T> comparison)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparison != null);
            return UncheckedMergeSort(list, 0, list.Count, new FunctorComparer<T>(comparison));
        }

        public static IList<T> StableSort<T>(this IList<T> list, IComparer<T> comparer)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparer != null);
            return UncheckedMergeSort(list, 0, list.Count, comparer);
        }

        public static IList<T> StableSort<T>(
            this IList<T> list, int index, int count, Comparison<T> comparison)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparison != null);
            return UncheckedMergeSort(list, index, count, new FunctorComparer<T>(comparison));
        }

        public static IList<T> StableSort<T>(
            this IList<T> list, int index, int count, IComparer<T> comparer)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparer != null);
            return UncheckedMergeSort(list, index, count, comparer);
        }

        public static IList<T> MergeSort<T>(this IList<T> list)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            return UncheckedMergeSort(list, 0, list.Count, Comparer<T>.Default);
        }

        public static IList<T> MergeSort<T>(this IList<T> list, Comparison<T> comparison)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparison != null);
            return UncheckedMergeSort(list, 0, list.Count, new FunctorComparer<T>(comparison));
        }

        public static IList<T> MergeSort<T>(this IList<T> list, IComparer<T> comparer)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparer != null);
            return UncheckedMergeSort(list, 0, list.Count, comparer);
        }

        public static IList<T> MergeSort<T>(
            this IList<T> list, int index, int count, Comparison<T> comparison)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparison != null);
            return UncheckedMergeSort(list, index, count, new FunctorComparer<T>(comparison));
        }

        public static IList<T> MergeSort<T>(
            this IList<T> list, int index, int count, IComparer<T> comparer)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparer != null);
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(count >= 0);
            Contract.Requires<ArgumentException>(list.Count - index < count);
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
