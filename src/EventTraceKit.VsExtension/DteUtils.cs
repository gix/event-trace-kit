namespace EventTraceKit.VsExtension
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    public static class DteUtils
    {
        public static ProjectInfo GetProjectInfo(this EnvDTE.Project project)
        {
            return new ProjectInfo(
                new Guid(project.Kind), project.FullName, project.Name);
        }

        public static EnvDTE.Project GetSelectedProject(this IVsMonitorSelection vsMonitorSelection)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IntPtr punkHierarchy = IntPtr.Zero;
            IntPtr punkSelectionContainer = IntPtr.Zero;
            try {
                vsMonitorSelection.GetCurrentSelection(
                    out punkHierarchy, out uint itemId, out var _,
                    out punkSelectionContainer);

                if (punkHierarchy == IntPtr.Zero)
                    return null;

                // Fail if multiple items are selected.
                if (itemId == (uint)VSConstants.VSITEMID.Selection)
                    return null;

                if (Marshal.GetTypedObjectForIUnknown(
                        punkHierarchy, typeof(IVsHierarchy)) is IVsHierarchy hierarchy
                    && hierarchy.GetProperty(
                        VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject,
                        out var project) >= 0) {
                    return project as EnvDTE.Project;
                }

                return null;
            } finally {
                if (punkHierarchy != IntPtr.Zero)
                    Marshal.Release(punkHierarchy);
                if (punkSelectionContainer != IntPtr.Zero)
                    Marshal.Release(punkSelectionContainer);
            }
        }
    }
}
