namespace EventTraceKit.VsExtension
{
    using System;

    public static class ExceptionUtils
    {
        public static void ThrowInvalidOperationException(string message)
        {
            throw new InvalidOperationException(message);
        }

        public static void ThrowInternalErrorException(string message)
        {
            throw new InternalErrorException(message);
        }
    }
}
