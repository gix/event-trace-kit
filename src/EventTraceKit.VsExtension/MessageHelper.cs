namespace EventTraceKit.VsExtension
{
    using System;
    using System.Diagnostics;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    public static class MessageHelper
    {
        public static void ShowWarningMessage(string message, string title)
        {
            VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public static void ShowErrorMessage(string message, string title = null, Exception exception = null)
        {
            if (exception != null)
                message = $"{message}\n\n{exception.Message}";

            VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                message,
                title ?? "Error",
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        [Conditional("DEBUG")]
        public static void ReportDebugException(Exception exception, string message)
        {
            ReportException(exception, message);
        }

        public static void ReportException(Exception exception, string message)
        {
            VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                $"{message}\n\n{exception.Message}",
                "Error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
