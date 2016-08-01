namespace EventTraceKit.VsExtension
{
    using System.Collections.Generic;
    using System.Linq;
    using EnvDTE;
    using EnvDTE80;

    public interface ISolutionFileGatherer
    {
        IEnumerable<string> GetFiles();
    }

    public class SolutionFileGatherer : ISolutionFileGatherer
    {
        private readonly DTE dte;

        public SolutionFileGatherer(DTE dte)
        {
            this.dte = dte;
        }

        public IEnumerable<string> GetFiles()
        {
            var solution = dte.Solution;
            foreach (Project project in Projects(solution)) {
                foreach (var fileName in Files(project))
                    yield return fileName;
            }
        }

        public static IEnumerable<Project> Projects(Solution solution)
        {
            return solution.Projects.Cast<Project>().SelectMany(EnumerateProject);
        }

        public IEnumerable<string> Files(Project project)
        {
            foreach (ProjectItem item in project.ProjectItems) {
                foreach (var fileName in Files(item))
                    yield return fileName;
            }
        }

        public IEnumerable<string> Files(ProjectItem item)
        {
            if (item.Kind == ProjectItemKind.PhysicalFile) {
                for (short i = 0; i < item.FileCount; ++i)
                    yield return item.FileNames[i] + " " + item.Kind;
            }

            if (item.ProjectItems != null) {
                foreach (var fileName in item.ProjectItems.Cast<ProjectItem>().SelectMany(Files))
                    yield return fileName;
            }
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
        private class ProjectItemKind
        {
            public const string PhysicalFile = "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}";
            public const string PhysicalFolder = "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}";
            public const string VirtualFolder = "{6BB5F8F0-4483-11D3-8BCF-00C04F8EC28C}";
            public const string Subproject = "{EA6618E8-6E24-4528-94BE-6889FE16485C";
        }
    }
}
