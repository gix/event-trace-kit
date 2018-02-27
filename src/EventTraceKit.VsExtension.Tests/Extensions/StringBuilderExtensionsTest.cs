namespace EventTraceKit.VsExtension.Tests.Extensions
{
    using System.Text;
    using VsExtension.Extensions;
    using Xunit;

    public class StringBuilderExtensionsTest
    {
        [Theory]
        [InlineData(0x00, "00")]
        [InlineData(0x9A, "9A")]
        [InlineData(0xAB, "AB")]
        [InlineData(0xFF, "FF")]
        public void Name(byte input, string expected)
        {
            var builder = new StringBuilder();
            builder.AppendHexByte(input);
            Assert.Equal(expected, builder.ToString());
        }
    }
}
