namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.VisualStudio;

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
            if (Guid.Parse(item.Kind) == VSConstants.GUID_ItemType_PhysicalFile) {
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
    }
}
