namespace EventTraceKit.VsExtension.Extensions
{
    using System;
    using System.Collections.Generic;

    internal static class ComparisonUtils
    {
        public static bool CompareValueT<T>(
            out int cmp, T first, T second) where T : struct, IComparable<T>
        {
            cmp = first.CompareTo(second);
            return cmp == 0;
        }

        public static bool Compare<T>(out int cmp, T first, T second)
            where T : IComparable
        {
            cmp = first.CompareTo(second);
            return cmp == 0;
        }

        public static bool CompareT<T>(
            out int cmp, T x, T y) where T : class, IComparable<T>
        {
            if (x == null || y == null)
                cmp = (x == null ? 1 : 0) - (y == null ? 1 : 0);
            else
                cmp = x.CompareTo(y);

            return cmp == 0;
        }

        public static bool CombineSequenceComparisonT<T>(
            out int cmp, IEnumerable<T> first, IEnumerable<T> second)
            where T : IComparable<T>
        {
            cmp = first.SequenceCompare(second);
            return cmp == 0;
        }
    }
}
