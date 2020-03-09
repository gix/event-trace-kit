namespace EventTraceKit.EventTracing.Compilation.CodeGen
{
    using EventTraceKit.EventTracing.Schema;

    internal interface ICodeGenNaming
    {
        string ContextId { get; }
        string EventDataDescriptorId { get; }
        string EventDescriptorId { get; }

        string GetIdentifier(Provider provider);
        string GetIdentifier(Channel channel);
        string GetIdentifier(Level level);
        string GetIdentifier(Task task);
        string GetIdentifier(Opcode opcode);
        string GetIdentifier(Keyword keyword);
        string GetIdentifier(Event evt);
        string GetIdentifier(Map map);
        string GetIdentifier(MapItem item, Map map);
        string GetIdentifier(LocalizedString @string);

        string GetTemplateSuffix(Template template);
        string GetTemplateId(Template template);
        string GetTemplateGuardId(Template template);

        string GetEventDescriptorId(Event evt);
        string GetEventFuncId(Event evt, string prefix = null, string suffix = null);
        string GetTaskGuidId(Task task);

        string GetProviderGuidId(Provider provider);
        string GetProviderControlGuidId(Provider provider);
        string GetProviderContextId(Provider provider);
        string GetProviderHandleId(Provider provider);
        string GetProviderTraitsId(Provider provider);
        string GetProviderLevelsId(Provider provider);
        string GetProviderKeywordsId(Provider provider);
        string GetProviderEnableBitsId(Provider provider);

        string GetNumberedArgId(int idx);
        string GetArgumentId(Property property, bool usePropertyName);
        string GetLengthArgumentId(Property property, bool usePropertyName);
    }
}
