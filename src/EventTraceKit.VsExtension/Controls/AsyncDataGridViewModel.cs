namespace EventTraceKit.VsExtension.Controls
{
    using System.Windows;

    public class AsyncDataGridViewModel : DependencyObject
    {
        public AsyncDataGridViewModel(
            HdvViewModel hdv, AsyncDataGridColumnsViewModel columnsViewModel)
        {
            CellsPresenterViewModel = new AsyncDataGridCellsPresenterViewModel(hdv, this);
            ColumnsViewModel = columnsViewModel;
            RowCount = 40;

            hdv.Updated += (s, e) => RaiseUpdate(e.Item);
        }

        public AsyncDataGridCellsPresenterViewModel CellsPresenterViewModel { get; }

        public AsyncDataGridColumnsViewModel ColumnsViewModel { get; }

        public int RowCount { get; private set; }

        public event ItemEventHandler<bool> Updated;

        public void RaiseUpdate(bool refreshViewModelFromModel = true)
        {
            Updated?.Invoke(this, new ItemEventArgs<bool>(refreshViewModelFromModel));
        }
    }
}
