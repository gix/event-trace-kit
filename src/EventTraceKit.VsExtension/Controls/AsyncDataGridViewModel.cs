namespace EventTraceKit.VsExtension.Controls
{
    using System.Windows;

    public class AsyncDataGridViewModel : DependencyObject
    {
        public AsyncDataGridViewModel(IDataView dataView)
        {
            CellsPresenterViewModel = new AsyncDataGridCellsPresenterViewModel(dataView, this);
            ColumnsViewModel = new AsyncDataGridColumnsViewModel(dataView, this);
            RowCount = 40;

            dataView.Updated += (s, e) => RaiseUpdate(e.Item);
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
