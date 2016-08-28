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
            if (targetType == typeof(sbyte)) {
                sbyte v;
                return sbyte.TryParse(value, style, culture, out v) ? (object)v : null;
            }
            if (targetType == typeof(short)) {
                short v;
                return short.TryParse(value, style, culture, out v) ? (object)v : null;
            }
            if (targetType == typeof(int)) {
                int v;
                return int.TryParse(value, style, culture, out v) ? (object)v : null;
            }
            if (targetType == typeof(long)) {
                long v;
                return long.TryParse(value, style, culture, out v) ? (object)v : null;
            }
            if (targetType == typeof(byte)) {
                byte v;
                return byte.TryParse(value, style, culture, out v) ? (object)v : null;
            }
            if (targetType == typeof(ushort)) {
                ushort v;
                return ushort.TryParse(value, style, culture, out v) ? (object)v : null;
            }
            if (targetType == typeof(uint)) {
                uint v;
                return uint.TryParse(value, style, culture, out v) ? (object)v : null;
            }
            if (targetType == typeof(ulong)) {
                ulong v;
                return ulong.TryParse(value, style, culture, out v) ? (object)v : null;
            }

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
