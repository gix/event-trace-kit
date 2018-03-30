namespace EventTraceKit.VsExtension.Tests.Controls
{
    using System;
    using EventTraceKit.VsExtension.Controls;
    using Xunit;

    public class AsyncDataGridRowSelectionTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(9)]
        public void Select_Single(int index)
        {
            var collection = new VirtualCollection(10);
            var selection = new AsyncDataGridRowSelection(collection);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;

            selection.Select(index);

            Assert.Equal(R(index), selection.GetSnapshot());
            Assert.Equal(index, collection.FocusIndex);
            Assert.Equal(1, selectionChanged);
        }

        [Fact]
        public void Select_IgnoresOutOfRange()
        {
            var collection = new VirtualCollection(10);
            var selection = new AsyncDataGridRowSelection(collection);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;
            collection.FocusIndex = 3;

            selection.Select(10);

            Assert.Equal(new MultiRange(), selection.GetSnapshot());
            Assert.Equal(3, collection.FocusIndex);
            Assert.Equal(0, selectionChanged);
        }

        [Fact]
        public void Select_Multiple_NoExtend()
        {
            var collection = new VirtualCollection(10);
            var selection = new AsyncDataGridRowSelection(collection);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;

            selection.Select(1);
            selection.Select(2);

            Assert.Equal(R(2), selection.GetSnapshot());
            Assert.Equal(2, collection.FocusIndex);
            Assert.Equal(2, selectionChanged);
        }

        [Fact]
        public void Select_Multiple_Extend()
        {
            var collection = new VirtualCollection(10);
            var selection = new AsyncDataGridRowSelection(collection);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;

            selection.Select(1, true);
            selection.Select(2, true);
            selection.Select(4, true);
            selection.Select(20, true);

            Assert.Equal(R(1, 3, 4, 5), selection.GetSnapshot());
            Assert.Equal(4, collection.FocusIndex);
            Assert.Equal(3, selectionChanged);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(int.MaxValue)]
        public void SelectAll(int size)
        {
            var collection = new VirtualCollection(size);
            var selection = new AsyncDataGridRowSelection(collection);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;
            collection.FocusIndex = 3;

            selection.SelectAll();

            Assert.Equal(R(0, size), selection.GetSnapshot());
            Assert.Equal(3, collection.FocusIndex);
            Assert.Equal(1, selectionChanged);
        }

        [Fact]
        public void SelectAll_NonEmpty()
        {
            var collection = new VirtualCollection(10);
            var selection = new AsyncDataGridRowSelection(collection);
            selection.Select(5);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;

            selection.SelectAll();

            Assert.Equal(R(0, 10), selection.GetSnapshot());
            Assert.Equal(5, collection.FocusIndex);
            Assert.Equal(1, selectionChanged);
        }

        [Fact]
        public void SelectRange()
        {
            var collection = new VirtualCollection(10);
            var selection = new AsyncDataGridRowSelection(collection);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;

            selection.SelectRange(2, 5);

            Assert.Equal(R(2, 6), selection.GetSnapshot());
            Assert.Equal(0, collection.FocusIndex);
            Assert.Equal(1, selectionChanged);
        }

        [Fact]
        public void SelectRange_NonEmpty()
        {
            var collection = new VirtualCollection(10);
            var selection = new AsyncDataGridRowSelection(collection);
            selection.Select(5);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;

            selection.SelectRange(2, 5);

            Assert.Equal(R(2, 6), selection.GetSnapshot());
            Assert.Equal(5, collection.FocusIndex);
            Assert.Equal(1, selectionChanged);
        }

        [Fact]
        public void SelectRange_Extend()
        {
            var collection = new VirtualCollection(10);
            var selection = new AsyncDataGridRowSelection(collection);
            selection.Select(7);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;

            selection.SelectRange(2, 5, true);

            Assert.Equal(R(2, 6, 7, 8), selection.GetSnapshot());
            Assert.Equal(7, collection.FocusIndex);
            Assert.Equal(1, selectionChanged);
        }

        [Fact]
        public void ToggleSingle()
        {
            var collection = new VirtualCollection(10);
            var selection = new AsyncDataGridRowSelection(collection);
            selection.Select(5);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;

            selection.ToggleSingle(2);
            selection.ToggleSingle(3);

            Assert.Equal(R(3), selection.GetSnapshot());
            Assert.Equal(3, collection.FocusIndex);
            Assert.Equal(2, selectionChanged);
        }

        [Fact]
        public void ToggleSingle_Extend()
        {
            var collection = new VirtualCollection(10);
            var selection = new AsyncDataGridRowSelection(collection);
            selection.Select(5);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;

            selection.ToggleSingle(2, true);
            selection.ToggleSingle(3, true);

            Assert.Equal(R(2, 4, 5, 6), selection.GetSnapshot());
            Assert.Equal(3, collection.FocusIndex);
            Assert.Equal(2, selectionChanged);
        }

        [Fact]
        public void ToggleExtent_Backward()
        {
            var collection = new VirtualCollection(10);
            var selection = new AsyncDataGridRowSelection(collection);
            selection.ToggleSingle(5);
            selection.ToggleSingle(3, true);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;

            selection.ToggleExtent(1);

            Assert.Equal(R(1, 4), selection.GetSnapshot());
            Assert.Equal(1, collection.FocusIndex);
            Assert.Equal(1, selectionChanged);
        }

        [Fact]
        public void ToggleExtent_Forward()
        {
            var collection = new VirtualCollection(10);
            var selection = new AsyncDataGridRowSelection(collection);
            selection.Select(1);
            selection.ToggleSingle(3, true);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;

            selection.ToggleExtent(5);

            Assert.Equal(R(3, 6), selection.GetSnapshot());
            Assert.Equal(5, collection.FocusIndex);
            Assert.Equal(1, selectionChanged);
        }

        [Fact]
        public void ToggleExtent_ExtendBackward()
        {
            var collection = new VirtualCollection(10);
            var selection = new AsyncDataGridRowSelection(collection);
            selection.Select(5);
            selection.ToggleSingle(3, true);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;

            selection.ToggleExtent(1, true);

            Assert.Equal(R(1, 4, 5, 6), selection.GetSnapshot());
            Assert.Equal(1, collection.FocusIndex);
            Assert.Equal(1, selectionChanged);
        }

        [Fact]
        public void ToggleExtent_ExtendForward()
        {
            var collection = new VirtualCollection(10);
            var selection = new AsyncDataGridRowSelection(collection);
            selection.Select(1);
            selection.ToggleSingle(3, true);
            int selectionChanged = 0;
            selection.SelectionChanged += (s, e) => ++selectionChanged;

            selection.ToggleExtent(5, true);

            Assert.Equal(R(1, 2, 3, 6), selection.GetSnapshot());
            Assert.Equal(5, collection.FocusIndex);
            Assert.Equal(1, selectionChanged);
        }

        private MultiRange R(int index)
        {
            var range = new MultiRange();
            range.Add(new Range(index, index + 1));
            return range;
        }

        private MultiRange R(int begin, int end)
        {
            var range = new MultiRange();
            range.Add(new Range(begin, end));
            return range;
        }

        private MultiRange R(int begin, int end, params int[] indices)
        {
            if (indices.Length % 2 != 0)
                throw new ArgumentException();

            var range = new MultiRange();
            range.Add(new Range(begin, end));
            for (int i = 0; i < indices.Length / 2; i += 2)
                range.Add(new Range(indices[i], indices[i + 1]));

            return range;
        }

        private class VirtualCollection : IVirtualCollection
        {
            public VirtualCollection(int rowCount = 0)
            {
                RowCount = rowCount;
            }

            public int FocusIndex { get; set; }
            public int RowCount { get; set; }
        }
    }
}
