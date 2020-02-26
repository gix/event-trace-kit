namespace EventTraceKit.EventTracing.Compilation.CodeGen
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using EventTraceKit.EventTracing.Internal.Extensions;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Schema.Base;
    using EventTraceKit.EventTracing.Support;

    internal abstract class BaseCodeGenNomenclature : ICodeGenNomenclature
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

        public string SanitizeIdentifier(string identifier)
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

        public abstract string GetTemplateSuffix(Template template);

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

        protected string MangleProperty(Property property, IList<Property> properties)
        {
            if (property.Kind == PropertyKind.Data)
                return MangleProperty((DataProperty)property, properties);
            string t = "n";
            if (property.Count.IsFixedMultiple) {
                t = t.ToUpperInvariant();
                t += property.Count.Value.Value;
            } else if (property.Count.DataPropertyRef != null) {
                t = t.ToUpperInvariant();
                t += "R" + properties.FindIndex(f => f.Name == property.Count.DataPropertyRef);
            }
            return t;
        }

        protected string MangleProperty(DataProperty data, IList<Property> properties)
        {
            if (data.InType.Name.Namespace != WinEventSchema.Namespace)
                throw new InternalException("cannot mangle type '{0}'", data.InType);

            string t = MangleType(data.InType);

            if (data.InType.Name != WinEventSchema.UnicodeString &&
                data.InType.Name != WinEventSchema.AnsiString &&
                data.InType.Name != WinEventSchema.Binary) {
                if (data.Count.IsFixedMultiple) {
                    t = t.ToUpperInvariant();
                    t += data.Count.Value.Value;
                } else if (data.Count.DataPropertyRef != null) {
                    t = t.ToUpperInvariant();
                    t += "R" + properties.FindIndex(f => f.Name == data.Count.DataPropertyRef);
                }
                return t;
            }

            string len = string.Empty;

            bool hasFixedCount = data.Count.IsFixedMultiple;
            bool hasFixedLength = data.Length.IsFixed;
            bool hasVarCount = data.Count.IsVariable;
            bool hasVarLength = data.Length.IsVariable;

            if (hasFixedCount || hasVarCount)
                t = t.ToUpperInvariant();

            if (hasFixedCount && hasFixedLength) {
                len = (data.Count.Value.Value * data.Length.Value.Value).ToString(
                    CultureInfo.InvariantCulture);
            } else if (hasVarCount && hasVarLength) {
                len = "r" + properties.FindIndex(f => f.Name == data.Length.DataPropertyRef);
                len += "R" + properties.FindIndex(f => f.Name == data.Count.DataPropertyRef);
            } else if (hasFixedCount && hasVarLength) {
                len = data.Count.Value + "r" + properties.FindIndex(f => f.Name == data.Length.DataPropertyRef);
            } else if (hasFixedLength && hasVarCount) {
                len = data.Length.Value + "r" + properties.FindIndex(f => f.Name == data.Count.DataPropertyRef);
            } else if (hasFixedCount || hasVarCount) {
                len = "R";
            } else if (hasFixedLength) {
                len = data.Length.Value.ToString();
            } else if (hasVarLength) {
                len = "r" + properties.FindIndex(f => f.Name == data.Length.DataPropertyRef);
            } else if (hasFixedCount || hasVarCount || hasFixedLength || hasVarLength) {
                throw new InternalException();
            }

            return t + len;
        }

        protected static string MangleType(InType type)
        {
            if (type.Name.Namespace != WinEventSchema.Namespace)
                throw new InternalException("cannot mangle type '{0}'", type);

            return type.Name.LocalName switch
            {
                "UnicodeString" => "z",
                "AnsiString" => "s",
                "Int8" => "c",
                "UInt8" => "u",
                "Int16" => "l",
                "UInt16" => "h",
                "Int32" => "d",
                "UInt32" => "q",
                "Int64" => "i",
                "UInt64" => "x",
                "Float" => "f",
                "Double" => "g",
                "Boolean" => "t",
                "Binary" => "b",
                "GUID" => "j",
                "Pointer" => "p",
                "FILETIME" => "m",
                "SYSTEMTIME" => "y",
                "SID" => "k",
                "HexInt32" => "d",
                "HexInt64" => "i",
                "CountedUnicodeString" => "w",
                "CountedAnsiString" => "a",
                "CountedBinary" => "e",
                _ => throw new InternalException("cannot mangle type '{0}'", type),
            };
        }

        protected string GetIdentifierFromName(QName name)
        {
            return GetIdentifierFromName(name.ToPrefixedString());
        }

        protected string GetIdentifierFromName(string name)
        {
            return SanitizeIdentifier(name);
        }

        private bool IsAlphaNumeric(char c)
        {
            return
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                (c >= '0' && c <= '9') ||
                (c == '_');
        }
    }
}
