namespace EventTraceKit.EventTracing.Tests.Compilation.ResGen
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using EventTraceKit.EventTracing.Compilation.ResGen;
    using EventTraceKit.EventTracing.Internal.Native;
    using EventTraceKit.EventTracing.Tests.Compilation.ResGen.TestCases;
    using EventTraceKit.EventTracing.Tests.Compilation.TestSupport;
    using Xunit;

    public class EventTemplateWriterTest
    {
        [Theory]
        [ResGenTestData(typeof(ResGenTestCases), ".wevt.v3-8.1-compat.bin")]
        public void WriteVersion3Compat(string inputResourceName, Type resourceAnchor, byte[] expectedWevt)
        {
            var manifest = TestHelper.LoadManifest(resourceAnchor, inputResourceName);

            using var tempFile = new TempFile();
            using (var writer = new EventTemplateWriter(tempFile.Stream)) {
                writer.Version = 3;
                writer.UseLegacyTemplateIds = true;
                writer.Write(manifest.Providers);
            }

            SequenceAssert.SequenceEqual(expectedWevt, tempFile.Stream.ReadAllBytes());
        }

        [Theory]
        [ResGenTestData(typeof(ResGenTestCases), ".wevt.v3.bin")]
        public void WriteVersion3(string inputResourceName, Type resourceAnchor, byte[] expectedWevt)
        {
            var manifest = TestHelper.LoadManifest(resourceAnchor, inputResourceName);

            using var tempFile = new TempFile();
            using (var writer = new EventTemplateWriter(tempFile.Stream)) {
                writer.Version = 3;
                writer.Write(manifest.Providers);
            }

            SequenceAssert.SequenceEqual(expectedWevt, tempFile.Stream.ReadAllBytes());
        }

        [Theory]
        [ResGenTestData(typeof(ResGenTestCases), ".wevt.v5.bin")]
        public void WriteVersion5(string inputResourceName, Type resourceAnchor, byte[] expectedWevt)
        {
            var manifest = TestHelper.LoadManifest(resourceAnchor, inputResourceName);

            using var tempFile = new TempFile();
            using (var writer = new EventTemplateWriter(tempFile.Stream))
                writer.Write(manifest.Providers);

            SequenceAssert.SequenceEqual(expectedWevt, tempFile.Stream.ReadAllBytes());
        }

        private static string DumpWevt(Stream stream)
        {
            stream.Position = 0;
            using var writer = new StringWriter();
            using var dumper = new EventTemplateDumper(writer);
            dumper.Verify = false;
            dumper.DumpWevtTemplate(stream);
            return writer.ToString();
        }

        private void ExtractMcResources()
        {
            string inputDir = @"...";
            string outputRoot = Path.Combine(inputDir, "res");

            foreach (var mcExe in new DirectoryInfo(inputDir).EnumerateFiles("mc*.exe")) {
                string version = FileVersionInfo.GetVersionInfo(mcExe.FullName).ProductVersion;

                var outputDir = Path.Combine(outputRoot, $"mc-{version}");
                Directory.CreateDirectory(outputDir);

                using var module = SafeModuleHandle.LoadImageResource(mcExe.FullName);

                foreach (var resourceType in new[] { UnsafeNativeMethods.RT_RCDATA, UnsafeNativeMethods.RT_HTML }) {
                    foreach (var resourceName in module.GetResourceNames(resourceType)) {
                        var resPath = Path.Combine(outputDir, resourceName.Name?.ToLowerInvariant() ?? resourceName.Id.ToString());
                        using var resource = module.OpenResource(resourceType, resourceName);
                        using var output = File.Open(resPath, FileMode.Create);
                        resource.CopyTo(output);
                    }
                }
            }
        }
    }
}
