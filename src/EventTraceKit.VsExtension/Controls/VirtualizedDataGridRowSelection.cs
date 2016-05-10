namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using EventTraceKit.VsExtension.Collections;

    public class VirtualizedDataGridRowSelection
    {
        private readonly VirtualizedDataGridCellsPresenterViewModel cellsViewModel;
        private readonly List<int> selectionChanges = new List<int>();
        private readonly MultiRange selectedIndices = new MultiRange();
        private int selectionAnchorRowIndex;
        private bool selectionRemoves;

        public VirtualizedDataGridRowSelection(
            VirtualizedDataGridCellsPresenterViewModel cellsViewModel)
        {
            this.cellsViewModel = cellsViewModel;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Count => selectedIndices.Count;

        public bool Contains(int row)
        {
            lock (selectedIndices)
                return IsSelectedAtRow(row);
        }

        public void ClearAll()
        {
            lock (selectedIndices)
                selectedIndices.Clear();
        }

        public void ToggleSingle(int rowIndex, bool extend)
        {
            lock (selectedIndices) {
                NotifyCollectionChangedAction reset;
                if (!extend) {
                    Reset();
                    SetSelection(rowIndex);
                    reset = NotifyCollectionChangedAction.Add;
                    selectionRemoves = false;
                } else {
                    selectionRemoves = IsSelectedAtRow(rowIndex);
                    if (selectionRemoves) {
                        ClearSelection(rowIndex);
                        reset = NotifyCollectionChangedAction.Remove;
                    } else {
                        SetSelection(rowIndex);
                        reset = NotifyCollectionChangedAction.Add;
                    }
                }

                selectionAnchorRowIndex = rowIndex;
                cellsViewModel.FocusIndex = rowIndex;

                if (reset != NotifyCollectionChangedAction.Reset)
                    OnCollectionChanged(reset, rowIndex);
            }
        }

        public void ToggleExtent(int rowIndex, bool extend)
        {
            lock (selectedIndices) {
                int min = Math.Min(selectionAnchorRowIndex, rowIndex);
                int max = Math.Max(selectionAnchorRowIndex, rowIndex);
                ToggleRange(min, max, !extend, selectionRemoves, !selectionRemoves);
                cellsViewModel.FocusIndex = rowIndex;
            }
        }

        public void SelectRange(int min, int max)
        {
            lock (selectedIndices)
                ToggleRange(min, max, true, true, true);
        }

        private bool IsSelectedAtRow(int row)
        {
            return selectedIndices.Contains(row);
        }

        private void ClearSelection(int row)
        {
            SetSelection(row, false);
        }

        private void SetSelection(int row)
        {
            SetSelection(row, true);
        }

        private void SetSelection(int row, bool selected)
        {
            if (selected)
                selectedIndices.Add(row);
            else
                selectedIndices.Remove(row);
        }

        private void ToggleRange(
            int min, int max, bool clearFirst = false, bool shouldRemove = true,
            bool shouldAdd = true)
        {
            min = Math.Max(0, min);
            max = Math.Min(max, cellsViewModel.RowCount - 1);

            if (clearFirst)
                Reset();

            selectionChanges.Clear();
            for (int i = min; i <= max; ++i) {
                bool flag = IsSelectedAtRow(i);

                if (shouldRemove && flag) {
                    ClearSelection(i);
                    selectionChanges.Add(i);
                }

                if (shouldAdd && !flag) {
                    SetSelection(i);
                    selectionChanges.Add(i);
                }
            }

            if (selectionChanges.Count > 0)
                OnCollectionChanged(
                    NotifyCollectionChangedAction.Remove, selectionChanges);

            selectionChanges.Clear();
        }

        private void Reset()
        {
            ClearAll();
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action)
        {
            CollectionChanged?.Invoke(
                this, new NotifyCollectionChangedEventArgs(action));
        }

        private void OnCollectionChanged(
            NotifyCollectionChangedAction action, int item)
        {
            CollectionChanged?.Invoke(
                this, new NotifyCollectionChangedEventArgs(action, item));
        }

        private void OnCollectionChanged(
            NotifyCollectionChangedAction action, IList<int> changes)
        {
            CollectionChanged?.Invoke(
                this, new NotifyCollectionChangedEventArgs(action, changes));
        }
    }
}
