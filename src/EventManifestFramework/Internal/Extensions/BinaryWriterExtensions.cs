namespace EventManifestFramework.Internal.Extensions
{
    using System;
    using System.IO;

    internal static class BinaryWriterExtensions
    {
        public static void WriteInt8(this BinaryWriter writer, sbyte value)
        {
            writer.Write(value);
        }

        public static void WriteInt16(this BinaryWriter writer, short value)
        {
            writer.Write(value);
        }

        public static void WriteInt32(this BinaryWriter writer, int value)
        {
            writer.Write(value);
        }

        public static void WriteInt64(this BinaryWriter writer, long value)
        {
            writer.Write(value);
        }

        public static void WriteUInt8(this BinaryWriter writer, byte value)
        {
            writer.Write(value);
        }

        public static void WriteUInt16(this BinaryWriter writer, ushort value)
        {
            writer.Write(value);
        }

        public static void WriteUInt32(this BinaryWriter writer, uint value)
        {
            writer.Write(value);
        }

        public static void WriteUInt64(this BinaryWriter writer, ulong value)
        {
            writer.Write(value);
        }

        public static void WriteBytes(this BinaryWriter writer, byte[] bytes)
        {
            writer.Write(bytes, 0, bytes.Length);
        }

        public static void WriteGuid(this BinaryWriter writer, Guid guid)
        {
            byte[] bytes = guid.ToByteArray();
            writer.Write(bytes, 0, bytes.Length);
        }

        public static void WriteUInt32At(this BinaryWriter writer, long offset, uint value)
        {
            writer.Flush();
            long pos = writer.BaseStream.Position;

            writer.BaseStream.Position = offset;
            try {
                writer.WriteUInt32(value);
                writer.Flush();
            } finally {
                writer.BaseStream.Position = pos;
            }
        }

        public static ReservedUInt32 ReserveUInt32(this BinaryWriter writer)
        {
            long offset = writer.BaseStream.Position;
            writer.WriteUInt32(0);
            return new ReservedUInt32(writer, offset);
        }

        public static void FillAlignment(this BinaryWriter writer, int alignment)
        {
            var mod = (int)(writer.BaseStream.Position % alignment);
            if (mod == 0)
                return;
            int fill = alignment - mod;
            for (int i = 0; i < fill; ++i)
                writer.WriteUInt8(0);
        }
    }

    internal sealed class ReservedUInt32
    {
        private readonly BinaryWriter writer;

        public ReservedUInt32(BinaryWriter writer, long offset)
        {
            this.writer = writer;
            Offset = offset;
        }

        public long Offset { get; }

        public void Update(uint value)
        {
            writer.WriteUInt32At(Offset, value);
        }

        public void UpdateRelative(int v = 0)
        {
            long value = writer.BaseStream.Position - Offset + v;
            writer.WriteUInt32At(Offset, (uint)value);
        }

        public void UpdateToCurrent()
        {
            writer.WriteUInt32At(Offset, (uint)writer.BaseStream.Position);
        }
    }
}
