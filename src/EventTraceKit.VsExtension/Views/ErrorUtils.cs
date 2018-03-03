namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;
    using Microsoft.Windows.TaskDialogs;

    public static class ErrorUtils
    {
        [Conditional("DEBUG")]
        public static void ReportDebugException(Window ownerWindow, Exception exception, string message)
        {
            ReportException(ownerWindow, exception, message);
        }

        public static void ReportException(Window ownerWindow, Exception exception, string message)
        {
            ReportException(ownerWindow.GetHandle(), exception, message);
        }

        public static HandleRef GetHandle(this Window window)
        {
            if (window != null)
                return new HandleRef(window, new WindowInteropHelper(window).Handle);
            return new HandleRef(null, IntPtr.Zero);
        }

        public static void ReportException(HandleRef ownerWindow, Exception exception, string message)
        {
            var dialog = new TaskDialog {
                Caption = "Error",
                Instruction = message,
                StartupLocation = TaskDialogStartupLocation.CenterOwner,
                CommonButtons = TaskDialogButtons.Ok,
                Icon = TaskDialogStandardIcon.Error,
                Content = exception.Message,
                IsCancelable = true,
                OwnerWindow = ownerWindow
            };
            dialog.Show();
        }
    }
}
