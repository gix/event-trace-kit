namespace EventManifestCompiler.CodeGen
{
    using EventManifestFramework.Schema;

    internal interface ICodeGenNomenclature
    {
        string EventDataDescriptorId { get; }
        string RegHandleId { get; }
        string EventDescriptorId { get; }

        string GetIdentifier(Provider provider);
        string GetIdentifier(Channel channel);
        string GetIdentifier(Level level);
        string GetIdentifier(Task task);
        string GetIdentifier(Opcode opcode);
        string GetIdentifier(Keyword keyword);
        string GetIdentifier(Event evt);
        string GetIdentifier(IMap map);
        string GetIdentifier(IMapItem item, IMap map);
        string GetIdentifier(LocalizedString @string);

        string GetTemplateSuffix(Template template);
        string GetTemplateId(Template template);
        string GetTemplateGuardId(Template template);

        string GetEventDescriptorId(Event evt);
        string GetEventFuncId(Event evt, string prefix = null, string suffix = null);
        string GetTaskGuidId(Task task);

        string GetProviderGuidId(Provider provider);
        string GetProviderContextId(Provider provider);
        string GetProviderHandleId(Provider provider);
        string GetProviderLevelsId(Provider provider);
        string GetProviderKeywordsId(Provider provider);
        string GetProviderEnableBitsId(Provider provider);

        string GetNumberedArgId(int idx);
        string GetArgumentId(Property property, bool usePropertyName);
        string GetLengthArgumentId(Property property, bool usePropertyName);
    }
}
