namespace InstrManifestCompiler.Support
{
    using System.IO;
    using System.Linq;

    internal static class WindowsSdkUtils
    {
        public static string FindSdkPath()
        {
            var sdkPaths = new[] {
                @"C:\Program Files (x86)\Windows Kits\8.1\Include\um",
                @"C:\Program Files (x86)\Windows Kits\8.0\Include\um"
            };

            return sdkPaths.FirstOrDefault(Directory.Exists);
        }
    }
}
