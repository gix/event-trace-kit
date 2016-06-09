namespace EventTraceKit.VsExtension.Controls
{
    using System.Windows;

    public class VirtualizedDataGridViewModel : DependencyObject
    {
        public VirtualizedDataGridViewModel(IDataView dataView)
        {
            CellsPresenterViewModel = new VirtualizedDataGridCellsPresenterViewModel(dataView, this);
            ColumnsViewModel = new VirtualizedDataGridColumnsViewModel(dataView, this);
            //ColumnsViewModel.ApplyPresetAssumeGridModelInSync(null);
            RowCount = 40;

            dataView.Updated += (s, e) => RaiseUpdate(e.Item);
        }

        public VirtualizedDataGridCellsPresenterViewModel CellsPresenterViewModel { get; }

        public VirtualizedDataGridColumnsViewModel ColumnsViewModel { get; }

        public int RowCount { get; private set; }

        public event ItemEventHandler<bool> Updated;

        public void RaiseUpdate(bool refreshViewModelFromModel = true)
        {
            Updated?.Invoke(this, new ItemEventArgs<bool>(refreshViewModelFromModel));
        }
    }
}
