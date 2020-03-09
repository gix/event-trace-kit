namespace EventTraceKit.EventTracing.Compilation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using EventTraceKit.EventTracing.Compilation.CodeGen;

    public sealed class CompilationOptions
    {
        public CompilationOptions()
        {
            GenerateResources = true;
        }

        public List<string> Inputs { get; set; }
        public string WinmetaPath { get; set; }
        public string SchemaPath { get; set; }
        public string OutputBaseName { get; set; }

        public bool GenerateResources { get; set; }
        public string MessageTableFile { get; set; }
        public string EventTemplateFile { get; set; }
        public string ResourceFile { get; set; }

        public Version CompatibilityLevel { get; set; }

        public CodeGenOptions CodeGenOptions { get; } = new CodeGenOptions();

        public void InferUnspecifiedOutputFiles(string baseName = null)
        {
            if (baseName == null)
                baseName = OutputBaseName;
            if (baseName == null && Inputs.Count > 0)
                baseName = Path.GetFileNameWithoutExtension(Inputs[0]);
            if (baseName == null)
                return;

            baseName = baseName.TrimStart(' ', '.');
            baseName = baseName.TrimEnd(' ', '.');

            if (CodeGenOptions.CodeHeaderFile == null)
                CodeGenOptions.CodeHeaderFile = baseName + ".h";
            if (MessageTableFile == null)
                MessageTableFile = baseName + ".msg.bin";
            if (EventTemplateFile == null)
                EventTemplateFile = baseName + ".wevt.bin";
            if (ResourceFile == null)
                ResourceFile = baseName + ".rc";
        }
    }

    public sealed class CodeGenOptions
    {
        public bool GenerateCode { get; set; } = true;
        public string CodeHeaderFile { get; set; }
        public string CodeGenerator { get; set; } = "cxx";
        public Func<ICodeGenerator> CodeGeneratorFactory { get; set; }
    }
}
