namespace EventTraceKit.VsExtension.Tests
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Threading;

    internal static class ProcessExtensions
    {
        public static ProcessResult Run(this Process process, TimeSpan? timeout = null)
        {
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            var outputReadTask = process.StandardOutput.ReadToEndAsync();
            var errorReadTask = process.StandardError.ReadToEndAsync();

            int timeoutMillis = timeout != null ? (int)timeout.Value.TotalMilliseconds : -1;

            if (!process.WaitForExit(timeoutMillis)) {
                process.Kill();
                outputReadTask.Forget();
                errorReadTask.Forget();
                return ProcessResult.Timeout;
            }

            // Wait until all processing has been completed, this includes any
            // asynchronous events for redirected output.
            process.WaitForExit();

            Task.WaitAll(outputReadTask, errorReadTask);
            return new ProcessResult(process.ExitCode, outputReadTask.Result, errorReadTask.Result);
        }
    }

    internal sealed class ProcessResult
    {
        public ProcessResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        private ProcessResult()
        {
            IsTimeout = true;
        }

        public static ProcessResult Timeout => new ProcessResult();

        public bool IsTimeout { get; }
        public int? ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }
    }
}
