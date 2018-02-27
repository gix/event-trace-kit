namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics.Contracts;
    using InstrManifestCompiler.EventManifestSchema.Base;

    [ContractClass(typeof(IEventManifestMetadataContract))]
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

    [ContractClassFor(typeof(IEventManifestMetadata))]
    internal abstract class IEventManifestMetadataContract : IEventManifestMetadata
    {
        ChannelCollection IEventManifestMetadata.Channels
        {
            get
            {
                Contract.Ensures(Contract.Result<ChannelCollection>() != null);
                return default(ChannelCollection);
            }
        }

        LevelCollection IEventManifestMetadata.Levels
        {
            get
            {
                Contract.Ensures(Contract.Result<LevelCollection>() != null);
                return default(LevelCollection);
            }
        }

        OpcodeCollection IEventManifestMetadata.Opcodes
        {
            get
            {
                Contract.Ensures(Contract.Result<OpcodeCollection>() != null);
                return default(OpcodeCollection);
            }
        }

        TaskCollection IEventManifestMetadata.Tasks
        {
            get
            {
                Contract.Ensures(Contract.Result<TaskCollection>() != null);
                return default(TaskCollection);
            }
        }

        KeywordCollection IEventManifestMetadata.Keywords
        {
            get
            {
                Contract.Ensures(Contract.Result<KeywordCollection>() != null);
                return default(KeywordCollection);
            }
        }

        XmlType IEventManifestMetadata.GetXmlType(QName name)
        {
            return default(XmlType);
        }

        XmlType IEventManifestMetadata.GetXmlType(byte value)
        {
            return default(XmlType);
        }

        InType IEventManifestMetadata.GetInType(QName name)
        {
            return default(InType);
        }

        InType IEventManifestMetadata.GetInType(byte value)
        {
            return default(InType);
        }

        LocalizedString IEventManifestMetadata.GetString(string stringRef)
        {
            Contract.Requires<ArgumentNullException>(stringRef != null);
            return default(LocalizedString);
        }
    }
}
