namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Collections;
    using EventTraceKit.Tracing;
    using Serialization;

    [SerializedShape(typeof(Settings.Persistence.TraceProvider))]
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
        private string executableName;
        private string enableEventIds;
        private string disableEventIds;
        private bool filterProcesses;
        private bool filterEvents;

        public EventProviderViewModel()
        {
            ProcessIds = new ObservableCollection<uint>();
            Events = new ObservableCollection<EventViewModel>();
            ToggleSelectedEventsCommand = new AsyncDelegateCommand<IList>(ToggleSelectedEvents);
        }

        public EventProviderViewModel(Guid id, string name)
            : this()
        {
            Id = id;
            Name = name;
            Level = 0xFF;
        }

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

        public string ExecutableName
        {
            get => executableName;
            set => SetProperty(ref executableName, value);
        }

        public string EnableEventIds
        {
            get => enableEventIds;
            set => SetProperty(ref enableEventIds, value);
        }

        public string DisableEventIds
        {
            get => disableEventIds;
            set => SetProperty(ref disableEventIds, value);
        }

        public bool FilterProcesses
        {
            get => filterProcesses;
            set => SetProperty(ref filterProcesses, value);
        }

        public bool FilterEvents
        {
            get => filterEvents;
            set => SetProperty(ref filterEvents, value);
        }

        public ObservableCollection<uint> ProcessIds { get; }

        public ObservableCollection<EventViewModel> Events { get; }

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

            if (!string.IsNullOrWhiteSpace(ExecutableName))
                descriptor.ExecutableName = ExecutableName;

            if (!string.IsNullOrWhiteSpace(EnableEventIds)) {
                descriptor.EventIds = SplitEventIds(EnableEventIds).ToList();
                descriptor.EnableEventIds = true;
            } else if (!string.IsNullOrWhiteSpace(DisableEventIds)) {
                descriptor.EventIds = SplitEventIds(DisableEventIds).ToList();
                descriptor.EnableEventIds = false;
            }

            if (FilterProcesses)
                descriptor.ProcessIds.AddRange(ProcessIds);
            if (FilterEvents)
                descriptor.EventIds.AddRange(
                    from x in Events where x.IsEnabled select x.Id);

            return descriptor;
        }

        private static IEnumerable<ushort> SplitEventIds(string str)
        {
            foreach (var item in str.Split(',')) {
                if (ushort.TryParse(item, out var id))
                    yield return id;
            }
        }

        public EventProviderViewModel DeepClone()
        {
            var clone = new EventProviderViewModel();
            clone.Id = Id;
            clone.Manifest = Manifest;
            clone.Level = Level;
            clone.MatchAnyKeyword = MatchAnyKeyword;
            clone.MatchAllKeyword = MatchAllKeyword;
            clone.IncludeSecurityId = IncludeSecurityId;
            clone.IncludeTerminalSessionId = IncludeTerminalSessionId;
            clone.IncludeStackTrace = IncludeStackTrace;
            clone.ExecutableName = ExecutableName;
            clone.EnableEventIds = EnableEventIds;
            clone.DisableEventIds = DisableEventIds;
            clone.ProcessIds.AddRange(ProcessIds);
            clone.Events.AddRange(Events.Select(x => x.DeepClone()));
            return clone;
        }

        private Task ToggleSelectedEvents(IList selectedObjects)
        {
            var selectedEvents = selectedObjects.Cast<EventViewModel>().ToList();
            bool enabled = !selectedEvents.All(x => x.IsEnabled);
            foreach (var evt in selectedEvents)
                evt.IsEnabled = enabled;

            return Task.CompletedTask;
        }
    }
}
