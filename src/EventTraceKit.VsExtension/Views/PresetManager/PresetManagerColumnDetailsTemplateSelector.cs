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

            return model.ColumnType switch
            {
                PresetManagerColumnType.LeftFreezableAreaSeparator => LeftFreezableAreaSeparatorColumnTemplate,
                PresetManagerColumnType.RightFreezableAreaSeparator => RightFreezableAreaSeparatorColumnTemplate,
                PresetManagerColumnType.Configurable => ConfigurableColumnTemplate,
                _ => throw new InvalidEnumArgumentException(
                    nameof(model.ColumnType),
                    Convert.ToInt32(model.ColumnType, CultureInfo.CurrentCulture),
                    typeof(PresetManagerColumnType)),
            };
        }

        public DataTemplate ConfigurableColumnTemplate { get; set; }
        public DataTemplate LeftFreezableAreaSeparatorColumnTemplate { get; set; }
        public DataTemplate RightFreezableAreaSeparatorColumnTemplate { get; set; }
    }
}
