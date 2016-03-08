namespace InstrManifestCompiler.EventManifestSchema.Base
{
    public sealed class EnableBit
    {
        public int Bit { get; private set; }
        public int Level { get; private set; }
        public ulong KeywordMask { get; private set; }

        public EnableBit(int bit, int level, ulong keywordMask)
        {
            Bit = bit;
            Level = level;
            KeywordMask = keywordMask;
        }

        public int GetByte(int byteSize)
        {
            int bits = byteSize * 32;
            return Bit / bits;
        }

        public ulong GetMask(int byteSize)
        {
            int bits = byteSize * 32;
            return (ulong)1 << (Bit - (Bit / bits * bits));
        }
    }
}
