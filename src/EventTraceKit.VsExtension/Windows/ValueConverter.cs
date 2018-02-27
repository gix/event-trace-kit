namespace EventTraceKit.VsExtension.Windows
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class ValueConverter<TSource, TTarget> : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TSource) && (value != null || typeof(TSource).IsValueType))
                throw InvalidSourceType<TSource>();
            if (!targetType.IsAssignableFrom(typeof(TTarget)))
                throw InvalidTargetType<TTarget>();
            if (TryConvert((TSource)value, parameter, culture, out var target))
                return target;
            return value;
        }

        public object ConvertBack(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TTarget) && (value != null || typeof(TTarget).IsValueType))
                throw InvalidSourceType<TTarget>();
            if (!targetType.IsAssignableFrom(typeof(TSource)))
                throw InvalidTargetType<TSource>();
            if (TryConvertBack((TTarget)value, parameter, culture, out var source))
                return source;
            return value;
        }

        private static ArgumentException InvalidSourceType<T>()
        {
            return new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture, "Value must be of type {0}.",
                    typeof(T).FullName));
        }

        private static InvalidOperationException InvalidTargetType<T>()
        {
            return new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture, "Target type must extend {0}.",
                    typeof(T).FullName));
        }

        protected virtual bool TryConvert(
            TSource value, object parameter, CultureInfo culture, out TTarget target)
        {
            var message = string.Format(
                CultureInfo.CurrentCulture,
                "{0} is not defined for this converter.", nameof(System.Convert));
            throw new NotSupportedException(message);
        }

        protected virtual bool TryConvertBack(
            TTarget value, object parameter, CultureInfo culture, out TSource source)
        {
            var message = string.Format(CultureInfo.CurrentCulture,
                "{0} is not defined for this converter.", nameof(ConvertBack));
            throw new NotSupportedException(message);
        }
    }
}
