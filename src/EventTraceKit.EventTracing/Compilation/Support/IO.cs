namespace EventTraceKit.EventTracing.Compilation.Support
{
    using System.IO;
    using System.Text;
    using System.Threading;

    internal static class IO
    {
        private static UTF8Encoding utf8WithBOM;
        private static UTF8Encoding utf8NoBOM;

        private static Encoding UTF8BOM
        {
            get
            {
                if (utf8WithBOM == null) {
                    var encoding = new UTF8Encoding(false, true);
                    Thread.MemoryBarrier();
                    utf8WithBOM = encoding;
                }
                return utf8WithBOM;
            }
        }

        private static Encoding UTF8NoBOM
        {
            get
            {
                if (utf8NoBOM == null) {
                    var encoding = new UTF8Encoding(false, true);
                    Thread.MemoryBarrier();
                    utf8NoBOM = encoding;
                }
                return utf8NoBOM;
            }
        }

        public static StreamWriter CreateStreamWriter(Stream output, bool bom = false)
        {
            return new StreamWriter(output, bom ? UTF8BOM : UTF8NoBOM, 0x400, true);
        }

        public static BinaryReader CreateBinaryReader(Stream input)
        {
            return new BinaryReader(input, UTF8NoBOM, true);
        }

        public static BinaryWriter CreateBinaryWriter(Stream output)
        {
            return new BinaryWriter(output, UTF8NoBOM, true);
        }
    }
}
