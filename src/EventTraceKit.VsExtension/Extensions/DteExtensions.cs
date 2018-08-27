namespace EventTraceKit.VsExtension.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    internal static class DteExtensions
    {
        public static IEnumerable<Project> ProjectsRecursive(this Solution solution)
        {
            return solution.Projects.Cast<Project>().SelectMany(EnumerateProject);
        }

        private static IEnumerable<Project> EnumerateProject(Project project)
        {
            if (project == null)
                yield break;

            if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                foreach (var subProject in GetSolutionFolderProjects(project))
                    yield return subProject;
            else
                yield return project;
        }

        private static IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            return solutionFolder.ProjectItems
                .Cast<ProjectItem>()
                .SelectMany(x => EnumerateProject(x.SubProject));
        }

        public static IEnumerable<string> FilesRecursive(this Solution solution)
        {
            foreach (Project project in solution.ProjectsRecursive()) {
                foreach (var fileName in project.FilesRecursive())
                    yield return fileName;
            }
        }

        public static IEnumerable<string> FilesRecursive(this Project project)
        {
            foreach (ProjectItem item in project.ProjectItems) {
                foreach (var fileName in FilesRecursive(item))
                    yield return fileName;
            }
        }

        public static IEnumerable<string> FilesRecursive(this ProjectItem item)
        {
            if (Guid.Parse(item.Kind) == VSConstants.GUID_ItemType_PhysicalFile) {
                for (short i = 0; i < item.FileCount; ++i)
                    yield return item.FileNames[i];
            }

            if (item.ProjectItems != null) {
                foreach (var fileName in item.ProjectItems.Cast<ProjectItem>().SelectMany(FilesRecursive))
                    yield return fileName;
            }
        }

        public static Project GetProjectByName(this Solution solution, string uniqueProjectName)
        {
            try {
                return solution.Item(uniqueProjectName);
            } catch (ArgumentException) {
                // Contrary to the documentation, Solution.Item() does not find
                // projects contained in solution folders.
                return solution.ProjectsRecursive().FirstOrDefault(x => x.UniqueName == uniqueProjectName);
            }
        }

        public static bool IsSupported(this Project project)
        {
            return project.Kind != null && IsSupportedProjectKind(new Guid(project.Kind));
        }

        public static bool IsSupportedProjectKind(this Guid kind)
        {
            return
                kind == VsProjectKinds.CppProjectKindId ||
                kind == VsProjectKinds.LegacyCSharpProjectKindId ||
                kind == VsProjectKinds.CSharpProjectKindId ||
                kind == VsProjectKinds.VisualBasicNetProjectKindId;
        }

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
                    out punkHierarchy, out uint itemId, out _,
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
                    return project as Project;
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
