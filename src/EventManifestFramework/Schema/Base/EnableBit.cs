namespace EventManifestFramework.Schema.Base
{
    public sealed class EnableBit
    {
        public EnableBit(int bit, int level, ulong keywordMask)
        {
            Bit = bit;
            Level = level;
            KeywordMask = keywordMask;
        }

        public int Bit { get; }
        public int Level { get; }
        public ulong KeywordMask { get; }

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
