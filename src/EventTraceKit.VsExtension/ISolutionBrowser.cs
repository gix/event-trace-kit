namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;

    public interface ISolutionBrowser
    {
        IEnumerable<ProjectInfo> EnumerateProjects();
        IEnumerable<ProjectInfo> EnumerateStartupProjects();
        IEnumerable<string> FindFiles(string extension);
        IEnumerable<DebugTargetInfo> StartupProjectDTI();
    }

    public sealed class ProjectInfo
    {
        public ProjectInfo(Guid kind, string fullName, string name)
        {
            Kind = kind;
            FullName = fullName;
            Name = name;
        }

        public Guid Kind { get; }
        public string FullName { get; }
        public string Name { get; }
    }

    public sealed class DebugTargetInfo
    {
        public DebugTargetInfo(ProjectInfo project, string command, string commandArguments)
        {
            Project = project;
            Command = command;
            CommandArguments = commandArguments;
        }

        public ProjectInfo Project { get; }
        public string Command { get; }
        public string CommandArguments { get; }
    }
}
