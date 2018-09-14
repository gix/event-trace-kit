namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using EventManifestFramework;
    using EventManifestFramework.Schema;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Threading;
    using EventTraceKit.VsExtension.Utilities;
    using Microsoft.VisualStudio.Threading;
    using Task = System.Threading.Tasks.Task;

    public class SolutionTraceSettingsContext : ITraceSettingsContext
    {
        private readonly ISolutionBrowser solutionBrowser;
        private readonly FileLruCache<EventManifest> manifestCache;

        public SolutionTraceSettingsContext(ISolutionBrowser solutionBrowser = null)
        {
            this.solutionBrowser = solutionBrowser;

            ProjectsInSolution = FindProjects();
            ManifestsInSolution = ThreadingExtensions.CreateAsyncLazy(FindManifests);

            manifestCache = new FileLruCache<EventManifest>(
                10, path => {
                    var diags = new DiagnosticCollector();

                    var parser = EventManifestParser.CreateWithWinmeta(diags);
                    var manifest = parser.ParseManifest(path);
                    if (manifest == null || diags.Diagnostics.Count != 0)
                        throw new Exception(
                            string.Join("\r\n", diags.Diagnostics.Select(x => x.Message)));

                    return manifest;
                });
        }

        public IReadOnlyList<ProjectInfo> ProjectsInSolution { get; }
        public AsyncLazy<IReadOnlyList<string>> ManifestsInSolution { get; }

        public async Task<EventManifest> GetManifest(string manifestFile)
        {
            if (string.IsNullOrEmpty(manifestFile) || !File.Exists(manifestFile))
                return new EventManifest();

            return await Task.Run(() => manifestCache.Get(manifestFile));
        }

        private IReadOnlyList<ProjectInfo> FindProjects()
        {
            var projects = new List<ProjectInfo>();
            projects.Add(new ProjectInfo(Guid.Empty, null, string.Empty));
            if (solutionBrowser != null) {
                try {
                    var supportedProjects = solutionBrowser.EnumerateProjects()
                        .Where(x => x.Kind.IsSupportedProjectKind());
                    projects.AddRange(supportedProjects.OrderBy(x => x.Name));
                } catch (Exception) {
                }
            }

            return projects;
        }

        private IReadOnlyList<string> FindManifests()
        {
            try {
                if (solutionBrowser != null)
                    return solutionBrowser.FindFiles(".man").ToList();
            } catch (Exception) {
            }

            return new string[0];
        }
    }
}
