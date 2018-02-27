namespace InstrManifestCompiler.Collections
{
    using System.Collections.Generic;

    internal static class MergeSort
    {
        public static List<T> Sort<T>(IList<T> values, int startIndex, int endIndex, IComparer<T> comparer)
        {
            if (startIndex >= endIndex - 1) {
                var result = new List<T>();
                for (int i = startIndex; i < endIndex; ++i)
                    result.Add(values[i]);
                return result;
            }

            int middle = startIndex + ((endIndex - startIndex) / 2);
            var left = Sort(values, startIndex, middle, comparer);
            var right = Sort(values, middle, endIndex, comparer);
            return Merge(left, right, comparer);
        }

        private static List<T> Merge<T>(List<T> left, List<T> right, IComparer<T> comparer)
        {
            var result = new List<T>(left.Count + right.Count);

            int leftIdx = 0;
            int rightIdx = 0;
            while (leftIdx < left.Count || rightIdx < right.Count) {
                var hasLeft = leftIdx < left.Count;
                var hasRight = rightIdx < right.Count;
                if (hasLeft && hasRight) {
                    if (comparer.Compare(left[leftIdx], right[rightIdx]) <= 0)
                        result.Add(left[leftIdx++]);
                    else
                        result.Add(right[rightIdx++]);
                } else if (hasLeft) {
                    result.Add(left[leftIdx++]);
                } else if (hasRight) {
                    result.Add(right[rightIdx++]);
                }
            }

            return result;
        }
    }
}
