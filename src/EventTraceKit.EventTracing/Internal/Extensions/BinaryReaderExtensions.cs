namespace EventTraceKit.EventTracing.Internal.Extensions
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
            int bytesRead = r.Read(b, 0, b.Length);
            if (bytesRead != b.Length)
                throw new EndOfStreamException("Failed to read Guid");
            return new Guid(b);
        }

        public static Guid ReadGuidAt(this BinaryReader r, long offset)
        {
            Stream stream = r.BaseStream;
            long old = stream.Position;
            try {
                stream.Position = offset;
                return r.ReadGuid();
            } finally {
                stream.Position = old;
            }
        }

        public static unsafe T ReadStruct<T>(this BinaryReader r)
            where T : struct
        {
            var buffer = new byte[Marshal.SizeOf<T>()];
            r.Read(buffer, 0, buffer.Length);
            fixed (byte* p = buffer)
                return Marshal.PtrToStructure<T>((IntPtr)p);
        }

        public static uint[] ReadUInt32At(this BinaryReader r, long offset, uint count)
        {
            Stream stream = r.BaseStream;
            long old = stream.Position;
            try {
                stream.Position = offset;
                var values = new uint[count];
                for (uint i = 0; i < count; ++i)
                    values[i] = r.ReadUInt32();
                return values;
            } finally {
                stream.Position = old;
            }
        }

        public static string ReadCountedStringAt(this BinaryReader r, long offset)
        {
            Stream stream = r.BaseStream;
            long old = stream.Position;
            try {
                stream.Position = offset;
                uint byteCount = r.ReadUInt32();
                return r.ReadPaddedString(Encoding.Unicode, byteCount - 4);
            } finally {
                stream.Position = old;
            }
        }

        public static string ReadZStringAt(this BinaryReader r, long offset)
        {
            if (offset == 0)
                return null;

            Stream stream = r.BaseStream;
            long old = stream.Position;
            try {
                stream.Position = offset;
                var buffer = new MemoryStream();
                var charBuffer = new byte[2];
                while (true) {
                    int bytesRead = r.Read(charBuffer, 0, charBuffer.Length);
                    if (bytesRead < 2 || charBuffer[0] == 0 && charBuffer[1] == 0)
                        break;
                    buffer.Write(charBuffer, 0, charBuffer.Length);
                }
                return Encoding.Unicode.GetString(buffer.ToArray());
            } finally {
                stream.Position = old;
            }
        }
    }
}
