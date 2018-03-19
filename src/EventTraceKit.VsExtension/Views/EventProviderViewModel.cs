namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;
    using EventManifestFramework.Schema;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Serialization;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.Win32;
    using Microsoft.Windows.TaskDialogs;
    using Task = System.Threading.Tasks.Task;

    [SerializedShape(typeof(Settings.Persistence.EventProvider))]
    public class EventProviderViewModel : ObservableModel
    {
        private Guid id;
        private string manifest;
        private string name;
        private bool isEnabled;

        private byte level = 0xFF;
        private ulong matchAnyKeyword;
        private ulong matchAllKeyword;

        private bool includeSecurityId;
        private bool includeTerminalSessionId;
        private bool includeStackTrace;

        private bool filterExecutableNames;
        private string executableNames;
        private bool filterProcessIds;
        private string processIds;
        private bool filterEventIds;
        private string eventIds;
        private bool eventIdsFilterIn = true;
        private string startupProject;

        private bool isSelected;

        public EventProviderViewModel()
        {
            ToggleSelectedEventsCommand = new AsyncDelegateCommand<IList>(ToggleSelectedEvents);
            BrowseManifestCommand = new AsyncDelegateCommand(BrowseManifest);
        }

        public EventProviderViewModel(Guid id, string name)
            : this()
        {
            this.id = id;
            this.name = NewName = name;
        }

        public EventProviderViewModel(
            Guid id, string name, string manifest)
            : this(id, name)
        {
            this.manifest = manifest;
        }

        public EventProviderViewModel(EventProviderViewModel source)
            : this()
        {
            Copy(source, this);
        }

        public ITraceSettingsContext Context { get; set; }

        public IEnumerable<string> SuggestedManifests => Context.ManifestsInSolution.Value;
        public IEnumerable<ProjectInfo> SuggestedProjects => Context.ProjectsInSolution.Value;

        public ICommand BrowseManifestCommand { get; }
        public ICommand ToggleSelectedEventsCommand { get; }

        public Func<Task<IReadOnlyList<Keyword>>> DefinedKeywordsSource =>
            async () => await GetSchemaListAsync(x => x.Keywords);

        public Func<Task<IReadOnlyList<Event>>> DefinedEventsSource =>
            async () => await GetSchemaListAsync(x => x.Events);

        public Func<Task<IReadOnlyList<Level>>> DefinedLevelsSource =>
            async () => await GetSchemaListAsync(x => x.Levels);

        public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name : Id.ToString();

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        public async Task<Provider> GetSchemaProviderAsync()
        {
            var eventManifest = await Context.GetManifest(Manifest);
            return eventManifest.Providers.FirstOrDefault(x => x.Id == Id);
        }

        private async Task<IReadOnlyList<T>> GetSchemaListAsync<T>(
            Func<Provider, IReadOnlyList<T>> selector)
        {
            var p = await GetSchemaProviderAsync();
            return (p != null ? selector(p) : null) ?? new T[0];
        }

        public Guid Id
        {
            get => id;
            set
            {
                if (SetProperty(ref id, value))
                    RaisePropertyChanged(nameof(DisplayName));
            }
        }

        public string Manifest
        {
            get => manifest;
            set => SetProperty(ref manifest, value);
        }

        public string Name
        {
            get => name;
            set
            {
                if (SetProperty(ref name, value))
                    RaisePropertyChanged(nameof(DisplayName));
                NewName = value;
            }
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => SetProperty(ref isEnabled, value);
        }

        public byte Level
        {
            get => level;
            set => SetProperty(ref level, value);
        }

        public ulong MatchAnyKeyword
        {
            get => matchAnyKeyword;
            set => SetProperty(ref matchAnyKeyword, value);
        }

        public ulong MatchAllKeyword
        {
            get => matchAllKeyword;
            set => SetProperty(ref matchAllKeyword, value);
        }

        public bool IncludeSecurityId
        {
            get => includeSecurityId;
            set => SetProperty(ref includeSecurityId, value);
        }

        public bool IncludeTerminalSessionId
        {
            get => includeTerminalSessionId;
            set => SetProperty(ref includeTerminalSessionId, value);
        }

        public bool IncludeStackTrace
        {
            get => includeStackTrace;
            set => SetProperty(ref includeStackTrace, value);
        }

        public string StartupProject
        {
            get => startupProject;
            set => SetProperty(ref startupProject, value);
        }

        public bool FilterExecutableNames
        {
            get => filterExecutableNames;
            set => SetProperty(ref filterExecutableNames, value);
        }

        public string ExecutableNames
        {
            get => executableNames;
            set => SetProperty(ref executableNames, value);
        }

        public bool FilterProcessIds
        {
            get => filterProcessIds;
            set => SetProperty(ref filterProcessIds, value);
        }

        public string ProcessIds
        {
            get => processIds;
            set => SetProperty(ref processIds, value);
        }

        public bool FilterEventIds
        {
            get => filterEventIds;
            set => SetProperty(ref filterEventIds, value);
        }

        public string EventIds
        {
            get => eventIds;
            set => SetProperty(ref eventIds, value);
        }

        public bool EventIdsFilterIn
        {
            get => eventIdsFilterIn;
            set => SetProperty(ref eventIdsFilterIn, value);
        }


        private bool isInRenamingingMode;
        private ICommand switchToRenamingModeCommand;
        private ICommand saveAndSwitchFromRenamingModeCommand;
        private ICommand discardAndSwitchFromRenamingModeCommand;

        public string NewName { get; set; }

        public bool IsInRenamingingMode
        {
            get => isInRenamingingMode;
            set => SetProperty(ref isInRenamingingMode, value);
        }

        public ICommand SwitchToRenamingModeCommand =>
            switchToRenamingModeCommand ??
            (switchToRenamingModeCommand = new DelegateCommand(o => IsInRenamingingMode = true));

        public ICommand SaveAndSwitchFromRenamingModeCommand =>
            saveAndSwitchFromRenamingModeCommand ??
            (saveAndSwitchFromRenamingModeCommand = new DelegateCommand(o => {
                if (!IsInRenamingingMode)
                    return;

                if (NewName != Name) {
                    Name = NewName;
                    SetModified();
                }

                IsInRenamingingMode = false;
            }));

        public ICommand DiscardAndSwitchFromRenamingModeCommand =>
            discardAndSwitchFromRenamingModeCommand ??
            (discardAndSwitchFromRenamingModeCommand = new DelegateCommand(o => {
                if (IsInRenamingingMode) {
                    NewName = Name;
                    IsInRenamingingMode = false;
                }
            }));

        private void SetModified()
        {
        }

        public EventProviderDescriptor CreateDescriptor()
        {
            var descriptor = new EventProviderDescriptor(Id);
            descriptor.Level = Level;
            descriptor.MatchAnyKeyword = MatchAnyKeyword;
            descriptor.MatchAllKeyword = MatchAllKeyword;
            descriptor.IncludeSecurityId = IncludeSecurityId;
            descriptor.IncludeTerminalSessionId = IncludeTerminalSessionId;
            descriptor.IncludeStackTrace = IncludeStackTrace;

            if (!string.IsNullOrWhiteSpace(Manifest))
                descriptor.Manifest = Manifest;

            if (FilterExecutableNames && !string.IsNullOrWhiteSpace(ExecutableNames))
                descriptor.ExecutableName = ExecutableNames;

            if (FilterProcessIds && !string.IsNullOrWhiteSpace(ProcessIds))
                descriptor.ProcessIds.AddRange(ParseUInt32List(ProcessIds).Distinct().OrderBySelf());

            if (FilterEventIds && !string.IsNullOrWhiteSpace(EventIds)) {
                descriptor.EventIds.AddRange(ParseUInt16List(EventIds).Distinct().OrderBySelf());
                descriptor.EventIdsFilterIn = EventIdsFilterIn;
            }

            if (!string.IsNullOrWhiteSpace(StartupProject))
                descriptor.StartupProjects = new List<string> { StartupProject };

            return descriptor;
        }

        private static IEnumerable<ushort> ParseUInt16List(string str)
        {
            foreach (var item in str.Split(',')) {
                if (ushort.TryParse(item, out var id))
                    yield return id;
            }
        }

        private static IEnumerable<uint> ParseUInt32List(string str)
        {
            foreach (var item in str.Split(',')) {
                if (uint.TryParse(item, out var id))
                    yield return id;
            }
        }

        public EventProviderViewModel DeepClone()
        {
            var clone = new EventProviderViewModel();
            Copy(this, clone);
            return clone;
        }

        private static void Copy(EventProviderViewModel source, EventProviderViewModel target)
        {
            target.Id = source.Id;
            target.Manifest = source.Manifest;
            target.Level = source.Level;
            target.MatchAnyKeyword = source.MatchAnyKeyword;
            target.MatchAllKeyword = source.MatchAllKeyword;
            target.IncludeSecurityId = source.IncludeSecurityId;
            target.IncludeTerminalSessionId = source.IncludeTerminalSessionId;
            target.IncludeStackTrace = source.IncludeStackTrace;
            target.StartupProject = source.StartupProject;
            target.FilterExecutableNames = source.FilterExecutableNames;
            target.ExecutableNames = source.ExecutableNames;
            target.FilterProcessIds = source.FilterProcessIds;
            target.ProcessIds = source.ProcessIds;
            target.FilterEventIds = source.FilterEventIds;
            target.EventIds = source.EventIds;
            target.EventIdsFilterIn = source.EventIdsFilterIn;
        }

        private Task ToggleSelectedEvents(IList selectedObjects)
        {
            var selectedEvents = selectedObjects.Cast<EventViewModel>().ToList();
            bool enabled = !selectedEvents.All(x => x.IsEnabled);
            foreach (var evt in selectedEvents)
                evt.IsEnabled = enabled;

            return Task.CompletedTask;
        }

        private Task BrowseManifest()
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select Manifest";
            dialog.Filter = "Manifest Files (*.man)|*.man|" +
                            "All Files (*.*)|*.*";
            dialog.DefaultExt = ".man";
            if (dialog.ShowModal() != true)
                return Task.CompletedTask;

            Manifest = dialog.FileName;
            return Task.CompletedTask;
        }
    }

    public static class VsModalExtensions
    {
        public static TaskDialogResult ShowModal(this TaskDialog dialog)
        {
            dialog.OwnerWindow = new HandleRef(null, GetDialogOwnerHwnd());
            return dialog.Show();
        }

        public static bool? ShowModal(this CommonDialog dialog)
        {
            var owner = GetDialogOwnerHwnd();
            if (owner != IntPtr.Zero && HwndSource.FromHwnd(owner)?.RootVisual is Window ownerWindow)
                return dialog.ShowDialog(ownerWindow);

            return dialog.ShowDialog();
        }

        public static IntPtr GetDialogOwnerHwnd()
        {
            var shell = ServiceProvider.GlobalProvider?.GetService<SVsUIShell, IVsUIShell>();
            if (shell != null && shell.GetDialogOwnerHwnd(out var owner) >= 0)
                return owner;

            return IntPtr.Zero;
        }
    }
}
