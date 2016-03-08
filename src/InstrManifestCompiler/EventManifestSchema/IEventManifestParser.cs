namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;

    [ContractClass(typeof(IEventManifestParserContract))]
    public interface IEventManifestParser
    {
        void AddMetadata(IEventManifestMetadata metadata);
        EventManifest ParseManifest(Stream input, string inputUri = null);
        IEventManifestMetadata ParseWinmeta(Stream input, string inputUri = null);
        IEventManifestMetadata ParseMetadata(Stream input, string inputUri = null);
    }

    /// <summary>Contract for <see cref="IEventManifestParser"/>.</summary>
    [ContractClassFor(typeof(IEventManifestParser))]
    internal abstract class IEventManifestParserContract : IEventManifestParser
    {
        void IEventManifestParser.AddMetadata(IEventManifestMetadata metadata)
        {
            Contract.Requires<ArgumentNullException>(metadata != null);
        }

        EventManifest IEventManifestParser.ParseManifest(Stream input, string inputUri)
        {
            Contract.Requires<ArgumentNullException>(input != null);
            return default(EventManifest);
        }

        IEventManifestMetadata IEventManifestParser.ParseWinmeta(Stream input, string inputUri)
        {
            Contract.Requires<ArgumentNullException>(input != null);
            return default(IEventManifestMetadata);
        }

        IEventManifestMetadata IEventManifestParser.ParseMetadata(Stream input, string inputUri)
        {
            Contract.Requires<ArgumentNullException>(input != null);
            return default(IEventManifestMetadata);
        }
    }
}
