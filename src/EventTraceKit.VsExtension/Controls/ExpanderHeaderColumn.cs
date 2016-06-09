namespace EventTraceKit.VsExtension.Controls
{
    using System.Windows;

    internal sealed class ExpanderHeaderColumn : VirtualizedDataGridColumn
    {
        public ExpanderHeaderColumn(
            VirtualizedDataGridColumnsViewModel columns, IDataView dataView)
            : base(columns, new Column(), dataView)
        {
            IsVisible = true;

            //var binding = new Binding {
            //    Source = columns,
            //    Path = new PropertyPath(nameof(columns.ActualWidth)),
            //    Mode = BindingMode.OneWay
            //};
            //BindingOperations.SetBinding(this, WidthProperty, binding);
        }

        private sealed class Column : IDataColumn
        {
            public string Name => string.Empty;
            public double Width => 100;
            public bool IsVisible => true;
            public bool IsResizable => false;
            public TextAlignment TextAlignment => 0;
        }
    }
}
