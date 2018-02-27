namespace NOpt.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NOpt.Extensions;
    using Xunit;
    using Xunit.Extensions;

    public class ListExtensionsTest
    {
        [Theory]
        [InlineData(10, 0)]
        [InlineData(11, 0)]
        [InlineData(12, 1)]
        [InlineData(13, 1)]
        [InlineData(14, 4)]
        [InlineData(15, 4)]
        [InlineData(16, 5)]
        public void WeakPredecessor_FullRange(int value, int expectedIndex)
        {
            IReadOnlyList<Tuple<int>> list = new List<Tuple<int>> {
                Tuple.Create(11),
                Tuple.Create(13),
                Tuple.Create(13),
                Tuple.Create(13),
                Tuple.Create(15)
            };

            int actualIndex = IReadOnlyListExtensions.WeakPredecessor(
                list, value, (l, r) => l.Item1.CompareTo(r));

            Assert.Equal(expectedIndex, actualIndex);
        }

        [Theory]
        // Test range [0, 5)
        [InlineData(0, 5, 10, 0)]
        [InlineData(0, 5, 11, 0)]
        [InlineData(0, 5, 12, 1)]
        [InlineData(0, 5, 13, 1)]
        [InlineData(0, 5, 14, 4)]
        [InlineData(0, 5, 15, 4)]
        [InlineData(0, 5, 16, 5)]
        // Test range [1, 5)
        [InlineData(1, 4, 10, 1)]
        [InlineData(1, 4, 11, 1)]
        [InlineData(1, 4, 12, 1)]
        [InlineData(1, 4, 13, 1)]
        [InlineData(1, 4, 14, 4)]
        [InlineData(1, 4, 15, 4)]
        [InlineData(1, 4, 16, 5)]
        // Test range [2, 3)
        [InlineData(2, 1, 10, 2)]
        [InlineData(2, 1, 11, 2)]
        [InlineData(2, 1, 12, 2)]
        [InlineData(2, 1, 13, 2)]
        [InlineData(2, 1, 14, 3)]
        [InlineData(2, 1, 15, 3)]
        [InlineData(2, 1, 16, 3)]
        public void WeakPredecessor_PartialRange(
            int index, int count, int value, int expectedIndex)
        {
            IReadOnlyList<Tuple<int>> list = new List<Tuple<int>> {
                Tuple.Create(11),
                Tuple.Create(13),
                Tuple.Create(13),
                Tuple.Create(13),
                Tuple.Create(15)
            };

            int actualIndex = IReadOnlyListExtensions.WeakPredecessor(
                list, index, count, value, (l, r) => l.Item1.CompareTo(r));

            Assert.Equal(expectedIndex, actualIndex);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 0)]
        public void WeakPredecessor_EmptyList(int value, int expected)
        {
            IReadOnlyList<Tuple<int>> list = new List<Tuple<int>>();

            int actual = IReadOnlyListExtensions.WeakPredecessor(
                list, value, (l, r) => l.Item1.CompareTo(r));

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(3, 1, typeof(ArgumentException))] // Index after end.
        [InlineData(0, 4, typeof(ArgumentException))] // Count too large.
        [InlineData(-1, 1, typeof(ArgumentOutOfRangeException))] // Negative index.
        [InlineData(1, -1, typeof(ArgumentOutOfRangeException))] // Negative count.
        public void WeakPredecessor_Preconditions(int index, int count, Type expectedException)
        {
            IReadOnlyList<string> list = new List<string> { "10", "11", "12" };

            Assert.Throws(
                expectedException,
                () => IReadOnlyListExtensions.WeakPredecessor(
                    list, index, count, "10", (l, r) => l.CompareTo(r)));
        }


        [Theory]
        [InlineData(10, 0)]
        [InlineData(11, 1)]
        [InlineData(12, 1)]
        [InlineData(13, 4)]
        [InlineData(14, 4)]
        [InlineData(15, 5)]
        [InlineData(16, 5)]
        public void WeakSuccessor_FullRange(int value, int expectedIndex)
        {
            IReadOnlyList<Tuple<int>> list = new List<Tuple<int>> {
                Tuple.Create(11),
                Tuple.Create(13),
                Tuple.Create(13),
                Tuple.Create(13),
                Tuple.Create(15)
            };

            int actualIndex = IReadOnlyListExtensions.WeakSuccessor(
                list, value, (l, r) => l.Item1.CompareTo(r));

            Assert.Equal(expectedIndex, actualIndex);
        }

        [Theory]
        // Test range [0, 5)
        [InlineData(0, 5, 10, 0)]
        [InlineData(0, 5, 11, 1)]
        [InlineData(0, 5, 12, 1)]
        [InlineData(0, 5, 13, 4)]
        [InlineData(0, 5, 14, 4)]
        [InlineData(0, 5, 15, 5)]
        [InlineData(0, 5, 16, 5)]
        // Test range [1, 5)
        [InlineData(1, 4, 10, 1)]
        [InlineData(1, 4, 11, 1)]
        [InlineData(1, 4, 12, 1)]
        [InlineData(1, 4, 13, 4)]
        [InlineData(1, 4, 14, 4)]
        [InlineData(1, 4, 15, 5)]
        [InlineData(1, 4, 16, 5)]
        // Test range [2, 3)
        [InlineData(2, 1, 10, 2)]
        [InlineData(2, 1, 11, 2)]
        [InlineData(2, 1, 12, 2)]
        [InlineData(2, 1, 13, 3)]
        [InlineData(2, 1, 14, 3)]
        [InlineData(2, 1, 15, 3)]
        [InlineData(2, 1, 16, 3)]
        public void WeakSuccessor_PartialRange(
            int index, int count, int value, int expectedIndex)
        {
            IReadOnlyList<Tuple<int>> list = new List<Tuple<int>> {
                Tuple.Create(11),
                Tuple.Create(13),
                Tuple.Create(13),
                Tuple.Create(13),
                Tuple.Create(15)
            };

            int actualIndex = IReadOnlyListExtensions.WeakSuccessor(
                list, index, count, value, (l, r) => l.Item1.CompareTo(r));

            Assert.Equal(expectedIndex, actualIndex);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 0)]
        public void WeakSuccessor_EmptyList(int value, int expectedIndex)
        {
            IReadOnlyList<Tuple<int>> list = new List<Tuple<int>>();

            int actualIndex = IReadOnlyListExtensions.WeakSuccessor(
                list, value, (l, r) => l.Item1.CompareTo(r));

            Assert.Equal(expectedIndex, actualIndex);
        }

        [Theory]
        [InlineData(3, 1, typeof(ArgumentException))] // Index after end.
        [InlineData(0, 4, typeof(ArgumentException))] // Count too large.
        [InlineData(-1, 1, typeof(ArgumentOutOfRangeException))] // Negative index.
        [InlineData(1, -1, typeof(ArgumentOutOfRangeException))] // Negative count.
        public void WeakSuccessor_Preconditions(int index, int count, Type expectedException)
        {
            IReadOnlyList<string> list = new List<string> { "10", "11", "12" };

            Assert.Throws(
                expectedException,
                () => IReadOnlyListExtensions.WeakSuccessor(
                    list, index, count, "10", (l, r) => l.CompareTo(r)));
        }

        [Theory]
        [InlineData(10, 0, 0)]
        [InlineData(11, 0, 1)]
        [InlineData(12, 0, 0)]
        [InlineData(13, 1, 3)]
        [InlineData(14, 0, 0)]
        [InlineData(15, 4, 1)]
        [InlineData(16, 0, 0)]
        public void EqualRange_FullRange(int value, int skip, int take)
        {
            IReadOnlyList<Tuple<int, int>> list = new List<Tuple<int, int>> {
                Tuple.Create(11, 0),
                Tuple.Create(13, 1),
                Tuple.Create(13, 2),
                Tuple.Create(13, 3),
                Tuple.Create(15, 4)
            };

            var range = IReadOnlyListExtensions.EqualRange(
                list, value, (l, r) => l.Item1.CompareTo(r));

            Assert.Equal(list.Skip(skip).Take(take), range);
        }

        [Theory]
        // Test range [0, 5)
        [InlineData(0, 5, 10, 0, 0)]
        [InlineData(0, 5, 11, 0, 1)]
        [InlineData(0, 5, 12, 0, 0)]
        [InlineData(0, 5, 13, 1, 3)]
        [InlineData(0, 5, 14, 0, 0)]
        [InlineData(0, 5, 15, 4, 1)]
        [InlineData(0, 5, 16, 0, 0)]
        // Test range [1, 5)
        [InlineData(1, 4, 10, 0, 0)]
        [InlineData(1, 4, 11, 0, 0)]
        [InlineData(1, 4, 12, 0, 0)]
        [InlineData(1, 4, 13, 1, 3)]
        [InlineData(1, 4, 14, 0, 0)]
        [InlineData(1, 4, 15, 4, 1)]
        [InlineData(1, 4, 16, 0, 0)]
        // Test range [2, 3)
        [InlineData(2, 1, 10, 0, 0)]
        [InlineData(2, 1, 11, 0, 0)]
        [InlineData(2, 1, 12, 0, 0)]
        [InlineData(2, 1, 13, 2, 1)]
        [InlineData(2, 1, 14, 0, 0)]
        [InlineData(2, 1, 15, 0, 0)]
        [InlineData(2, 1, 16, 0, 0)]
        public void EqualRange_PartialRange(
            int index, int count, int value, int skip, int take)
        {
            IReadOnlyList<Tuple<int, int>> list = new List<Tuple<int, int>> {
                Tuple.Create(11, 0),
                Tuple.Create(13, 1),
                Tuple.Create(13, 2),
                Tuple.Create(13, 3),
                Tuple.Create(15, 4)
            };

            var range = IReadOnlyListExtensions.EqualRange(
                list, index, count, value, (l, r) => l.Item1.CompareTo(r));

            Assert.Equal(list.Skip(skip).Take(take), range);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void EqualRange_EmptyList(int value)
        {
            IReadOnlyList<Tuple<int>> list = new List<Tuple<int>>();

            var range = IReadOnlyListExtensions.EqualRange(
                list, value, (l, r) => l.Item1.CompareTo(r));

            Assert.Equal(Enumerable.Empty<Tuple<int>>(), range);
        }

        [Theory]
        [InlineData(3, 1, typeof(ArgumentException))] // Index after end.
        [InlineData(0, 4, typeof(ArgumentException))] // Count too large.
        [InlineData(-1, 1, typeof(ArgumentOutOfRangeException))] // Negative index.
        [InlineData(1, -1, typeof(ArgumentOutOfRangeException))] // Negative count.
        public void EqualRange_Preconditions(int index, int count, Type expectedException)
        {
            IReadOnlyList<string> list = new List<string> { "10", "11", "12" };

            Assert.Throws(
                expectedException,
                () => IReadOnlyListExtensions.EqualRange(
                    list, index, count, "10", (l, r) => l.CompareTo(r)));
        }
    }
}
