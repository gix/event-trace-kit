namespace EventManifestFramework
{
    using System.IO;
    using EventManifestFramework.Schema;

    public interface IEventManifestParser
    {
        void AddMetadata(IEventManifestMetadata metadata);
        EventManifest ParseManifest(Stream input, string inputUri = null);
        IEventManifestMetadata ParseWinmeta(Stream input, string inputUri = null);
        IEventManifestMetadata ParseMetadata(Stream input, string inputUri = null);
    }
}
