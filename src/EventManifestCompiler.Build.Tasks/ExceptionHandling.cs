namespace EventManifestCompiler.Build.Tasks
{
    using System;
    using System.IO;
    using System.Security;

    internal static class ExceptionHandling
    {
        internal static bool NotExpectedException(Exception ex)
        {
            return
                !(ex is UnauthorizedAccessException) &&
                !(ex is NotSupportedException) &&
                (!(ex is ArgumentException) || ex is ArgumentNullException) &&
                !(ex is SecurityException) &&
                !(ex is IOException);
        }
    }
}
