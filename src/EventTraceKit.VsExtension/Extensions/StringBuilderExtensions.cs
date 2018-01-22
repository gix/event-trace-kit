namespace EventTraceKit.VsExtension.Extensions
{
    using System.Text;

    public static class StringBuilderExtensions
    {
        private const string HexChars = "0123456789ABCDEF";

        public static void AppendHexByte(this StringBuilder builder, byte b)
        {
            int hi = (b >> 4) & 0xF;
            int lo = b & 0xF;
            builder.Append(HexChars[hi]);
            builder.Append(HexChars[lo]);
        }
    }
}
