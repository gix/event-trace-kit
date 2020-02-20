namespace EventTraceKit.EventTracing.Schema
{
    using EventTraceKit.EventTracing.Support;

    public abstract class ProviderItem : SourceItem
    {
        public Provider Provider { get; set; }
        public abstract void Accept(IProviderItemVisitor visitor);
    }
}
