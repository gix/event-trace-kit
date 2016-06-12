namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    public class AsyncDataGridRowSelection
    {
        private readonly AsyncDataGridCellsPresenterViewModel cellsViewModel;
        private readonly List<int> selectionChanges = new List<int>();
        private readonly MultiRange selectedIndices = new MultiRange();
        private int anchorRowIndex;
        private bool selectionRemoves;

        public AsyncDataGridRowSelection(
            AsyncDataGridCellsPresenterViewModel cellsViewModel)
        {
            this.cellsViewModel = cellsViewModel;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Count => selectedIndices.Count;

        public MultiRange GetSnapshot()
        {
            lock (selectedIndices)
                return new MultiRange(selectedIndices);
        }

        public void RestoreSnapshot(MultiRange selectedRows)
        {
            lock (selectedIndices) {
                Reset();
                selectedIndices.UnionWith(selectedRows);

                // FIXME
                //selectionChanges.Clear();
                //selectionChanges.AddRange(selectedRows);
                //OnCollectionChanged(NotifyCollectionChangedAction.Add, selectionChanges);
                //selectionChanges.Clear();
            }
        }

        public bool Contains(int rowIndex)
        {
            lock (selectedIndices)
                return IsSelected(rowIndex);
        }

        public void Clear()
        {
            lock (selectedIndices)
                Reset();
        }

        public void SelectAll()
        {
            lock (selectedIndices) {
                Reset();
                ToggleRange(0, cellsViewModel.RowCount - 1, false, true);
                // FIXME
                //OnCollectionChanged(NotifyCollectionChangedAction.Add, selectionChanges);
            }
        }

        public void ToggleSingle(int rowIndex, bool extend)
        {
            lock (selectedIndices) {
                NotifyCollectionChangedAction action;
                if (!extend) {
                    Reset();
                    Select(rowIndex);
                    action = NotifyCollectionChangedAction.Add;
                    selectionRemoves = false;
                } else {
                    selectionRemoves = IsSelected(rowIndex);
                    if (selectionRemoves) {
                        Unselect(rowIndex);
                        action = NotifyCollectionChangedAction.Remove;
                    } else {
                        Select(rowIndex);
                        action = NotifyCollectionChangedAction.Add;
                    }
                }

                anchorRowIndex = rowIndex;
                cellsViewModel.FocusIndex = rowIndex;

                if (action != NotifyCollectionChangedAction.Reset)
                    OnCollectionChanged(action, rowIndex);
            }
        }

        public void ToggleExtent(int rowIndex, bool extend)
        {
            lock (selectedIndices) {
                int min = Math.Min(anchorRowIndex, rowIndex);
                int max = Math.Max(anchorRowIndex, rowIndex);
                if (!extend)
                    Reset();
                ToggleRange(min, max, selectionRemoves, !selectionRemoves);
                cellsViewModel.FocusIndex = rowIndex;
            }
        }

        public void Select(int rowIndex, bool extend)
        {
            lock (selectedIndices) {
                if (!extend)
                    Reset();

                Select(rowIndex);
                selectionRemoves = false;
                anchorRowIndex = rowIndex;
                cellsViewModel.FocusIndex = rowIndex;
                OnCollectionChanged(NotifyCollectionChangedAction.Add, rowIndex);
            }
        }

        public void SelectExtent(int rowIndex, bool extend)
        {
            lock (selectedIndices) {
                if (!extend)
                    Reset();

                int min = Math.Min(anchorRowIndex, rowIndex);
                int max = Math.Max(anchorRowIndex, rowIndex);
                ToggleRange(min, max, false, true);
                cellsViewModel.FocusIndex = rowIndex;
            }
        }

        public void SelectRange(int min, int max)
        {
            lock (selectedIndices) {
                Reset();
                ToggleRange(min, max, false, true);
            }
        }

        public void SelectRange(int min, int max, bool extend)
        {
            lock (selectedIndices) {
                if (!extend)
                    Reset();
                ToggleRange(min, max, false, true);
            }
        }

        public void SetRange(int min, int max, bool selected)
        {
            lock (selectedIndices)
                ToggleRange(min, max, !selected, selected);
        }

        private bool IsSelected(int rowIndex)
        {
            return selectedIndices.Contains(rowIndex);
        }

        private void Select(int rowIndex)
        {
            selectedIndices.Add(rowIndex);
        }

        private void Unselect(int rowIndex)
        {
            selectedIndices.Remove(rowIndex);
        }

        private void UnselectAll()
        {
            selectedIndices.Clear();
        }

        private void ToggleRange(int min, int max, bool shouldRemove, bool shouldAdd)
        {
            min = Math.Max(0, min);
            max = Math.Min(max, cellsViewModel.RowCount - 1);

            selectionChanges.Clear();
            for (int index = min; index <= max; ++index) {
                bool selected = IsSelected(index);

                if (shouldRemove && selected) {
                    Unselect(index);
                    selectionChanges.Add(index);
                }

                if (shouldAdd && !selected) {
                    Select(index);
                    selectionChanges.Add(index);
                }
            }

            if (selectionChanges.Count > 0)
                OnCollectionChanged(
                    NotifyCollectionChangedAction.Remove, selectionChanges);

            selectionChanges.Clear();
        }

        private void Reset()
        {
            UnselectAll();
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
