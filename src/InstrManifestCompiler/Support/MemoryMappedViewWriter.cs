namespace InstrManifestCompiler.Support
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Runtime.InteropServices;
    using System.Text;

    internal sealed class MemoryMappedViewWriter : IDisposable
    {
        private readonly Stream output;

        private MemoryMappedFile mappedFile;
        private MemoryMappedViewAccessor accessor;
        private byte[] stringBuffer = new byte[0x100];
        private long capacity;
        private long position;

        public MemoryMappedViewWriter(Stream output, long initialCapacity = 0x10000)
        {
            Contract.Requires<ArgumentNullException>(output != null);
            this.output = output;

            Allocate(initialCapacity);
        }

        public long Capacity
        {
            get { return capacity; }
        }

        public long Position
        {
            get { return position; }
            set
            {
                position = value;
                if (position > capacity)
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
            EnsureSpace(offset, Marshal.SizeOf<T>());
            accessor.Write(offset, ref resource);
            offset += Marshal.SizeOf<T>();
        }

        public void WriteResource<T>(ref T resource) where T : struct
        {
            EnsureSpace(Marshal.SizeOf<T>());
            accessor.Write(position, ref resource);
            position += Marshal.SizeOf<T>();
        }

        private int GetByteCount(string name)
        {
            // Count includes NUL and 4-byte length.
            int count =  Encoding.Unicode.GetByteCount(name) + 2 + 4;
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

        public void WriteArray<T>(T[] array) where T : struct
        {
            int byteCount = array.Length * Marshal.SizeOf<T>();
            EnsureSpace(byteCount);
            accessor.WriteArray(position, array, 0, array.Length);
            position += byteCount;
        }

        public void WriteArray<T>(ref long offset, T[] values) where T : struct
        {
            int size = values.Length * Marshal.SizeOf<T>();
            EnsureSpace(offset, size);
            accessor.WriteArray(offset, values, 0, values.Length);
            offset += size;
        }

        public void WriteArray<T>(ref long offset, T[] values, int index, int count) where T : struct
        {
            int byteCount = count * Marshal.SizeOf<T>();
            EnsureSpace(offset, byteCount);
            accessor.WriteArray(offset, values, index, count);
            offset += byteCount;
        }

        public void WriteUInt32(ref long offset, uint value)
        {
            EnsureSpace(offset, 4);
            accessor.Write(offset, value);
            offset += 4;
        }

        public void WriteUInt8(byte value)
        {
            EnsureSpace(1);
            accessor.Write(position, value);
            position += 1;
        }

        public void WriteUInt16(ushort value)
        {
            EnsureSpace(2);
            accessor.Write(position, value);
            position += 2;
        }

        public void WriteUInt32(uint value)
        {
            EnsureSpace(4);
            accessor.Write(position, value);
            position += 4;
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

            if (output is FileStream fileOutput) {
                mappedFile = MemoryMappedFile.CreateFromFile(
                    fileOutput,
                    null,
                    newCapacity,
                    MemoryMappedFileAccess.ReadWrite,
                    null,
                    HandleInheritability.None,
                    true);
            } else {
                throw new NotImplementedException();
            }

            accessor = mappedFile.CreateViewAccessor();
            capacity = newCapacity;
        }

        private void EnsureSpace(long offset, int byteCount)
        {
            long requiredCapacity = offset + byteCount;
            if (requiredCapacity > capacity)
                GrowTo(requiredCapacity);
        }

        private void EnsureSpace(int byteCount)
        {
            long requiredCapacity = position + byteCount;
            if (requiredCapacity > capacity)
                GrowTo(requiredCapacity);
        }

        private void GrowTo(long requiredCapacity)
        {
            long newCapacity = capacity;
            while (newCapacity < requiredCapacity)
                newCapacity *= 2;
            Allocate(newCapacity);
        }
    }
}
