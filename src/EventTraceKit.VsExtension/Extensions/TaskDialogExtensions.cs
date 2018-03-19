namespace EventTraceKit.VsExtension.Extensions
{
    using System.Windows;
    using EventTraceKit.VsExtension.Windows;
    using Microsoft.Windows.TaskDialogs;

    public static class TaskDialogExtensions
    {
        public static TaskDialogResult Show(this TaskDialog dialog, Window owner)
        {
            if (owner == null)
                return dialog.Show();

            dialog.OwnerWindow = owner.GetHandleRef();
            return dialog.Show();
        }
    }
}
