namespace InstrManifestCompiler.Support
{
    using System;
    using System.Diagnostics.Contracts;

    internal static class BitConverterEx
    {
        public static void FromUInt32(uint value, byte[] buffer, int index)
        {
            Contract.Requires<ArgumentException>(index + 4 <= buffer.Length);
            buffer[index + 0] = (byte)(value & 0xFF);
            buffer[index + 1] = (byte)((value >> 8) & 0xFF);
            buffer[index + 2] = (byte)((value >> 16) & 0xFF);
            buffer[index + 3] = (byte)((value >> 24) & 0xFF);
        }
    }
}
