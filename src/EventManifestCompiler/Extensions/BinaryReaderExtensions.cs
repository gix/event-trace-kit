namespace EventManifestCompiler.Extensions
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    internal static class BinaryReaderExtensions
    {
        public static string ReadPaddedString(this BinaryReader reader, uint byteCount)
        {
            return reader.ReadPaddedString(Encoding.ASCII, byteCount);
        }

        public static string ReadPaddedString(this BinaryReader reader, Encoding encoding, uint byteCount)
        {
            var bytes = new byte[byteCount];
            reader.Read(bytes, 0, bytes.Length);
            return encoding.GetString(bytes).TrimEnd('\0');
        }

        public static Guid ReadGuid(this BinaryReader r)
        {
            var b = new byte[16];
            r.Read(b, 0, b.Length);
            return new Guid(b);
        }

        public static unsafe T ReadStruct<T>(this BinaryReader r)
            where T : struct
        {
            var buffer = new byte[Marshal.SizeOf<T>()];
            r.Read(buffer, 0, buffer.Length);
            fixed (byte* p = buffer)
                return Marshal.PtrToStructure<T>((IntPtr)p);
        }
    }
}
