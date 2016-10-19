namespace EventTraceKit.VsExtension
{
    using System.Windows;
    using System.Windows.Controls;

    public class PresetManagerColumnContainerStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            var model = item as PresetManagerColumnViewModel;
            if (model == null)
                return base.SelectStyle(item, container);
            if (model.ColumnType == PresetManagerColumnType.Configurable)
                return ConfigurableColumnStyle;
            return SeparatorColumnStyle;
        }

        public Style ConfigurableColumnStyle { get; set; }
        public Style SeparatorColumnStyle { get; set; }
    }
}