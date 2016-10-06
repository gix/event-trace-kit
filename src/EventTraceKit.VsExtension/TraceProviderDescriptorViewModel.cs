namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Collections;
    using Serialization;

    [SerializedShape(typeof(Settings.TraceProvider))]
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

        public TraceProviderDescriptorViewModel()
        {
            ProcessIds = new ObservableCollection<uint>();
            Events = new ObservableCollection<TraceEventDescriptorViewModel>();
            ToggleSelectedEventsCommand = new AsyncDelegateCommand<object>(ToggleSelectedEvents);
        }

        public TraceProviderDescriptorViewModel(Guid id, string name)
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

        public TraceProviderDescriptor ToModel()
        {
            var descriptor = new TraceProviderDescriptor(Id);
            descriptor.Level = Level;
            descriptor.MatchAnyKeyword = MatchAnyKeyword;
            descriptor.MatchAllKeyword = MatchAllKeyword;
            descriptor.IncludeSecurityId = IncludeSecurityId;
            descriptor.IncludeTerminalSessionId = IncludeTerminalSessionId;
            descriptor.IncludeStackTrace = IncludeStackTrace;
            if (!string.IsNullOrWhiteSpace(Manifest))
                descriptor.Manifest = Manifest;
            descriptor.ProcessIds.AddRange(ProcessIds);
            descriptor.EventIds.AddRange(Events.Where(x => x.IsEnabled).Select(x => x.Id));
            return descriptor;
        }

        public TraceProviderDescriptorViewModel DeepClone()
        {
            var clone = new TraceProviderDescriptorViewModel();
            clone.Id = Id;
            clone.Manifest = Manifest;
            clone.Level = Level;
            clone.MatchAnyKeyword = MatchAnyKeyword;
            clone.MatchAllKeyword = MatchAllKeyword;
            clone.IncludeSecurityId = IncludeSecurityId;
            clone.IncludeTerminalSessionId = IncludeTerminalSessionId;
            clone.IncludeStackTrace = IncludeStackTrace;
            clone.ProcessIds.AddRange(ProcessIds);
            clone.Events.AddRange(Events.Select(x => x.DeepClone()));
            return clone;
        }

        private Task ToggleSelectedEvents(object selectedObjects)
        {
            var selectedEvents = ((IList)selectedObjects).Cast<TraceEventDescriptorViewModel>().ToList();
            bool enabled = !selectedEvents.All(x => x.IsEnabled);
            foreach (var evt in selectedEvents)
                evt.IsEnabled = enabled;

            return Task.CompletedTask;
        }
    }
}
