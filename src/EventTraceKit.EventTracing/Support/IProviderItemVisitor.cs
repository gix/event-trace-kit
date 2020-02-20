namespace EventTraceKit.EventTracing.Support
{
    using EventTraceKit.EventTracing.Schema;

    public interface IProviderItemVisitor
    {
        void VisitBitMap(BitMap bitMap);
        void VisitChannel(Channel channel);
        void VisitEvent(Event evt);
        void VisitFilter(Filter filter);
        void VisitLevel(Level level);
        void VisitKeyword(Keyword keyword);
        void VisitOpcode(Opcode opcode);
        void VisitPatternMap(PatternMap patternMap);
        void VisitTask(Task task);
        void VisitTemplate(Template template);
        void VisitValueMap(ValueMap valueMap);
    }
}
