namespace EventTraceKit.VsExtension.Windows
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    [ValueConversion(typeof(object), typeof(Visibility))]
    public sealed class OfTypeConverter : IValueConverter
    {
        public Type Type { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Type == null)
                return Boxed.True;
            return Boxed.Bool(Type.IsInstanceOfType(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
