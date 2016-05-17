namespace EventTraceKit.VsExtension.Controls
{
    internal sealed class ExpanderHeaderColumn : VirtualizedDataGridColumn
    {
        public ExpanderHeaderColumn(
            VirtualizedDataGridColumnsViewModel columns,
            IDataColumn columnModel, IDataView dataView)
            : base(columns, columnModel, dataView)
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
