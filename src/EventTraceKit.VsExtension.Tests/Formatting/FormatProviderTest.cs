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
        [InlineData("N0", 0, "0")]
        [InlineData("N0", 123, "123")]
        [InlineData("N0", 1234567, "1,234,567")]
        [InlineData("D0", 1234567, "1234567")]
        [InlineData("X", (sbyte)0xA, "0xA")]
        [InlineData("X", (byte)0xA, "0xA")]
        [InlineData("X", (short)0xABC, "0xABC")]
        [InlineData("X", (ushort)0xABC, "0xABC")]
        [InlineData("X", (int)0xABC123, "0xABC123")]
        [InlineData("X", (uint)0xABC123, "0xABC123")]
        [InlineData("X", (long)0xABC123456, "0xABC123456")]
        [InlineData("X", (ulong)0xABC123456, "0xABC123456")]
        [InlineData("Xs", (sbyte)0xA, "A")]
        [InlineData("Xs", (byte)0xA, "A")]
        [InlineData("Xs", (short)0xABC, "ABC")]
        [InlineData("Xs", (ushort)0xABC, "ABC")]
        [InlineData("Xs", (int)0xABC123, "ABC123")]
        [InlineData("Xs", (uint)0xABC123, "ABC123")]
        [InlineData("Xs", (long)0xABC123456, "ABC123456")]
        [InlineData("Xs", (ulong)0xABC123456, "ABC123456")]
        [InlineData("PX", (sbyte)0xA, "0x0A")]
        [InlineData("PX", (byte)0xA, "0x0A")]
        [InlineData("PX", (short)0xABC, "0x0ABC")]
        [InlineData("PX", (ushort)0xABC, "0x0ABC")]
        [InlineData("PX", (int)0xABC123, "0x00ABC123")]
        [InlineData("PX", (uint)0xABC123, "0x00ABC123")]
        [InlineData("PX", (long)0xABC123456, "0x0000000ABC123456")]
        [InlineData("PX", (ulong)0xABC123456, "0x0000000ABC123456")]
        [InlineData("PXs", (byte)0xA, "0A")]
        [InlineData("PXs", (sbyte)0xA, "0A")]
        [InlineData("PXs", (short)0xABC, "0ABC")]
        [InlineData("PXs", (ushort)0xABC, "0ABC")]
        [InlineData("PXs", (int)0xABC123, "00ABC123")]
        [InlineData("PXs", (uint)0xABC123, "00ABC123")]
        [InlineData("PXs", (long)0xABC123456, "0000000ABC123456")]
        [InlineData("PXs", (ulong)0xABC123456, "0000000ABC123456")]
        public void FormatNumber(string format, object value, string expected)
        {
            var provider = new NumericalFormatProvider(CultureInfo.InvariantCulture);
            string actual = string.Format(provider, $"{{0:{format}}}", value);
            Assert.Equal(expected, actual);
        }
    }
}
