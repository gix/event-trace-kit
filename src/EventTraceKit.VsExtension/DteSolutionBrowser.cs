namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using EnvDTE;
    using EnvDTE80;
    using EventTraceKit.VsExtension.Extensions;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.VCProjectEngine;
    using VSLangProj;

    public class DteSolutionBrowser : ISolutionBrowser
    {
        private readonly DTE dte;

        public DteSolutionBrowser(DTE dte)
        {
            this.dte = dte ?? throw new ArgumentNullException(nameof(dte));
        }

        public IEnumerable<ProjectInfo> EnumerateProjects()
        {
            var result = new List<ProjectInfo>();

            var solution = dte.Solution;
            var projects = solution.Projects;
            foreach (Project project in projects)
                result.Add(project.GetProjectInfo());

            return result;
        }

        public IEnumerable<ProjectInfo> EnumerateStartupProjects()
        {
            foreach (Project project in StartupProjects())
                yield return project.GetProjectInfo();
        }

        public IEnumerable<string> EnumerateFiles()
        {
            var solution = dte.Solution;
            foreach (Project project in Projects(solution)) {
                foreach (var fileName in Files(project))
                    yield return fileName;
            }
        }

        public IEnumerable<string> FindFiles(string extension)
        {
            foreach (var fileName in EnumerateFiles()) {
                if (fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
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
                    yield return item.FileNames[i];
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

        public IEnumerable<DebugTargetInfo> EnumerateDebugInfos()
        {
            foreach (Project project in dte.Solution.Projects) {
                var dti = GetDTI(project);
                if (dti != null)
                    yield return dti;
            }
        }

        private static DebugTargetInfo GetDTI(Project project)
        {
            var kind = new Guid(project.Kind);

            if (kind == VsProjectKinds.CppProjectKindId)
                return GetNativeDTI(project);
            if (kind == VsProjectKinds.LegacyCSharpProjectKindId)
                return GetManagedDTI(project);
            if (kind == VsProjectKinds.CSharpProjectKindId)
                return GetManagedDTI(project);

            return null;
        }

        private static DebugTargetInfo GetNativeDTI(Project project)
        {
            var vcp = (VCProject)project.Object;
            var cfg = vcp.ActiveConfiguration;
            var debugSettings = (VCDebugSettings)cfg.DebugSettings;

            var debuggerFlavor = debugSettings.DebuggerFlavor;
            if (debuggerFlavor != eDebuggerTypes.eLocalDebugger)
                return null;

            string command = cfg.Evaluate(debugSettings.Command);
            string args = cfg.Evaluate(debugSettings.CommandArguments);
            return new DebugTargetInfo(project.GetProjectInfo(), command, args);
        }

        private static DebugTargetInfo GetManagedDTI(Project project)
        {
            try {
                var config = project.ConfigurationManager.ActiveConfiguration;
                var cfgProperties = config.Properties;

                var startAction = cfgProperties.GetValue<prjStartAction>(
                    nameof(ProjectConfigurationProperties.StartAction));

                string command;
                switch (startAction) {
                    case prjStartAction.prjStartActionProject:
                        var rootPath = Path.GetDirectoryName(project.FullName);
                        var outputPath = cfgProperties.GetValue<string>(
                            nameof(ProjectConfigurationProperties.OutputPath));
                        command = Path.Combine(rootPath, outputPath);
                        break;
                    case prjStartAction.prjStartActionProgram:
                        command = cfgProperties.GetValue<string>(
                            nameof(ProjectConfigurationProperties.StartProgram));
                        break;
                    case prjStartAction.prjStartActionURL:
                    case prjStartAction.prjStartActionNone:
                        return null;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var commandArguments = cfgProperties.GetValue<string>(
                    nameof(ProjectConfigurationProperties.StartArguments));

                return new DebugTargetInfo(project.GetProjectInfo(), command, commandArguments);
            } catch (Exception) {
                return null;
            }
        }

        private IEnumerable<Project> StartupProjects()
        {
            var solution = dte.Solution;
            var solutionBuild = solution.SolutionBuild;
            var startupProjects = solutionBuild.StartupProjects;
            if (startupProjects == null)
                yield break;

            foreach (string name in (Array)startupProjects)
                yield return solution.Item(name);
        }

        public IEnumerable<DebugTargetInfo> StartupProjectDTI()
        {
            foreach (var project in StartupProjects())
                yield return GetDTI(project);
        }
    }
}
