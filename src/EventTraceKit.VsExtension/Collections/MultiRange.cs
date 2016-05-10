namespace EventTraceKit.VsExtension.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class MultiRange : IEnumerable<int>
    {
        private struct Range : IComparable<int>
        {
            public readonly int Begin;
            public readonly int End;

            public Range(int begin, int end)
            {
                Begin = begin;
                End = end;
            }

            public int CompareTo(int value)
            {
                if (Begin > value) return 1;
                if (End <= value) return -1;
                return 0;
            }
        }

        private readonly List<Range> ranges = new List<Range>(5);

        public int Count { get; private set; }

        public bool Contains(int value)
        {
            return BinarySearch(value) >= 0;
        }

        public void Add(int value)
        {
            int idx = BinarySearch(value);
            if (idx >= 0)
                return;

            ranges.Insert(~idx, new Range(value, value + 1));
            Merge();
            ++Count;
        }

        public void Remove(int value)
        {
            int idx = BinarySearch(value);
            if (idx < 0)
                return;

            Range old = ranges[idx];
            if (value == old.Begin && value + 1 == old.End)
                ranges.RemoveAt(idx);
            else if (value == old.Begin)
                ranges[idx] = new Range(value + 1, old.End);
            else {
                ranges[idx] = new Range(old.Begin, value);
                if (value + 1 != old.End)
                    ranges.Insert(idx + 1, new Range(value + 1, old.End));
            }

            --Count;
        }

        public void Clear()
        {
            ranges.Clear();
            Count = 0;
        }

        private void Merge()
        {
            for (int i = ranges.Count - 2; i >= 0; --i) {
                var range = ranges[i];
                var next = ranges[i + 1];
                if (range.End == next.Begin) {
                    ranges[i] = new Range(range.Begin, next.End);
                    ranges.RemoveAt(i + 1);
                }
            }
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

        internal IEnumerable<Tuple<int, int>> GetRanges()
        {
            return ranges.Select(x => Tuple.Create(x.Begin, x.End)).ToList();
        }
    }
}
