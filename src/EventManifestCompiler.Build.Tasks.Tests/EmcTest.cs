namespace EventManifestCompiler.Build.Tasks.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Xunit;

    public class EmcTest
    {
        private StubBuildEngine buildEngine = new StubBuildEngine();

        [Fact]
        public void CommandLineArgs()
        {
            var emc = new Emc();
            emc.BuildEngine = buildEngine;
            emc.Source = new TaskItem("manifest.man");
            emc.EventmanPath = "eventman.xsd";
            emc.WinmetaPath = "winmeta.xml";
            emc.OutputBaseName = "t";
            emc.HeaderFile = "out.h";
            emc.MessageTableFile = "mt.bin";
            emc.EventTemplateFile = "wevt.bin";
            emc.ResourceFile = "res.rc";
            emc.GenerateResources = true;
            emc.GenerateCode = true;
            emc.CodeGenerator = "cxx";
            emc.LogNamespace = "trace";
            emc.EtwNamespace = "etw";
            emc.UseLoggingPrefix = true;
            emc.LoggingPrefix = "Write";
            emc.GenerateDefines = true;
            emc.Extensions = new[] { "ext1.dll", "ext2.dll" };
            emc.ResourceGenOnlySources = new[] {
                new TaskItem("extra1.man"),
                new TaskItem("extra2.man")
            };
            emc.AdditionalOptions = "-cstatic";

            // Ignore the result of Execute as it will always fail. But the
            // executed command line will be logged. That's all we need.
            emc.Execute();
            var actualArgs = GetExecutedCommandLineArgs();

            var expectedArgs = new HashSet<string>() {
                "-out:t",
                "-header-file:out.h",
                "-msg-file:mt.bin",
                "-wevt-file:wevt.bin",
                "-rc-file:res.rc",
                "-schema:eventman.xsd",
                "-winmeta:winmeta.xml",
                "-resgen-manifest:extra1.man",
                "-resgen-manifest:extra2.man",
                "-res",
                "-code",
                "-ext:ext1.dll",
                "-ext:ext2.dll",
                "-generator:cxx",
                "-clog-ns:trace",
                "-cetw-ns:etw",
                "-cuse-prefix",
                "-cprefix:Write",
                "-cdefines",
                "-cstatic",
                "manifest.man",
            };
            Assert.EndsWith("emc.exe", actualArgs.First());
            Assert.Equal(expectedArgs, new HashSet<string>(actualArgs.Skip(1)));
        }

        private string GetExecutedCommandLine()
        {
            var eventArgs = buildEngine.LoggedMessageEvents.OfType<TaskCommandLineEventArgs>().FirstOrDefault();
            if (eventArgs is null)
                throw new InvalidOperationException("No TaskCommandLineEventArgs logged");

            return eventArgs.CommandLine;
        }

        private List<string> GetExecutedCommandLineArgs()
        {
            var commandLine = GetExecutedCommandLine();
            int idx = commandLine.IndexOf("emc.exe", StringComparison.OrdinalIgnoreCase);
            if (idx != -1) {
                idx += "emc.exe".Length;
                commandLine = '"' + commandLine.Substring(0, idx) + '"' + commandLine.Substring(idx);
            }

            return CommandLineUtils.EnumerateCommandLineArgs(commandLine).ToList();
        }

        private class StubBuildEngine : IBuildEngine
        {
            public bool ContinueOnError { get; set; }
            public int LineNumberOfTaskNode { get; set; }
            public int ColumnNumberOfTaskNode { get; set; }
            public string ProjectFileOfTaskNode { get; set; }

            public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
            {
                return true;
            }

            public List<CustomBuildEventArgs> LoggedCustomEvents { get; } = new List<CustomBuildEventArgs>();
            public List<BuildErrorEventArgs> LoggedErrorEvents { get; } = new List<BuildErrorEventArgs>();
            public List<BuildMessageEventArgs> LoggedMessageEvents { get; } = new List<BuildMessageEventArgs>();
            public List<BuildWarningEventArgs> LoggedWarningEvents { get; } = new List<BuildWarningEventArgs>();

            public void LogCustomEvent(CustomBuildEventArgs e)
            {
                LoggedCustomEvents.Add(e);
            }

            public void LogErrorEvent(BuildErrorEventArgs e)
            {
                LoggedErrorEvents.Add(e);
            }

            public void LogMessageEvent(BuildMessageEventArgs e)
            {
                LoggedMessageEvents.Add(e);
            }

            public void LogWarningEvent(BuildWarningEventArgs e)
            {
                LoggedWarningEvents.Add(e);
            }
        }
    }
}
