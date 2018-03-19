namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;

    public interface IVsSolutionManager
    {
        event EventHandler<StartupProjectChangedEventArgs> StartupProjectChanged;
        IEnumerable<ProjectInfo> EnumerateStartupProjects();
    }

    public sealed class StartupProjectChangedEventArgs : EventArgs
    {
        public StartupProjectChangedEventArgs(IReadOnlyList<ProjectInfo> projects)
        {
            Projects = projects;
        }

        public IReadOnlyList<ProjectInfo> Projects { get; }
    }
}
