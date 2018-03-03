namespace EventManifestFramework.Support
{
    public interface ISourceItem
    {
        SourceLocation Location { get; set; }
    }

    public abstract class SourceItem : ISourceItem
    {
        public SourceLocation Location { get; set; }
    }
}
