namespace InstrManifestCompiler.CodeGen
{
    using System.IO;
    using InstrManifestCompiler.EventManifestSchema;

    public interface ICodeGeneratorMetadata
    {
        string Name { get; }
    }

    public interface ICodeGenerator
    {
        void Generate(EventManifest manifest, Stream output);
    }
}
