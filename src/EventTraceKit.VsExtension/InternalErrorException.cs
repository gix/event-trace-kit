namespace EventTraceKit.VsExtension
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class InternalErrorException : InvalidOperationException
    {
        public InternalErrorException()
        {
        }

        public InternalErrorException(string message)
            : base(message)
        {
        }

        public InternalErrorException(Exception innerException)
            : base(innerException?.Message, innerException)
        {
        }

        public InternalErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InternalErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
