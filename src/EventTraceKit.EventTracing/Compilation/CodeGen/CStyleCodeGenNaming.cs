namespace EventTraceKit.EventTracing.Compilation.CodeGen
{
    using System.Globalization;
    using System.Text;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Schema.Base;

    internal abstract class CStyleCodeGenNaming : ICodeGenNaming
    {
        public virtual string ContextId => "context";
        public virtual string EventDataDescriptorId => "data";
        public virtual string EventDescriptorId => "descriptor";

        public virtual string GetIdentifier(Provider provider)
        {
            return provider.Symbol;
        }

        public virtual string GetIdentifier(Channel channel)
        {
            if (channel.Symbol == null)
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_CHANNEL_{1}",
                    channel.Provider.Symbol,
                    channel.Id ?? GetIdentifierFromName(channel.Name));

            return channel.Symbol;
        }

        public string GetIdentifier(Level level)
        {
            if (level.Symbol == null)
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_LEVEL_{1}",
                    level.Provider.Symbol,
                    GetIdentifierFromName(level.Name));

            return level.Symbol;
        }

        public string GetIdentifier(Task task)
        {
            if (task.Symbol == null)
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_TASK_{1}",
                    task.Provider.Symbol,
                    GetIdentifierFromName(task.Name));

            return task.Symbol;
        }

        public string GetIdentifier(Opcode opcode)
        {
            if (opcode.Symbol == null)
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_OPCODE_{1}",
                    opcode.Provider.Symbol,
                    GetIdentifierFromName(opcode.Name));

            return opcode.Symbol;
        }

        public string GetIdentifier(Keyword keyword)
        {
            if (keyword.Symbol == null)
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_KEYWORD_{1}",
                    keyword.Provider.Symbol,
                    GetIdentifierFromName(keyword.Name));

            return keyword.Symbol;
        }

        public virtual string GetIdentifier(Event evt)
        {
            if (evt.Symbol == null) {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_EVENT_0x{1:x}_{2:x}_{3:x}_{4:x}_{5:x}_{6:x}_{7:x}",
                    evt.Provider.Symbol,
                    evt.Value,
                    evt.Version,
                    evt.GetDescriptorChannelValue(),
                    evt.LevelValue,
                    evt.OpcodeValue,
                    evt.TaskValue,
                    evt.KeywordMask);
            }

            return evt.Symbol;
        }

        public string GetIdentifier(Map map)
        {
            return map.Symbol;
        }

        public virtual string GetIdentifier(MapItem item, Map map)
        {
            if (string.IsNullOrWhiteSpace(item.Symbol))
                return $"{map.Symbol}{map.Items.IndexOf(item)}";

            return item.Symbol;
        }

        public string GetIdentifier(LocalizedString @string)
        {
            if (@string.Symbol == null)
                return "MSG_" + GetIdentifierFromName(@string.Name);
            return @string.Symbol;
        }

        public virtual string GetIdentifier(Property property)
        {
            return SanitizeIdentifier(property.Name.Value);
        }

        public static string SanitizeIdentifier(string identifier)
        {
            var builder = new StringBuilder(identifier);

            int i;
            for (i = 0; i < builder.Length; ++i) {
                if (char.IsDigit(builder[i]))
                    builder[i] = '_';
                else
                    break;
            }

            for (; i < builder.Length; ++i) {
                if (!IsAlphaNumeric(builder[i]))
                    builder[i] = '_';
            }

            return builder.ToString();
        }

        public virtual string GetTemplateSuffix(Template template)
        {
            if (template.Properties.Count == 0)
                return string.Empty;
            var builder = new StringBuilder(template.Properties.Count);
            foreach (var property in template.Properties)
                builder.Append(TemplateTypeCode.MangleProperty(property));
            return builder.ToString();
        }

        public abstract string GetTemplateId(Template template);

        public abstract string GetTemplateGuardId(Template template);

        public abstract string GetEventDescriptorId(Event evt);

        public abstract string GetEventFuncId(Event evt, string prefix = null, string suffix = null);

        public abstract string GetTaskGuidId(Task task);

        public abstract string GetProviderGuidId(Provider provider);

        public virtual string GetProviderControlGuidId(Provider provider)
        {
            return GetProviderGuidId(provider) + "_ControlGuid";
        }

        public abstract string GetProviderContextId(Provider provider);

        public virtual string GetProviderHandleId(Provider provider)
        {
            return GetIdentifier(provider) + "_Handle";
        }

        public virtual string GetProviderTraitsId(Provider provider)
        {
            return $"{provider.Symbol}_Traits";
        }

        public virtual string GetProviderLevelsId(Provider provider)
        {
            return GetIdentifier(provider) + "Levels";
        }

        public virtual string GetProviderKeywordsId(Provider provider)
        {
            return GetIdentifier(provider) + "Keywords";
        }

        public virtual string GetProviderEnableBitsId(Provider provider)
        {
            return GetIdentifier(provider) + "EnableBits";
        }

        public abstract string GetNumberedArgId(int idx);

        public abstract string GetArgumentId(Property property, bool usePropertyName);

        public abstract string GetLengthArgumentId(Property property, bool usePropertyName);

        protected string GetIdentifierFromName(QName name)
        {
            return GetIdentifierFromName(name.ToPrefixedString());
        }

        protected string GetIdentifierFromName(string name)
        {
            return SanitizeIdentifier(name);
        }

        private static bool IsAlphaNumeric(char c)
        {
            return
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                (c >= '0' && c <= '9') ||
                (c == '_');
        }
    }
}
