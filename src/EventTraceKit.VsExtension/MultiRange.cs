namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;

    public sealed class MultiRange : IEnumerable<int>
    {
        private readonly List<Range> ranges = new List<Range>(5);

        public MultiRange()
        {
        }

        public MultiRange(MultiRange source)
        {
            ranges.AddRange(source.ranges);
        }

        public int Count { get; private set; }

        internal IEnumerable<Range> GetRanges()
        {
            return ranges;
        }

        public bool Contains(int value)
        {
            return BinarySearch(value) >= 0;
        }

        public void Add(Range range)
        {
            bool containsAll =
                ranges.Count > 0
                && range.Begin <= ranges[0].Begin
                && range.End >= ranges[ranges.Count - 1].End;
            if (containsAll) {
                Count = range.Length;
                ranges.Clear();
                ranges.Add(range);
                return;
            }

            int idx = ranges.BinarySearch(range);
            if (idx < 0)
                ranges.Insert(~idx, range);

            Count += range.Length;
            Merge();
        }

        public void Add(int value)
        {
            int idx = BinarySearch(value);
            if (idx >= 0)
                return;

            ++Count;
            ranges.Insert(~idx, new Range(value, value + 1));
            Merge();
        }

        public void Remove(int value)
        {
            int idx = BinarySearch(value);
            if (idx < 0)
                return;

            --Count;
            Range range = ranges[idx];
            if (value == range.Begin && value + 1 == range.End)
                // If the range only contains value, remove it.
                ranges.RemoveAt(idx);
            else if (value == range.Begin)
                // Drop value from the beginning of the range.
                ranges[idx] = new Range(value + 1, range.End);
            else if (value + 1 == range.End)
                // Drop value from the end of the range.
                ranges[idx] = new Range(range.Begin, value);
            else {
                // Value is in the middle. Split into two ranges.
                ranges[idx] = new Range(range.Begin, value);
                ranges.Insert(idx + 1, new Range(value + 1, range.End));
            }
        }

        public void Clear()
        {
            ranges.Clear();
            Count = 0;
        }

        public void UnionWith(MultiRange other)
        {
            if (ranges.Count == 0) {
                ranges.AddRange(other.ranges);
                return;
            }

            Count += other.Count;
            ranges.AddRange(other.ranges);
            ranges.Sort();
            Merge();
        }

        public IEnumerator<int> GetEnumerator()
        {
            foreach (var range in ranges) {
                for (int i = range.Begin; i < range.End; ++i)
                    yield return i;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private int BinarySearch(int value)
        {
            if (ranges.Count == 0)
                return ~0;

            // [lo, hi)
            int lo = 0;
            int hi = ranges.Count;
            do {
                int mid = (int)(((long)lo + hi) / 2);
                int cmp = ranges[mid].CompareTo(value);
                if (cmp < 0)
                    lo = mid + 1; // [mid + 1, hi)
                else if (cmp == 0)
                    return mid;
                else
                    hi = mid; // [lo, mid)
            } while (lo < hi);

            return ~lo;
        }

        private void Merge()
        {
            for (int i = ranges.Count - 2; i >= 0; --i) {
                var range = ranges[i];
                var next = ranges[i + 1];
                Debug.Assert(range.Begin <= next.Begin);
                if (range.End >= next.Begin) {
                    ranges[i] = new Range(range.Begin, Math.Max(range.End, next.End));
                    ranges.RemoveAt(i + 1);
                    Count -= Math.Min(range.End, next.End) - next.Begin;
                }
            }
        }
    }
}
