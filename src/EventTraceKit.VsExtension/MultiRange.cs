namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public sealed class MultiRange : IEnumerable<int>, IEquatable<MultiRange>
    {
        private readonly List<Range> ranges = new List<Range>(5);

        public MultiRange()
        {
        }

        public MultiRange(MultiRange source)
        {
            Count = source.Count;
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
            if (range.Length == 0)
                return;

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

        public void Remove(Range range)
        {
            if (range.Length == 0)
                return;

            bool containsNothing =
                ranges.Count > 0
                && (range.End <= ranges[0].Begin
                    || range.Begin >= ranges[ranges.Count - 1].End);
            if (containsNothing)
                return;

            bool containsAll =
                ranges.Count > 0
                && range.Begin <= ranges[0].Begin
                && range.End >= ranges[ranges.Count - 1].End;
            if (containsAll) {
                Count = 0;
                ranges.Clear();
                return;
            }

            for (int i = ranges.Count - 1; i >= 0; --i) {
                var curr = ranges[i];

                // If the range to be removed lies past our end we are done.
                if (range.Begin >= curr.End)
                    break;

                if (range.End <= curr.Begin)
                    continue; // The range to be removed lies before the current range, skip.

                // The current range and range to be removed overlap.
                Debug.Assert(range.Begin < curr.End && range.End > curr.Begin);

                // If the current range is fully contained, just remove it.
                //   |-- range[i] -|
                // |----- range -----|
                if (curr.Begin >= range.Begin && curr.End <= range.End) {
                    ranges.RemoveAt(i);
                    Count -= curr.Length;
                    continue;
                }

                // |----|-- range[i] -|
                // |--- range ---|
                if (curr.Begin >= range.Begin && curr.End >= range.End) {
                    ranges[i] = new Range(range.End, curr.End);
                    Count -= range.End - curr.Begin;
                    continue;
                }

                // |-- range[i] -|
                //      |--- range ---|
                if (curr.Begin <= range.Begin && curr.End <= range.End) {
                    ranges[i] = new Range(curr.Begin, range.Begin);
                    Count -= curr.End - range.Begin;
                    continue;
                }

                // If the current range is a superset, split it.
                // |-- range[i] --|
                //   |- range -|
                if (curr.Begin < range.Begin && curr.End > range.End) {
                    ranges[i] = new Range(curr.Begin, range.Begin);
                    ranges.Insert(i + 1, new Range(range.End, curr.End));
                    Count -= range.Length;
                    continue;
                }
            }
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
                // If the range only contains one value, remove it.
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
                Count = other.Count;
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

        public override bool Equals(object obj)
        {
            return obj is MultiRange range && Equals(range);
        }

        public bool Equals(MultiRange other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Count == other.Count && ranges.SequenceEqual(other.ranges);
        }

        public override int GetHashCode()
        {
            unchecked {
                return ((ranges != null ? ranges.GetHashCode() : 0) * 397) ^ Count;
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
