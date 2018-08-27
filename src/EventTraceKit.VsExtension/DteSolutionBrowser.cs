namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using EnvDTE;
    using EventTraceKit.VsExtension.Extensions;
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
            foreach (Project project in dte.Solution.ProjectsRecursive())
                yield return project.GetProjectInfo();
        }

        public IEnumerable<ProjectInfo> EnumerateStartupProjects()
        {
            foreach (Project project in StartupProjects())
                yield return project.GetProjectInfo();
        }

        public IEnumerable<string> FindFiles(string extension)
        {
            foreach (var fileName in dte.Solution.FilesRecursive()) {
                if (fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    yield return fileName;
            }
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

            foreach (string name in (Array)startupProjects) {
                Project project = solution.GetProjectByName(name);
                if (project != null)
                    yield return project;
            }
        }

        public IEnumerable<DebugTargetInfo> StartupProjectDTI()
        {
            foreach (var project in StartupProjects())
                yield return GetDTI(project);
        }
    }
}
