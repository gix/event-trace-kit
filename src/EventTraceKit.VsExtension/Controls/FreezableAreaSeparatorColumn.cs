namespace EventTraceKit.VsExtension.Controls
{
    internal sealed class FreezableAreaSeparatorColumn : AsyncDataGridColumn
    {
        public FreezableAreaSeparatorColumn(
            AsyncDataGridColumnsViewModel columns, AsyncDataViewModel adv)
            : base(columns, CreateColumnView(), adv, true)
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
