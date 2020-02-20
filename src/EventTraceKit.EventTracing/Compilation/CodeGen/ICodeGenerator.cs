namespace EventTraceKit.EventTracing.Compilation.CodeGen
{
    using System.IO;
    using EventTraceKit.EventTracing.Schema;

    public interface ICodeGeneratorMetadata
    {
        string Name { get; }
    }

    public interface ICodeGenerator
    {
        void Generate(EventManifest manifest, Stream output);
    }
}
