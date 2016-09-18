namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Collections;

    public class TraceProviderDescriptorViewModel : ViewModel
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
        private bool filterEvents;

        private string manifestOrProvider;

        public TraceProviderDescriptorViewModel(Guid id, string name)
        {
            ProcessIds = new ObservableCollection<uint>();
            Events = new ObservableCollection<TraceEventDescriptorViewModel>();
            ToggleSelectedEventsCommand = new AsyncDelegateCommand(ToggleSelectedEvents);

            Id = id;
            Name = name;
            Level = 0xFF;
        }

        public TraceProviderDescriptorViewModel(TraceProviderDescriptor provider)
        {
            ProcessIds = new ObservableCollection<uint>();
            Events = new ObservableCollection<TraceEventDescriptorViewModel>();
            ToggleSelectedEventsCommand = new AsyncDelegateCommand(ToggleSelectedEvents);

            Id = provider.Id;
            Manifest = provider.Manifest ?? provider.ProviderBinary;
            Level = provider.Level;
            MatchAnyKeyword = provider.MatchAnyKeyword;
            MatchAllKeyword = provider.MatchAllKeyword;
            IncludeSecurityId = provider.IncludeSecurityId;
            IncludeTerminalSessionId = provider.IncludeTerminalSessionId;
            IncludeStackTrace = provider.IncludeStackTrace;
            if (provider.ProcessIds != null)
                ProcessIds.AddRange(provider.ProcessIds);
            if (provider.EventIds != null)
                Events.AddRange(provider.EventIds.Select(x => new TraceEventDescriptorViewModel { Id = x }));
        }

        private Task ToggleSelectedEvents(object selectedObjects)
        {
            var selectedEvents = ((IList)selectedObjects).Cast<TraceEventDescriptorViewModel>().ToList();
            bool enabled = !selectedEvents.All(x => x.IsEnabled);
            foreach (var evt in selectedEvents)
                evt.IsEnabled = enabled;

            return Task.CompletedTask;
        }

        public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name : Id.ToString();

        public Guid Id
        {
            get { return id; }
            set
            {
                if (SetProperty(ref id, value))
                    RaisePropertyChanged(nameof(DisplayName));
            }
        }

        public string Manifest
        {
            get { return manifest; }
            set { SetProperty(ref manifest, value); }
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (SetProperty(ref name, value))
                    RaisePropertyChanged(nameof(DisplayName));
            }
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set { SetProperty(ref isEnabled, value); }
        }

        public byte Level
        {
            get { return level; }
            set { SetProperty(ref level, value); }
        }

        public ulong MatchAnyKeyword
        {
            get { return matchAnyKeyword; }
            set { SetProperty(ref matchAnyKeyword, value); }
        }

        public ulong MatchAllKeyword
        {
            get { return matchAllKeyword; }
            set { SetProperty(ref matchAllKeyword, value); }
        }

        public bool IncludeSecurityId
        {
            get { return includeSecurityId; }
            set { SetProperty(ref includeSecurityId, value); }
        }

        public bool IncludeTerminalSessionId
        {
            get { return includeTerminalSessionId; }
            set { SetProperty(ref includeTerminalSessionId, value); }
        }

        public bool IncludeStackTrace
        {
            get { return includeStackTrace; }
            set { SetProperty(ref includeStackTrace, value); }
        }

        public bool FilterEvents
        {
            get { return filterEvents; }
            set { SetProperty(ref filterEvents, value); }
        }

        public ObservableCollection<uint> ProcessIds { get; }
        public ObservableCollection<TraceEventDescriptorViewModel> Events { get; }

        public ICommand ToggleSelectedEventsCommand { get; }

        public TraceProviderDescriptor ToModel()
        {
            var descriptor = new TraceProviderDescriptor(Id);
            descriptor.Level = Level;
            descriptor.MatchAnyKeyword = MatchAnyKeyword;
            descriptor.MatchAllKeyword = MatchAllKeyword;
            descriptor.IncludeSecurityId = IncludeSecurityId;
            descriptor.IncludeTerminalSessionId = IncludeTerminalSessionId;
            descriptor.IncludeStackTrace = IncludeStackTrace;

            if (!string.IsNullOrWhiteSpace(Manifest)) {
                var binaryExtensions = new[] { ".dll", ".exe" };
                if (binaryExtensions.Any(x => Manifest.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                    descriptor.SetProviderBinary(Manifest);
                else
                    descriptor.SetManifest(Manifest);
            }

            descriptor.ProcessIds.AddRange(ProcessIds);
            descriptor.EventIds.AddRange(Events.Where(x => x.IsEnabled).Select(x => x.Id));
            return descriptor;
        }
    }
}