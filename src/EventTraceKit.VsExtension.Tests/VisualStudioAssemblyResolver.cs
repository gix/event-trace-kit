namespace EventTraceKit.VsExtension.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using Xunit;

    [CollectionDefinition("VisualStudioAssemblyResolver")]
    public class VisualStudioAssemblyResolverFixture : ICollectionFixture<VisualStudioAssemblyResolver>
    {
    }

    internal class VisualStudioAssemblyResolver : IDisposable
    {
        private readonly string visualStudioInstallationPath;
        private readonly ConcurrentDictionary<string, Assembly> lookupCache = new ConcurrentDictionary<string, Assembly>();
        private readonly List<string> assemblyDirectories;

        public VisualStudioAssemblyResolver()
        {
            visualStudioInstallationPath = FindVisualStudioInstallationPath();

            if (visualStudioInstallationPath != null) {
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
                assemblyDirectories = new List<string> {
                    Path.Combine(visualStudioInstallationPath, "Common7", "IDE", "PrivateAssemblies"),
                    Path.Combine(visualStudioInstallationPath, "Common7", "IDE", "PublicAssemblies")
                };
            }
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return LoadAssembly(new AssemblyName(args.Name));
        }

        private Assembly LoadAssembly(AssemblyName assemblyName)
        {
            return lookupCache.GetOrAdd(assemblyName.Name, x => {
                foreach (var directory in assemblyDirectories) {
                    var assemblyPath = Path.Combine(directory, x + ".dll");
                    var result = LoadAssembly(assemblyPath);
                    if (result != null)
                        return result;
                }

                return null;
            });
        }

        private static Assembly LoadAssembly(string assemblyPath)
        {
            try {
                if (File.Exists(assemblyPath))
                    return Assembly.LoadFrom(assemblyPath);
            } catch {
            }
            return null;
        }

        private static string FindVisualStudioInstallationPath()
        {
            string vswhere = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe";
            if (!File.Exists(vswhere))
                return null;

            var result = Run(vswhere, "-latest -property installationPath");
            if (result.IsTimeout || result.ExitCode != 0)
                result = Run(vswhere, "-latest -prerelease -property installationPath");
            if (result.IsTimeout || result.ExitCode != 0)
                return null;

            string installationPath = result.StandardOutput.Trim();
            if (!Directory.Exists(installationPath))
                return null;

            return installationPath;
        }

        private static ProcessResult Run(string exePath, string arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            return process.Run(TimeSpan.FromMilliseconds(1000));
        }
    }
}
