namespace NOpt.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    internal static class IReadOnlyListExtensions
    {
        public static int WeakPredecessor<T>(
            this IReadOnlyList<T> list, T value)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Ensures(Contract.Result<int>() >= 0);

            return WeakPredecessor(list, 0, list.Count, value, Comparer<T>.Default.Compare);
        }

        public static int WeakPredecessor<T>(
            this IReadOnlyList<T> list, T value, IComparer<T> comparer)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparer != null);
            Contract.Ensures(Contract.Result<int>() >= 0);

            return WeakPredecessor(list, 0, list.Count, value, comparer.Compare);
        }

        public static int WeakPredecessor<T, TValue>(
            this IReadOnlyList<T> list, TValue value, Func<T, TValue, int> comparer)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparer != null);
            Contract.Ensures(Contract.Result<int>() >= 0);

            return WeakPredecessor(list, 0, list.Count, value, comparer);
        }

        public static int WeakPredecessor<T, TValue>(
            this IReadOnlyList<T> list, int index, int count, TValue value,
            Func<T, TValue, int> comparer)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparer != null);
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0 && count >= 0);
            Contract.Requires<ArgumentException>(list.Count - index >= count);
            Contract.Ensures(Contract.Result<int>() >= 0);

            while (count > 0) {
                int half = count / 2;
                int mid = index + half;

                if (comparer(list[mid], value) < 0) {
                    // Value in upper half.
                    index = mid + 1;
                    count -= half + 1;
                } else {
                    // Value in lower half.
                    count = half;
                }
            }

            return index;
        }

        public static int WeakSuccessor<T>(
            this IReadOnlyList<T> list, T value)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= list.Count);

            return WeakSuccessor(list, 0, list.Count, value, Comparer<T>.Default.Compare);
        }

        public static int WeakSuccessor<T>(
            this IReadOnlyList<T> list, T value, IComparer<T> comparer)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparer != null);
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= list.Count);

            return WeakSuccessor(list, 0, list.Count, value, comparer.Compare);
        }

        public static int WeakSuccessor<T, TValue>(
            this IReadOnlyList<T> list, TValue value, Func<T, TValue, int> comparer)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparer != null);
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= list.Count);

            return WeakSuccessor(list, 0, list.Count, value, comparer);
        }

        public static int WeakSuccessor<T, TValue>(
            this IReadOnlyList<T> list, int index, int count, TValue value,
            Func<T, TValue, int> comparer)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparer != null);
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0 && count >= 0);
            Contract.Requires<ArgumentException>(list.Count - index >= count);
            Contract.Ensures(Contract.Result<int>() >= index);
            Contract.Ensures(Contract.Result<int>() <= index + count);

            while (count > 0) {
                int half = count / 2;
                int mid = index + half;

                if (comparer(list[mid], value) <= 0) {
                    // Value in upper half.
                    index = mid + 1;
                    count -= half + 1;
                } else {
                    // Value in lower half.
                    count = half;
                }
            }

            return index;
        }

        public static IEnumerable<T> EqualRange<T>(
            this IReadOnlyList<T> list, T value)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);

            return EqualRange(list, 0, list.Count, value, Comparer<T>.Default.Compare);
        }

        public static IEnumerable<T> EqualRange<T>(
            this IReadOnlyList<T> list, T value, IComparer<T> comparer)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparer != null);
            Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);

            return EqualRange(list, 0, list.Count, value, comparer.Compare);
        }

        public static IEnumerable<T> EqualRange<T, TValue>(
            this IReadOnlyList<T> list, TValue value, Func<T, TValue, int> comparer)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparer != null);
            Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);

            return EqualRange(list, 0, list.Count, value, comparer);
        }

        public static IEnumerable<T> EqualRange<T, TValue>(
            this IReadOnlyList<T> list, int index, int count, TValue value,
            Func<T, TValue, int> comparer)
        {
            Contract.Requires<ArgumentNullException>(list != null);
            Contract.Requires<ArgumentNullException>(comparer != null);
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(count >= 0);
            Contract.Requires<ArgumentException>(list.Count - index >= count);
            Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);

            int idx = list.WeakPredecessor(index, count, value, comparer);
            int end = list.WeakSuccessor(index, count, value, comparer);
            for (; idx < end; ++idx)
                yield return list[idx];
        }
    }
}
