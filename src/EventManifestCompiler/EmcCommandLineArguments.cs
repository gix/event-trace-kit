namespace EventManifestCompiler
{
    using System.Collections.Generic;
    using EventTraceKit.EventTracing.Compilation;
    using NOption;

    internal sealed class EmcCommandLineArguments
    {
        public bool ShowHelp { get; set; }
        public bool ShowVersion { get; set; }
        public string DumpMessageTable { get; set; }
        public string DumpEventTemplate { get; set; }
        public string OutputManifest { get; set; }
        public bool Verify { get; set; }
        public Dictionary<string, OptTable> GeneratorOpts { get; set; }

        public CompilationOptions CompilationOptions { get; } = new CompilationOptions();
        public DecompilationOptions DecompilationOptions { get; } = new DecompilationOptions();
    }
}
