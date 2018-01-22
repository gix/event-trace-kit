namespace EventTraceKit.VsExtension.Windows
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public sealed class HexNumberConverter : IValueConverter
    {
        public static readonly HexNumberConverter Instance = new HexNumberConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!IsHexType(value?.GetType()))
                throw InvalidSourceType("Number type");
            if (!targetType.IsAssignableFrom(typeof(string)))
                throw InvalidTargetType<string>();
            var width = parameter as string;
            return "0x" + ((IFormattable)value).ToString("X" + width, culture);
        }

        public object ConvertBack(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string))
                throw InvalidSourceType(typeof(string).FullName);
            if (!IsHexType(targetType))
                throw InvalidTargetType<int>();
            return TryConvertBack((string)value, targetType, parameter, culture) ?? value;
        }

        private static bool IsHexType(Type type)
        {
            return
                type == typeof(sbyte) ||
                type == typeof(short) ||
                type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(byte) ||
                type == typeof(ushort) ||
                type == typeof(uint) ||
                type == typeof(ulong)
                ;
        }

        private object TryConvertBack(
            string value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !value.StartsWith("0x"))
                return null;

            value = value.Substring(2);

            var style = NumberStyles.HexNumber;
            if (targetType == typeof(sbyte))
                return sbyte.TryParse(value, style, culture, out var v) ? (object)v : null;
            if (targetType == typeof(short))
                return short.TryParse(value, style, culture, out short v) ? (object)v : null;
            if (targetType == typeof(int))
                return int.TryParse(value, style, culture, out int v) ? (object)v : null;
            if (targetType == typeof(long))
                return long.TryParse(value, style, culture, out long v) ? (object)v : null;
            if (targetType == typeof(byte))
                return byte.TryParse(value, style, culture, out byte v) ? (object)v : null;
            if (targetType == typeof(ushort))
                return ushort.TryParse(value, style, culture, out ushort v) ? (object)v : null;
            if (targetType == typeof(uint))
                return uint.TryParse(value, style, culture, out uint v) ? (object)v : null;
            if (targetType == typeof(ulong))
                return ulong.TryParse(value, style, culture, out ulong v) ? (object)v : null;

            return null;
        }

        private static ArgumentException InvalidSourceType(string typeName)
        {
            return new ArgumentException($"Value must be of type {typeName}.");
        }

        private static InvalidOperationException InvalidTargetType<T>()
        {
            return new InvalidOperationException(
                $"Target type must extend {typeof(T).FullName}.");
        }
    }
}
