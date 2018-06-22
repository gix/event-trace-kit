namespace EventTraceKit.VsExtension.Extensions
{
    using System;
    using System.Runtime.InteropServices;
    using EnvDTE;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    internal static class DteExtensions
    {
        public static ProjectInfo GetProjectInfo(this Project project)
        {
            return new ProjectInfo(
                new Guid(project.Kind), project.FullName, project.Name);
        }

        public static Project GetSelectedProject(this IVsMonitorSelection vsMonitorSelection)
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

        public static T GetValue<T>(this Properties properties, string name)
        {
            try {
                var property = properties.Item(name);
                if (property?.Value is T val) {
                    return val;
                }

                return default;
            } catch (Exception) {
                return default;
            }
        }

        public static bool TryGetProperty<T>(this Properties properties, string name, out T value)
        {
            try {
                var property = properties.Item(name);
                if (property?.Value is T val) {
                    value = val;
                    return true;
                }

                value = default;
                return false;
            } catch (Exception) {
                value = default;
                return false;
            }
        }
    }
}
