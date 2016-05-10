namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public sealed class NegativeDoubleValueConverter : IValueConverter
    {
        public static NegativeDoubleValueConverter Instance { get; } =
            new NegativeDoubleValueConverter();

        public object Convert(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            double? nullable = value as double?;
            return nullable.HasValue ? -nullable.GetValueOrDefault() : value;

        }

        public object ConvertBack(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            double? nullable = value as double?;
            return nullable.HasValue ? -nullable.GetValueOrDefault() : value;
        }
    }
}
