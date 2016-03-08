namespace InstrManifestCompiler.Support
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

        public static StreamWriter CreateStreamWriter(Stream stream, bool bom = false)
        {
            return new StreamWriter(stream, bom ? UTF8BOM : UTF8NoBOM, 0x400, true);
        }

        public static BinaryReader CreateBinaryReader(Stream stream)
        {
            return new BinaryReader(stream, UTF8NoBOM, false);
        }

        public static BinaryWriter CreateBinaryWriter(Stream stream)
        {
            return new BinaryWriter(stream, UTF8NoBOM, false);
        }
    }
}
