namespace EventTraceKit.VsExtension.Tests.Formatting
{
    using System;
    using System.Globalization;
    using VsExtension.Formatting;
    using Xunit;

    public class FormatProviderTest
    {
        [Theory]
        [InlineData("G", "01:23:45.1234567", "01:23:45.1234567")]
        [InlineData("g", "01:23:45.1234567", "23:45.1234567")]
        [InlineData("s", "01:23:45.1234567", "5025.1234567")]
        [InlineData("m", "01:23:45.1234567", "5025123.4567")]
        [InlineData("u", "01:23:45.1234567", "5025123456.7")]
        [InlineData("t", "01:23:45.1234567", "50251234567")]
        public void FormatTimeSpan(string format, string timeSpan, string expected)
        {
            var provider = new TimeSpanFormatProvider();
            string actual = provider.Format(format, TimeSpan.Parse(timeSpan), CultureInfo.InvariantCulture);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("sN", 50251234567, "5,025.123456700")]
        [InlineData("mN", 50251234567, "5,025,123.456700")]
        [InlineData("uN", 50251234567, "5,025,123,456.700")]
        [InlineData("nN", 50251234567, "5,025,123,456,700")]
        [InlineData("tN", 50251234567, "50,251,234,567")]
        public void FormatTimePoint(string format, long ticks, string expected)
        {
            var provider = new TimePointFormatProvider();
            string actual = provider.Format(format, new TimePoint(ticks), CultureInfo.InvariantCulture);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("N0", 1234567, "1,234,567")]
        [InlineData("D0", 1234567, "1234567")]
        [InlineData("X", 0xABC123, "0x00ABC123")]
        [InlineData("X", (long)0xABC123, "0x0000000000ABC123")]
        [InlineData("X", (short)0xA, "0x000A")]
        [InlineData("Xs", 0xABC123, "00ABC123")]
        public void FormatNumber(string format, object value, string expected)
        {
            var provider = new NumericalFormatProvider();
            string actual = provider.Format(format, value, CultureInfo.InvariantCulture);
            Assert.Equal(expected, actual);
        }
    }
}
