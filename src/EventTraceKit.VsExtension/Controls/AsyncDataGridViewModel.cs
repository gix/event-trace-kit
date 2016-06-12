namespace EventTraceKit.VsExtension.Controls
{
    using System.Windows;

    public class AsyncDataGridViewModel : DependencyObject
    {
        public AsyncDataGridViewModel(
            HdvViewModel hdv, AsyncDataGridColumnsViewModel columnsViewModel)
        {
            CellsPresenterViewModel = new AsyncDataGridCellsPresenterViewModel(hdv);
            ColumnsViewModel = columnsViewModel;
        }

        public AsyncDataGridCellsPresenterViewModel CellsPresenterViewModel { get; }

        public AsyncDataGridColumnsViewModel ColumnsViewModel { get; }

        public AsyncDataGridRowSelection RowSelection =>
            CellsPresenterViewModel.RowSelection;

        public int FocusIndex => CellsPresenterViewModel.FocusIndex;

        public event ItemEventHandler<bool> Updated;

        internal void RaiseUpdated(bool refreshViewModelFromModel = true)
        {
            Updated?.Invoke(this, new ItemEventArgs<bool>(refreshViewModelFromModel));
        }
    }
}
