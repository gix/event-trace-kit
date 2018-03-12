namespace EventTraceKit.VsExtension.Views
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Serialization;

    [SerializedShape(typeof(Settings.Persistence.SystemCollector))]
    public class SystemCollectorViewModel : CollectorViewModel
    {
        public SystemCollectorViewModel()
        {
            KernelFlags.Add(new KernelFlag(false, "Process", "Process create/delete", 0x00000001));
            KernelFlags.Add(new KernelFlag(false, "Image Load", "Kernel and user mode Image Load/Unload events", 0x00000004));
        }

        private string name = "System Collector";

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public class KernelFlag : ObservableModel
        {
            private bool isEnabled;

            public KernelFlag(bool isEnabled, string name, string description, uint flagValue)
            {
                this.isEnabled = isEnabled;
                this.name = name;
                this.description = description;
                FlagValue = flagValue;
            }

            public bool IsEnabled
            {
                get => isEnabled;
                set => SetProperty(ref isEnabled, value);
            }

            private string name;

            public string Name
            {
                get => name;
                set => SetProperty(ref name, value);
            }

            private string description;

            public string Description
            {
                get => description;
                set => SetProperty(ref description, value);
            }

            public uint FlagValue { get; }
        }

        public ObservableCollection<KernelFlag> KernelFlags { get; } =
            new ObservableCollection<KernelFlag>();

        public override CollectorViewModel DeepClone()
        {
            var clone = new SystemCollectorViewModel();
            clone.KernelFlags.AddRange(KernelFlags.Select(x => new KernelFlag(x.IsEnabled, x.Name, x.Description, x.FlagValue)));
            return clone;
        }

        public override CollectorDescriptor CreateDescriptor()
        {
            var descriptor = new SystemCollectorDescriptor();
            descriptor.KernelFlags = KernelFlags.Where(x => x.IsEnabled).Aggregate(0u, (a, x) => a | x.FlagValue);
            return descriptor;
        }
    }
}
