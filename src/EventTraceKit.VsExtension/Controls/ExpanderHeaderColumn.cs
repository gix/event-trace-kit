namespace EventTraceKit.VsExtension.Controls
{
    internal sealed class ExpanderHeaderColumn : AsyncDataGridColumn
    {
        public ExpanderHeaderColumn(
            AsyncDataGridColumnsViewModel columns, HdvViewModel hdv)
            : base(columns, CreateColumnView(), hdv, true)
        {
        }

        private static DataColumnView CreateColumnView()
        {
            var column = new DataColumn<object> { Name = string.Empty };
            var info = new DataColumnViewInfo { IsVisible = true };
            return column.CreateView(info);
        }
    }
}
