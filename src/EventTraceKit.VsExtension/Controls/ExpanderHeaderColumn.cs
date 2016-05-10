namespace EventTraceKit.VsExtension.Controls
{
    internal sealed class ExpanderHeaderColumn : VirtualizedDataGridColumn
    {
        public ExpanderHeaderColumn(
            VirtualizedDataGridColumnsViewModel columnsViewModel,
            IDataColumn columnModel, IDataView dataView)
            : base(columnsViewModel, columnModel, dataView)
        {
            IsVisible = true;

            //var binding = new Binding {
            //    Source = columnsViewModel,
            //    Path = new PropertyPath(nameof(columnsViewModel.ActualWidth)),
            //    Mode = BindingMode.OneWay
            //};
            //BindingOperations.SetBinding(this, WidthProperty, binding);
        }
    }
}
