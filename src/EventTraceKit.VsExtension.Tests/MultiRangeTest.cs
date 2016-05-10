namespace EventTraceKit.VsExtension.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using EventTraceKit.VsExtension.Collections;
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
            Assert.Equal(new Tuple<int, int>[0], range.GetRanges());
            Assert.False(range.Contains(42));
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
        public void AddOutOfOrder()
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
        public void AddBegin()
        {
            var range = new MultiRange { 11 };

            range.Add(10);

            Assert.Equal(2, range.Count);
            Assert.Equal(new[] { R(10, 12) }, range.GetRanges());
            Assert.True(range.Contains(10));
        }

        [Fact]
        public void AddEnd()
        {
            var range = new MultiRange { 11 };

            range.Add(12);

            Assert.Equal(2, range.Count);
            Assert.Equal(new[] { R(11, 13) }, range.GetRanges());
            Assert.True(range.Contains(12));

        }

        [Fact]
        public void AddMid()
        {
            var range = new MultiRange();

            range.Add(10);
            range.Add(12);
            range.Add(11);

            Assert.Equal(3, range.Count);
            Assert.Equal(new[] { R(10, 13) }, range.GetRanges());
        }

        [Fact]
        public void RemoveNotContained()
        {
            var range = new MultiRange { 10, 11, 12 };

            range.Remove(42);

            Assert.Equal(3, range.Count);
            Assert.Equal(new[] { R(10, 13) }, range.GetRanges());
        }

        [Fact]
        public void RemoveBegin()
        {
            var range = new MultiRange { 10, 11, 12 };

            range.Remove(10);

            Assert.Equal(2, range.Count);
            Assert.Equal(new[] { R(11, 13) }, range.GetRanges());
        }

        [Fact]
        public void RemoveMid()
        {
            var range = new MultiRange { 10, 11, 12, 13, 14 };

            range.Remove(12);

            Assert.Equal(4, range.Count);
            Assert.Equal(new[] { R(10, 12), R(13, 15) }, range.GetRanges());
        }

        [Fact]
        public void RemoveEnd()
        {
            var range = new MultiRange { 10, 11, 12 };

            range.Remove(12);

            Assert.Equal(2, range.Count);
            Assert.Equal(new[] { R(10, 12) }, range.GetRanges());
        }

        [Fact]
        public void Clear()
        {
            var range = new MultiRange { 10, 11, 12, 23, 24, 25 };

            range.Clear();

            Assert.Equal(0, range.Count);
            Assert.Equal(new Tuple<int, int>[0], range.GetRanges());
        }

        [Fact]
        public void Perf()
        {
            var values = new List<int>();
            for (int i = 0; i < 100; ++i) {
                for (int j = 0; j < 5; ++j)
                    values.Add(6 * i + j);
            }

            var list = new List<bool>();
            var range = new MultiRange();
            foreach (var value in values) {
                range.Add(value);
                while (list.Count < value + 1)
                    list.Add(false);
                list[value] = true;
            }

            Shuffle(values, new Random());

            var sw = new Stopwatch();
            sw.Restart();

            bool rangeResult = true;
            for (int i = 0; i < 1000; i++) {
                foreach (var value in values)
                    rangeResult &= range.Contains(value);
            }

            sw.Stop();
            var rangeDuration = sw.Elapsed;

            sw.Restart();

            bool listResult = true;
            for (int i = 0; i < 1000; i++) {
                foreach (var value in values)
                    listResult &= list[value];
            }

            sw.Stop();
            var listDuration = sw.Elapsed;

            output.WriteLine("Range: {0} {1}", rangeDuration, rangeResult);
            output.WriteLine("List:  {0} {1}", listDuration, listResult);
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

        private static Tuple<int, int> R(int value)
        {
            return Tuple.Create(value, value + 1);
        }

        private static Tuple<int, int> R(int begin, int end)
        {
            return Tuple.Create(begin, end);
        }
    }
}
