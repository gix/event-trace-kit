namespace EventTraceKit.VsExtension.Windows
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
            return value is double v ? -v : value;

        }

        public object ConvertBack(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is double v ? -v : value;
        }
    }
}
