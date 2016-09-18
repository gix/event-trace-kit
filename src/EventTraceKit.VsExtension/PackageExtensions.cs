namespace EventTraceKit.VsExtension
{
    using System;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    public static class PackageExtensions
    {
        public static void ShowToolWindow<T>(this Package package)
            where T : ToolWindowPane
        {
            ToolWindowPane window = package.FindToolWindow(typeof(T), 0, true);
            if (window?.Frame == null)
                throw new NotSupportedException("Cannot create tool window");

            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
