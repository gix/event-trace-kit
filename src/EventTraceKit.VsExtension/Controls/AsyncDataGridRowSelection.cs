namespace EventTraceKit.VsExtension.Controls
{
    using System;

    public class AsyncDataGridRowSelection
    {
        private readonly IVirtualCollection collection;
        private readonly MultiRange selectedIndices = new MultiRange();
        private int anchorRowIndex;
        private bool selectionRemoves;

        public AsyncDataGridRowSelection(IVirtualCollection collection)
        {
            this.collection = collection;
        }

        public event EventHandler SelectionChanged;

        public int Count => selectedIndices.Count;

        public MultiRange GetSnapshot()
        {
            lock (selectedIndices)
                return new MultiRange(selectedIndices);
        }

        public void RestoreSnapshot(MultiRange selectedRows)
        {
            lock (selectedIndices) {
                UnselectAll();
                selectedIndices.UnionWith(selectedRows);
                RaiseSelectionChanged();
            }
        }

        public bool Contains(int rowIndex)
        {
            lock (selectedIndices)
                return IsSelected(rowIndex);
        }

        public void Clear()
        {
            lock (selectedIndices) {
                UnselectAll();
                RaiseSelectionChanged();
            }
        }

        public void SelectAll()
        {
            lock (selectedIndices) {
                UnselectAll();
                SetRangeInternal(0, collection.RowCount - 1, true);
                RaiseSelectionChanged();
            }
        }

        public void ToggleSingle(int rowIndex, bool extend = false)
        {
            lock (selectedIndices) {
                if (!extend) {
                    UnselectAll();
                    SelectIdx(rowIndex);
                    selectionRemoves = false;
                } else {
                    selectionRemoves = IsSelected(rowIndex);
                    if (selectionRemoves)
                        UnselectIdx(rowIndex);
                    else
                        SelectIdx(rowIndex);
                }

                anchorRowIndex = rowIndex;
                collection.FocusIndex = rowIndex;

                RaiseSelectionChanged();
            }
        }

        public void ToggleExtent(int rowIndex, bool extend = false)
        {
            lock (selectedIndices) {
                int min = Math.Min(anchorRowIndex, rowIndex);
                int max = Math.Max(anchorRowIndex, rowIndex);
                if (!extend)
                    UnselectAll();
                ToggleRangeInternal(min, max, selectionRemoves, !selectionRemoves);
                collection.FocusIndex = rowIndex;
            }
        }

        public void Select(int rowIndex, bool extend = false)
        {
            lock (selectedIndices) {
                if (rowIndex >= collection.RowCount)
                    return;

                if (!extend)
                    UnselectAll();

                SelectIdx(rowIndex);
                selectionRemoves = false;
                anchorRowIndex = rowIndex;
                collection.FocusIndex = rowIndex;
                RaiseSelectionChanged();
            }
        }

        public void SelectExtent(int rowIndex, bool extend = false)
        {
            lock (selectedIndices) {
                if (!extend)
                    UnselectAll();

                int min = Math.Min(anchorRowIndex, rowIndex);
                int max = Math.Max(anchorRowIndex, rowIndex);
                SetRangeInternal(min, max, true);
                collection.FocusIndex = rowIndex;
            }
        }

        public void SelectRange(int min, int max, bool extend = false)
        {
            lock (selectedIndices) {
                if (!extend)
                    UnselectAll();

                SetRangeInternal(min, max, true);
                RaiseSelectionChanged();
            }
        }

        public void SetRange(int min, int max, bool selected)
        {
            lock (selectedIndices)
                SetRangeInternal(min, max, selected);
        }

        private bool IsSelected(int rowIndex)
        {
            return selectedIndices.Contains(rowIndex);
        }

        private void SelectIdx(int rowIndex)
        {
            selectedIndices.Add(rowIndex);
        }

        private void UnselectIdx(int rowIndex)
        {
            selectedIndices.Remove(rowIndex);
        }

        private void UnselectAll()
        {
            selectedIndices.Clear();
        }

        private void SetRangeInternal(int min, int max, bool selected)
        {
            min = Math.Max(min, 0);
            max = Math.Min(max, collection.RowCount - 1);

            if (selected)
                selectedIndices.Add(new Range(min, max + 1));
            else
                selectedIndices.Remove(new Range(min, max + 1));
        }

        private void ToggleRangeInternal(int min, int max, bool shouldRemove, bool shouldAdd)
        {
            min = Math.Max(min, 0);
            max = Math.Min(max, collection.RowCount - 1);

            bool changed = false;
            for (int index = min; index <= max; ++index) {
                bool selected = IsSelected(index);

                if (shouldRemove && selected) {
                    UnselectIdx(index);
                    changed = true;
                }

                if (shouldAdd && !selected) {
                    SelectIdx(index);
                    changed = true;
                }
            }

            if (changed)
                RaiseSelectionChanged();
        }

        private void RaiseSelectionChanged()
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
