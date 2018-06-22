namespace EventTraceKit.VsExtension.Windows
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    public sealed class DelegateValueConverter<TValue, TResult> : IValueConverter
    {
        private readonly Func<TValue, TResult> converter;
        private readonly Func<TResult, TValue> convertBack;

        public DelegateValueConverter(Func<TValue, TResult> converter)
            : this(converter, null)
        {
        }

        public DelegateValueConverter(
            Func<TValue, TResult> converter, Func<TResult, TValue> convertBack)
        {
            this.converter = converter;
            this.convertBack = convertBack;
        }

        public object Convert(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TValue tv)
                return converter(tv);
            return DependencyProperty.UnsetValue;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (convertBack != null && value is TResult result)
                return convertBack(result);
            return DependencyProperty.UnsetValue;

        }
    }

    public sealed class DelegateValueConverter<TValue, TParam, TResult> : IValueConverter
    {
        private readonly Func<TValue, TParam, TResult> converter;
        private readonly Func<TResult, TParam, TValue> convertBack;

        public DelegateValueConverter(Func<TValue, TParam, TResult> converter)
            : this(converter, null)
        {
        }

        public DelegateValueConverter(
            Func<TValue, TParam, TResult> converter, Func<TResult, TParam, TValue> convertBack)
        {
            this.converter = converter;
            this.convertBack = convertBack;
        }

        public object Convert(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TValue tv && parameter is TParam param)
                return converter(tv, param);
            return DependencyProperty.UnsetValue;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (convertBack != null && value is TResult result && parameter is TParam param)
                return convertBack(result, param);
            return DependencyProperty.UnsetValue;

        }
    }
}
