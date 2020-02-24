namespace EventTraceKit.EventTracing.Tests.Compilation.TestSupport
{
    using System;
    using System.IO;

    public sealed class TempFile : IDisposable
    {
        public TempFile()
        {
            FileName = Path.GetTempFileName();
            Stream = File.Open(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        }

        public string FileName { get; }
        public FileStream Stream { get; }

        public void Dispose()
        {
            Stream.Dispose();
            File.Delete(FileName);
        }
    }
}
