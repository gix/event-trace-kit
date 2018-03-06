namespace EventManifestCompiler
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;

    internal interface ICodeGenOptions
    {
        bool UseCustomEventEnabledChecks { get; }
        bool SkipDefines { get; }
        bool GenerateStubs { get; }
        string EtwNamespace { get; }
        string LogNamespace { get; }
        string AlwaysInlineAttribute { get; }
        string NoInlineAttribute { get; }
        string LogCallPrefix { get; }
    }

    [Export(typeof(ICodeGenOptions))]
    internal sealed class EmcOpts : ICodeGenOptions
    {
        public EmcOpts()
        {
            GenerateCode = true;
            GenerateResources = true;
            LogCallPrefix = "EventWrite";
            AlwaysInlineAttribute = "FORCEINLINE";
            NoInlineAttribute = "DECLSPEC_NOINLINE";
            UseCustomEventEnabledChecks = true;
            SkipDefines = false;
            EtwNamespace = "etw";
            CodeGenerator = "cxx";
        }

        public bool ShowHelp { get; set; }
        public bool ShowVersion { get; set; }
        public string DumpMessageTable { get; set; }
        public string DumpEventTemplate { get; set; }
        public string OutputManifest { get; set; }

        public List<string> Inputs { get; set; }
        public string WinmetaPath { get; set; }
        public string SchemaPath { get; set; }
        public string OutputBaseName { get; set; }

        public bool GenerateResources { get; set; }
        public string MessageTableFile { get; set; }
        public string EventTemplateFile { get; set; }
        public string ResourceFile { get; set; }

        public bool GenerateCode { get; set; }
        public string CodeHeaderFile { get; set; }
        public string CodeSourceFile { get; set; }
        public string CodeGenerator { get; set; }
        public string EtwNamespace { get; set; }
        public string LogNamespace { get; set; }
        public string LogCallPrefix { get; set; }
        public bool UseCustomEventEnabledChecks { get; set; }
        public bool SkipDefines { get; set; }
        public bool GenerateStubs { get; set; }
        public string AlwaysInlineAttribute { get; set; }
        public string NoInlineAttribute { get; set; }

        public string CompatibilityLevel { get; set; }

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

            if (CodeHeaderFile == null)
                CodeHeaderFile = baseName + ".h";
            if (CodeSourceFile == null)
                CodeSourceFile = baseName + ".cpp";
            if (MessageTableFile == null)
                MessageTableFile = baseName + ".msg.bin";
            if (EventTemplateFile == null)
                EventTemplateFile = baseName + ".wevt.bin";
            if (ResourceFile == null)
                ResourceFile = baseName + ".rc";
        }
    }
}
