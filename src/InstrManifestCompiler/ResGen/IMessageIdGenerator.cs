namespace InstrManifestCompiler.ResGen
{
    using InstrManifestCompiler.EventManifestSchema;

    public interface IMessageIdGenerator
    {
        uint CreateId(Provider provider);
        uint CreateId(Channel channel, Provider provider);
        uint CreateId(Level level, Provider provider);
        uint CreateId(Task task, Provider provider);
        uint CreateId(Opcode opcode, Provider provider);
        uint CreateId(Keyword keyword, Provider provider);
        uint CreateId(Event evt, Provider provider);
        uint CreateId(IMapItem item, IMap map, Provider provider);
        uint CreateId(ValueMapItem item, ValueMap map, Provider provider);
        uint CreateId(BitMapItem item, BitMap map, Provider provider);
        uint CreateId(Filter filter, Provider provider);
    }
}
