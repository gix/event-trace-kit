namespace EventTraceKit.VsExtension
{
    using System;
    using System.Globalization;

    public class CellValue
    {
        private string precomputed;

        public CellValue(object value, IFormatProvider formatProvider, string format)
        {
            Value = value;
            FormatProvider = formatProvider;
            Format = format;
        }

        public object Value { get; }
        public IFormatProvider FormatProvider { get; }
        public string Format { get; }

        public override string ToString()
        {
            return precomputed ?? FormatValue();
        }

        private string FormatValue()
        {
            string result = null;

            if (FormatProvider?.GetFormat(Value?.GetType()) is ICustomFormatter formatter)
                result = formatter.Format(Format, Value, CultureInfo.CurrentCulture);

            if (result == null && Value is IFormattable formattable)
                result = formattable.ToString(Format, FormatProvider);

            return result ?? Value?.ToString() ?? string.Empty;
        }

        public void PrecomputeString()
        {
            precomputed = FormatValue();
        }
    }
}
