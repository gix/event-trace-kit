namespace EventTraceKit.VsExtension.Views
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using EventTraceKit.EventTracing.Schema;
    using Microsoft.VisualStudio.Threading;

    public interface ITraceSettingsContext
    {
        IReadOnlyList<ProjectInfo> ProjectsInSolution { get; }
        AsyncLazy<IReadOnlyList<string>> ManifestsInSolution { get; }
        Task<EventManifest> GetManifest(string manifestFile);
    }
}
