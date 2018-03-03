namespace EventManifestCompiler.CodeGen
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using EventManifestCompiler.Extensions;
    using EventManifestCompiler.Support;
    using EventManifestFramework.Schema;
    using EventManifestFramework.Schema.Base;
    using EventManifestFramework.Support;

    [Export(typeof(ICodeGenerator))]
    [ExportMetadata("Name", "cxx")]
    internal sealed class CxxCodeGenerator : ICodeGenerator
    {
        // Size of the integer type used to store enable bits.
        private const int EnableByteSize = 4;

        private static readonly Template DefaultTemplate = new Template(
            Located.Create(string.Empty));

        private readonly ICodeGenOptions options;
        private readonly string etwNamespace = string.Empty;
        private readonly ICodeGenNomenclature naming;

        private IndentableTextWriter ow;

        private sealed class Nomenclature : BaseCodeGenNomenclature
        {
            private readonly CxxCodeGenerator generator;
            private string etwMacroPrefix;

            public Nomenclature(CxxCodeGenerator generator)
            {
                this.generator = generator;
            }

            public override string GetIdentifier(MapItem item, Map map)
            {
                if (string.IsNullOrWhiteSpace(item.Symbol))
                    return $"{map.Symbol}{map.Items.IndexOf(item)}";

                return item.Symbol;
            }

            public override string GetTemplateSuffix(Template template)
            {
                if (template.Properties.Count == 0)
                    return "EventDescriptor";
                var builder = new StringBuilder(template.Properties.Count);
                foreach (var property in template.Properties)
                    builder.Append(MangleProperty(property, template.Properties));
                return builder.ToString();
            }

            public override string GetTemplateId(Template template)
            {
                return "WriteEvent_" + GetTemplateSuffix(template);
            }

            public override string GetTemplateGuardId(Template template)
            {
                var prefix = GetEtwMacroPrefix();
                var suffix = GetTemplateSuffix(template);
                return $"{prefix}ETW_TEMPLATE_{suffix}_DEFINED";
            }

            private string GetEtwMacroPrefix()
            {
                if (etwMacroPrefix == null) {
                    if (!string.IsNullOrEmpty(generator.etwNamespace))
                        etwMacroPrefix = generator.etwNamespace.Trim(':').Replace("::", "_") + "_";
                    else
                        etwMacroPrefix = string.Empty;
                }
                return etwMacroPrefix;
            }

            public override string GetEventDescriptorId(Event evt)
            {
                return GetIdentifier(evt) + "Desc";
            }

            public override string GetProviderGuidId(Provider provider)
            {
                return provider.Symbol;
            }

            public override string GetProviderContextId(Provider provider)
            {
                return GetIdentifier(provider) + "Context";
            }

            public override string GetTaskGuidId(Task task)
            {
                return GetIdentifierFromName(task.Name) + "Id";
            }

            public override string GetEventFuncId(Event evt, string prefix = null, string suffix = null)
            {
                return (prefix ?? string.Empty) + GetIdentifier(evt) + (suffix ?? string.Empty);
            }

            public override string GetNumberedArgId(int idx)
            {
                return "arg" + idx;
            }

            public override string GetArgumentId(Property property, bool usePropertyName)
            {
                return usePropertyName ? GetIdentifier(property) : GetNumberedArgId(property.Index);
            }

            public override string GetLengthArgumentId(Property property, bool usePropertyName)
            {
                return GetArgumentId(property, usePropertyName) + "Len";
            }
        }

        [ImportingConstructor]
        public CxxCodeGenerator(ICodeGenOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            this.options = options;
            if (!string.IsNullOrWhiteSpace(options.EtwNamespace))
                etwNamespace = ConvertToCxxNamespace(options.EtwNamespace);

            naming = new Nomenclature(this);
        }

        private string ConvertToCxxNamespace(string ns)
        {
            string[] parts = ns.Trim().Split('.');
            return "::" + string.Join("::", parts) + "::";
        }

        public void Generate(EventManifest manifest, Stream output)
        {
            using (var baseWriter = IO.CreateStreamWriter(output))
            using (var writer = new IndentableTextWriter(baseWriter)) {
                try {
                    ow = writer;
                    GenerateCore(manifest);
                } finally {
                    ow = null;
                }
            }
        }

        private void WriteNamespaceBegin(string ns)
        {
            if (ns == null)
                return;

            string[] parts = ns.Split('.');
            for (int i = 0; i < parts.Length; ++i) {
                if (i == parts.Length - 1) {
                    ow.WriteLine("namespace {0}", parts[i]);
                    ow.WriteLine("{");
                } else {
                    ow.WriteLine("namespace {0} {{", parts[i]);
                }
            }
            ow.WriteLine();
        }

        private void WriteNamespaceEnd(string ns)
        {
            if (ns == null)
                return;

            string[] parts = ns.Split('.');
            ow.WriteLine();
            ow.WriteLine("{0} // {1}", new string('}', parts.Length), string.Join("::", parts));
        }

        private void GenerateCore(EventManifest manifest)
        {
            WriteHeader();
            WriteTemplates(manifest.Providers);

            WriteNamespaceBegin(options.LogNamespace);
            foreach (var provider in manifest.Providers) {
                ow.WriteLine("//");
                ow.WriteLine("// Provider {0}", provider.Name);
                ow.WriteLine("//");
                ow.WriteLine(
                    "EXTERN_C __declspec(selectany) GUID const {0} = {1};",
                    naming.GetProviderGuidId(provider),
                    FormatGuid(provider.Id));
                ow.WriteLine();

                WriteRegistration(provider);
                if (!options.SkipDefines) {
                    WriteChannels(provider);
                    WriteLevels(provider);
                    WriteOpcodes(provider);
                    WriteTasks(provider);
                    WriteKeywords(provider);
                }
                WriteMaps(provider);
                WriteEventDescriptors(provider);
                WriteEvents(provider);
            }

            WriteNamespaceEnd(options.LogNamespace);

            ow.Flush();
        }

        private void WriteHeader()
        {
            ow.WriteLine("#pragma once");
            ow.WriteLine("#include <cstdint>");
            ow.WriteLine("#include <cwchar>");
            ow.WriteLine("#include <evntprov.h>");
            ow.WriteLine();
        }

        private void WriteRegistration(Provider provider)
        {
            if (options.UseCustomEventEnabledChecks) {
                var enableBits = provider.EnableBits;
                WriteEnableBits(provider);
                ow.WriteLine(
                    "EXTERN_C __declspec(selectany) {0}TraceContextEx {1} " +
                    "= {{ 0, 0, 0, 0, 0, false, 0, 0, {2}, {3}, {4}, {5} }};",
                    etwNamespace,
                    naming.GetProviderContextId(provider),
                    enableBits.Count,
                    naming.GetProviderEnableBitsId(provider),
                    naming.GetProviderKeywordsId(provider),
                    naming.GetProviderLevelsId(provider));
            } else {
                ow.WriteLine(
                    "EXTERN_C __declspec(selectany) {0}TraceContext {1} = {{}};",
                    etwNamespace,
                    naming.GetProviderContextId(provider));
            }
            ow.WriteLine("EXTERN_C __declspec(selectany) REGHANDLE {0} = {{}};",
                         naming.GetProviderHandleId(provider));
            ow.WriteLine();

            ow.WriteLine("//");
            ow.WriteLine("// Registration with ETW");
            ow.WriteLine("//");
            ow.WriteLine("inline void EventRegister{0}()", provider.Symbol);
            ow.WriteLine("{");
            using (ow.IndentScope())
                ow.WriteLine(
                    "{0}EventRegister({1}, {0}{2}, &{3}, &{4});",
                    etwNamespace,
                    naming.GetProviderGuidId(provider),
                    options.UseCustomEventEnabledChecks ? "ControlCallbackEx" : "ControlCallback",
                    naming.GetProviderContextId(provider),
                    naming.GetProviderHandleId(provider));
            ow.WriteLine("}");
            ow.WriteLine();

            ow.WriteLine("inline void EventUnregister{0}()", provider.Symbol);
            ow.WriteLine("{");
            using (ow.IndentScope())
                ow.WriteLine("{0}EventUnregister(&{1});",
                             etwNamespace, naming.GetProviderHandleId(provider));
            ow.WriteLine("}");
            ow.WriteLine();
        }

        private void WriteEnableBits(Provider provider)
        {
            var enableBits = provider.EnableBits;
            int enableByteCount = (enableBits.Count + 31) / 32;
            if (enableByteCount == 0)
                return;

            ow.WriteLine(
                "EXTERN_C __declspec(selectany) DECLSPEC_CACHEALIGN uint32_t {0}[{1}] = {{}};",
                naming.GetProviderEnableBitsId(provider),
                enableByteCount);

            ow.Write(
                "EXTERN_C __declspec(selectany) uint64_t const {0}[{1}] = {{ ",
                naming.GetProviderKeywordsId(provider),
                enableBits.Count);
            WriteList(enableBits, b => $"0x{b.KeywordMask:X8}");
            ow.WriteLine(" };");

            ow.Write(
                "EXTERN_C __declspec(selectany) uint8_t const {0}[{1}] = {{ ",
                naming.GetProviderLevelsId(provider), enableBits.Count);
            WriteList(enableBits, b => b.Level);
            ow.WriteLine(" };");
        }

        private void WriteList<T, TItem>(IReadOnlyList<T> list, Func<T, TItem> selector)
        {
            for (int i = 0; i < list.Count; ++i) {
                if (i > 0)
                    ow.Write(", ");
                ow.Write(selector(list[i]));
            }
        }

        private void WriteChannels(Provider provider)
        {
            if (provider.Channels.Count == 0)
                return;

            ow.WriteLine("// Channels");
            foreach (var channel in provider.Channels)
                ow.WriteLine("uint8_t const {0} = 0x{1:X};",
                             naming.GetIdentifier(channel), channel.Value);

            ow.WriteLine();
        }

        private void WriteLevels(Provider provider)
        {
            if (provider.Levels.Count == 0)
                return;

            ow.WriteLine("// Levels");
            foreach (var level in provider.Levels)
                ow.WriteLine("uint8_t const {0} = 0x{1:X};",
                             naming.GetIdentifier(level), level.Value);
            ow.WriteLine();
        }

        private void WriteOpcodes(Provider provider)
        {
            if (provider.Opcodes.Count == 0)
                return;

            ow.WriteLine("// Opcodes");
            foreach (var opcode in provider.Opcodes)
                ow.WriteLine("uint8_t const {0} = 0x{1:X};",
                             naming.GetIdentifier(opcode), opcode.Value);
            ow.WriteLine();
        }

        private void WriteTasks(Provider provider)
        {
            if (provider.Tasks.Count == 0)
                return;

            ow.WriteLine("// Tasks");
            foreach (var task in provider.Tasks) {
                ow.WriteLine("uint16_t const {0} = 0x{1:X};", naming.GetIdentifier(task), task.Value);
                if (task.Guid.GetValueOrDefault() != Guid.Empty)
                    ow.WriteLine("EXTERN_C __declspec(selectany) GUID const {0} = {1};",
                                 naming.GetTaskGuidId(task), FormatGuid(task.Guid.GetValueOrDefault()));
            }
            ow.WriteLine();
        }

        private void WriteKeywords(Provider provider)
        {
            if (provider.Keywords.Count == 0)
                return;

            ow.WriteLine("// Keywords");
            foreach (var keyword in provider.Keywords)
                ow.WriteLine("uint64_t const {0} = 0x{1:X8};",
                             naming.GetIdentifier(keyword), keyword.Mask);
            ow.WriteLine();
        }

        private void WriteMaps(Provider provider)
        {
            if (provider.Maps.Count == 0)
                return;

            ow.WriteLine("//");
            ow.WriteLine("// Maps");
            ow.WriteLine("//");
            ow.WriteLine();

            foreach (var map in provider.Maps.Where(m => m.Kind == MapKind.BitMap).Cast<BitMap>())
                if (map.Symbol != null && map.Items.Count > 0)
                    WriteMap(map);

            foreach (var map in provider.Maps.Where(m => m.Kind == MapKind.ValueMap).Cast<ValueMap>())
                if (map.Symbol != null && map.Items.Count > 0)
                    WriteMap(map);
        }

        private void WriteMap(Map map)
        {
            ow.WriteLine("enum {0}", naming.GetIdentifier(map));
            ow.WriteLine("{");
            using (ow.IndentScope()) {
                foreach (var item in map.Items) {
                    string symbol = naming.GetIdentifier(item, map);
                    if (map.Kind == MapKind.BitMap)
                        ow.WriteLine("{0} = 0x{1:X},", symbol, item.Value);
                    else
                        ow.WriteLine("{0} = {1},", symbol, item.Value);
                }
            }
            ow.WriteLine("};");
            ow.WriteLine();
        }

        private void WriteEventDescriptors(Provider provider)
        {
            if (provider.Events.Count == 0)
                return;

            ow.WriteLine("//");
            ow.WriteLine("// Event Descriptors");
            ow.WriteLine("//");
            foreach (var evt in provider.Events)
                WriteEventDescriptor(evt);
            ow.WriteLine();
        }

        private void WriteEventDescriptor(Event evt)
        {
            if (evt.NotLogged.GetValueOrDefault())
                return;

            byte version = evt.Version;
            uint channel = evt.ChannelValue;
            byte level = evt.LevelValue;
            byte opcode = evt.OpcodeValue;
            ushort task = evt.TaskValue;
            ulong keywordMask = evt.KeywordMask;

            ow.WriteLine(
                "EXTERN_C __declspec(selectany) EVENT_DESCRIPTOR const {0} = " +
                "{{ 0x{1:X4}, 0x{2:X2}, 0x{3:X2}, 0x{4:X2}, 0x{5:X2}, 0x{6:X4}, 0x{7:X16} {8};",
                naming.GetEventDescriptorId(evt),
                evt.Value, version, channel, level, opcode, task, keywordMask, "}");
            if (!options.SkipDefines)
                ow.WriteLine("uint16_t const {0}Id = 0x{1:X};", naming.GetIdentifier(evt), evt.Value);
        }

        private void WriteEvents(Provider provider)
        {
            for (int i = 0; i < provider.Events.Count; ++i) {
                if (i > 0)
                    ow.WriteLine();
                WriteEvent(provider.Events[i]);
            }
        }

        private void WriteEvent(Event evt)
        {
            if (evt.NotLogged.GetValueOrDefault())
                return;

            var template = evt.Template ?? DefaultTemplate;
            var properties = template.Properties;
            var enableFuncId = naming.GetEventFuncId(evt, "EventEnabled");
            var providerHandleId = naming.GetProviderHandleId(evt.Provider);
            var descriptorId = naming.GetEventDescriptorId(evt);

            ow.WriteLine("//");
            ow.WriteLine("// Event {0}", evt.Symbol);
            ow.WriteLine("//");
            if (!string.IsNullOrWhiteSpace(options.AlwaysInlineAttribute))
                ow.Write("{0} ", options.AlwaysInlineAttribute);
            ow.WriteLine("bool {0}()", enableFuncId);
            ow.WriteLine("{");
            using (ow.IndentScope()) {
                if (options.UseCustomEventEnabledChecks)
                    ow.WriteLine(
                        "return ({0}[{1}] & 0x{2:X8}) != 0;",
                        naming.GetProviderEnableBitsId(evt.Provider),
                        evt.EnableBit.GetIndex(EnableByteSize),
                        evt.EnableBit.GetMask(EnableByteSize));
                else
                    ow.WriteLine(
                        "return EventEnabled({0}, &{1}) != FALSE;",
                        providerHandleId,
                        descriptorId);
            }
            ow.WriteLine("}");
            ow.WriteLine();

            if (evt.Message != null)
                ow.WriteLine("// Message: {0}", evt.Message.Value);

            if (!string.IsNullOrWhiteSpace(options.AlwaysInlineAttribute))
                ow.Write("{0} ", options.AlwaysInlineAttribute);
            ow.WriteLine("unsigned long {0}({1}",
                         naming.GetEventFuncId(evt, options.LogCallPrefix),
                         properties.Count == 0 ? ")" : "");
            if (properties.Count != 0) {
                using (ow.IndentScope()) {
                    ParamStyle style = options.GenerateStubs ? ParamStyle.Type : ParamStyle.Full;
                    AppendParams(ow, properties, true, style);
                    ow.WriteLine(")");
                }
            }
            ow.WriteLine("{");
            using (ow.IndentScope()) {
                if (options.GenerateStubs) {
                    ow.WriteLine("return ERROR_SUCCESS;");
                } else {
                    string args = GetArgs(properties);

                    ow.WriteLine("return {0}()", enableFuncId);
                    ow.WriteLine("     ? {0}{1}({2}, &{3}{4}{5})",
                                 etwNamespace,
                                 naming.GetTemplateId(template),
                                 providerHandleId,
                                 descriptorId,
                                 properties.Count > 0 ? ", " : "",
                                 args);
                    ow.WriteLine("     : ERROR_SUCCESS;");
                }
            }
            ow.WriteLine("}");
        }

        private string GetArgs(PropertyCollection properties)
        {
            var buffer = new StringBuilder();
            using (var w = new StringWriter(buffer))
                AppendParams(w, properties, true, ParamStyle.Name);
            return buffer.ToString();
        }

        private void WriteTemplates(IEnumerable<Provider> providers)
        {
            ow.WriteLine("//");
            ow.WriteLine("// Shared Event Templates");
            ow.WriteLine("//");
            ow.WriteLine();
            WriteNamespaceBegin(options.EtwNamespace);
            var writtenTemplates = new HashSet<string>();
            foreach (var provider in providers) {
                foreach (var evt in provider.Events) {
                    Template template = evt.Template ?? DefaultTemplate;
                    string name = naming.GetTemplateId(template);
                    if (writtenTemplates.Contains(name))
                        continue;
                    WriteTemplate(template);
                    writtenTemplates.Add(name);
                }
            }
            WriteNamespaceEnd(options.EtwNamespace);
            ow.WriteLine();
        }

        private void WriteTemplate(Template template)
        {
            var properties = template.Properties;
            string templateId = naming.GetTemplateId(template);
            string guardId = naming.GetTemplateGuardId(template);
            string countExpr = properties.Count.ToStringInvariant();

            ow.WriteLine("//");
            ow.WriteLine("// Template from manifest: {0}", template.Id ?? "(null)");
            ow.WriteLine("//");

            ow.WriteLine("#ifndef {0}", guardId);
            ow.WriteLine("#define {0}", guardId);
            ow.WriteLine();
            if (!string.IsNullOrWhiteSpace(options.NoInlineAttribute))
                ow.Write("{0} ", options.NoInlineAttribute);
            ow.WriteLine("inline ULONG");
            ow.WriteLine("{0}(", templateId);
            using (ow.IndentScope()) {
                ow.WriteLine("_In_ REGHANDLE {0},", naming.RegHandleId);
                ow.WriteLine("_In_ EVENT_DESCRIPTOR const* {0}{1}",
                             naming.EventDescriptorId,
                             properties.Count > 0 ? "," : string.Empty);
                AppendParams(ow, properties, false, ParamStyle.Full);
                ow.WriteLine(")");
            }
            ow.WriteLine("{");
            using (ow.IndentScope()) {
                if (properties.Count > 0) {
                    ow.WriteLine("EVENT_DATA_DESCRIPTOR {0}[{1}];", naming.EventDataDescriptorId, countExpr);
                    foreach (Property property in properties)
                        AppendDataDesc(property, properties, ow);
                }
                ow.WriteLine(
                    "return EventWrite({0}, {1}, {2}, {3});",
                    naming.RegHandleId,
                    naming.EventDescriptorId,
                    countExpr,
                    properties.Count > 0 ? naming.EventDataDescriptorId : "nullptr");
            }
            ow.WriteLine("}");
            ow.WriteLine("#endif // {0}", guardId);
            ow.WriteLine();
        }

        private string FormatGuid(Guid guid)
        {
            byte[] bytes = guid.ToByteArray();

            var builder = new StringBuilder(68);
            builder.Append("{ ");
            builder.AppendFormat("0x{0:X2}{1:X2}{2:X2}{3:X2}", bytes[3], bytes[2], bytes[1], bytes[0]);
            builder.Append(", ");
            builder.AppendFormat("0x{0:X2}{1:X2}", bytes[5], bytes[4]);
            builder.Append(", ");
            builder.AppendFormat("0x{0:X2}{1:X2}", bytes[7], bytes[6]);
            builder.Append(", { ");
            for (int i = 0; i < 8; ++i)
                builder.AppendFormat(i > 0 ? ", 0x{0:X2}" : "0x{0:X2}", bytes[8 + i]);
            builder.Append(" }}");

            return builder.ToString();
        }

        private enum ParamStyle
        {
            Name,
            Type,
            Full
        }

        private void AppendParams(
            TextWriter writer, IList<Property> properties, bool usePropertyName,
            ParamStyle style)
        {
            foreach (var property in properties) {
                AppendParam(writer, property, properties, usePropertyName, style);
                if (style == ParamStyle.Name) {
                    if (property.Index != properties.Count - 1)
                        writer.Write(", ");
                } else {
                    if (property.Index != properties.Count - 1)
                        writer.Write(',');
                    writer.WriteLine();
                }
            }
        }

        private void AppendParam(
            TextWriter writer, Property property, IList<Property> properties,
            bool usePropertyName, ParamStyle style)
        {
            if (HasExtraLengthParam(property)) {
                string lenArgName = naming.GetLengthArgumentId(property, usePropertyName);
                if (style == ParamStyle.Name)
                    writer.Write("{0}, ", lenArgName);
                else
                    writer.WriteLine("_In_ ULONG {0},", lenArgName);
            }

            string argName = naming.GetArgumentId(property, usePropertyName);
            if (style == ParamStyle.Name)
                writer.Write(argName);
            else
                writer.Write(
                    "{1} {2} {3}{0}",
                    argName,
                    GetArgSalSpec(property, properties, usePropertyName),
                    GetArgType(property),
                    GetParamQualifier(property));
        }

        private void AppendDataDesc(Property property, IList<Property> properties, TextWriter writer)
        {
            if (IsStringData(property) && !property.Count.IsSpecified && !property.Length.IsSpecified) {
                var data = (DataProperty)property;
                string argName = naming.GetNumberedArgId(property.Index);

                writer.WriteLine(
                    "EventDataDescCreate(&{0}[{1}],",
                    naming.EventDataDescriptorId,
                    data.Index);

                if (data.InType.Name == WinEventSchema.UnicodeString) {
                    writer.WriteLine("                    ({0} != nullptr) ? {0} : L\"null\",", argName);
                    writer.WriteLine("                    static_cast<ULONG>(({0} != nullptr) ? ((wcslen({0}) + 1) * sizeof(WCHAR)) : sizeof(L\"null\")));", argName);
                } else {
                    writer.WriteLine("                    ({0} != nullptr) ? {0} : \"null\",", argName);
                    writer.WriteLine("                    static_cast<ULONG>(({0} != nullptr) ? ((strlen({0}) + 1) * sizeof(CHAR)) : sizeof(\"null\")));", argName);
                }
                return;
            }

            writer.WriteLine(
                "EventDataDescCreate(&{0}[{1}], {3}{2}, {4});",
                naming.EventDataDescriptorId,
                property.Index,
                naming.GetNumberedArgId(property.Index),
                GetDataPtrQualifier(property),
                GetDataSizeExpr(property, properties));
        }

        private static bool IsStringData(Property property)
        {
            if (property.Kind == PropertyKind.Struct)
                return false;
            var name = ((DataProperty)property).InType.Name;
            return name == WinEventSchema.UnicodeString ||
                   name == WinEventSchema.AnsiString;
        }

        private bool HasExtraLengthParam(Property property)
        {
            if (property.Kind == PropertyKind.Struct)
                return true;

            var data = (DataProperty)property;
            bool isStr = data.InType.Name == WinEventSchema.UnicodeString ||
                         data.InType.Name == WinEventSchema.AnsiString;
            if (isStr && data.Count.IsSpecified && !data.Length.IsSpecified)
                return true;

            return false;
        }

        private string GetArgSalSpec(
            Property property, IList<Property> properties, bool usePropertyName)
        {
            if (property.Kind == PropertyKind.Struct)
                return "_In_";

            var data = (DataProperty)property;
            if (data.InType.Name.Namespace != WinEventSchema.Namespace)
                throw new InternalException("unhandled type '{0}'", data.InType);

            switch (data.InType.Name.LocalName) {
                case "Int8":
                case "UInt8":
                case "Int16":
                case "UInt16":
                case "Int32":
                case "UInt32":
                case "Int64":
                case "UInt64":
                case "Float":
                case "Double":
                case "Boolean":
                case "HexInt32":
                case "HexInt64":
                case "GUID":
                case "FILETIME":
                case "SYSTEMTIME":
                case "SID":
                    if (data.Count.IsSpecified)
                        return string.Format("_In_reads_({0})", GetCountExpr(data, properties, string.Empty));
                    return "_In_";

                case "Pointer":
                    if (data.Count.IsSpecified)
                        return string.Format("_In_reads_({0})", GetCountExpr(data, properties, string.Empty));
                    return "_In_opt_";

                case "UnicodeString":
                case "AnsiString":
                    if (!data.Count.IsSpecified && !data.Length.IsSpecified)
                        return "_In_opt_";

                    string expr;
                    if (data.Count.IsSpecified && !data.Length.IsSpecified) {
                        if (data.Count.DataPropertyRef != null)
                            expr = naming.GetNumberedArgId(properties.FindIndex(f => f.Name == data.Count.DataPropertyRef));
                        else
                            expr = naming.GetLengthArgumentId(data, usePropertyName);
                    } else {
                        expr = string.Format(
                            "{0}{1}",
                            GetNumberExpr(data.Length, properties, string.Empty),
                            GetNumberExpr(data.Count, properties));
                    }
                    return $"_In_reads_({expr})";

                case "Binary":
                    return string.Format(
                        "_In_reads_({0}{1})",
                        GetNumberExpr(data.Length, properties, string.Empty),
                        GetNumberExpr(data.Count, properties));

                default:
                    throw new InternalException("unhandled type '{0}'", data.InType);
            }
        }

        private string GetArgType(Property property)
        {
            if (property.Kind == PropertyKind.Struct)
                return "const PVOID";

            var data = (DataProperty)property;
            if (data.InType.Name.Namespace != WinEventSchema.Namespace)
                throw new InternalException("unhandled type '{0}'", data.InType);

            switch (data.InType.Name.LocalName) {
                case "Int8": return "int8_t const";
                case "UInt8": return "uint8_t const";
                case "Int16": return "int16_t const";
                case "UInt16": return "uint16_t const";
                case "Int32": return "int32_t const";
                case "UInt32": return "uint32_t const";
                case "Int64": return "int64_t const";
                case "UInt64": return "uint64_t const";
                case "Float": return "float const";
                case "Double": return "double const";
                case "Boolean": return "BOOL const";

                case "UnicodeString":
                    if (data.Length.IsSpecified)
                        return "PCWCH";
                    return "PCWSTR";
                case "AnsiString":
                    if (data.Length.IsSpecified)
                        return "LPCCH";
                    return "LPCSTR";

                case "Pointer": return "void const*";
                case "Binary": return "uint8_t const*";
                case "GUID": return "GUID const&";
                case "FILETIME": return "FILETIME const*";
                case "SYSTEMTIME": return "SYSTEMTIME const*";
                case "SID": return "SID const*";
                case "HexInt32": return "int32_t const";
                case "HexInt64": return "int64_t const";
                default:
                    throw new InternalException("unhandled type '{0}'", data.InType);
            }
        }

        private bool IsPtrLike(Property property)
        {
            if (property.Kind == PropertyKind.Struct)
                return true;

            switch (((DataProperty)property).InType.Name.LocalName) {
                case "UnicodeString":
                case "AnsiString":
                case "Binary":
                case "FILETIME":
                case "SYSTEMTIME":
                case "SID":
                    return true;

                default:
                    return false;
            }
        }

        private string GetParamQualifier(Property property)
        {
            if (IsPtrLike(property))
                return string.Empty;
            return property.Count.IsSpecified ? "*" : string.Empty;
        }

        private string GetDataPtrQualifier(Property property)
        {
            if (IsPtrLike(property))
                return string.Empty;
            return property.Count.IsSpecified ? string.Empty : "&";
        }

        private string GetDataSizeExpr(Property property, IList<Property> properties)
        {
            if (property.Kind == PropertyKind.Struct) {
                string lengthExpr = naming.GetLengthArgumentId(property, false);
                string countExpr = null;
                if (property.Count.IsSpecified)
                    countExpr = GetCountExpr(property, properties, string.Empty) + " * ";
                return string.Format(CultureInfo.InvariantCulture, "{0}{1}", countExpr, lengthExpr);
            }

            var data = (DataProperty)property;
            if (data.InType.Name.Namespace != WinEventSchema.Namespace)
                throw new InternalException("unhandled type '{0}'", data.InType);

            switch (data.InType.Name.LocalName) {
                case "Int8":
                case "UInt8":
                case "Int16":
                case "UInt16":
                case "Int32":
                case "UInt32":
                case "Int64":
                case "UInt64":
                case "Float":
                case "Double":
                case "Boolean":
                case "HexInt32":
                case "HexInt64":
                    return string.Format("sizeof({0}){1}", GetArgType(data), GetCountExpr(data, properties));

                case "GUID": return "sizeof(GUID)" + GetCountExpr(data, properties);
                case "FILETIME": return "sizeof(FILETIME)" + GetCountExpr(data, properties);
                case "SYSTEMTIME": return "sizeof(SYSTEMTIME)" + GetCountExpr(data, properties);

                case "Pointer":
                    return "sizeof(void*)" + GetCountExpr(data, properties);

                case "UnicodeString":
                case "AnsiString":
                    if (!data.Count.IsSpecified && !data.Length.IsSpecified) {
                        throw new Exception();
                    }

                    string lengthExpr;
                    if (data.Count.IsSpecified && !data.Length.IsSpecified)
                        lengthExpr = naming.GetLengthArgumentId(data, false);
                    else
                        lengthExpr = GetLengthExpr(data, properties, string.Empty);

                    string countExpr = null;
                    if (data.Count.IsSpecified && data.Length.IsSpecified)
                        countExpr = GetCountExpr(data, properties);

                    string type = data.InType.Name == WinEventSchema.UnicodeString ? "WCHAR" : "CHAR";

                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "(ULONG)(sizeof({0}){1}*{2})",
                        type,
                        countExpr,
                        lengthExpr);

                case "Binary":
                    return string.Format(
                        "(ULONG)sizeof(char){0}{1}",
                        GetLengthExpr(data, properties),
                        GetCountExpr(data, properties));

                case "SID":
                    return string.Format(
                        "GetLengthSid((PSID){0})",
                        naming.GetNumberedArgId(properties.IndexOf(data)));

                default:
                    throw new InternalException("unhandled type '{0}'", data.InType);
            }
        }

        private string GetCountExpr(Property property, IList<Property> properties, string prefix = "*")
        {
            return GetNumberExpr(property.Count, properties, prefix);
        }

        private string GetLengthExpr(Property property, IList<Property> properties, string prefix = "*")
        {
            return GetNumberExpr(property.Length, properties, prefix);
        }

        private string GetNumberExpr(IPropertyNumber number, IList<Property> properties, string prefix = "*")
        {
            if (number.IsVariable) {
                int idx = properties.FindIndex(f => f.Name == number.DataPropertyRef);
                return prefix + naming.GetNumberedArgId(idx);
            }
            if (number.IsFixed)
                return prefix + number.Value;
            return string.Empty;
        }
    }
}
