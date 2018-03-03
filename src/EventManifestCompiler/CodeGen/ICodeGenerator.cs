namespace EventManifestCompiler.CodeGen
{
    using System.IO;
    using EventManifestFramework.Schema;

    public interface ICodeGeneratorMetadata
    {
        string Name { get; }
    }

    public interface ICodeGenerator
    {
        void Generate(EventManifest manifest, Stream output);
    }
}
