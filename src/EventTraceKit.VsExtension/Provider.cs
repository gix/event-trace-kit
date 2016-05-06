namespace EventTraceKit.VsExtension
{
    using System;

    public class ProviderSpec
    {
        public ProviderSpec(
            Guid id, byte level = 0xFF,
            ulong anyKeywordMask = 0xFFFFFFFFFFFFFFFFUL,
            ulong allKeywordMask = 0)
        {
            Id = id;
            Level = level;
            MatchAnyKeyword = anyKeywordMask;
            MatchAllKeyword = allKeywordMask;
        }

        public Guid Id { get; set; }
        public byte Level { get; set; }
        public ulong MatchAnyKeyword { get; set; }
        public ulong MatchAllKeyword { get; set; }
    }

}
