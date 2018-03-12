namespace EventTraceKit.VsExtension.Windows
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using EventTraceKit.VsExtension.Extensions;

    [ValueConversion(typeof(object), typeof(bool))]
    public sealed class IsEmptyEnumerableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is IEnumerable enumerable) || !enumerable.Any();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
