namespace InstrManifestCompiler.Support
{
    using System;

    internal static class BitConverterEx
    {
        public static void FromUInt32(uint value, byte[] buffer, int index)
        {
            if (index + 4 > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(index), index, null);

            buffer[index + 0] = (byte)(value & 0xFF);
            buffer[index + 1] = (byte)((value >> 8) & 0xFF);
            buffer[index + 2] = (byte)((value >> 16) & 0xFF);
            buffer[index + 3] = (byte)((value >> 24) & 0xFF);
        }
    }
}
