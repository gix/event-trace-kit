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

        public int GetIndex(int bytesPerItem)
        {
            int bits = bytesPerItem * 8;
            return Bit / bits;
        }

        public ulong GetMask(int bytesPerItem)
        {
            int bits = bytesPerItem * 8;
            return (ulong)1 << (Bit - (Bit / bits * bits));
        }
    }
}
