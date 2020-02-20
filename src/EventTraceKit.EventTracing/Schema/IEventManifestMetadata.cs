namespace EventTraceKit.EventTracing.Schema
{
    using System.Collections.Generic;
    using EventTraceKit.EventTracing.Schema.Base;

    public interface IEventManifestMetadata
    {
        IReadOnlyList<Channel> Channels { get; }
        IReadOnlyList<Level> Levels { get; }
        IReadOnlyList<Opcode> Opcodes { get; }
        IReadOnlyList<Task> Tasks { get; }
        IReadOnlyList<Keyword> Keywords { get; }
        IReadOnlyList<PatternMap> NamedQueries { get; }
        IReadOnlyList<LocalizedString> Strings { get; }

        XmlType GetXmlType(QName name);
        XmlType GetXmlType(byte value);
        InType GetInType(QName name);
        InType GetInType(byte value);

        LocalizedString GetString(string stringRef);
    }
}
