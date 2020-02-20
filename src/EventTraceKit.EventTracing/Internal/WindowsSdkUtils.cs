namespace EventTraceKit.EventTracing.Internal
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal static class WindowsSdkUtils
    {
        public static string FindSdkPath()
        {
            return EnumerateSdkIncludePaths().FirstOrDefault(Directory.Exists);
        }

        public static IEnumerable<string> EnumerateSdkIncludePaths()
        {
            var winKits10 = new DirectoryInfo(@"C:\Program Files (x86)\Windows Kits\10\Include");
            foreach (var dir in winKits10.EnumerateDirectories().Reverse()) {
                var path = Path.Combine(dir.FullName, "um");
                if (Directory.Exists(path))
                    yield return path;
            }

            yield return @"C:\Program Files (x86)\Windows Kits\8.1\Include\um";
            yield return @"C:\Program Files (x86)\Windows Kits\8.0\Include\um";
        }
    }
}
