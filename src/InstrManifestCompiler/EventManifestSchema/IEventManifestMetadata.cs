namespace InstrManifestCompiler.EventManifestSchema
{
    using InstrManifestCompiler.EventManifestSchema.Base;

    public interface IEventManifestMetadata
    {
        ChannelCollection Channels { get; }
        LevelCollection Levels { get; }
        OpcodeCollection Opcodes { get; }
        TaskCollection Tasks { get; }
        KeywordCollection Keywords { get; }

        XmlType GetXmlType(QName name);
        XmlType GetXmlType(byte value);
        InType GetInType(QName name);
        InType GetInType(byte value);

        LocalizedString GetString(string stringRef);
    }
}
