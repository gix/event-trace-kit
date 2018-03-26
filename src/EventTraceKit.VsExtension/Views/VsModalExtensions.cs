namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;
    using EventTraceKit.VsExtension.Extensions;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.Win32;
    using Microsoft.Windows.TaskDialogs;

    public static class VsModalExtensions
    {
        public static TaskDialogResult ShowModal(this TaskDialog dialog)
        {
            dialog.OwnerWindow = new HandleRef(null, GetDialogOwnerHwnd());
            return dialog.Show();
        }

        public static bool? ShowModal(this CommonDialog dialog)
        {
            var owner = GetDialogOwnerHwnd();
            if (owner != IntPtr.Zero && HwndSource.FromHwnd(owner)?.RootVisual is Window ownerWindow)
                return dialog.ShowDialog(ownerWindow);

            return dialog.ShowDialog();
        }

        public static IntPtr GetDialogOwnerHwnd()
        {
            var shell = ServiceProvider.GlobalProvider?.GetService<SVsUIShell, IVsUIShell>();
            if (shell != null && shell.GetDialogOwnerHwnd(out var owner) >= 0)
                return owner;

            return IntPtr.Zero;
        }
    }
}
