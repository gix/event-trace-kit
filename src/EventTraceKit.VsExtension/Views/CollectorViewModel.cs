namespace EventTraceKit.VsExtension.Views
{
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Serialization;

    [SerializedShape(typeof(Settings.Persistence.Collector))]
    public abstract class CollectorViewModel : ObservableModel
    {
        public abstract ITraceSettingsContext Context { get; set; }
        public abstract CollectorViewModel DeepClone();
        public abstract CollectorDescriptor CreateDescriptor();
    }
}
