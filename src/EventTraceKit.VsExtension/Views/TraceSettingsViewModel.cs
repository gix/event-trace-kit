namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using AutoMapper;
    using EventManifestFramework;
    using EventManifestFramework.Schema;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Serialization;
    using EventTraceKit.VsExtension.Settings;
    using EventTraceKit.VsExtension.Settings.Persistence;
    using Task = System.Threading.Tasks.Task;
    using TaskEx = EventTraceKit.VsExtension.Threading.TaskExtensions;

    public interface ITraceSettingsContext
    {
        AsyncLazy<IReadOnlyList<ProjectInfo>> ProjectsInSolution { get; }
        AsyncLazy<IReadOnlyList<string>> ManifestsInSolution { get; }
        Task<EventManifest> GetManifest(string manifestFile);
    }

    public class TraceSettingsContext : ITraceSettingsContext
    {
        private readonly ISolutionBrowser solutionBrowser;
        private readonly FileCache<EventManifest> manifestCache;

        public TraceSettingsContext(ISolutionBrowser solutionBrowser = null)
        {
            this.solutionBrowser = solutionBrowser;

            ProjectsInSolution = new AsyncLazy<IReadOnlyList<ProjectInfo>>(() => FindProjects());
            ManifestsInSolution = new AsyncLazy<IReadOnlyList<string>>(() => FindManifests());

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

        public AsyncLazy<IReadOnlyList<ProjectInfo>> ProjectsInSolution { get; }
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
            try {
                if (solutionBrowser != null)
                    projects.AddRange(solutionBrowser.EnumerateProjects().OrderBy(x => x.Name));
            } catch (Exception) {
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

    [SerializedShape(typeof(TraceSettings))]
    public class TraceSettingsViewModel : ObservableModel
    {
        private readonly ISettingsStore settingsStore;
        private readonly IMapper mapper = SettingsSerializer.CreateMapper();
        private ITraceSettingsContext context;

        private bool? dialogResult;
        private ICommand saveCommand;
        private ICommand restoreCommand;
        private ICommand newProfileCommand;
        private ICommand copyProfileCommand;
        private ICommand deleteProfileCommand;
        private TraceProfileViewModel activeProfile;

        public TraceSettingsViewModel(
            ISettingsStore settingsStore = null,
            ISolutionBrowser solutionBrowser = null)
        {
            this.settingsStore = settingsStore;

            context = new TraceSettingsContext(solutionBrowser);

            Profiles = new AcqRelObservableCollection<TraceProfileViewModel>(
                x => x.Context = null, x => x.Context = context);

            if (settingsStore != null) {
                LoadFromSettings();
                Title = $"Trace Settings ({settingsStore.Name})";
            } else {
                Title = "Trace Settings";
            }
        }

        public string Title { get; }

        public bool? DialogResult
        {
            get => dialogResult;
            set => SetProperty(ref dialogResult, value);
        }

        public ICommand SaveCommand =>
            saveCommand ?? (saveCommand = new AsyncDelegateCommand(Save));

        public ICommand RestoreCommand =>
            restoreCommand ?? (restoreCommand = new AsyncDelegateCommand(Restore, () => settingsStore != null));

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

        private async Task Save()
        {
            var settings = settingsStore.GetValue(SettingsKeys.Tracing) ?? new TraceSettings();
            settings.Profiles.SetRange(GetProfiles());
            settings.ActiveProfile = ActiveProfile?.Name;
            settingsStore.SetValue(SettingsKeys.Tracing, settings);

            await TaskEx.RunWithProgress(() => settingsStore.Save(), "Saving");
            DialogResult = true;
        }

        private Task Restore()
        {
            LoadFromSettings();
            return Task.CompletedTask;
        }

        private void LoadFromSettings()
        {
            var traceSettings = settingsStore.GetValue(SettingsKeys.Tracing);
            if (traceSettings != null) {
                Profiles.SetRange(traceSettings.Profiles.Select(
                    x => mapper.Map(x, new TraceProfileViewModel())));
                ActiveProfile = Profiles.FirstOrDefault(x => x.Name == traceSettings.ActiveProfile);
            }
        }

        public TraceProfileDescriptor GetDescriptor()
        {
            return ActiveProfile?.CreateDescriptor();
        }

        public Dictionary<EventKey, string> GetEventSymbols()
        {
            return ActiveProfile?.GetEventSymbols() ?? new Dictionary<EventKey, string>();
        }

        public IEnumerable<TraceProfile> GetProfiles()
        {
            return Profiles.Select(x => mapper.Map<TraceProfile>(x));
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
