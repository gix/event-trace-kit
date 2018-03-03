namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using EventManifestFramework.Schema;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Collections;
    using EventTraceKit.VsExtension.Serialization;
    using Microsoft.Win32;
    using Task = System.Threading.Tasks.Task;

    [SerializedShape(typeof(Settings.Persistence.EventProvider))]
    public class EventProviderViewModel : ViewModel
    {
        private Guid id;
        private string manifest;
        private string name;
        private bool isEnabled;

        private byte level;
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
        private bool enableEvents;
        private string startupProject;

        private bool isSelected;

        public EventProviderViewModel()
        {
            ToggleSelectedEventsCommand = new AsyncDelegateCommand<IList>(ToggleSelectedEvents);
            BrowseManifestCommand = new AsyncDelegateCommand(BrowseManifest);
            keywordSelector = new KeywordListBox(this);
        }

        public EventProviderViewModel(Guid id, string name)
            : this()
        {
            Id = id;
            Name = name;
            Level = 0xFF;
        }

        public EventProviderViewModel(
            Guid id, string name, string manifest, IEnumerable<EventViewModel> events)
            : this(id, name)
        {
            this.manifest = manifest;
            Events.AddRange(events);
        }

        public EventProviderViewModel(EventProviderViewModel source)
            : this()
        {
            Copy(source, this);
        }

        public ITraceSettingsContext Context { get; set; }

        public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name : Id.ToString();

        public ICommand ToggleSelectedEventsCommand { get; }

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

        public ICommand BrowseManifestCommand { get; }
        public IEnumerable<string> Manifests => Context.ManifestsInSolution.Value;
        public IEnumerable<ProjectInfo> Projects => Context.ProjectsInSolution.Value;

        public string Name
        {
            get => name;
            set
            {
                if (SetProperty(ref name, value))
                    RaisePropertyChanged(nameof(DisplayName));
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

        private bool manifestLoaded;
        public bool ManifestLoaded
        {
            get => manifestLoaded;
            set => SetProperty(ref manifestLoaded, value);
        }

        private LazyListBox keywordSelector;
        public LazyListBox KeywordSelector
        {
            get => keywordSelector;
            set => SetProperty(ref keywordSelector, value);
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

        public string StartupProject
        {
            get => startupProject;
            set => SetProperty(ref startupProject, value);
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

        public bool EnableEvents
        {
            get => enableEvents;
            set => SetProperty(ref enableEvents, value);
        }

        public ObservableCollection<EventViewModel> Events { get; } =
            new ObservableCollection<EventViewModel>();

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
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
                descriptor.EnableEventIds = EnableEvents;
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
            target.ExecutableNames = source.ExecutableNames;
            target.ProcessIds = source.ProcessIds;
            target.StartupProject = source.StartupProject;
            target.EventIds = source.EventIds;
            target.EnableEvents = source.EnableEvents;
            target.Events.Clear();
            target.Events.AddRange(source.Events.Select(x => x.DeepClone()));
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
            if (dialog.ShowDialog(Context.DialogOwner) != true)
                return Task.CompletedTask;

            Manifest = dialog.FileName;
            return Task.CompletedTask;
        }
    }


    public abstract class SelectorItemModel : ViewModel
    {
        private bool isEnabled;
        private bool isSelected;

        public bool IsEnabled
        {
            get => isEnabled;
            set => SetProperty(ref isEnabled, value);
        }

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }
    }

    public class KeywordInfo : SelectorItemModel
    {
        public KeywordInfo(Keyword keyword)
        {
            Name = keyword.Name.Value.ToPrefixedString();
            Mask = keyword.Mask.Value;
        }

        public string Name { get; }
        public ulong Mask { get; }
    }

    public class LazyListBox : ListBox
    {
        public LazyListBox()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        protected virtual Task<IEnumerable> LoadItems()
        {
            return Task.FromResult((IEnumerable)Enumerable.Empty<object>());
        }

        private async void OnLoaded(object sender, RoutedEventArgs args)
        {
            foreach (var keyword in await LoadItems())
                Items.Add(keyword);
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            Items.Clear();
        }
    }

    public class KeywordListBox : LazyListBox
    {
        private readonly EventProviderViewModel provider;

        public KeywordListBox(EventProviderViewModel provider)
        {
            this.provider = provider;
        }

        protected override async Task<IEnumerable> LoadItems()
        {
            provider.ManifestLoaded = true;
            return await LoadKeywords();
        }

        private Task<EventManifest> EventManifest => provider.Context.GetManifest(provider.Manifest);

        private async Task<IEnumerable<KeywordInfo>> LoadKeywords()
        {
            var eventManifest = await EventManifest;
            if (eventManifest == null)
                return Enumerable.Empty<KeywordInfo>();

            var providerItem = eventManifest.Providers
                .FirstOrDefault(x => x.Id.Value == provider.Id);

            return GetKeywords(providerItem).ToList();
        }

        private static IEnumerable<KeywordInfo> GetKeywords(Provider providerItem)
        {
            if (providerItem == null)
                yield break;

            foreach (var keyword in providerItem.Keywords)
                yield return new KeywordInfo(keyword);
        }
    }
}
