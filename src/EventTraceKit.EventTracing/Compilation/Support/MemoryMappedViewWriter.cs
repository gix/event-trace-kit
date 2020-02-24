namespace EventTraceKit.EventTracing.Compilation.Support
{
    using System;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Runtime.InteropServices;
    using System.Text;

    internal sealed class MemoryMappedViewWriter : IDisposable
    {
        private readonly FileStream output;

        private MemoryMappedFile mappedFile;
        private MemoryMappedViewAccessor accessor;
        private byte[] stringBuffer = new byte[0x100];
        private long position;

        public MemoryMappedViewWriter(FileStream output, long initialCapacity = 0x10000)
        {
            this.output = output ?? throw new ArgumentNullException(nameof(output));

            Allocate(initialCapacity);
        }

        public long Capacity { get; private set; }

        public long Position
        {
            get => position;
            set
            {
                position = value;
                if (position > Capacity)
                    GrowTo(position);
            }
        }

        public void Dispose()
        {
            accessor.Dispose();
            mappedFile.Dispose();
            output.SetLength(Position);
        }

        public void Flush()
        {
            accessor.Flush();
            output.Flush();
        }

        public void WriteResource<T>(ref long offset, ref T resource) where T : struct
        {
            var size = Marshal.SizeOf<T>();
            EnsureSpace(offset, size);
            accessor.Write(offset, ref resource);
            offset += size;
        }

        public void WriteResource<T>(ref T resource) where T : struct
        {
            WriteResource(ref position, ref resource);
        }

        private int GetByteCount(string name)
        {
            // Count includes NUL and 4-byte length.
            int count = Encoding.Unicode.GetByteCount(name) + 2 + 4;
            return (count + 3) & ~3;
        }

        private int EncodeName(string name, ref byte[] buf)
        {
            int count = GetByteCount(name);
            if (buf.Length < count)
                buf = new byte[count];
            byte[] countBytes = BitConverter.GetBytes((uint)count);
            Buffer.BlockCopy(countBytes, 0, buf, 0, 4);
            int byteCount = Encoding.Unicode.GetBytes(name, 0, name.Length, buf, 4);
            for (int i = 4 + byteCount; i < count; ++i)
                buf[i] = 0;
            return count;
        }
        public void WriteString(ref long offset, string str)
        {
            int count = EncodeName(str, ref stringBuffer);
            WriteArray(ref offset, stringBuffer, 0, count);
        }

        public void WriteArray<T>(T[] values) where T : struct
        {
            WriteArray(ref position, values);
        }

        public void WriteArray<T>(ref long offset, T[] values) where T : struct
        {
            WriteArray(ref offset, values, 0, values.Length);
        }

        public void WriteArray<T>(ref long offset, T[] values, int index, int count) where T : struct
        {
            int byteCount = count * Marshal.SizeOf<T>();
            EnsureSpace(offset, byteCount);
            accessor.WriteArray(offset, values, index, count);
            offset += byteCount;
        }

        public void WriteUInt8(ref long offset, byte value)
        {
            EnsureSpace(1);
            accessor.Write(offset, value);
            offset += 1;
        }

        public void WriteUInt16(ref long offset, ushort value)
        {
            EnsureSpace(2);
            accessor.Write(offset, value);
            offset += 2;
        }

        public void WriteUInt32(ref long offset, uint value)
        {
            EnsureSpace(offset, 4);
            accessor.Write(offset, value);
            offset += 4;
        }

        public void WriteUInt8(byte value)
        {
            WriteUInt8(ref position, value);
        }

        public void WriteUInt16(ushort value)
        {
            WriteUInt16(ref position, value);
        }

        public void WriteUInt32(uint value)
        {
            WriteUInt32(ref position, value);
        }

        public void FillAlignment(int alignment)
        {
            var mod = (int)(position % alignment);
            if (mod == 0)
                return;
            int fill = alignment - mod;
            for (int i = 0; i < fill; ++i)
                WriteUInt8(0);
        }

        private void Allocate(long newCapacity)
        {
            if (accessor != null) {
                accessor.Dispose();
                mappedFile.Dispose();
            }

            mappedFile = MemoryMappedFile.CreateFromFile(
                output,
                null,
                newCapacity,
                MemoryMappedFileAccess.ReadWrite,
                null,
                HandleInheritability.None,
                true);

            accessor = mappedFile.CreateViewAccessor();
            Capacity = newCapacity;
        }

        private void EnsureSpace(long offset, int byteCount)
        {
            long requiredCapacity = offset + byteCount;
            if (requiredCapacity > Capacity)
                GrowTo(requiredCapacity);
        }

        private void EnsureSpace(int byteCount)
        {
            long requiredCapacity = position + byteCount;
            if (requiredCapacity > Capacity)
                GrowTo(requiredCapacity);
        }

        private void GrowTo(long requiredCapacity)
        {
            long newCapacity = Capacity;
            while (newCapacity < requiredCapacity)
                newCapacity *= 2;
            Allocate(newCapacity);
        }
    }
}
