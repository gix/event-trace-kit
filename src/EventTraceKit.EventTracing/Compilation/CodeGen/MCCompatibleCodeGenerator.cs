namespace EventTraceKit.EventTracing.Compilation.CodeGen
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using EventTraceKit.EventTracing.Compilation.Support;
    using EventTraceKit.EventTracing.Internal.Extensions;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Schema.Base;
    using EventTraceKit.EventTracing.Support;

    [Export(typeof(ICodeGenerator))]
    [ExportMetadata("Name", "mc")]
    internal sealed class MCCompatibleCodeGenerator : ICodeGenerator
    {
        private static readonly Template DefaultTemplate = new Template(
            Located.Create("(null)"));

        private readonly ICodeGenOptions options;
        private readonly ICodeGenNomenclature naming = new Nomenclature();

        private IndentableTextWriter ow;

        private sealed class Nomenclature : BaseCodeGenNomenclature
        {
            public override string ContextId => "Context";
            public override string EventDataDescriptorId => "EventData";

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
                    return string.Empty;
                var builder = new StringBuilder(template.Properties.Count);
                foreach (var property in template.Properties)
                    builder.Append(MangleProperty(property, template.Properties));
                return builder.ToString();
            }

            public override string GetTemplateId(Template template)
            {
                return "McTemplateU0" + GetTemplateSuffix(template);
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

            public override string GetProviderHandleId(Provider provider)
            {
                if (provider.ControlGuid == null)
                    return GetIdentifierFromName(provider.Name) + "Handle";
                return provider.Symbol + "_Handle";
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
            using var baseWriter = IO.CreateStreamWriter(output);
            using var writer = new IndentableTextWriter(baseWriter);
            try {
                ow = writer;
                GenerateCore(manifest);
            } finally {
                ow = null;
            }
        }

        private void GenerateCore(EventManifest manifest)
        {
            WriteHeader(manifest.Providers.Any(x => x.ControlGuid != null));

            foreach (var provider in manifest.Providers) {
                ow.WriteLine("//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                ow.WriteLine("// Provider \"{0}\" event count {1}", provider.Name, provider.Events.Count);
                ow.WriteLine("//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                ow.WriteLine();
                ow.WriteLine("// Provider GUID = {0:D}", provider.Id);
                ow.WriteLine(
                    "EXTERN_C __declspec(selectany) const GUID {0} = {1};",
                    naming.GetProviderGuidId(provider),
                    FormatGuid(provider.Id));
                ow.WriteLine();

                WriteProviderTraits(provider);
                WriteChannels(provider);
                WriteLevels(provider);
                WriteOpcodes(provider);
                WriteTasks(provider);
                WriteKeywords(provider);
                WriteEventDescriptors(provider);

                ow.WriteLine("//");
                ow.WriteLine("// MCGEN_DISABLE_PROVIDER_CODE_GENERATION macro:");
                ow.WriteLine("// Define this macro to have the compiler skip the generated functions in this");
                ow.WriteLine("// header.");
                ow.WriteLine("//");
                ow.WriteLine("#ifndef MCGEN_DISABLE_PROVIDER_CODE_GENERATION");
                ow.WriteLine();
                WriteRegistration(provider);
                WriteEventMacros(provider);
                ow.WriteLine("#endif // MCGEN_DISABLE_PROVIDER_CODE_GENERATION");
                ow.WriteLine();
            }

            WriteTemplates(manifest.Providers);

            ow.WriteLine();
            ow.WriteLine("#if defined(__cplusplus)");
            ow.WriteLine("};");
            ow.WriteLine("#endif");

            var primaryResourceSet = manifest.Resources.FirstOrDefault();
            if (primaryResourceSet != null)
                WriteMessages(primaryResourceSet);

            ow.Flush();
        }

        private void WriteHeader(bool includeEventSet)
        {
            WritePreamble(includeEventSet);
        }

        private static bool RequiresTraits(Provider provider)
        {
            return provider.ControlGuid != null || provider.GroupGuid != null;
        }

        private void WriteRegistration(Provider provider)
        {
            string providerId = naming.GetIdentifier(provider);
            string registerFunction = "McGenEventRegister";
            string contextId = naming.GetProviderContextId(provider);
            string handleOrContextId = naming.GetProviderHandleId(provider);
            string controlGuidId = naming.GetProviderGuidId(provider);

            if (provider.ControlGuid != null) {
                providerId = "_" + provider.Symbol;
                controlGuidId = naming.GetProviderControlGuidId(provider);
            }

            if (RequiresTraits(provider)) {
                registerFunction = "McGenEventRegisterContext";
                handleOrContextId = contextId;
            }

            WriteMaps(provider);

            WriteEnableBits(provider);

            ow.WriteLine("//");
            ow.WriteLine("// Register with ETW using the control GUID specified in the manifest.");
            ow.WriteLine("// Invoke this macro during module initialization (i.e. program startup,");
            ow.WriteLine("// DLL process attach, or driver load) to initialize the provider.");
            ow.WriteLine("// Note that if this function returns an error, the error means that");
            ow.WriteLine("// will not work, but no action needs to be taken -- even if EventRegister");
            ow.WriteLine("// returns an error, it is generally safe to use EventWrite and");
            ow.WriteLine("// EventUnregister macros (they will be no-ops if EventRegister failed).");
            ow.WriteLine("//");
            ow.WriteLine("#ifndef EventRegister{0}", providerId);
            ow.WriteLine("#define EventRegister{0}() {1}(&{2}, McGenControlCallbackV2, &{3}, &{4})",
                providerId,
                registerFunction,
                controlGuidId,
                contextId,
                handleOrContextId);
            ow.WriteLine("#endif");
            ow.WriteLine();

            ow.WriteLine("//");
            ow.WriteLine("// Register with ETW using a specific control GUID (i.e. a GUID other than what");
            ow.WriteLine("// is specified in the manifest). Advanced scenarios only.");
            ow.WriteLine("//");
            ow.WriteLine("#ifndef EventRegisterByGuid{0}", providerId);
            ow.WriteLine("#define EventRegisterByGuid{0}(Guid) {1}(&(Guid), McGenControlCallbackV2, &{2}, &{3})",
                         providerId,
                         registerFunction,
                         contextId,
                         handleOrContextId);
            ow.WriteLine("#endif");
            ow.WriteLine();

            ow.WriteLine("//");
            ow.WriteLine("// Unregister with ETW and close the provider.");
            ow.WriteLine("// Invoke this macro during module shutdown (i.e. program exit, DLL process");
            ow.WriteLine("// detach, or driver unload) to unregister the provider.");
            ow.WriteLine("// Note that you MUST call EventUnregister before DLL or driver unload");
            ow.WriteLine("// (not optional): failure to unregister a provider before DLL or driver unload");
            ow.WriteLine("// will result in crashes.");
            ow.WriteLine("//");
            ow.WriteLine("#ifndef EventUnregister{0}", providerId);
            ow.WriteLine("#define EventUnregister{0}() McGenEventUnregister(&{1})", providerId, naming.GetProviderHandleId(provider));
            ow.WriteLine("#endif");
            ow.WriteLine();
        }

        private void WriteEnableBits(Provider provider)
        {
            var enableBits = provider.EnableBits;
            int enableByteCount = (enableBits.Count + 31) / 32;

            if (enableByteCount != 0) {
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
                    "EXTERN_C __declspec(selectany) const unsigned char {0}[{1}] = {{",
                    naming.GetProviderLevelsId(provider),
                    enableBits.Count);
                WriteList(enableBits, b => b.Level);
                ow.WriteLine("};");
            }

            ow.WriteLine();
            ow.WriteLine("//");
            ow.WriteLine("// Provider context");
            ow.WriteLine("//");

            if (enableByteCount == 0) {
                ow.WriteLine(
                    "EXTERN_C __declspec(selectany) MCGEN_TRACE_CONTEXT {0} " +
                    "= {{0, (ULONG_PTR){1}}};",
                    naming.GetProviderContextId(provider),
                    $"{provider.Symbol}_Traits");
            } else {
                ow.WriteLine(
                    "EXTERN_C __declspec(selectany) MCGEN_TRACE_CONTEXT {0} " +
                    "= {{0, (ULONG_PTR){1}, 0, 0, 0, 0, 0, 0, {2}, {3}, {4}, {5}}};",
                    naming.GetProviderContextId(provider),
                    $"{provider.Symbol}_Traits",
                    enableBits.Count,
                    naming.GetProviderEnableBitsId(provider),
                    naming.GetProviderKeywordsId(provider),
                    naming.GetProviderLevelsId(provider));
            }

            ow.WriteLine();

            ow.WriteLine("//");
            ow.WriteLine("// Provider REGHANDLE");
            ow.WriteLine("//");
            ow.WriteLine(
                "#define {0} ({1}.RegistrationHandle)",
                naming.GetProviderHandleId(provider),
                naming.GetProviderContextId(provider));
            ow.WriteLine();

            ow.WriteLine("//");
            ow.WriteLine("// This macro is set to 0, indicating that the EventWrite[Name] macros do not");
            ow.WriteLine("// have an Activity parameter. This is controlled by the -km and -um options.");
            ow.WriteLine("//");
            ow.WriteLine("#define {0} 0", $"{provider.Symbol}_EventWriteActivity");
            ow.WriteLine();
        }

        private void WriteList<T, TItem>(IReadOnlyList<T> list, Func<T, TItem> selector)
        {
            for (int i = 0; i < list.Count; ++i) {
                if (i > 0)
                    ow.Write(", ");
                ow.Write(selector(list[i]));
            }
        }

        private static IEnumerable<int> EnumerateCodePoints(string str)
        {
            for (int i = 0; i < str.Length;) {
                int cp;
                if (char.IsSurrogate(str, i)) {
                    cp = char.ConvertToUtf32(str, i);
                    i += 2;
                } else {
                    cp = str[i];
                    ++i;
                }

                yield return cp;
            }
        }

        private static string ToCStringLiteral(string str, out int length)
        {
            length = 0;

            var builder = new StringBuilder();
            var buffer = new byte[4];
            foreach (int cp in EnumerateCodePoints(str)) {
                if (IsPrintableAscii(cp)) {
                    builder.Append((char)cp);
                    ++length;
                    continue;
                }

                string s = char.ConvertFromUtf32(cp);
                int len = Encoding.UTF8.GetBytes(s, 0, s.Length, buffer, 0);
                for (int i = 0; i < len; ++i) {
                    builder.AppendFormat("\\{0:D03}", Convert.ToString(buffer[i], 8));
                    ++length;
                }
            }

            return builder.ToString();
        }

        private static string ToCStringLiteral(byte[] bytes)
        {
            return string.Concat(bytes.Select(x => $"\\x{x:X2}"));
        }

        private static bool IsPrintableAscii(int cp)
        {
            return cp >= 0x20 && cp <= 0x7E;
        }

        private void WriteProviderTraits(Provider provider)
        {
            if (provider.ControlGuid != null) {
                ow.WriteLine("// Control GUID = {0:D}", provider.ControlGuid);
                ow.WriteLine(
                    "EXTERN_C __declspec(selectany) const GUID {0} = {1};",
                    naming.GetProviderControlGuidId(provider),
                    FormatGuid(provider.ControlGuid.Value));
                ow.WriteLine();
            }

            var traitsMacroId = $"{provider.Symbol}_Traits";
            ow.WriteLine("#ifndef {0}", traitsMacroId);
            if (provider.ControlGuid != null || provider.GroupGuid != null || provider.IncludeNameInTraits == true) {
                int totalSize = 2 /*Traits size*/;

                string providerName;
                if (provider.IncludeNameInTraits == true) {
                    providerName = ToCStringLiteral(provider.Name, out var literalLength) + "\\x00";
                    totalSize += literalLength + 1;
                } else {
                    providerName = "\\x00";
                    totalSize += 1;
                }

                string groupGuidTrait = null;
                if (provider.GroupGuid != null) {
                    groupGuidTrait = "\\x13\\x00\\x01" + ToCStringLiteral(provider.GroupGuid.Value.ToByteArray());
                    totalSize += 2 /*Trait size*/ + 1 /*Trait type*/ + 16 /*Guid size*/;
                }

                string decodeGuidTrait = null;
                if (provider.ControlGuid != null) {
                    decodeGuidTrait = "\\x13\\x00\\x02" + ToCStringLiteral(provider.Id.Value.ToByteArray());
                    totalSize += 2 /*Trait size*/ + 1 /*Trait type*/ + 16 /*Guid size*/;
                }

                byte[] sizeBytes = BitConverter.GetBytes((ushort)totalSize);

                ow.WriteLine("#define {0} ( \\", traitsMacroId);
                ow.WriteLine("    \"{0}\" /* Total size of traits = {1} */ \\", ToCStringLiteral(sizeBytes), totalSize);
                if (provider.IncludeNameInTraits == true)
                    ow.WriteLine("    \"{0}\" /* Provider name */ \\", providerName);
                else
                    ow.WriteLine("    \"{0}\" /* Provider name omitted */ \\", providerName);
                if (groupGuidTrait != null)
                    ow.WriteLine("    \"{0}\" /* Group guid */ \\", groupGuidTrait);
                if (decodeGuidTrait != null)
                    ow.WriteLine("    \"{0}\" /* Decode guid */ \\", decodeGuidTrait);
                ow.WriteLine("    )");
            } else {
                ow.WriteLine("#define {0} NULL", traitsMacroId);
            }
            ow.WriteLine("#endif // {0}", traitsMacroId);
            ow.WriteLine();
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
            ow.WriteLine("#ifndef {0}", guard);
            ow.WriteLine("#define {0}", guard);
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
            ow.WriteLine("}} {0};", naming.GetIdentifier(map));
            ow.WriteLine("#endif // {0}", guard);
            ow.WriteLine();
        }

        private void WriteEventDescriptors(Provider provider)
        {
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
            byte channel = evt.GetDescriptorChannelValue();
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
            var propImpls = properties.Select(x => CreatePropImpl(x)).ToList();

            ow.WriteLine("//");
            ow.WriteLine("// Enablement check macro for {0}", symbol);
            ow.WriteLine("//");
            ow.WriteLine(
                "#define {0}() MCGEN_EVENT_BIT_SET({1}, {2})",
                enableFuncId,
                naming.GetProviderEnableBitsId(evt.Provider),
                evt.EnableBit.Bit);
            ow.WriteLine();

            var argsBuilder = new StringBuilder();
            using (var w = new StringWriter(argsBuilder))
                AppendParams(w, propImpls, true, true);
            string args = argsBuilder.ToString();

            ow.WriteLine("//");
            ow.WriteLine("// Event write macros for {0}", symbol);
            ow.WriteLine("//");
            if (propImpls.Any(x => x.RequiresPacking)) {
                ow.WriteLine("// MC Note :: Macro for event id = {0}", evt.Value);
                ow.WriteLine("// This event contains complex types that require the caller to pack the data.");
                ow.WriteLine("// Refer to the note at the top of this header for additional details.");
                ow.WriteLine("//");
            }
            ow.WriteLine("#define {0}({1}) \\", naming.GetEventFuncId(evt, options.LogCallPrefix), args);
            ow.WriteLine("        MCGEN_EVENT_ENABLED({0}) \\", symbol);
            ow.WriteLine("        ? {0}(&{1}, &{2}{3}{4}) : 0",
                         naming.GetTemplateId(template),
                         naming.GetProviderContextId(evt.Provider),
                         naming.GetEventDescriptorId(evt),
                         properties.Count > 0 ? ", " : "",
                         args);
            ow.WriteLine("#define {0}_AssumeEnabled({1}) \\", naming.GetEventFuncId(evt, options.LogCallPrefix), args);
            ow.WriteLine("        {0}(&{1}, &{2}{3}{4})",
                         naming.GetTemplateId(template),
                         naming.GetProviderContextId(evt.Provider),
                         naming.GetEventDescriptorId(evt),
                         properties.Count > 0 ? ", " : "",
                         args);

            ow.WriteLine();
        }

        private void WriteTemplates(IEnumerable<Provider> providers)
        {
            ow.WriteLine("//");
            ow.WriteLine("// MCGEN_DISABLE_PROVIDER_CODE_GENERATION macro:");
            ow.WriteLine("// Define this macro to have the compiler skip the generated functions in this");
            ow.WriteLine("// header.");
            ow.WriteLine("//");
            ow.WriteLine("#ifndef MCGEN_DISABLE_PROVIDER_CODE_GENERATION");
            ow.WriteLine();

            ow.WriteLine("//");
            ow.WriteLine("// Template Functions");
            ow.WriteLine("//");
            var allTemplates = new Dictionary<string, Template>();
            foreach (var provider in providers) {
                foreach (var evt in provider.Events) {
                    if (evt.NotLogged.GetValueOrDefault())
                        continue;

                    Template template = evt.Template ?? DefaultTemplate;
                    string name = naming.GetTemplateId(template);
                    if (allTemplates.ContainsKey(name))
                        continue;
                    allTemplates.Add(name, template);
                }
            }

            foreach (var template in allTemplates.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Value)) {
                WriteTemplate(template);
            }

            ow.WriteLine("#endif // MCGEN_DISABLE_PROVIDER_CODE_GENERATION");
        }

        private void WriteTemplate(Template template)
        {
            var properties = template.Properties;
            string templateId = naming.GetTemplateId(template);
            string guardId = naming.GetTemplateGuardId(template);
            string countMacroName = templateId + "_ARGCOUNT";
            var propImpls = properties.Select(x => CreatePropImpl(x)).ToList();
            int argCount = propImpls.Sum(x => x.GetArgCount());

            ow.WriteLine("//");
            ow.WriteLine("//Template from manifest : {0}", template.Id ?? "(null)");
            ow.WriteLine("//");

            ow.WriteLine("#ifndef {0}", guardId);
            ow.WriteLine("#define {0}", guardId);
            ow.WriteLine("ETW_INLINE");
            ow.WriteLine("ULONG");
            ow.WriteLine("{0}(", templateId);
            using (ow.IndentScope()) {
                ow.WriteLine("_In_ PMCGEN_TRACE_CONTEXT {0},", naming.ContextId);
                ow.WriteLine("_In_ PCEVENT_DESCRIPTOR {0}{1}",
                             naming.EventDescriptorId,
                             properties.Count > 0 ? "," : string.Empty);
                AppendParams(ow, propImpls, false, false);
                ow.WriteLine(")");
            }
            ow.WriteLine("{");
            ow.WriteLine("#define {0} {1}", countMacroName, argCount);
            using (ow.IndentScope()) {
                ow.WriteLine();
                ow.WriteLine("EVENT_DATA_DESCRIPTOR {0}[{1} + 1];", naming.EventDataDescriptorId, countMacroName);
                ow.WriteLine();
                int index = 0;
                foreach (var property in propImpls) {
                    property.AppendDataDescriptor(ref index, ow);
                    ow.WriteLine();
                }

                ow.WriteLine(
                    "return McGenEventWrite({0}, {1}, NULL, {2} + 1, {3});",
                    naming.ContextId,
                    naming.EventDescriptorId,
                    countMacroName,
                    naming.EventDataDescriptorId);
            }
            ow.WriteLine("}");
            ow.WriteLine("#endif // {0}", guardId);
            ow.WriteLine();
        }

        private void WriteMessages(LocalizedResourceSet resourceSet)
        {
            ow.WriteLine();

            var messages = resourceSet.Strings.Used().Where(x => !x.Imported).ToList();
            messages.StableSortBy(m => m.Id);
            foreach (var message in messages) {
                ow.WriteLine("#define {0,-36} 0x{1:X8}L", naming.GetIdentifier(message), message.Id);
            }
        }

        private string FormatGuid(Guid guid)
        {
            return guid.ToString("X").Replace(",", ", ");
        }

        private void AppendParams(TextWriter writer, IList<PropImpl> properties, bool usePropertyName, bool nameOnly)
        {
            int index = 0;
            foreach (var property in properties) {
                if (nameOnly)
                    property.AppendParameterName(writer, usePropertyName);
                else
                    property.AppendParameter(writer, usePropertyName);

                if (nameOnly) {
                    if (index++ != properties.Count - 1)
                        writer.Write(", ");
                } else {
                    if (index++ != properties.Count - 1)
                        writer.Write(',');
                    writer.WriteLine();
                }
            }
        }

        private static string GetNumberExpr(ICodeGenNomenclature naming, IPropertyNumber number, string prefix = "*")
        {
            if (number.IsVariable) {
                int idx = number.DataProperty.Index;
                return prefix + naming.GetNumberedArgId(idx);
            }
            if (number.IsFixedMultiple)
                return prefix + number.Value;
            return string.Empty;
        }

        private void WritePreamble(bool includeEventSet)
        {
            string preamble = @"//**********************************************************************`
//* This is an include file generated by Event Manifest Compiler.      *`
//**********************************************************************`
#pragma once

//*****************************************************************************
//
// Notes on the ETW event code generated by MC:
//
// - Structures and arrays of structures are treated as an opaque binary blob.
//   The caller is responsible for packing the data for the structure into a
//   single region of memory, with no padding between values. The macro will
//   have an extra parameter for the length of the blob.
// - Arrays of nul-terminated strings must be packed by the caller into a
//   single binary blob containing the correct number of strings, with a nul
//   after each string. The size of the blob is specified in characters, and
//   includes the final nul.
// - If a SID is provided, its length will be determined by calling
//   GetLengthSid.
// - Arrays of SID are treated as a single binary blob. The caller is
//   responsible for packing the SID values into a single region of memory with
//   no padding.
// - The length attribute on the data element in the manifest is significant
//   for values with intype win:UnicodeString, win:AnsiString, or win:Binary.
//   The length attribute must be specified for win:Binary, and is optional for
//   win:UnicodeString and win:AnsiString (if no length is given, the strings
//   are assumed to be nul-terminated). For win:UnicodeString, the length is
//   measured in characters, not bytes.
// - For an array of win:UnicodeString, win:AnsiString, or win:Binary, the
//   length attribute applies to every value in the array, so every value in
//   the array must have the same length. The values in the array are provided
//   to the macro via a single pointer -- the caller is responsible for packing
//   all of the values into a single region of memory with no padding between
//   values.
// - Values of type win:CountedUnicodeString, win:CountedAnsiString, and
//   win:CountedBinary can be generated and collected on Vista or later.
//   However, they may not decode properly without the Windows 10 2018 Fall
//   Update.
// - Arrays of type win:CountedUnicodeString, win:CountedAnsiString, and
//   win:CountedBinary must be packed by the caller into a single region of
//   memory. The format for each item is a UINT16 byte-count followed by that
//   many bytes of data. When providing the array to the generated macro, you
//   must provide the total size of the packed array data, including the UINT16
//   sizes for each item. In the case of win:CountedUnicodeString, the data
//   size is specified in WCHAR (16-bit) units. In the case of
//   win:CountedAnsiString and win:CountedBinary, the data size is specified in
//   bytes.
//
//*****************************************************************************

#include <wmistr.h>
#include <evntrace.h>
#include <evntprov.h>

#if !defined(ETW_INLINE)
#define ETW_INLINE DECLSPEC_NOINLINE __inline
#endif

#if defined(__cplusplus)
extern ""C"" {
#endif

//
// MCGEN_DISABLE_PROVIDER_CODE_GENERATION macro:
// Define this macro to have the compiler skip the generated functions in this
// header.
//
#ifndef MCGEN_DISABLE_PROVIDER_CODE_GENERATION

//
// MCGEN_USE_KERNEL_MODE_APIS macro:
// Controls whether the generated code uses kernel-mode or user-mode APIs.
// - Set to 0 to use Windows user-mode APIs such as EventRegister.
// - Set to 1 to use Windows kernel-mode APIs such as EtwRegister.
// Default is based on whether the _ETW_KM_ macro is defined (i.e. by wdm.h).
// Note that the APIs can also be overridden directly, e.g. by setting the
// MCGEN_EVENTWRITETRANSFER or MCGEN_EVENTREGISTER macros.
//
#ifndef MCGEN_USE_KERNEL_MODE_APIS
  #ifdef _ETW_KM_
    #define MCGEN_USE_KERNEL_MODE_APIS 1
  #else
    #define MCGEN_USE_KERNEL_MODE_APIS 0
  #endif
#endif // MCGEN_USE_KERNEL_MODE_APIS

//
// MCGEN_HAVE_EVENTSETINFORMATION macro:
// Controls how McGenEventSetInformation uses the EventSetInformation API.
// - Set to 0 to disable the use of EventSetInformation
//   (McGenEventSetInformation will always return an error).
// - Set to 1 to directly invoke MCGEN_EVENTSETINFORMATION.
// - Set to 2 to to locate EventSetInformation at runtime via GetProcAddress
//   (user-mode) or MmGetSystemRoutineAddress (kernel-mode).
// Default is determined as follows:
// - If MCGEN_EVENTSETINFORMATION has been customized, set to 1
//   (i.e. use MCGEN_EVENTSETINFORMATION).
// - Else if the target OS version has EventSetInformation, set to 1
//   (i.e. use MCGEN_EVENTSETINFORMATION).
// - Else set to 2 (i.e. try to dynamically locate EventSetInformation).
// Note that an McGenEventSetInformation function will only be generated if one
// or more provider in a manifest has provider traits.
//
#ifndef MCGEN_HAVE_EVENTSETINFORMATION
  #ifdef MCGEN_EVENTSETINFORMATION             // if MCGEN_EVENTSETINFORMATION has been customized,
    #define MCGEN_HAVE_EVENTSETINFORMATION   1 //   directly invoke MCGEN_EVENTSETINFORMATION(...).
  #elif MCGEN_USE_KERNEL_MODE_APIS             // else if using kernel-mode APIs,
    #if NTDDI_VERSION >= 0x06040000            //   if target OS is Windows 10 or later,
      #define MCGEN_HAVE_EVENTSETINFORMATION 1 //     directly invoke MCGEN_EVENTSETINFORMATION(...).
    #else                                      //   else
      #define MCGEN_HAVE_EVENTSETINFORMATION 2 //     find ""EtwSetInformation"" via MmGetSystemRoutineAddress.
    #endif                                     // else (using user-mode APIs)
  #else                                        //   if target OS and SDK is Windows 8 or later,
    #if WINVER >= 0x0602 && defined(EVENT_FILTER_TYPE_SCHEMATIZED)
      #define MCGEN_HAVE_EVENTSETINFORMATION 1 //     directly invoke MCGEN_EVENTSETINFORMATION(...).
    #else                                      //   else
      #define MCGEN_HAVE_EVENTSETINFORMATION 2 //     find ""EventSetInformation"" via GetModuleHandleExW/GetProcAddress.
    #endif
  #endif
#endif // MCGEN_HAVE_EVENTSETINFORMATION

//
// MCGEN_EVENTWRITETRANSFER macro:
// Override to use a custom API.
//
#ifndef MCGEN_EVENTWRITETRANSFER
  #if MCGEN_USE_KERNEL_MODE_APIS
    #define MCGEN_EVENTWRITETRANSFER   EtwWriteTransfer
  #else
    #define MCGEN_EVENTWRITETRANSFER   EventWriteTransfer
  #endif
#endif // MCGEN_EVENTWRITETRANSFER

//
// MCGEN_EVENTREGISTER macro:
// Override to use a custom API.
//
#ifndef MCGEN_EVENTREGISTER
  #if MCGEN_USE_KERNEL_MODE_APIS
    #define MCGEN_EVENTREGISTER        EtwRegister
  #else
    #define MCGEN_EVENTREGISTER        EventRegister
  #endif
#endif // MCGEN_EVENTREGISTER

//
// MCGEN_EVENTSETINFORMATION macro:
// Override to use a custom API.
// (McGenEventSetInformation also affected by MCGEN_HAVE_EVENTSETINFORMATION.)
//
#ifndef MCGEN_EVENTSETINFORMATION
  #if MCGEN_USE_KERNEL_MODE_APIS
    #define MCGEN_EVENTSETINFORMATION  EtwSetInformation
  #else
    #define MCGEN_EVENTSETINFORMATION  EventSetInformation
  #endif
#endif // MCGEN_EVENTSETINFORMATION

//
// MCGEN_EVENTUNREGISTER macro:
// Override to use a custom API.
//
#ifndef MCGEN_EVENTUNREGISTER
  #if MCGEN_USE_KERNEL_MODE_APIS
    #define MCGEN_EVENTUNREGISTER      EtwUnregister
  #else
    #define MCGEN_EVENTUNREGISTER      EventUnregister
  #endif
#endif // MCGEN_EVENTUNREGISTER

//
// MCGEN_PENABLECALLBACK macro:
// Override to use a custom function pointer type.
// (Should match the type used by MCGEN_EVENTREGISTER.)
//
#ifndef MCGEN_PENABLECALLBACK
  #if MCGEN_USE_KERNEL_MODE_APIS
    #define MCGEN_PENABLECALLBACK      PETWENABLECALLBACK
  #else
    #define MCGEN_PENABLECALLBACK      PENABLECALLBACK
  #endif
#endif // MCGEN_PENABLECALLBACK

//
// MCGEN_GETLENGTHSID macro:
// Override to use a custom API.
//
#ifndef MCGEN_GETLENGTHSID
  #if MCGEN_USE_KERNEL_MODE_APIS
    #define MCGEN_GETLENGTHSID(p)      RtlLengthSid((PSID)(p))
  #else
    #define MCGEN_GETLENGTHSID(p)      GetLengthSid((PSID)(p))
  #endif
#endif // MCGEN_GETLENGTHSID

//
// MCGEN_EVENT_ENABLED macro:
// Controls how the EventWrite[EventName] macros determine whether an event is
// enabled. The default behavior is for EventWrite[EventName] to use the
// EventEnabled[EventName] macros.
//
#ifndef MCGEN_EVENT_ENABLED
#define MCGEN_EVENT_ENABLED(EventName) EventEnabled##EventName()
#endif

//
// MCGEN_EVENT_BIT_SET macro:
// Implements testing a bit in an array of ULONG, optimized for CPU type.
//
#ifndef MCGEN_EVENT_BIT_SET
#  if defined(_M_IX86) || defined(_M_X64)
#    define MCGEN_EVENT_BIT_SET(EnableBits, BitPosition) ((((const unsigned char*)EnableBits)[BitPosition >> 3] & (1u << (BitPosition & 7))) != 0)
#  else
#    define MCGEN_EVENT_BIT_SET(EnableBits, BitPosition) ((EnableBits[BitPosition >> 5] & (1u << (BitPosition & 31))) != 0)
#  endif
#endif // MCGEN_EVENT_BIT_SET

//
// MCGEN_ENABLE_CHECK macro:
// Determines whether the specified event would be considered as enabled
// based on the state of the specified context. Slightly faster than calling
// McGenEventEnabled directly.
//
#ifndef MCGEN_ENABLE_CHECK
#define MCGEN_ENABLE_CHECK(Context, Descriptor) (Context.IsEnabled && McGenEventEnabled(&Context, &Descriptor))
#endif

#if !defined(MCGEN_TRACE_CONTEXT_DEF)
#define MCGEN_TRACE_CONTEXT_DEF
typedef struct _MCGEN_TRACE_CONTEXT
{
    TRACEHANDLE            RegistrationHandle;
    TRACEHANDLE            Logger;      // Used as pointer to provider traits.
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
#endif // MCGEN_TRACE_CONTEXT_DEF

#if !defined(MCGEN_LEVEL_KEYWORD_ENABLED_DEF)
#define MCGEN_LEVEL_KEYWORD_ENABLED_DEF
//
// Determines whether an event with a given Level and Keyword would be
// considered as enabled based on the state of the specified context.
// Note that you may want to use MCGEN_ENABLE_CHECK instead of calling this
// function directly.
//
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
#endif // MCGEN_LEVEL_KEYWORD_ENABLED_DEF

#if !defined(MCGEN_EVENT_ENABLED_DEF)
#define MCGEN_EVENT_ENABLED_DEF
//
// Determines whether the specified event would be considered as enabled based
// on the state of the specified context. Note that you may want to use
// MCGEN_ENABLE_CHECK instead of calling this function directly.
//
FORCEINLINE
BOOLEAN
McGenEventEnabled(
    _In_ PMCGEN_TRACE_CONTEXT EnableInfo,
    _In_ PCEVENT_DESCRIPTOR EventDescriptor
    )
{
    return McGenLevelKeywordEnabled(EnableInfo, EventDescriptor->Level, EventDescriptor->Keyword);
}
#endif // MCGEN_EVENT_ENABLED_DEF

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

    This is the notification callback for Windows Vista and later.

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
#endif // MCGEN_PRIVATE_ENABLE_CALLBACK_V2

    return;
}

#endif // MCGEN_CONTROL_CALLBACK

#ifndef McGenEventWrite_def
#define McGenEventWrite_def
DECLSPEC_NOINLINE __inline
ULONG __stdcall
McGenEventWrite(
    _In_ PMCGEN_TRACE_CONTEXT Context,
    _In_ PCEVENT_DESCRIPTOR Descriptor,
    _In_opt_ LPCGUID ActivityId,
    _In_range_(1, 128) ULONG EventDataCount,
    _Inout_updates_(EventDataCount) EVENT_DATA_DESCRIPTOR* EventData
    )
{
    const USHORT UNALIGNED* Traits;

    // Some customized MCGEN_EVENTWRITETRANSFER macros might ignore ActivityId.
    UNREFERENCED_PARAMETER(ActivityId);

    Traits = (const USHORT UNALIGNED*)(UINT_PTR)Context->Logger;

    if (Traits == NULL) {
        EventData[0].Ptr = 0;
        EventData[0].Size = 0;
        EventData[0].Reserved = 0;
    } else {
        EventData[0].Ptr = (ULONG_PTR)Traits;
        EventData[0].Size = *Traits;
        EventData[0].Reserved = 2; // EVENT_DATA_DESCRIPTOR_TYPE_PROVIDER_METADATA
    }

    return MCGEN_EVENTWRITETRANSFER(
        Context->RegistrationHandle,
        Descriptor,
        ActivityId,
        NULL,
        EventDataCount,
        EventData);
}
#endif // McGenEventWrite_def
";

            string eventSetCode = @"#ifndef McGenEventSetInformation_def
#define McGenEventSetInformation_def
_IRQL_requires_max_(PASSIVE_LEVEL)
DECLSPEC_NOINLINE __inline
ULONG __stdcall
McGenEventSetInformation(
    _In_ REGHANDLE RegHandle,
    _In_ EVENT_INFO_CLASS InformationClass,
    _In_opt_bytecount_(InformationLength) PVOID EventInformation,
    _In_ ULONG InformationLength
    )
/*++

Routine Description:

    This function invokes EventSetInformation to provide additional information
    to the ETW runtime.

    Note that the implementation of this function depends on the values of
    the MCGEN_HAVE_EVENTSETINFORMATION and MCGEN_EVENTSETINFORMATION macros.
    Depending on the values of these macros, this function may call
    EventSetInformation directly, may dynamically-load EventSetInformation
    via GetProcAddress, or may call a replacement function.

Arguments:

    RegHandle - Registration handle returned by EventRegister.

    InformationClass - Type of operation to be performed on the registration
                       object.

    EventInformation - The input buffer.

    InformationLength - Size of the input buffer.

--*/
{
    ULONG Error;

#if MCGEN_HAVE_EVENTSETINFORMATION == 1

#pragma warning(suppress: 6387) // It's ok for EventInformation to be null if InformationLength is 0.
    Error = MCGEN_EVENTSETINFORMATION(
        RegHandle,
        InformationClass,
        EventInformation,
        InformationLength);

#elif MCGEN_HAVE_EVENTSETINFORMATION == 2

#if MCGEN_USE_KERNEL_MODE_APIS
    typedef NTSTATUS(NTAPI* PFEtwSetInformation)(
        _In_ REGHANDLE regHandle,
        _In_ EVENT_INFO_CLASS informationClass,
        _In_opt_bytecount_(informationLength) PVOID eventInformation,
        _In_ ULONG informationLength);
    static UNICODE_STRING strEtwSetInformation = {
        sizeof(L""EtwSetInformation"") - 2,
        sizeof(L""EtwSetInformation"") - 2,
        L""EtwSetInformation""
    };
    PFEtwSetInformation pfEtwSetInformation;
#pragma warning(push)
#pragma warning(disable: 4055) // Allow the cast from a PVOID to a PFN
    pfEtwSetInformation = (PFEtwSetInformation)MmGetSystemRoutineAddress(&strEtwSetInformation);
#pragma warning(pop)
    if (pfEtwSetInformation)
    {
        Error = pfEtwSetInformation(
            RegHandle,
            InformationClass,
            EventInformation,
            InformationLength);
    }
    else
    {
        Error = STATUS_NOT_SUPPORTED;
    }
#else // !MCGEN_USE_KERNEL_MODE_APIS
    HMODULE hEventing;
    Error = ERROR_NOT_SUPPORTED;
    if (GetModuleHandleExW(0, L""api-ms-win-eventing-provider-l1-1-0"", &hEventing) ||
        GetModuleHandleExW(0, L""advapi32"", &hEventing))
    {
        typedef ULONG(WINAPI* PFEventSetInformation)(
            _In_ REGHANDLE regHandle,
            _In_ EVENT_INFO_CLASS informationClass,
            _In_opt_bytecount_(informationLength) PVOID eventInformation,
            _In_ ULONG informationLength);
        PFEventSetInformation pfEventSetInformation =
            (PFEventSetInformation)GetProcAddress(hEventing, ""EventSetInformation"");
        if (pfEventSetInformation)
        {
            Error = pfEventSetInformation(
                RegHandle,
                InformationClass,
                EventInformation,
                InformationLength);
        }

        FreeLibrary(hEventing);
    }
#endif // MCGEN_USE_KERNEL_MODE_APIS

#else // MCGEN_HAVE_EVENTSETINFORMATION == 0

    (void)RegHandle;
    (void)InformationClass;
    (void)EventInformation;
    (void)InformationLength;

  #if MCGEN_USE_KERNEL_MODE_APIS
    Error = STATUS_NOT_SUPPORTED;
  #else // !MCGEN_USE_KERNEL_MODE_APIS
    Error = ERROR_NOT_SUPPORTED;
  #endif // MCGEN_USE_KERNEL_MODE_APIS

#endif // MCGEN_HAVE_EVENTSETINFORMATION

    return Error;
}
#endif // McGenEventSetInformation_def

#ifndef McGenEventRegisterContext_def
#define McGenEventRegisterContext_def
#pragma warning(push)
#pragma warning(disable:6103)
_IRQL_requires_max_(PASSIVE_LEVEL)
DECLSPEC_NOINLINE __inline
ULONG __stdcall
McGenEventRegisterContext(
    _In_ LPCGUID ProviderId,
    _In_opt_ MCGEN_PENABLECALLBACK EnableCallback,
    _In_opt_ PVOID CallbackContext,
    _Inout_ MCGEN_TRACE_CONTEXT* Context
    )
/*++

Routine Description:

    This function registers the provider with ETW and registers provider
    traits. The EventRegister[ProviderName] macro will use this function
    instead of McGenEventRegister if the provider has traits to be registered.

Arguments:

    ProviderId - Provider ID to register with ETW.

    EnableCallback - Callback to be used.

    CallbackContext - Context for the callback.

    Context - Provider context.

Remarks:

    Should not be called if the provider is already registered (i.e. should not
    be called if Context->RegistrationHandle != 0). Repeatedly registering a
    provider is a bug and may indicate a race condition.

--*/
{
    ULONG Error;

    if (Context->RegistrationHandle != 0)
    {
#if MCGEN_USE_KERNEL_MODE_APIS
        Error = (ULONG)STATUS_INVALID_PARAMETER;
#else
        Error = ERROR_INVALID_PARAMETER;
#endif
    }
    else
    {
        Error = MCGEN_EVENTREGISTER(
            ProviderId,
            EnableCallback,
            CallbackContext,
            &Context->RegistrationHandle);
        if (Error == 0 && Context->Logger != 0)
        {
            (void)McGenEventSetInformation(
                Context->RegistrationHandle,
                (EVENT_INFO_CLASS)2, // EventProviderSetTraits
                (void*)Context->Logger,
                *(USHORT const UNALIGNED*)Context->Logger);
        }
    }

    return Error;
}
#pragma warning(pop)
#endif // McGenEventRegisterContext_def
";

            string registerCode = @"#if !defined(McGenEventRegisterUnregister)
#define McGenEventRegisterUnregister

#pragma warning(push)
#pragma warning(disable:6103)
DECLSPEC_NOINLINE __inline
ULONG __stdcall
McGenEventRegister(
    _In_ LPCGUID ProviderId,
    _In_opt_ MCGEN_PENABLECALLBACK EnableCallback,
    _In_opt_ PVOID CallbackContext,
    _Inout_ PREGHANDLE RegHandle
    )
/*++

Routine Description:

    This function registers the provider with ETW.

Arguments:

    ProviderId - Provider ID to register with ETW.

    EnableCallback - Callback to be used.

    CallbackContext - Context for the callback.

    RegHandle - Pointer to registration handle.

Remarks:

    Should not be called if the provider is already registered (i.e. should not
    be called if *RegHandle != 0). Repeatedly registering a provider is a bug
    and may indicate a race condition. However, for compatibility with previous
    behavior, this function will return SUCCESS in this case.

--*/
{
    ULONG Error;

    if (*RegHandle != 0)
    {
        Error = 0; // ERROR_SUCCESS
    }
    else
    {
        Error = MCGEN_EVENTREGISTER(ProviderId, EnableCallback, CallbackContext, RegHandle);
    }

    return Error;
}
#pragma warning(pop)

DECLSPEC_NOINLINE __inline
ULONG __stdcall
McGenEventUnregister(_Inout_ PREGHANDLE RegHandle)
/*++

Routine Description:

    Unregister from ETW and set *RegHandle = 0.

Arguments:

    RegHandle - the pointer to the provider registration handle

Remarks:

    If provider has not been registered (i.e. if *RegHandle == 0),
    return SUCCESS. It is safe to call McGenEventUnregister even if the
    call to McGenEventRegister returned an error.

--*/
{
    ULONG Error;

    if(*RegHandle == 0)
    {
        Error = 0; // ERROR_SUCCESS
    }
    else
    {
        Error = MCGEN_EVENTUNREGISTER(*RegHandle);
        *RegHandle = (REGHANDLE)0;
    }

    return Error;
}

#endif // McGenEventRegisterUnregister
";
            ow.WriteLine(preamble);
            if (includeEventSet)
                ow.WriteLine(eventSetCode);
            ow.WriteLine(registerCode);
            ow.WriteLine("#endif // MCGEN_DISABLE_PROVIDER_CODE_GENERATION");
            ow.WriteLine();
        }

        private PropImpl CreatePropImpl(Property property)
        {
            if (property.Kind == PropertyKind.Struct) {
                return new StructPropImpl(naming, (StructProperty)property);
            }

            if (property.Kind == PropertyKind.Data) {
                var dataProperty = (DataProperty)property;
                if (dataProperty.InType.Name.Namespace != WinEventSchema.Namespace)
                    throw new InternalException("unhandled type '{0}'", dataProperty.InType);

                switch (dataProperty.InType.Name.LocalName) {
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
                        return new PrimitivePropImpl(naming, dataProperty);

                    case "UnicodeString":
                    case "AnsiString":
                        return new StringPropImpl(naming, dataProperty);

                    case "CountedUnicodeString":
                    case "CountedAnsiString":
                        return new CountedStringPropImpl(naming, dataProperty);

                    case "Binary":
                        return new BinaryPropImpl(naming, dataProperty);

                    case "CountedBinary":
                        return new CountedBinaryPropImpl(naming, dataProperty);

                    case "GUID":
                        return new RecordPropImpl(naming, dataProperty, "GUID");
                    case "FILETIME":
                        return new RecordPropImpl(naming, dataProperty, "FILETIME");
                    case "SYSTEMTIME":
                        return new RecordPropImpl(naming, dataProperty, "SYSTEMTIME");

                    case "SID":
                        return new SecurityIdPropImpl(naming, dataProperty);

                    case "Pointer":
                        return new PointerPropImpl(naming, dataProperty);
                }
            }

            throw new InternalException("unhandled type '{0}'", property);
        }

        private class StructPropImpl : PropImpl
        {
            public StructPropImpl(ICodeGenNomenclature naming, StructProperty property)
                : base(naming, property)
            {
            }

            protected new StructProperty Property => (StructProperty)base.Property;

            protected override string GetArgSalSpec(bool usePropertyName)
            {
                return "_In_";
            }

            protected override string GetArgType()
            {
                return "const void*";
            }

            protected override bool IsPtrLike()
            {
                return true;
            }

            protected override string GetDataSizeExpr()
            {
                string lengthExpr = naming.GetLengthArgumentId(Property, false);
                string countExpr = null;
                if (Property.Count.IsMultiple)
                    countExpr = GetCountExpr(string.Empty) + " * ";
                return $"{countExpr}{lengthExpr}";
            }

            public override int GetArgCount()
            {
                return 1;
            }

            protected override bool HasExtraLengthParam()
            {
                return true;
            }

            public override void AppendDataDescriptor(ref int index, TextWriter writer)
            {
                writer.WriteLine(
                    "EventDataDescCreate(&{0}[{1}],{3}{2}, {4});",
                    naming.EventDataDescriptorId,
                    index + 1,
                    naming.GetNumberedArgId(Property.Index),
                    GetDataPtrQualifier(),
                    GetDataSizeExpr());
                ++index;
            }
        }

        private class PointerPropImpl : DataPropImpl
        {
            public PointerPropImpl(ICodeGenNomenclature naming, DataProperty property)
                : base(naming, property)
            {
            }

            protected override string GetDataSizeExpr()
            {
                if (!Property.Count.IsSpecified)
                    return "sizeof(const void*)  ";
                return "sizeof(const void*)" + GetCountExpr();
            }

            protected override string GetArgType()
            {
                return "const void*";
            }

            protected override string GetArgSalSpec(bool usePropertyName)
            {
                if (Property.Count.IsMultiple)
                    return string.Format("_In_reads_({0})", GetCountExpr(string.Empty));
                return "_In_opt_";
            }
        }

        private class RecordPropImpl : DataPropImpl
        {
            private readonly string structName;

            public RecordPropImpl(ICodeGenNomenclature naming, DataProperty property, string structName)
                : base(naming, property)
            {
                this.structName = structName;
            }

            protected override string GetDataSizeExpr()
            {
                if (!Property.Count.IsSpecified)
                    return "sizeof(" + structName + ")  ";
                return "sizeof(" + structName + ")" + GetCountExpr();
            }

            protected override string GetArgType()
            {
                return "const " + structName + "*";
            }

            protected override string GetArgSalSpec(bool usePropertyName)
            {
                if (Property.Count.IsMultiple)
                    return $"_In_reads_({GetCountExpr(string.Empty)})";
                return "_In_";
            }

            protected override bool IsPtrLike()
            {
                return true;
            }
        }

        private class PrimitivePropImpl : DataPropImpl
        {
            public PrimitivePropImpl(ICodeGenNomenclature naming, DataProperty property)
                : base(naming, property)
            {
            }

            protected override string GetArgSalSpec(bool usePropertyName)
            {
                if (Property.Count.IsMultiple)
                    return $"_In_reads_({GetCountExpr(string.Empty)})";
                return "_In_";
            }

            protected override string GetArgType()
            {
                return Property.InType.Name.LocalName switch
                {
                    "Int8" => "const signed char",
                    "UInt8" => "const unsigned char",
                    "Int16" => "const signed short",
                    "UInt16" => "const unsigned short",
                    "Int32" => "const signed int",
                    "UInt32" => "const unsigned int",
                    "Int64" => "const signed __int64",
                    "UInt64" => "const unsigned __int64",
                    "Float" => "const float",
                    "Double" => "const double",
                    "Boolean" => "const signed int",
                    "HexInt32" => "const signed int",
                    "HexInt64" => "const signed __int64",
                    _ => throw new InternalException("unhandled type '{0}'", Property.InType),
                };
            }

            protected override string GetDataSizeExpr()
            {
                switch (Property.InType.Name.LocalName) {
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
                        string space = null;
                        if (!Property.Count.IsMultiple)
                            space = "  ";
                        return string.Format("sizeof({0}){1}{2}", GetArgType(), GetCountExpr(), space);
                }

                throw new InternalException("unhandled type '{0}'", Property.InType);
            }
        }

        private class CountedStringPropImpl : DataPropImpl
        {
            public CountedStringPropImpl(ICodeGenNomenclature naming, DataProperty property)
                : base(naming, property)
            {
            }

            public override bool RequiresPacking => Property.Count.IsMultiple;

            public override void AppendDataDescriptor(ref int index, TextWriter writer)
            {
                if (Property.Count.IsMultiple) {
                    AppendCountedDataDescriptor(ref index, writer);
                    return;
                }

                base.AppendDataDescriptor(ref index, writer);
            }

            public override int GetArgCount()
            {
                if (Property.Count.IsMultiple)
                    return 1;
                return 2;
            }

            protected override bool HasExtraLengthParam()
            {
                return true;
            }

            protected override bool IsPtrLike()
            {
                return true;
            }

            protected override string GetArgType()
            {
                return Property.InType.Name.LocalName switch
                {
                    "CountedUnicodeString" => "const WCHAR*",
                    "CountedAnsiString" => "const char*",
                    _ => throw new InternalException("unhandled type '{0}'", Property.InType),
                };
            }

            protected override string GetDataSizeExpr()
            {
                string lengthExpr = naming.GetLengthArgumentId(Property, false);

                string countExpr = null;
                if (Property.Count.IsSpecified && Property.Length.IsSpecified)
                    countExpr = GetCountExpr();

                string type = Property.InType.Name == WinEventSchema.CountedUnicodeString ? "WCHAR" : "char";

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "(USHORT)(sizeof({0}){1}*{2})",
                    type,
                    countExpr,
                    lengthExpr);
            }

            protected override string GetArgSalSpec(bool usePropertyName)
            {
                string expr = naming.GetLengthArgumentId(Property, usePropertyName);
                return $"_In_reads_({expr})";
            }
        }

        private class SecurityIdPropImpl : DataPropImpl
        {
            public SecurityIdPropImpl(ICodeGenNomenclature naming, DataProperty property)
                : base(naming, property)
            {
            }

            public override bool RequiresPacking => Property.Count.IsMultiple;

            public override void AppendDataDescriptor(ref int index, TextWriter writer)
            {
                if (Property.Count.IsMultiple) {
                    writer.WriteLine(
                        "EventDataDescCreate(&{0}[{1}], {2}, {4});",
                        naming.EventDataDescriptorId,
                        index + 1,
                       naming.GetNumberedArgId(Property.Index),
                        "",
                        GetDataSizeExpr());
                } else {
                    writer.WriteLine(
                        "EventDataDescCreate(&{0}[{1}],({2} != NULL) ? {2} : (const void*)\"\\0\\0\\0\\0\\0\\0\\0\", {4});",
                       naming.EventDataDescriptorId,
                        index + 1,
                        naming.GetNumberedArgId(Property.Index),
                        "",
                        GetDataSizeExpr());
                }
                ++index;
            }

            public override int GetArgCount()
            {
                return 1;
            }

            protected override bool HasExtraLengthParam()
            {
                return RequiresPacking;
            }

            protected override string GetCountExpr(string prefix = "*")
            {
                return prefix + naming.GetLengthArgumentId(Property, false);
            }

            protected override bool IsPtrLike()
            {
                return true;
            }

            protected override string GetArgSalSpec(bool usePropertyName)
            {
                if (RequiresPacking)
                    return $"_In_reads_({GetCountExpr(string.Empty)})";
                return "_In_";
            }

            protected override string GetArgType()
            {
                if (RequiresPacking)
                    return "const unsigned char*";
                return "const SID*";
            }

            protected override string GetDataSizeExpr()
            {
                if (RequiresPacking)
                    return naming.GetLengthArgumentId(Property, false);
                return string.Format(
                    "({0} != NULL) ? MCGEN_GETLENGTHSID({0}) : 8",
                    naming.GetNumberedArgId(Property.Index));
            }
        }

        private class BinaryPropImpl : DataPropImpl
        {
            public BinaryPropImpl(ICodeGenNomenclature naming, DataProperty property)
                : base(naming, property)
            {
            }

            protected override bool IsPtrLike()
            {
                return true;
            }

            public override void AppendDataDescriptor(ref int index, TextWriter writer)
            {
                string comment = null;
                if (Property.Count.IsMultiple)
                    comment = string.Format("  // Blob containing {0} concatenated strings; each string has the same length ({1})",
                        GetCountExpr(string.Empty),
                        GetLengthExpr(string.Empty));

                writer.WriteLine(
                    "EventDataDescCreate(&{0}[{1}],{3}{2}, {4});{5}",
                    naming.EventDataDescriptorId,
                    index + 1,
                    naming.GetNumberedArgId(Property.Index),
                    GetDataPtrQualifier(),
                    GetDataSizeExpr(),
                    comment);
                ++index;
            }

            protected override string GetArgSalSpec(bool usePropertyName)
            {
                return string.Format(
                    "_In_reads_({0}{1})",
                    GetNumberExpr(naming, Property.Length, string.Empty),
                    GetNumberExpr(naming, Property.Count));
            }

            protected override string GetArgType()
            {
                return "const unsigned char*";
            }

            protected override string GetDataSizeExpr()
            {
                return string.Format(
                    "(ULONG)sizeof(char){0}{1}",
                    GetLengthExpr(),
                    GetCountExpr());
            }
        }

        private class CountedBinaryPropImpl : DataPropImpl
        {
            public CountedBinaryPropImpl(ICodeGenNomenclature naming, DataProperty property)
                : base(naming, property)
            {
            }

            public override bool RequiresPacking => Property.Count.IsMultiple;

            public override int GetArgCount()
            {
                if (Property.Count.IsMultiple)
                    return 1;
                return 2;
            }

            protected override bool HasExtraLengthParam()
            {
                return true;
            }

            public override void AppendDataDescriptor(ref int index, TextWriter writer)
            {
                if (Property.Count.IsMultiple) {
                    AppendCountedDataDescriptor(ref index, writer);
                    return;
                }

                base.AppendDataDescriptor(ref index, writer);
            }

            protected override bool IsPtrLike()
            {
                return true;
            }

            protected override string GetLengthExpr(string prefix = "*")
            {
                return prefix + naming.GetLengthArgumentId(Property, false);
            }

            protected override string GetDataSizeExpr()
            {
                if (Property.Count.IsSpecified)
                    return string.Format(
                        "(USHORT)(sizeof(char){0})",
                        GetLengthExpr());
                return string.Format(
                    "(USHORT)(sizeof(char){0}{1})",
                    GetLengthExpr(),
                    GetCountExpr());
            }

            protected override string GetArgSalSpec(bool usePropertyName)
            {
                string expr = naming.GetLengthArgumentId(Property, usePropertyName);
                return $"_In_reads_({expr})";
            }

            protected override string GetArgType()
            {
                return "const unsigned char*";
            }
        }

        private class StringPropImpl : DataPropImpl
        {
            public StringPropImpl(ICodeGenNomenclature naming, DataProperty property)
                : base(naming, property)
            {
            }

            public override bool RequiresPacking => Property.Count.IsMultiple;

            public override void AppendDataDescriptor(ref int index, TextWriter writer)
            {
                if (Property.Count.IsMultiple) {
                    string comment;
                    if (Property.Length.IsSpecified) {
                        comment = $" // Blob containing {GetCountExpr(string.Empty)} concatenated strings;" +
                                  $" each string has the same length ({GetLengthExpr(string.Empty)})";
                    } else {
                        comment = $" // Blob containing {GetCountExpr(string.Empty)} concatenated nul-terminated strings";
                    }

                    writer.WriteLine(
                        "EventDataDescCreate(&{0}[{1}],{2}, {3});{4}",
                        naming.EventDataDescriptorId,
                        index + 1,
                        naming.GetNumberedArgId(Property.Index),
                        GetDataSizeExpr(),
                        comment);

                    ++index;
                    return;
                }

                if (!Property.Length.IsSpecified) {
                    string argName = naming.GetNumberedArgId(Property.Index);

                    writer.WriteLine(
                        "EventDataDescCreate(&{0}[{1}],",
                        naming.EventDataDescriptorId,
                        index + 1);

                    if (Property.InType.Name == WinEventSchema.UnicodeString) {
                        writer.WriteLine("                    ({0} != NULL) ? {0} : L\"NULL\",", argName);
                        writer.WriteLine("                    ({0} != NULL) ? (ULONG)((wcslen({0}) + 1) * sizeof(WCHAR)) : (ULONG)sizeof(L\"NULL\"));", argName);
                    } else {
                        writer.WriteLine("                    ({0} != NULL) ? {0} : \"NULL\",", argName);
                        writer.WriteLine("                    ({0} != NULL) ? (ULONG)((strlen({0}) + 1) * sizeof(char)) : (ULONG)sizeof(\"NULL\"));", argName);
                    }

                    ++index;
                    return;
                }

                base.AppendDataDescriptor(ref index, writer);
            }

            public override int GetArgCount()
            {
                return 1;
            }

            protected override bool HasExtraLengthParam()
            {
                if (Property.Count.IsMultiple && !Property.Length.IsSpecified)
                    return true;

                return false;
            }

            protected override bool IsPtrLike()
            {
                return true;
            }

            protected override string GetArgSalSpec(bool usePropertyName)
            {
                if (!Property.Count.IsMultiple && !Property.Length.IsSpecified)
                    return "_In_opt_";

                string expr;
                if (Property.Count.IsSpecified && !Property.Length.IsSpecified) {
                    if (Property.Count.IsVariable)
                        expr = naming.GetNumberedArgId(Property.Count.DataProperty.Index);
                    else
                        expr = naming.GetLengthArgumentId(Property, usePropertyName);
                } else {
                    expr = string.Format(
                        "{0}{1}",
                        GetNumberExpr(naming, Property.Length, string.Empty),
                        GetNumberExpr(naming, Property.Count));
                }
                return $"_In_reads_({expr})";
            }

            protected override string GetArgType()
            {
                switch (Property.InType.Name.LocalName) {
                    case "UnicodeString":
                        if (Property.Length.IsSpecified)
                            return "const WCHAR*";
                        return "PCWSTR";
                    case "AnsiString":
                        if (Property.Length.IsSpecified)
                            return "const char*";
                        return "PCSTR";
                }

                throw new InternalException("unhandled type '{0}'", Property.InType);
            }

            protected override string GetDataSizeExpr()
            {
                if (!Property.Count.IsSpecified && !Property.Length.IsSpecified) {
                    throw new Exception();
                }
                string lengthExpr;
                if (Property.Count.IsSpecified && !Property.Length.IsSpecified)
                    lengthExpr = naming.GetLengthArgumentId(Property, false);
                else
                    lengthExpr = GetLengthExpr(string.Empty);

                string countExpr = null;
                if (Property.Count.IsSpecified && Property.Length.IsSpecified)
                    countExpr = GetCountExpr();

                string type = Property.InType.Name == WinEventSchema.UnicodeString ? "WCHAR" : "char";

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "(ULONG)(sizeof({0}){1}*{2})",
                    type,
                    countExpr,
                    lengthExpr);
            }
        }

        private abstract class DataPropImpl : PropImpl
        {
            protected DataPropImpl(ICodeGenNomenclature naming, DataProperty property)
                : base(naming, property)
            {
            }

            protected new DataProperty Property => (DataProperty)base.Property;

            protected void AppendCountedDataDescriptor(ref int index, TextWriter writer)
            {
                writer.WriteLine(
                    "EventDataDescCreate(&{0}[{1}],{2}, {4}); // Blob contains data for {3} chunks; each chunk is a 16-bit ByteCount followed by ByteCount bytes of data.",
                    naming.EventDataDescriptorId,
                    index + 1,
                    naming.GetNumberedArgId(Property.Index),
                    GetCountExpr(string.Empty),
                    GetDataSizeExpr());
                ++index;
            }
        }

        private abstract class PropImpl
        {
            protected readonly ICodeGenNomenclature naming;

            protected PropImpl(ICodeGenNomenclature naming, Property property)
            {
                this.naming = naming;
                Property = property;
            }

            protected Property Property { get; }

            public virtual bool RequiresPacking => false;

            public void AppendParameterName(TextWriter writer, bool usePropertyName)
            {
                if (HasExtraLengthParam()) {
                    string lenArgName = naming.GetLengthArgumentId(Property, usePropertyName);
                    writer.Write("{0}, ", lenArgName);
                }

                string argName = naming.GetArgumentId(Property, usePropertyName);
                writer.Write(argName);
            }

            public void AppendParameter(TextWriter writer, bool usePropertyName)
            {
                if (HasExtraLengthParam()) {
                    string lenArgName = naming.GetLengthArgumentId(Property, usePropertyName);
                    writer.WriteLine("_In_ ULONG {0},", lenArgName);
                }

                string argName = naming.GetArgumentId(Property, usePropertyName);
                writer.Write(
                    "{1} {2} {3}{0}",
                    argName,
                    GetArgSalSpec(usePropertyName),
                    GetArgType(),
                    GetParamQualifier());
            }

            public virtual void AppendDataDescriptor(ref int index, TextWriter writer)
            {
                if (HasExtraLengthParam()) {
                    writer.WriteLine(
                        "EventDataDescCreate(&{0}[{1}],&{0}[{2}].Size, {3});",
                        naming.EventDataDescriptorId,
                        index + 1,
                        index + 2,
                        "sizeof(USHORT)");
                    writer.WriteLine();
                    writer.WriteLine(
                        "EventDataDescCreate(&{0}[{1}], {3}{2}, {4});",
                        naming.EventDataDescriptorId,
                        index + 2,
                        naming.GetNumberedArgId(Property.Index),
                        GetDataPtrQualifier(),
                        GetDataSizeExpr());
                    index += 2;
                } else {
                    writer.WriteLine(
                        "EventDataDescCreate(&{0}[{1}],{3}{2}, {4});",
                        naming.EventDataDescriptorId,
                        index + 1,
                        naming.GetNumberedArgId(Property.Index),
                        GetDataPtrQualifier(),
                        GetDataSizeExpr());
                    ++index;
                }
            }

            public virtual int GetArgCount()
            {
                return HasExtraLengthParam() ? 2 : 1;
            }

            protected virtual bool HasExtraLengthParam()
            {
                return false;
            }

            protected virtual bool IsPtrLike()
            {
                return false;
            }

            protected abstract string GetArgSalSpec(bool usePropertyName);

            protected abstract string GetArgType();

            protected string GetParamQualifier()
            {
                if (IsPtrLike())
                    return " ";

                if (this is PrimitivePropImpl) {
                    return Property.Count.IsMultiple ? "*" : " ";
                }

                if (this is PointerPropImpl) {
                    return Property.Count.IsMultiple ? "*" : " ";
                }

                return Property.Count.IsMultiple ? " *" : " ";
            }

            protected string GetDataPtrQualifier()
            {
                if (IsPtrLike())
                    return string.Empty;
                return Property.Count.IsMultiple ? " " : "&";
            }

            protected abstract string GetDataSizeExpr();

            protected virtual string GetCountExpr(string prefix = "*")
            {
                return GetNumberExpr(naming, Property.Count, prefix);
            }

            protected virtual string GetLengthExpr(string prefix = "*")
            {
                return GetNumberExpr(naming, Property.Length, prefix);
            }
        }
    }

    public static class CodeGenExtensions
    {
        public static byte GetDescriptorChannelValue(this Event @event)
        {
            if (@event.Channel?.Value != null)
                return @event.Channel.Value.Value;
            if (@event.Provider.ControlGuid != null)
                return 12; // ProviderMetadata
            return 0;
        }
    }
}
