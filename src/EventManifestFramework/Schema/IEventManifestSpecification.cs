namespace EventManifestFramework.Schema
{
    public interface IEventManifestSpecification
    {
        bool IsSatisfiedBy(Provider provider);
        bool IsSatisfiedBy(Event @event);
        bool IsSatisfiedBy(Channel channel);
        bool IsSatisfiedBy(Level level);
        bool IsSatisfiedBy(Task task);
        bool IsSatisfiedBy(Opcode opcode);
        bool IsSatisfiedBy(Keyword keyword);
        bool IsSatisfiedBy(Filter filter);
        bool IsSatisfiedBy(Template template);
        bool IsSatisfiedBy(Map map);
        bool IsSatisfiedBy(DataProperty property);
        bool IsSatisfiedBy(StructProperty property);
        bool IsSatisfiedBy(LocalizedString @string);
    }
}
