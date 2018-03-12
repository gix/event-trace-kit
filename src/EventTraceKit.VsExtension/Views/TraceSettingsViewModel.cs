namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using EventManifestFramework;
    using EventManifestFramework.Schema;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Serialization;
    using Microsoft.VisualStudio.Shell;
    using Task = System.Threading.Tasks.Task;

    public interface ITraceSettingsContext
    {
        Window DialogOwner { get; }
        Lazy<IReadOnlyList<ProjectInfo>> ProjectsInSolution { get; }
        Lazy<IReadOnlyList<string>> ManifestsInSolution { get; }
        Task<EventManifest> GetManifest(string manifestFile);
    }

    [SerializedShape(typeof(Settings.Persistence.TraceSettings))]
    public class TraceSettingsViewModel : ObservableModel, ITraceSettingsContext
    {
        private readonly Lazy<IReadOnlyList<ProjectInfo>> projectsInSolution;
        private readonly Lazy<IReadOnlyList<string>> manifestsInSolution;

        private bool? dialogResult;
        private TraceProfileViewModel activeProfile;
        private ICommand newProfileCommand;
        private ICommand copyProfileCommand;
        private ICommand deleteProfileCommand;
        private Window dialogOwner;

        public TraceSettingsViewModel()
        {
            AcceptCommand = new AsyncDelegateCommand(Accept);
            Profiles = new AcqRelObservableCollection<TraceProfileViewModel>(
                x => x.Context = null, x => x.Context = this);
            projectsInSolution = new Lazy<IReadOnlyList<ProjectInfo>>(FindProjects);
            manifestsInSolution = new Lazy<IReadOnlyList<string>>(FindManifests);

            manifestCache = new FileCache<EventManifest>(
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

        Window ITraceSettingsContext.DialogOwner => dialogOwner;
        Lazy<IReadOnlyList<ProjectInfo>> ITraceSettingsContext.ProjectsInSolution => projectsInSolution;
        Lazy<IReadOnlyList<string>> ITraceSettingsContext.ManifestsInSolution => manifestsInSolution;

        private readonly FileCache<EventManifest> manifestCache;

        async Task<EventManifest> ITraceSettingsContext.GetManifest(string manifestFile)
        {
            if (string.IsNullOrEmpty(manifestFile) || !File.Exists(manifestFile))
                return new EventManifest();

            return await Task.Run(() => manifestCache.Get(manifestFile));
        }

        private static EnvDTE.DTE GetDte()
        {
            return ServiceProvider.GlobalProvider.GetService<Microsoft.VisualStudio.Shell.Interop.SDTE, EnvDTE.DTE>();
        }

        private static IReadOnlyList<ProjectInfo> FindProjects()
        {
            var dte = GetDte();
            if (dte == null)
                return new ProjectInfo[0];

            try {
                var projectProvider = new DefaultProjectProvider(GetDte());
                return projectProvider.EnumerateProjects().OrderBy(x => x.Name).ToList();
            } catch (Exception) {
                return new ProjectInfo[0];
            }
        }

        private static IReadOnlyList<string> FindManifests()
        {
            var dte = GetDte();
            if (dte == null)
                return new string[0];

            try {
                var gatherer = new SolutionFileGatherer(dte);
                return gatherer.FindFiles(".man").ToList();
            } catch (Exception) {
                return new string[0];
            }
        }

        public ICommand AcceptCommand { get; }

        public bool? DialogResult
        {
            get => dialogResult;
            set => SetProperty(ref dialogResult, value);
        }

        public ICommand NewProfileCommand =>
            newProfileCommand ?? (newProfileCommand = new AsyncDelegateCommand(NewProfile));

        public ICommand CopyProfileCommand =>
            copyProfileCommand ?? (copyProfileCommand = new AsyncDelegateCommand(CopyPreset, CanCopyPreset));

        public ICommand DeleteProfileCommand =>
            deleteProfileCommand ?? (deleteProfileCommand = new AsyncDelegateCommand(DeletePreset, CanDeletePreset));

        public TraceProfileViewModel ActiveProfile
        {
            get => activeProfile;
            set => SetProperty(ref activeProfile, value);
        }

        public ObservableCollection<TraceProfileViewModel> Profiles { get; }

        public void Attach(Window window)
        {
            dialogOwner = window;
            foreach (var session in Profiles) {
                session.Context = this;
                foreach (var collector in session.Collectors) {
                    collector.Context = this;
                    if (collector is EventCollectorViewModel ec) {
                        foreach (var provider in ec.Providers)
                            provider.Context = this;
                    }
                }
            }
        }

        public void Detach()
        {
            dialogOwner = null;
        }

        private Task NewProfile()
        {
            var newProfile = new TraceProfileViewModel {
                Name = "Unnamed",
                Collectors = {
                    new EventCollectorViewModel {
                        Name = "Default"
                    }
                }
            };

            Profiles.Add(newProfile);
            ActiveProfile = newProfile;
            return Task.CompletedTask;
        }

        private bool CanCopyPreset()
        {
            return ActiveProfile != null;
        }

        private Task CopyPreset()
        {
            if (ActiveProfile != null) {
                var newPreset = ActiveProfile.DeepClone();
                newPreset.Name = "Copy of " + ActiveProfile.Name;
                Profiles.Add(newPreset);
                ActiveProfile = newPreset;
            }

            return Task.CompletedTask;
        }

        private bool CanDeletePreset()
        {
            return ActiveProfile != null;
        }

        private Task DeletePreset()
        {
            if (ActiveProfile != null) {
                Profiles.Remove(ActiveProfile);
                ActiveProfile = null;
            }

            return Task.CompletedTask;
        }

        private Task Accept()
        {
            DialogResult = true;
            return Task.CompletedTask;
        }

        public TraceProfileDescriptor GetDescriptor()
        {
            return ActiveProfile?.CreateDescriptor();
        }

        public Dictionary<EventKey, string> GetEventSymbols()
        {
            return ActiveProfile?.GetEventSymbols() ?? new Dictionary<EventKey, string>();
        }
    }

    public class TraceSettingsDesignTimeModel : TraceSettingsViewModel
    {
        public TraceSettingsDesignTimeModel()
        {
            var provider = new EventProviderViewModel();
            provider.Id = new Guid("9ED16FBE-E642-4D9F-B425-E339FEDC91F8");
            provider.Name = "Design Provider";
            provider.IncludeStackTrace = true;

            var collector = new EventCollectorViewModel();
            collector.Id = new Guid("381F7EC1-AE97-41F9-8C48-727C49D3E210");
            collector.Name = "Design Collector";
            collector.Providers.Add(provider);

            var preset = new TraceProfileViewModel();
            preset.Name = "Design Profile";
            preset.Id = new Guid("7DB6B9B1-9ACF-42C8-B6B1-CEEB6F783689");
            preset.Collectors.Add(collector);
            preset.SelectedCollector = collector;

            Profiles.Add(preset);
            ActiveProfile = preset;
        }
    }
}
