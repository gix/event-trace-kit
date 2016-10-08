namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

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

        public static InvalidEnumArgumentException InvalidEnumArgumentException<T>(
            T value, string argumentName) where T : struct
        {
            return new InvalidEnumArgumentException(
                argumentName, Convert.ToInt32(value, CultureInfo.CurrentCulture), typeof(T));
        }
    }
}
