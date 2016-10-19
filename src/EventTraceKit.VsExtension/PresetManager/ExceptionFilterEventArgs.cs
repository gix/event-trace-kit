namespace EventTraceKit.VsExtension
{
    using System;

    public class ExceptionFilterEventArgs : EventArgs
    {
        public ExceptionFilterEventArgs(Exception exception, string message)
        {
            Exception = exception;
            Message = message;
        }

        public Exception Exception { get; }

        public string Message { get; }
    }
}
