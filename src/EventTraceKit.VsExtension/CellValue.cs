namespace EventTraceKit.VsExtension
{
    using System;

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

            ICustomFormatter formatter;
            if ((formatter = Value as ICustomFormatter) != null)
                result = formatter.Format(Format, Value, FormatProvider);

            IFormattable formattable;
            if (result == null && (formattable = Value as IFormattable) != null)
                result = formattable.ToString(Format, FormatProvider);

            return result ?? Value?.ToString() ?? string.Empty;
        }

        public void PrecomputeString()
        {
            precomputed = FormatValue();
        }
    }
}
