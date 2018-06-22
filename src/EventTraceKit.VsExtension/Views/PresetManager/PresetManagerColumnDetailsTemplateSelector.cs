namespace EventTraceKit.VsExtension.Views.PresetManager
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
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
                    throw new InvalidEnumArgumentException(
                        nameof(model.ColumnType),
                        Convert.ToInt32(model.ColumnType, CultureInfo.CurrentCulture),
                        typeof(PresetManagerColumnType));
            }
        }

        public DataTemplate ConfigurableColumnTemplate { get; set; }
        public DataTemplate LeftFreezableAreaSeparatorColumnTemplate { get; set; }
        public DataTemplate RightFreezableAreaSeparatorColumnTemplate { get; set; }
    }
}
