namespace EventTraceKit.VsExtension.Controls
{
    using System.Windows;

    public class VirtualizedDataGridViewModel : DependencyObject
    {
        public VirtualizedDataGridViewModel(IDataView dataView)
        {
            CellsPresenterViewModel = new VirtualizedDataGridCellsPresenterViewModel(this);
            ColumnsViewModel = new VirtualizedDataGridColumnsViewModel(dataView, this);
            ColumnsViewModel.ApplyPresetAssumeGridModelInSync();
            RowCount = 40;
        }

        public event ItemEventHandler<bool> Updated;

        public VirtualizedDataGridCellsPresenterViewModel CellsPresenterViewModel { get; }

        public VirtualizedDataGridColumnsViewModel ColumnsViewModel { get; }

        public int RowCount { get; private set; }

        public void RaiseUpdate(bool refreshViewModelFromModel = true)
        {
            Updated?.Invoke(this, new ItemEventArgs<bool>(refreshViewModelFromModel));
        }

        public bool RequestUpdate(bool refreshViewModelFromModel = true)
        {
            //if (this.IsReady) {
            RaiseUpdate(refreshViewModelFromModel);
            return true;
            //}

            //this.refreshViewModelOnUpdateRequest |= refreshViewModelFromModel;
            return false;
        }
    }
}