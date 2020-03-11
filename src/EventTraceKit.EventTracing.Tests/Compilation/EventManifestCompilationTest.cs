namespace EventTraceKit.EventTracing.Tests.Compilation
{
    using System;
    using System.Globalization;
    using System.IO;
    using EventTraceKit.EventTracing.Compilation;
    using EventTraceKit.EventTracing.Compilation.CodeGen;
    using EventTraceKit.EventTracing.Schema;
    using Xunit;

    public class EventManifestCompilationTest
    {
        private readonly CompilationOptions options = new CompilationOptions();
        private readonly DiagnosticsCollector diags = new DiagnosticsCollector();
        private readonly EventManifestCompilation compilation;

        public EventManifestCompilationTest()
        {
            compilation = EventManifestCompilation.Create(diags, options);
            options.CodeGenOptions.CodeGeneratorFactory = () => new StubCodeGenerator();
        }

        private sealed class StubCodeGenerator : ICodeGenerator
        {
            public void Generate(EventManifest manifest, Stream output)
            {
                using (var writer = new StreamWriter(output)) {
                    foreach (var provider in manifest.Providers)
                        writer.Write("{0:B}", provider.Id.Value);
                }
            }
        }

        [Fact]
        public void Emit_succeeds_without_manifests()
        {
            bool success = compilation.Emit();

            Assert.True(success);
            Assert.Empty(diags.Diagnostics);
        }

        [Fact]
        public void EmitCode_fails_without_manifests()
        {
            bool success = compilation.EmitCode();

            Assert.False(success);
            Assert.Single(diags.Diagnostics);
            Assert.Contains("no manifests", diags.Diagnostics[0].FormattedMessage);
        }

        [Fact]
        public void EmitCode_fails_without_CodeGen_manifests()
        {
            compilation.AddResourceGenManifests(new EventManifest());

            bool success = compilation.EmitCode();

            Assert.False(success);
            Assert.Single(diags.Diagnostics);
            Assert.Contains("no manifests", diags.Diagnostics[0].FormattedMessage);
        }

        [Fact]
        public void EmitCode()
        {
            using var headerFile = TemporaryFile.Create();
            options.CodeGenOptions.CodeHeaderFile = headerFile.FilePath;

            compilation.AddCodeGenManifests(new EventManifest {
                Providers = {
                    new Provider("MyProvider", new Guid("{7ec0bfa4-4099-4d8a-ac1b-7a39bcfdbfcb}"), "MyProv")
                }
            });

            bool success = compilation.EmitCode();

            Assert.True(success);
            Assert.Empty(diags.Diagnostics);
            Assert.Equal("{7ec0bfa4-4099-4d8a-ac1b-7a39bcfdbfcb}", File.ReadAllText(headerFile.FilePath));
        }

        [Fact]
        public void EmitCode_ignores_ResGen_manifests()
        {
            using var headerFile = TemporaryFile.Create();
            options.CodeGenOptions.CodeHeaderFile = headerFile.FilePath;

            compilation.AddCodeGenManifests(new EventManifest {
                Providers = {
                    new Provider("MyProvider", new Guid("{7ec0bfa4-4099-4d8a-ac1b-7a39bcfdbfcb}"), "MyProv")
                }
            });
            compilation.AddResourceGenManifests(new EventManifest {
                Providers = {
                    new Provider("OtherProvider", new Guid("{d32b4fb6-2049-4d1e-a746-54996775e30b}"), "OtherProv")
                }
            });

            bool success = compilation.EmitCode();

            Assert.True(success);
            Assert.Empty(diags.Diagnostics);
            Assert.Equal("{7ec0bfa4-4099-4d8a-ac1b-7a39bcfdbfcb}", File.ReadAllText(headerFile.FilePath));
        }

        [Fact]
        public void EmitEventTemplate_fails_without_manifests()
        {
            using var headerFile = TemporaryFile.Create();
            options.EventTemplateFile = headerFile.FilePath;

            bool success = compilation.EmitEventTemplate();

            Assert.False(success);
            Assert.Single(diags.Diagnostics);
            Assert.Contains("no manifests", diags.Diagnostics[0].FormattedMessage);
        }

        [Fact]
        public void EmitEventTemplate()
        {
            using var wevtFile = TemporaryFile.Create();
            options.EventTemplateFile = wevtFile.FilePath;

            compilation.AddResourceGenManifests(new EventManifest {
                Providers = {
                    new Provider("MyProvider", new Guid("{7ec0bfa4-4099-4d8a-ac1b-7a39bcfdbfcb}"), "MyProv")
                }
            });

            bool success = compilation.EmitEventTemplate();

            Assert.True(success);
            Assert.Empty(diags.Diagnostics);
            Assert.NotEqual(0, new FileInfo(wevtFile.FilePath).Length);
        }

        [Fact]
        public void EmitMessageTables()
        {
            using var msgFile = TemporaryFile.Create(".de-DE.tmp");
            options.MessageTableFile = msgFile.FilePath.Replace(".de-DE", "");

            var manifest = new EventManifest();
            manifest.AddResourceSet(new LocalizedResourceSet(CultureInfo.GetCultureInfo("de-DE")));
            compilation.AddManifests(manifest);

            bool success = compilation.EmitMessageTables();

            Assert.True(success);
            Assert.Empty(diags.Diagnostics);
            Assert.NotEqual(0, new FileInfo(msgFile.FilePath).Length);
        }
    }
}
