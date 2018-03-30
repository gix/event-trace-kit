namespace EventTraceKit.VsExtension.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Xunit;
    using Xunit.Abstractions;

    public class MultiRangeTest
    {
        private readonly ITestOutputHelper output;

        public MultiRangeTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Default()
        {
            var range = new MultiRange();

            Assert.Equal(0, range.Count);
            Assert.Equal(new Range[0], range.GetRanges());
            Assert.False(range.Contains(42));
        }

        [Fact]
        public void Copy()
        {
            var range = new MultiRange();
            range.Add(23);
            range.Add(42);

            var copy = new MultiRange(range);

            Assert.Equal(2, copy.Count);
            Assert.Equal(new[] { R(23), R(42) }, range.GetRanges());
        }

        [Fact]
        public void Copy_Empty()
        {
            var range = new MultiRange();

            var copy = new MultiRange(range);

            Assert.Equal(0, copy.Count);
            Assert.Equal(new Range[0], copy.GetRanges());
            Assert.False(copy.Contains(42));
        }

        [Fact]
        public void Add()
        {
            var range = new MultiRange();

            range.Add(42);

            Assert.Equal(1, range.Count);
            Assert.Equal(new[] { R(42) }, range.GetRanges());
            Assert.True(range.Contains(42));
        }

        [Fact]
        public void Add_OutOfOrder()
        {
            var range = new MultiRange();

            range.Add(23);
            range.Add(42);
            range.Add(10);

            Assert.Equal(3, range.Count);
            Assert.Equal(new[] { R(10), R(23), R(42) }, range.GetRanges());
            Assert.True(range.Contains(10));
            Assert.True(range.Contains(23));
            Assert.True(range.Contains(42));
            Assert.False(range.Contains(22));
            Assert.False(range.Contains(24));
        }

        [Fact]
        public void Add_Begin()
        {
            var range = new MultiRange { 11 };

            range.Add(10);

            Assert.Equal(2, range.Count);
            Assert.Equal(new[] { R(10, 12) }, range.GetRanges());
            Assert.True(range.Contains(10));
        }

        [Fact]
        public void Add_End()
        {
            var range = new MultiRange { 11 };

            range.Add(12);

            Assert.Equal(2, range.Count);
            Assert.Equal(new[] { R(11, 13) }, range.GetRanges());
            Assert.True(range.Contains(12));

        }

        [Fact]
        public void Add_Mid()
        {
            var range = new MultiRange();

            range.Add(10);
            range.Add(12);
            range.Add(11);

            Assert.Equal(3, range.Count);
            Assert.Equal(new[] { R(10, 13) }, range.GetRanges());
        }

        [Fact]
        public void AddRange_Empty()
        {
            var range = new MultiRange();

            range.Add(new Range(10, 20));

            Assert.Equal(10, range.Count);
            Assert.Equal(new[] { R(10, 20) }, range.GetRanges());
        }

        [Fact]
        public void AddRange_Super()
        {
            var range = new MultiRange { 10, 11 };

            range.Add(new Range(5, 15));

            Assert.Equal(10, range.Count);
            Assert.Equal(new[] { R(5, 15) }, range.GetRanges());
        }

        [Fact]
        public void AddRange_OverlappedBegin()
        {
            var range = new MultiRange { 7, 8, 9 };

            range.Add(new Range(5, 8));

            Assert.Equal(5, range.Count);
            Assert.Equal(new[] { R(5, 10) }, range.GetRanges());
        }

        [Fact]
        public void AddRange_OverlappedEnd()
        {
            var range = new MultiRange { 10, 11, 12 };

            range.Add(new Range(11, 15));

            Assert.Equal(5, range.Count);
            Assert.Equal(new[] { R(10, 15) }, range.GetRanges());
        }

        [Fact]
        public void AddRange_Multiple()
        {
            var range = new MultiRange();

            range.Add(new Range(20, 30));
            range.Add(new Range(15, 25));
            range.Add(new Range(25, 35));
            range.Add(new Range(10, 40));
            range.Add(new Range(40, 45));
            range.Add(new Range(50, 55));
            range.Add(new Range(5, 6));

            Assert.Equal(41, range.Count);
            Assert.Equal(new[] { R(5, 6), R(10, 45), R(50, 55) }, range.GetRanges());
        }

        [Fact]
        public void Remove_NotContained()
        {
            var range = new MultiRange { 10, 11, 12 };

            range.Remove(42);

            Assert.Equal(3, range.Count);
            Assert.Equal(new[] { R(10, 13) }, range.GetRanges());
        }

        [Fact]
        public void Remove_Begin()
        {
            var range = new MultiRange { 10, 11, 12 };

            range.Remove(10);

            Assert.Equal(2, range.Count);
            Assert.Equal(new[] { R(11, 13) }, range.GetRanges());
        }

        [Fact]
        public void Remove_Mid()
        {
            var range = new MultiRange { 10, 11, 12, 13, 14 };

            range.Remove(12);

            Assert.Equal(4, range.Count);
            Assert.Equal(new[] { R(10, 12), R(13, 15) }, range.GetRanges());
        }

        [Fact]
        public void Remove_End()
        {
            var range = new MultiRange { 10, 11, 12 };

            range.Remove(12);

            Assert.Equal(2, range.Count);
            Assert.Equal(new[] { R(10, 12) }, range.GetRanges());
        }

        [Fact]
        public void RemoveRange_NotContained()
        {
            var range = new MultiRange { 10, 11, 12 };

            range.Remove(new Range(13, 14));

            Assert.Equal(3, range.Count);
            Assert.Equal(new[] { R(10, 13) }, range.GetRanges());
        }

        [Fact]
        public void RemoveRange_Begin()
        {
            var range = new MultiRange { 10, 11, 12, 13, 14 };

            range.Remove(new Range(10, 12));

            Assert.Equal(3, range.Count);
            Assert.Equal(new[] { R(12, 15) }, range.GetRanges());
        }

        [Fact]
        public void RemoveRange_Mid()
        {
            var range = new MultiRange { 10, 11, 12, 13, 14 };

            range.Remove(new Range(11, 14));

            Assert.Equal(2, range.Count);
            Assert.Equal(new[] { R(10), R(14) }, range.GetRanges());
        }

        [Fact]
        public void RemoveRange_End()
        {
            var range = new MultiRange { 10, 11, 12, 13, 14 };

            range.Remove(new Range(13, 15));

            Assert.Equal(3, range.Count);
            Assert.Equal(new[] { R(10, 13) }, range.GetRanges());
        }

        [Fact]
        public void RemoveRange_All()
        {
            var range = new MultiRange { 10, 11, 12, 13, 14 };

            range.Remove(new Range(10, 15));

            Assert.Equal(0, range.Count);
            Assert.Equal(new Range[0], range.GetRanges());
        }

        [Fact]
        public void RemoveRange_Super()
        {
            var range = new MultiRange { 10, 11, 12, 13, 14 };

            range.Remove(new Range(8, 16));

            Assert.Equal(0, range.Count);
            Assert.Equal(new Range[0], range.GetRanges());
        }

        public static IEnumerable<object[]> RemoveRange_Multiple_Cases
        {
            get
            {
                for (int b = 10; b <= 17; ++b) {
                    for (int e = 11; e <= 18; ++e) {
                        if (b <= e)
                            yield return new object[] { b, e };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(RemoveRange_Multiple_Cases))]
        public void RemoveRange_Multiple(int begin, int end)
        {
            var range = new MultiRange { 10, 11, 13, 14, 16, 17 };
            var expected = new MultiRange(range);
            for (int i = begin; i < end; ++i)
                expected.Remove(i);

            range.Remove(new Range(begin, end));

            Assert.Equal(expected.Count, range.Count);
            Assert.Equal(expected.GetRanges(), range.GetRanges());
        }

        [Fact]
        public void Clear()
        {
            var range = new MultiRange { 10, 11, 12, 23, 24, 25 };

            range.Clear();

            Assert.Equal(0, range.Count);
            Assert.Equal(new Range[0], range.GetRanges());
        }

        [Fact]
        public void UnionWith_Empty()
        {
            var range1 = new MultiRange();
            var range2 = new MultiRange { 10, 11, 12 };

            range1.UnionWith(range2);

            Assert.Equal(3, range1.Count);
            Assert.Equal(3, range2.Count);
            Assert.Equal(new[] { R(10, 13) }, range1.GetRanges());
            Assert.Equal(new[] { R(10, 13) }, range2.GetRanges());
        }

        [Fact]
        public void UnionWith_Empty2()
        {
            var range1 = new MultiRange { 10, 11, 12 };
            var range2 = new MultiRange();

            range1.UnionWith(range2);

            Assert.Equal(3, range1.Count);
            Assert.Equal(0, range2.Count);
            Assert.Equal(new[] { R(10, 13) }, range1.GetRanges());
            Assert.Empty(range2.GetRanges());
        }

        [Fact]
        public void UnionWith_NonOverlapping()
        {
            var range1 = new MultiRange { 23, 24, 25 };
            var range2 = new MultiRange { 10, 11, 12 };

            range1.UnionWith(range2);

            Assert.Equal(6, range1.Count);
            Assert.Equal(3, range2.Count);
            Assert.Equal(new[] { R(10, 13), R(23, 26) }, range1.GetRanges());
        }

        [Fact]
        public void UnionWith_Adjacent()
        {
            var range1 = new MultiRange { 10, 11, 12 };
            var range2 = new MultiRange { 13, 14, 15 };

            range1.UnionWith(range2);

            Assert.Equal(6, range1.Count);
            Assert.Equal(3, range2.Count);
            Assert.Equal(new[] { R(10, 16) }, range1.GetRanges());
        }

        [Fact]
        public void UnionWith_Overlapping()
        {
            var range1 = new MultiRange { 10, 11, 12 };
            var range2 = new MultiRange { 11, 12, 13 };

            range1.UnionWith(range2);

            Assert.Equal(4, range1.Count);
            Assert.Equal(3, range2.Count);
            Assert.Equal(new[] { R(10, 14) }, range1.GetRanges());
        }

        [Fact]
        public void Equality()
        {
            var range = new MultiRange { 1, 2, 10, 100 };
            var equalRange = new MultiRange { 1, 2, 10, 100 };
            var differentRange = new MultiRange { 1, 2, 11, 100 };

            Assert.True(range.Equals(range));
            Assert.True(range.Equals(equalRange));
            Assert.True(!range.Equals(differentRange));
            Assert.True(!range.Equals(null));

            Assert.True(range.Equals((object)range));
            Assert.True(range.Equals((object)equalRange));
            Assert.True(!range.Equals((object)differentRange));
            Assert.True(!range.Equals((object)null));
        }

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i >= 1; --i) {
                int j = rng.Next(i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        private static Range R(int value)
        {
            return new Range(value, value + 1);
        }

        private static Range R(int begin, int end)
        {
            return new Range(begin, end);
        }
    }
}
