namespace EventTraceKit.VsExtension.Extensions
{
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;
    using Microsoft.Windows.TaskDialogs;

    public static class TaskDialogExtensions
    {
        public static TaskDialogResult Show(this TaskDialog dialog, Window owner)
        {
            if (owner == null)
                return dialog.Show();

            var wih = new WindowInteropHelper(owner);
            dialog.OwnerWindow = new HandleRef(owner, wih.Handle);
            return dialog.Show();
        }
    }
}
