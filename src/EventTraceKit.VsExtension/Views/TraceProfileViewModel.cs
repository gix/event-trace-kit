namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Collections;
    using Serialization;

    [SerializedShape(typeof(Settings.Persistence.TraceProfile))]
    public class TraceProfileViewModel : ViewModel
    {
        private Guid id;
        private string name;
        private EventCollectorViewModel selectedCollector;

        public TraceProfileViewModel()
        {
            Collectors = new AcqRelObservableCollection<CollectorViewModel>(
                x => x.Context = null, x => x.Context = Context);
        }

        public Guid Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public ITraceSettingsContext Context { get; set; }

        public ObservableCollection<CollectorViewModel> Collectors { get; }

        public EventCollectorViewModel SelectedCollector
        {
            get => selectedCollector;
            set => SetProperty(ref selectedCollector, value);
        }

        public TraceProfileViewModel DeepClone()
        {
            var clone = new TraceProfileViewModel();
            clone.id = Guid.NewGuid();
            clone.name = name;
            clone.Context = Context;
            clone.Collectors.AddRange(Collectors.Select(x => x.DeepClone()));
            return clone;
        }

        public TraceProfileDescriptor CreateDescriptor()
        {
            var descriptor = new TraceProfileDescriptor();
            descriptor.Collectors.AddRange(Collectors.Select(x => x.CreateDescriptor()));
            return descriptor;
        }

        public Dictionary<EventKey, string> GetEventSymbols()
        {
            return new Dictionary<EventKey, string>();
        }
    }
}
