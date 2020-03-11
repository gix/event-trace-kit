namespace EventTraceKit.EventTracing.Tests.Compilation
{
    using System;
    using System.IO;

    public sealed class TemporaryFile : IDisposable
    {
        public string FilePath { get; }

        public static TemporaryFile Create(string extension = ".tmp")
        {
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentNullException(nameof(extension));

            if (extension[0] != '.')
                extension = '.' + extension;

            string path;
            try {
                path = Path.Combine(Path.GetTempPath(), "tmp" + Guid.NewGuid().ToString("N") + extension);
                if (File.Exists(path))
                    throw new Exception("Guid should be unique");

                File.WriteAllText(path, string.Empty);
            } catch (Exception ex) {
                throw new IOException($"Failed to create temp file: {ex.Message}", ex);
            }

            return new TemporaryFile(path);
        }

        public TemporaryFile(string filePath)
        {
            FilePath = filePath;
        }

        public void Dispose()
        {
            try {
                File.Delete(FilePath);
            } catch (DirectoryNotFoundException) {
            }
        }
    }
}
