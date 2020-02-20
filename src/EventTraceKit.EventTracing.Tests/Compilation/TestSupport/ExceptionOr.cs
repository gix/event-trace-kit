namespace EventTraceKit.EventTracing.Tests.Compilation.TestSupport
{
    using System;

    public struct ExceptionOr<T>
    {
        private readonly T value;

        public ExceptionOr(T value)
            : this()
        {
            this.value = value;
        }

        public ExceptionOr(Exception exception)
            : this()
        {
            Exception = exception;
        }

        public static implicit operator ExceptionOr<T>(T value)
        {
            return new ExceptionOr<T>(value);
        }

        public static implicit operator ExceptionOr<T>(Exception exception)
        {
            return new ExceptionOr<T>(exception);
        }

        public bool HasValue => Exception == null;
        public T Value => Exception != null ? throw Exception : value;
        public Exception Exception { get; }
    }
}
