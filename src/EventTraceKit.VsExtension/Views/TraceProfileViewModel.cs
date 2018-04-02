namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Settings.Persistence;
    using Serialization;

    [SerializedShape(typeof(TraceProfile))]
    public class TraceProfileViewModel : ObservableModel
    {
        private string name;
        private CollectorViewModel selectedCollector;

        public TraceProfileViewModel()
            : this(Guid.NewGuid())
        {
        }

        public TraceProfileViewModel(Guid id)
        {
            Id = id;
            Collectors = new AcqRelObservableCollection<CollectorViewModel>(
                x => x.Context = null, x => x.Context = Context);
        }

        public Guid Id { get; }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public ITraceSettingsContext Context
        {
            get => context;
            set
            {
                context = value;
                foreach (var collector in Collectors)
                    collector.Context = value;
            }
        }

        public ObservableCollection<CollectorViewModel> Collectors { get; }

        public CollectorViewModel SelectedCollector
        {
            get => selectedCollector ?? Collectors.FirstOrDefault();
            set => SetProperty(ref selectedCollector, value);
        }

        private ICommand addOrRemoveSystemCollectorCommand;
        private ITraceSettingsContext context;

        public ICommand AddOrRemoveSystemCollectorCommand =>
            addOrRemoveSystemCollectorCommand ??
            (addOrRemoveSystemCollectorCommand = new AsyncDelegateCommand(AddOrRemoveSystemCollector));

        private Task AddOrRemoveSystemCollector()
        {
            var systemCollector = Collectors.FirstOrDefault(x => x is SystemCollectorViewModel);
            if (systemCollector == null) {
                systemCollector = new SystemCollectorViewModel();
                Collectors.Add(systemCollector);
                SelectedCollector = systemCollector;
            } else {
                Collectors.Remove(systemCollector);
                SelectedCollector = Collectors.FirstOrDefault();
            }

            return Task.CompletedTask;
        }

        public TraceProfileViewModel DeepClone()
        {
            var clone = new TraceProfileViewModel();
            clone.name = name;
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
