namespace EventTraceKit.VsExtension.Extensions
{
    using System.Windows;
    using System.Windows.Interop;
    using Microsoft.Windows.TaskDialogs;

    public static class TaskDialogExtensions
    {
        public static TaskDialogResult Show(this TaskDialog dialog, Window owner)
        {
            var wih = new WindowInteropHelper(owner);
            dialog.OwnerWindow = wih.Handle;
            return dialog.Show();
        }
    }
}
