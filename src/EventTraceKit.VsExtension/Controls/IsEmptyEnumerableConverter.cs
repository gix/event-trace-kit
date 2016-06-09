namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    [ValueConversion(typeof(object), typeof(bool))]
    public sealed class IsEmptyEnumerableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumerable = value as IEnumerable;
            return enumerable == null || !Any(enumerable);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

        private static bool Any(IEnumerable source)
        {
            IEnumerator e = source.GetEnumerator();
            return e.MoveNext();
        }
    }
}