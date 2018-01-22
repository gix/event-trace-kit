namespace EventTraceKit.VsExtension
{
    using System.Windows;
    using System.Windows.Controls;

    public class PresetManagerColumnDetailsTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is PresetManagerColumnViewModel model))
                return base.SelectTemplate(item, container);

            switch (model.ColumnType) {
                case PresetManagerColumnType.LeftFreezableAreaSeparator:
                    return LeftFreezableAreaSeparatorColumnTemplate;
                case PresetManagerColumnType.RightFreezableAreaSeparator:
                    return RightFreezableAreaSeparatorColumnTemplate;
                case PresetManagerColumnType.Configurable:
                    return ConfigurableColumnTemplate;
                default:
                    throw ExceptionUtils.InvalidEnumArgumentException(
                        model.ColumnType, "model.ColumnType");
            }
        }

        public DataTemplate ConfigurableColumnTemplate { get; set; }
        public DataTemplate LeftFreezableAreaSeparatorColumnTemplate { get; set; }
        public DataTemplate RightFreezableAreaSeparatorColumnTemplate { get; set; }
    }
}
