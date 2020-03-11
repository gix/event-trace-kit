namespace EventTraceKit.EventTracing.Support
{
    using System;
    using System.IO;

    internal static class FileUtilities
    {
        public static FileStream TryCreateFile(
            IDiagnostics diags, string fileName, int? bufferSize = null, FileOptions? options = null)
        {
            try {
                string fullPath = Path.GetFullPath(fileName);
                EnsureDirectoryExists(fullPath);

                if (bufferSize.HasValue && options.HasValue)
                    return File.Create(fileName, bufferSize.Value, options.Value);

                return File.Create(fileName);
            } catch (Exception ex) {
                diags.ReportError("Failed to create '{0}': {1}\n{2}", fileName, ex.Message, ex.StackTrace);
                return null;
            }
        }

        public static void EnsureDirectoryExists(string filePath)
        {
            string directoryName = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryName))
                Directory.CreateDirectory(directoryName);
        }
    }
}
