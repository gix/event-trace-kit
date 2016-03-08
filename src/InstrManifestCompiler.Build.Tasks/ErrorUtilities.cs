namespace InstrManifestCompiler.Build.Tasks
{
    using System;
    using System.IO;

    internal static class ErrorUtilities
    {
        internal static void ThrowInternalError(string message, params object[] args)
        {
            //throw new InternalErrorException(ResourceUtilities.FormatString(message, args));
            throw new Exception(string.Format(message, args));
        }

        internal static void ThrowInternalError(string message, Exception innerException, params object[] args)
        {
            //throw new InternalErrorException(ResourceUtilities.FormatString(message, args), innerException);
            throw new Exception(string.Format(message, args), innerException);
        }

        internal static void VerifyThrow(bool condition, string unformattedMessage)
        {
            if (!condition)
                ThrowInternalError(unformattedMessage, null, null);
        }

        internal static void VerifyThrowInternalRooted(string value)
        {
            if (!Path.IsPathRooted(value))
                ThrowInternalError("{0} unexpectedly not a rooted path", value);
        }
    }
}
