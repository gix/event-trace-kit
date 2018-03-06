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
    using EventManifestFramework.Internal.Extensions;
    using EventManifestFramework.Schema;
    using EventManifestFramework.Schema.Base;
    using EventManifestFramework.Support;

    [Export(typeof(ICodeGenerator))]
    [ExportMetadata("Name", "mc")]
    internal sealed class MCCompatibleCodeGenerator : ICodeGenerator
    {
        private const int EnableByteSize = 4;

        private static readonly Template DefaultTemplate = new Template(
            Located.Create(string.Empty));

        private readonly ICodeGenOptions options;
        private readonly ICodeGenNomenclature naming = new Nomenclature();

        private IndentableTextWriter ow;

        private sealed class Nomenclature : BaseCodeGenNomenclature
        {
            public override string EventDataDescriptorId => "EventData";

            public override string RegHandleId => "RegHandle";

            public override string EventDescriptorId => "Descriptor";

            public override string GetIdentifier(Provider provider)
            {
                return GetIdentifierFromName(provider.Name);
            }

            public override string GetIdentifier(MapItem item, Map map)
            {
                return map.Symbol + item.Symbol;
            }

            public override string GetTemplateSuffix(Template template)
            {
                if (template.Properties.Count == 0)
                    return "EventDescriptor";
                var builder = new StringBuilder("_", template.Properties.Count);
                foreach (var property in template.Properties)
                    builder.Append(MangleProperty(property, template.Properties));
                return builder.ToString();
            }

            public override string GetTemplateId(Template template)
            {
                return "Template" + GetTemplateSuffix(template);
            }

            public override string GetTemplateGuardId(Template template)
            {
                return GetTemplateId(template) + "_def";
            }

            public override string GetEventDescriptorId(Event evt)
            {
                return GetIdentifier(evt);
            }

            public override string GetProviderGuidId(Provider provider)
            {
                return provider.Symbol;
            }

            public override string GetProviderContextId(Provider provider)
            {
                return provider.Symbol + "_Context";
            }

            public override string GetTaskGuidId(Task task)
            {
                return GetIdentifierFromName(task.Name) + "Id";
            }

            public override string GetEventFuncId(Event evt, string prefix = null, string suffix = null)
            {
                return prefix + GetIdentifier(evt) + suffix;
            }

            public override string GetNumberedArgId(int idx)
            {
                return "_Arg" + idx;
            }

            public override string GetArgumentId(Property property, bool usePropertyName)
            {
                return usePropertyName ? GetIdentifier(property) : GetNumberedArgId(property.Index);
            }

            public override string GetLengthArgumentId(Property property, bool usePropertyName)
            {
                return GetArgumentId(property, usePropertyName) + "_Len_";
            }
        }

        [ImportingConstructor]
        public MCCompatibleCodeGenerator(ICodeGenOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            this.options = options;
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

        private void GenerateCore(EventManifest manifest)
        {
            WriteHeader();
            foreach (var provider in manifest.Providers) {
                ow.WriteLine("//+");
                ow.WriteLine("// Provider {0} Event Count {1}", provider.Name, provider.Events.Count);
                ow.WriteLine("//+");
                ow.WriteLine(
                    "EXTERN_C __declspec(selectany) const GUID {0} = {1};",
                    naming.GetProviderGuidId(provider),
                    FormatGuid(provider.Id));
                ow.WriteLine();

                WriteChannels(provider);
                WriteLevels(provider);
                WriteOpcodes(provider);
                WriteTasks(provider);
                WriteKeywords(provider);
                WriteEventDescriptors(provider);

                ow.WriteLine("//");
                ow.WriteLine("// Allow Diasabling of code generation");
                ow.WriteLine("//");
                ow.WriteLine("#ifndef MCGEN_DISABLE_PROVIDER_CODE_GENERATION");
                ow.WriteLine();
                WriteRegistration(provider);
                WriteEventMacros(provider);
                ow.WriteLine("#endif // MCGEN_DISABLE_PROVIDER_CODE_GENERATION");
                ow.WriteLine();
                ow.WriteLine();
            }

            WriteTemplates(manifest.Providers);

            ow.WriteLine("#if defined(__cplusplus)");
            ow.WriteLine("};");
            ow.WriteLine("#endif");

            var primaryResourceSet = manifest.Resources.FirstOrDefault();
            if (primaryResourceSet != null)
                WriteMessages(primaryResourceSet);

            ow.Flush();
        }

        private void WriteHeader()
        {
            WritePreamble();
        }

        private void WriteRegistration(Provider provider)
        {
            ow.WriteLine("//");
            ow.WriteLine("// Globals ");
            ow.WriteLine("//");
            ow.WriteLine();
            WriteMaps(provider);
            ow.WriteLine();

            WriteEnableBits(provider);

            ow.WriteLine();
            ow.WriteLine("EXTERN_C __declspec(selectany) REGHANDLE {0} = (REGHANDLE)0;",
                         naming.GetProviderHandleId(provider));

            WriteUnregisterCode();

            ow.WriteLine("//");
            ow.WriteLine("// Register with ETW Vista +");
            ow.WriteLine("//");
            ow.WriteLine("#ifndef EventRegister{0}", naming.GetIdentifier(provider));
            ow.WriteLine("#define EventRegister{0}() McGenEventRegister(&{1}, McGenControlCallbackV2, &{2}, &{3})",
                         naming.GetIdentifier(provider),
                         naming.GetProviderGuidId(provider),
                         naming.GetProviderContextId(provider),
                         naming.GetProviderHandleId(provider));
            ow.WriteLine("#endif");
            ow.WriteLine();
            ow.WriteLine("//");
            ow.WriteLine("// UnRegister with ETW");
            ow.WriteLine("//");
            ow.WriteLine("#ifndef EventUnregister{0}", naming.GetIdentifier(provider));
            ow.WriteLine("#define EventUnregister{0}() McGenEventUnregister(&{1})", naming.GetIdentifier(provider), naming.GetProviderHandleId(provider));
            ow.WriteLine("#endif");
            ow.WriteLine();
        }

        private void WriteEnableBits(Provider provider)
        {
            var enableBits = provider.EnableBits;
            int enableByteCount = (enableBits.Count + 31) / 32;

            if (enableByteCount == 0) {
                ow.WriteLine(
                    "EXTERN_C __declspec(selectany) MCGEN_TRACE_CONTEXT {0} " +
                    "= {{0}};",
                    naming.GetProviderContextId(provider));
                return;
            }

            ow.WriteLine();
            ow.WriteLine("//");
            ow.WriteLine("// Event Enablement Bits");
            ow.WriteLine("//");
            ow.WriteLine(
                "EXTERN_C __declspec(selectany) DECLSPEC_CACHEALIGN ULONG {0}[{1}];",
                naming.GetProviderEnableBitsId(provider),
                enableByteCount);

            ow.Write(
                "EXTERN_C __declspec(selectany) const ULONGLONG {0}[{1}] = {{",
                naming.GetProviderKeywordsId(provider),
                enableBits.Count);
            WriteList(enableBits, b => $"0x{b.KeywordMask:x}");
            ow.WriteLine("};");

            ow.Write(
                "EXTERN_C __declspec(selectany) const UCHAR {0}[{1}] = {{",
                naming.GetProviderLevelsId(provider),
                enableBits.Count);
            WriteList(enableBits, b => b.Level);
            ow.WriteLine("};");

            ow.WriteLine(
                "EXTERN_C __declspec(selectany) MCGEN_TRACE_CONTEXT {0} " +
                "= {{0, 0, 0, 0, 0, 0, 0, 0, {1}, {2}, {3}, {4}}};",
                naming.GetProviderContextId(provider),
                enableBits.Count,
                naming.GetProviderEnableBitsId(provider),
                naming.GetProviderKeywordsId(provider),
                naming.GetProviderLevelsId(provider));
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

            ow.WriteLine("//");
            ow.WriteLine("// Channel");
            ow.WriteLine("//");
            foreach (var channel in provider.Channels)
                ow.WriteLine("#define {0} 0x{1:x}",
                             naming.GetIdentifier(channel), channel.Value);

            ow.WriteLine();
        }

        private void WriteLevels(Provider provider)
        {
            if (provider.Levels.Count == 0 || provider.Levels.All(t => t.Imported))
                return;

            ow.WriteLine("//");
            ow.WriteLine("// Levels");
            ow.WriteLine("//");
            foreach (var level in provider.Levels.Where(t => !t.Imported))
                ow.WriteLine("#define {0} 0x{1:x}",
                             naming.GetIdentifier(level), level.Value);
            ow.WriteLine();
        }

        private void WriteOpcodes(Provider provider)
        {
            var allOpcodes = provider.GetAllOpcodes();
            if (!allOpcodes.Any() || allOpcodes.All(t => t.Imported))
                return;

            ow.WriteLine("//");
            ow.WriteLine("// Opcodes");
            ow.WriteLine("//");
            foreach (var opcode in allOpcodes.Where(t => !t.Imported))
                ow.WriteLine("#define {0} 0x{1:x}",
                             naming.GetIdentifier(opcode), opcode.Value);

            ow.WriteLine();
        }

        private void WriteTasks(Provider provider)
        {
            if (provider.Tasks.Count == 0 || provider.Tasks.All(t => t.Imported))
                return;

            ow.WriteLine("//");
            ow.WriteLine("// Tasks");
            ow.WriteLine("//");
            foreach (var task in provider.Tasks.Where(t => !t.Imported)) {
                ow.WriteLine("#define {0} 0x{1:x}", naming.GetIdentifier(task), task.Value);
                if (task.Guid.GetValueOrDefault() != Guid.Empty)
                    ow.WriteLine("EXTERN_C __declspec(selectany) const GUID {0} = {1};",
                                 naming.GetTaskGuidId(task), FormatGuid(task.Guid.GetValueOrDefault()));
            }
            ow.WriteLine();
        }

        private void WriteKeywords(Provider provider)
        {
            if (provider.Keywords.Count == 0 || provider.Keywords.All(t => t.Imported))
                return;

            ow.WriteLine("//");
            ow.WriteLine("// Keyword");
            ow.WriteLine("//");
            foreach (var keyword in provider.Keywords.Where(t => !t.Imported))
                ow.WriteLine("#define {0} 0x{1:X}",
                             naming.GetIdentifier(keyword), keyword.Mask);
            ow.WriteLine();
        }

        private void WriteMaps(Provider provider)
        {
            if (provider.Maps.Count == 0)
                return;

            foreach (var map in provider.Maps.Where(m => m.Kind == MapKind.BitMap).Cast<BitMap>())
                if (map.Symbol != null && map.Items.Count > 0)
                    WriteMap(map);

            foreach (var map in provider.Maps.Where(m => m.Kind == MapKind.ValueMap).Cast<ValueMap>())
                if (map.Symbol != null && map.Items.Count > 0)
                    WriteMap(map);
        }

        private void WriteMap(Map map)
        {
            string guard = string.Format("_{0}_def", naming.GetIdentifier(map));
            ow.WriteLine("#ifndef {0} ", guard);
            ow.WriteLine("#define {0} ", guard);
            ow.WriteLine("typedef enum _{0}", naming.GetIdentifier(map));
            ow.WriteLine("{");
            using (ow.IndentScope()) {
                foreach (var item in map.Items) {
                    if (item.Symbol == null)
                        continue;
                    string symbol = naming.GetIdentifier(item, map);
                    ow.WriteLine("{0} = {1},", symbol, unchecked((int)item.Value.Value));
                }
            }
            ow.WriteLine("}}{0};", naming.GetIdentifier(map));
            ow.WriteLine("#endif");
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
                "EXTERN_C __declspec(selectany) const EVENT_DESCRIPTOR {0} = " +
                "{{0x{1:x}, 0x{2:x}, 0x{3:x}, 0x{4:x}, 0x{5:x}, 0x{6:x}, 0x{7:x}{8};",
                naming.GetEventDescriptorId(evt),
                evt.Value, version, channel, level, opcode, task, keywordMask, "}");
            ow.WriteLine("#define {0}_value 0x{1:x}", naming.GetIdentifier(evt), evt.Value);
        }

        private void WriteEventMacros(Provider provider)
        {
            for (int i = 0; i < provider.Events.Count; ++i) {
                if (i > 0)
                    ow.WriteLine();
                WriteEventMacro(provider.Events[i]);
            }
        }

        private void WriteEventMacro(Event evt)
        {
            if (evt.NotLogged.GetValueOrDefault())
                return;

            var template = evt.Template ?? DefaultTemplate;
            var properties = template.Properties;
            var enableFuncId = naming.GetEventFuncId(evt, "EventEnabled");
            var symbol = naming.GetIdentifier(evt);

            ow.WriteLine("//");
            ow.WriteLine("// Enablement check macro for {0}", symbol);
            ow.WriteLine("//");
            ow.WriteLine();
            ow.WriteLine(
                "#define {0}() (({1}[{2}] & 0x{3:X8}) != 0)",
                enableFuncId,
                naming.GetProviderEnableBitsId(evt.Provider),
                evt.EnableBit.GetIndex(EnableByteSize),
                evt.EnableBit.GetMask(EnableByteSize));
            ow.WriteLine();

            var argsBuilder = new StringBuilder();
            using (var w = new StringWriter(argsBuilder))
                AppendParams(w, properties, true, true);
            string args = argsBuilder.ToString();

            ow.WriteLine("//");
            ow.WriteLine("// Event Macro for {0}", symbol);
            ow.WriteLine("//");
            ow.WriteLine("#define {0}({1})\\", naming.GetEventFuncId(evt, options.LogCallPrefix), args);
            ow.WriteLine("        {0}() ?\\", enableFuncId);
            ow.WriteLine("        {0}({1}, &{2}{3}{4})\\",
                         naming.GetTemplateId(template),
                         naming.GetProviderHandleId(evt.Provider),
                         naming.GetEventDescriptorId(evt),
                         properties.Count > 0 ? ", " : "",
                         args);
            ow.WriteLine("        : ERROR_SUCCESS\\");
        }

        private void WriteTemplates(IEnumerable<Provider> providers)
        {
            ow.WriteLine("//");
            ow.WriteLine("// Allow Diasabling of code generation");
            ow.WriteLine("//");
            ow.WriteLine("#ifndef MCGEN_DISABLE_PROVIDER_CODE_GENERATION");
            ow.WriteLine();

            ow.WriteLine("//");
            ow.WriteLine("// Template Functions");
            ow.WriteLine("//");
            var writtenTemplates = new HashSet<string>();
            foreach (var provider in providers) {
                foreach (var evt in provider.Events) {
                    if (evt.NotLogged.GetValueOrDefault())
                        continue;

                    Template template = evt.Template ?? DefaultTemplate;
                    string name = naming.GetTemplateId(template);
                    if (writtenTemplates.Contains(name))
                        continue;
                    WriteTemplate(template);
                    writtenTemplates.Add(name);
                }
            }

            ow.WriteLine("#endif // MCGEN_DISABLE_PROVIDER_CODE_GENERATION");
        }

        private void WriteTemplate(Template template)
        {
            var properties = template.Properties;
            string templateId = naming.GetTemplateId(template);
            string guardId = naming.GetTemplateGuardId(template);
            string suffix = naming.GetTemplateSuffix(template);
            string countMacroName = "ARGUMENT_COUNT" + suffix;

            ow.WriteLine("//");
            ow.WriteLine("//Template from manifest : {0}", template.Id ?? "(null)");
            ow.WriteLine("//");

            ow.WriteLine("#ifndef {0}", guardId);
            ow.WriteLine("#define {0}", guardId);
            ow.WriteLine("ETW_INLINE");
            ow.WriteLine("ULONG");
            ow.WriteLine("{0}(", templateId);
            using (ow.IndentScope()) {
                ow.WriteLine("_In_ REGHANDLE {0},", naming.RegHandleId);
                ow.WriteLine("_In_ PCEVENT_DESCRIPTOR {0}{1}",
                             naming.EventDescriptorId,
                             properties.Count > 0 ? "," : string.Empty);
                AppendParams(ow, properties, false, false);
                ow.WriteLine(")");
            }
            ow.WriteLine("{");
            using (ow.IndentScope()) {
                if (properties.Count > 0) {
                    ow.WriteLine("#define {0} {1}", countMacroName, properties.Count);
                    ow.WriteLine();
                    ow.WriteLine("EVENT_DATA_DESCRIPTOR {0}[{1}];", naming.EventDataDescriptorId, countMacroName);
                    ow.WriteLine();
                    foreach (Property property in properties) {
                        AppendDataDesc(property, properties, ow);
                        ow.WriteLine();
                    }
                }

                ow.WriteLine(
                    "return EventWrite({0}, {1}, {2}, {3});",
                    naming.RegHandleId,
                    naming.EventDescriptorId,
                    properties.Count > 0 ? countMacroName : "0",
                    properties.Count > 0 ? naming.EventDataDescriptorId : "NULL");
            }
            ow.WriteLine("}");
            ow.WriteLine("#endif");
            ow.WriteLine();
        }

        private void WriteMessages(LocalizedResourceSet resourceSet)
        {
            var messages = resourceSet.Strings.Used().Where(x => !x.Imported).ToList();
            messages.StableSortBy(m => m.Id);
            foreach (var message in messages) {
                ow.WriteLine("#define {0,-24} 0x{1:X8}L", naming.GetIdentifier(message), message.Id);
            }
        }

        private string FormatGuid(Guid guid)
        {
            return guid.ToString("X");
        }

        private void AppendParams(TextWriter writer, IList<Property> properties, bool usePropertyName, bool nameOnly)
        {
            foreach (var property in properties) {
                AppendParam(writer, property, properties, usePropertyName, nameOnly);
                if (nameOnly) {
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
            TextWriter writer, Property property, IList<Property> properties, bool usePropertyName, bool nameOnly)
        {
            if (HasExtraLengthParam(property)) {
                string lenArgName = naming.GetLengthArgumentId(property, usePropertyName);
                if (nameOnly)
                    writer.Write("{0}, ", lenArgName);
                else
                    writer.WriteLine("_In_ ULONG {0},", lenArgName);
            }

            string argName = naming.GetArgumentId(property, usePropertyName);
            if (nameOnly)
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
            if (IsStringData(property) && !property.Count.IsMultiple && !property.Length.IsSpecified) {
                var data = (DataProperty)property;
                string argName = naming.GetNumberedArgId(property.Index);

                writer.WriteLine(
                    "EventDataDescCreate(&{0}[{1}],",
                    naming.EventDataDescriptorId,
                    data.Index);

                if (data.InType.Name == WinEventSchema.UnicodeString) {
                    writer.WriteLine("                    ({0} != NULL) ? {0} : L\"NULL\",", argName);
                    writer.WriteLine("                    ({0} != NULL) ? (ULONG)((wcslen({0}) + 1) * sizeof(WCHAR)) : (ULONG)sizeof(L\"NULL\"));", argName);
                } else {
                    writer.WriteLine("                    ({0} != NULL) ? {0} : \"NULL\",", argName);
                    writer.WriteLine("                    ({0} != NULL) ? (ULONG)((strlen({0}) + 1) * sizeof(CHAR)) : (ULONG)sizeof(\"NULL\"));", argName);
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
            if (isStr && data.Count.IsMultiple && !data.Length.IsSpecified)
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
                    if (data.Count.IsMultiple)
                        return string.Format("_In_reads_({0})", GetCountExpr(data, properties, string.Empty));
                    return "_In_";

                case "Pointer":
                    if (data.Count.IsMultiple)
                        return string.Format("_In_reads_({0})", GetCountExpr(data, properties, string.Empty));
                    return "_In_opt_";

                case "UnicodeString":
                case "AnsiString":
                    if (!data.Count.IsMultiple && !data.Length.IsSpecified)
                        return "_In_opt_";

                    string expr;
                    if (data.Count.IsSpecified && !data.Length.IsSpecified) {
                        if (data.Count.IsVariable)
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
                case "Int8": return "const char";
                case "UInt8": return "const UCHAR";
                case "Int16": return "const signed short";
                case "UInt16": return "const unsigned short";
                case "Int32": return "const signed int";
                case "UInt32": return "const unsigned int";
                case "Int64": return "signed __int64";
                case "UInt64": return "unsigned __int64";
                case "Float": return "const float";
                case "Double": return "const double";
                case "Boolean": return "const BOOL";

                case "UnicodeString":
                    if (data.Length.IsSpecified)
                        return "PCWCH";
                    return "PCWSTR";
                case "AnsiString":
                    if (data.Length.IsSpecified)
                        return "LPCCH";
                    return "LPCSTR";

                case "Pointer": return "const void *";
                case "Binary": return "const BYTE*";
                case "GUID": return "LPCGUID";
                case "FILETIME": return "const FILETIME*";
                case "SYSTEMTIME": return "const SYSTEMTIME*";
                case "SID": return "const SID *";
                case "HexInt32": return "const signed int";
                case "HexInt64": return "signed __int64";
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
                case "GUID":
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
            return property.Count.IsMultiple ? "*" : string.Empty;
        }

        private string GetDataPtrQualifier(Property property)
        {
            if (IsPtrLike(property))
                return string.Empty;
            return property.Count.IsMultiple ? string.Empty : "&";
        }

        private string GetDataSizeExpr(Property property, IList<Property> properties)
        {
            if (property.Kind == PropertyKind.Struct) {
                string lengthExpr = naming.GetLengthArgumentId(property, false);
                string countExpr = null;
                if (property.Count.IsMultiple)
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
                    return "sizeof(PVOID)" + GetCountExpr(data, properties);

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
            if (number.IsFixedMultiple)
                return prefix + number.Value;
            return string.Empty;
        }

        private void WriteUnregisterCode()
        {
            string code = @"
#if !defined(McGenEventRegisterUnregister)
#define McGenEventRegisterUnregister
#pragma warning(push)
#pragma warning(disable:6103)
DECLSPEC_NOINLINE __inline
ULONG __stdcall
McGenEventRegister(
    _In_ LPCGUID ProviderId,
    _In_opt_ PENABLECALLBACK EnableCallback,
    _In_opt_ PVOID CallbackContext,
    _Inout_ PREGHANDLE RegHandle
    )
/*++

Routine Description:

    This function register the provider with ETW USER mode.

Arguments:
    ProviderId - Provider Id to be register with ETW.

    EnableCallback - Callback to be used.

    CallbackContext - Context for this provider.

    RegHandle - Pointer to Registration handle.

Remarks:

    If the handle != NULL will return ERROR_SUCCESS

--*/
{
    ULONG Error;


    if (*RegHandle) {
        //
        // already registered
        //
        return ERROR_SUCCESS;
    }

    Error = EventRegister( ProviderId, EnableCallback, CallbackContext, RegHandle);

    return Error;
}
#pragma warning(pop)


DECLSPEC_NOINLINE __inline
ULONG __stdcall
McGenEventUnregister(_Inout_ PREGHANDLE RegHandle)
/*++

Routine Description:

    Unregister from ETW USER mode

Arguments:
            RegHandle this is the pointer to the provider context
Remarks:
            If Provider has not register RegHandle = NULL,
            return ERROR_SUCCESS
--*/
{
    ULONG Error;


    if(!(*RegHandle)) {
        //
        // Provider has not registerd
        //
        return ERROR_SUCCESS;
    }

    Error = EventUnregister(*RegHandle);
    *RegHandle = (REGHANDLE)0;

    return Error;
}
#endif
";
            ow.WriteLine(code);
        }

        private void WritePreamble()
        {
            string preamble = @"//**********************************************************************`
//* This is an include file generated by Instrumentation Compiler.     *`
//**********************************************************************`
#pragma once
#include <wmistr.h>
#include <evntrace.h>
#include ""evntprov.h""
//
//  Initial Defs
//
#if !defined(ETW_INLINE)
#define ETW_INLINE DECLSPEC_NOINLINE __inline
#endif

#if defined(__cplusplus)
extern ""C"" {
#endif

//
// Allow Diasabling of code generation
//
#ifndef MCGEN_DISABLE_PROVIDER_CODE_GENERATION
#if  !defined(McGenDebug)
#define McGenDebug(a,b)
#endif


#if !defined(MCGEN_TRACE_CONTEXT_DEF)
#define MCGEN_TRACE_CONTEXT_DEF
typedef struct _MCGEN_TRACE_CONTEXT
{
    TRACEHANDLE            RegistrationHandle;
    TRACEHANDLE            Logger;
    ULONGLONG              MatchAnyKeyword;
    ULONGLONG              MatchAllKeyword;
    ULONG                  Flags;
    ULONG                  IsEnabled;
    UCHAR                  Level;
    UCHAR                  Reserve;
    USHORT                 EnableBitsCount;
    PULONG                 EnableBitMask;
    const ULONGLONG*       EnableKeyWords;
    const UCHAR*           EnableLevel;
} MCGEN_TRACE_CONTEXT, *PMCGEN_TRACE_CONTEXT;
#endif

#if !defined(MCGEN_LEVEL_KEYWORD_ENABLED_DEF)
#define MCGEN_LEVEL_KEYWORD_ENABLED_DEF
FORCEINLINE
BOOLEAN
McGenLevelKeywordEnabled(
    _In_ PMCGEN_TRACE_CONTEXT EnableInfo,
    _In_ UCHAR Level,
    _In_ ULONGLONG Keyword
    )
{
    //
    // Check if the event Level is lower than the level at which
    // the channel is enabled.
    // If the event Level is 0 or the channel is enabled at level 0,
    // all levels are enabled.
    //

    if ((Level <= EnableInfo->Level) || // This also covers the case of Level == 0.
        (EnableInfo->Level == 0)) {

        //
        // Check if Keyword is enabled
        //

        if ((Keyword == (ULONGLONG)0) ||
            ((Keyword & EnableInfo->MatchAnyKeyword) &&
             ((Keyword & EnableInfo->MatchAllKeyword) == EnableInfo->MatchAllKeyword))) {
            return TRUE;
        }
    }

    return FALSE;

}
#endif

#if !defined(MCGEN_EVENT_ENABLED_DEF)
#define MCGEN_EVENT_ENABLED_DEF
FORCEINLINE
BOOLEAN
McGenEventEnabled(
    _In_ PMCGEN_TRACE_CONTEXT EnableInfo,
    _In_ PCEVENT_DESCRIPTOR EventDescriptor
    )
{

    return McGenLevelKeywordEnabled(EnableInfo, EventDescriptor->Level, EventDescriptor->Keyword);

}
#endif


//
// EnableCheckMacro
//
#ifndef MCGEN_ENABLE_CHECK
#define MCGEN_ENABLE_CHECK(Context, Descriptor) (Context.IsEnabled &&  McGenEventEnabled(&Context, &Descriptor))
#endif

#if !defined(MCGEN_CONTROL_CALLBACK)
#define MCGEN_CONTROL_CALLBACK

DECLSPEC_NOINLINE __inline
VOID
__stdcall
McGenControlCallbackV2(
    _In_ LPCGUID SourceId,
    _In_ ULONG ControlCode,
    _In_ UCHAR Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ PEVENT_FILTER_DESCRIPTOR FilterData,
    _Inout_opt_ PVOID CallbackContext
    )
/*++

Routine Description:

    This is the notification callback for Vista.

Arguments:

    SourceId - The GUID that identifies the session that enabled the provider.

    ControlCode - The parameter indicates whether the provider
                  is being enabled or disabled.

    Level - The level at which the event is enabled.

    MatchAnyKeyword - The bitmask of keywords that the provider uses to
                      determine the category of events that it writes.

    MatchAllKeyword - This bitmask additionally restricts the category
                      of events that the provider writes.

    FilterData - The provider-defined data.

    CallbackContext - The context of the callback that is defined when the provider
                      called EtwRegister to register itself.

Remarks:

    ETW calls this function to notify provider of enable/disable

--*/
{
    PMCGEN_TRACE_CONTEXT Ctx = (PMCGEN_TRACE_CONTEXT)CallbackContext;
    ULONG Ix;
#ifndef MCGEN_PRIVATE_ENABLE_CALLBACK_V2
    UNREFERENCED_PARAMETER(SourceId);
    UNREFERENCED_PARAMETER(FilterData);
#endif

    if (Ctx == NULL) {
        return;
    }

    switch (ControlCode) {

        case EVENT_CONTROL_CODE_ENABLE_PROVIDER:
            Ctx->Level = Level;
            Ctx->MatchAnyKeyword = MatchAnyKeyword;
            Ctx->MatchAllKeyword = MatchAllKeyword;
            Ctx->IsEnabled = EVENT_CONTROL_CODE_ENABLE_PROVIDER;

            for (Ix = 0; Ix < Ctx->EnableBitsCount; Ix += 1) {
                if (McGenLevelKeywordEnabled(Ctx, Ctx->EnableLevel[Ix], Ctx->EnableKeyWords[Ix]) != FALSE) {
                    Ctx->EnableBitMask[Ix >> 5] |= (1 << (Ix % 32));
                } else {
                    Ctx->EnableBitMask[Ix >> 5] &= ~(1 << (Ix % 32));
                }
            }
            break;

        case EVENT_CONTROL_CODE_DISABLE_PROVIDER:
            Ctx->IsEnabled = EVENT_CONTROL_CODE_DISABLE_PROVIDER;
            Ctx->Level = 0;
            Ctx->MatchAnyKeyword = 0;
            Ctx->MatchAllKeyword = 0;
            if (Ctx->EnableBitsCount > 0) {
                RtlZeroMemory(Ctx->EnableBitMask, (((Ctx->EnableBitsCount - 1) / 32) + 1) * sizeof(ULONG));
            }
            break;

        default:
            break;
    }

#ifdef MCGEN_PRIVATE_ENABLE_CALLBACK_V2
    //
    // Call user defined callback
    //
    MCGEN_PRIVATE_ENABLE_CALLBACK_V2(
        SourceId,
        ControlCode,
        Level,
        MatchAnyKeyword,
        MatchAllKeyword,
        FilterData,
        CallbackContext
        );
#endif

    return;
}

#endif
";
            ow.WriteLine(preamble);
            ow.WriteLine("#endif // MCGEN_DISABLE_PROVIDER_CODE_GENERATION");
            ow.WriteLine();
        }
    }
}
