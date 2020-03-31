namespace EventTraceKit.EventTracing.Compilation.CodeGen
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Schema;
    using EventTraceKit.EventTracing.Compilation.Support;
    using EventTraceKit.EventTracing.Internal.Extensions;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Schema.Base;
    using EventTraceKit.EventTracing.Support;

    [CodeGenerator]
    internal sealed class CxxCodeGeneratorProvider : ICodeGeneratorProvider
    {
        public string Name => "cxx";
        public object CreateOptions() => new CxxCodeGenOptions();

        public ICodeGenerator CreateGenerator(object options)
        {
            return new CxxCodeGenerator(options as CxxCodeGenOptions ?? new CxxCodeGenOptions());
        }
    }

    internal sealed class CxxCodeGenOptions
    {
        [JoinedOption("etw-ns", HelpText = "Namespace where common ETW code is placed. Use '.' as separator (e.g. Company.Product.ETW)")]
        public string EtwNamespace { get; set; } = "etw";

        [JoinedOption("log-ns", HelpText = "Namespace where generated code is placed. Use '.' as separator (e.g. Company.Product.Tracing)")]
        public string LogNamespace { get; set; } = "trace";

        [JoinedOption("use-prefix", HelpText = "Prefix for generated logging functions")]
        public bool UseLoggingPrefix { get; set; }

        [JoinedOption("prefix", HelpText = "Prefix for generated logging functions")]
        public string LoggingPrefix { get; set; }

        [FlagOption("defines", HelpText = "Generate code definitions for non-essential resources")]
        public bool GenerateDefines { get; set; }

        [JoinedOption("std", HelpText = "C++ standard version")]
        public CxxLangStandard LangStandard { get; set; } = CxxLangStandard.Cxx20;

        [FlagOption("static", HelpText = "Enlists all providers in the list of static providers")]
        public bool IsStaticProvider { get; set; }
    }

    internal sealed class CxxCodeGenerator : ICodeGenerator
    {
        private static readonly Template DefaultTemplate = new Template(
            Located.Create("(null)"));

        private readonly CxxCodeGenOptions options;
        private readonly Naming naming;
        private readonly string etwNamespacePrefix = string.Empty;
        private readonly string etwMacroPrefix = string.Empty;
        private readonly string loggingPrefix;
        private readonly Dictionary<Provider, List<Activity>> activitiesByProvider =
            new Dictionary<Provider, List<Activity>>();

        private IndentableTextWriter ow;

        private sealed class Naming : CStyleCodeGenNaming
        {
            private readonly string etwMacroPrefix;

            public Naming(string etwMacroPrefix)
            {
                this.etwMacroPrefix = etwMacroPrefix;
            }

            public override string GetIdentifier(MapItem item, Map map)
            {
                if (string.IsNullOrWhiteSpace(item.Symbol))
                    return $"{map.Symbol}{map.Items.IndexOf(item)}";

                return item.Symbol;
            }

            public override string GetTemplateSuffix(Template template)
            {
                var suffix = base.GetTemplateSuffix(template);
                if (suffix.Length != 0)
                    return "_" + suffix;
                return suffix;
            }

            public override string GetTemplateId(Template template)
            {
                return "EmcTemplate" + GetTemplateSuffix(template);
            }

            public override string GetTemplateGuardId(Template template)
            {
                var prefix = etwMacroPrefix;
                var suffix = GetTemplateSuffix(template);
                return $"{prefix}EMC_TEMPLATE{suffix}_DEFINED";
            }

            public string GetActivityId(Template template)
            {
                return "Activity" + GetTemplateSuffix(template);
            }

            public string GetActivityGuardId(Template template)
            {
                var prefix = etwMacroPrefix;
                var suffix = GetTemplateSuffix(template);
                return $"{prefix}EMC_ACTIVITY{suffix}_DEFINED";
            }

            public override string GetEventDescriptorId(Event evt)
            {
                return GetIdentifier(evt) + "Desc";
            }

            public override string GetProviderGuidId(Provider provider)
            {
                return provider.Symbol + "Id";
            }

            public override string GetProviderControlGuidId(Provider provider)
            {
                return provider.Symbol + "ControlId";
            }

            public override string GetProviderContextId(Provider provider)
            {
                return GetIdentifier(provider) + "Context";
            }

            public override string GetProviderHandleId(Provider provider)
            {
                return GetProviderContextId(provider) + ".RegistrationHandle";
            }

            public override string GetProviderTraitsId(Provider provider)
            {
                return $"{provider.Symbol}Traits";
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
        public CxxCodeGenerator(CxxCodeGenOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));

            if (!string.IsNullOrWhiteSpace(options.EtwNamespace)) {
                etwNamespacePrefix = ConvertToCxxNamespacePrefix(options.EtwNamespace);
                etwMacroPrefix = ConvertToMacroPrefix(options.EtwNamespace);
            }

            naming = new Naming(etwMacroPrefix);
            loggingPrefix = options.UseLoggingPrefix ? options.LoggingPrefix : null;
        }

        private static string ConvertToCxxNamespacePrefix(string ns)
        {
            string[] parts = ns.Trim().Split('.');
            return "::" + string.Join("::", parts) + "::";
        }

        private static string ConvertToMacroPrefix(string ns)
        {
            string[] parts = ns.Trim().Split('.');
            return string.Join("_", parts) + "_";
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

        private void WriteNamespaceBegin(string ns, string innerInlineNS = null)
        {
            if (string.IsNullOrEmpty(ns))
                return;

            string[] parts = ns.Split('.');
            if (options.LangStandard >= CxxLangStandard.Cxx17) {
                ow.WriteLine("namespace {0}", string.Join("::", parts));
                ow.WriteLine("{");
            } else {
                for (int i = 0; i < parts.Length; ++i) {
                    ow.WriteLine("namespace {0}", parts[i]);
                    ow.WriteLine("{");
                }
            }

            if (innerInlineNS != null) {
                ow.WriteLine("inline namespace {0}", innerInlineNS);
                ow.WriteLine("{");
            }

            ow.WriteLine();
        }

        private void WriteNamespaceEnd(string ns, string innerInlineNS = null)
        {
            if (string.IsNullOrEmpty(ns))
                return;

            if (innerInlineNS != null)
                ow.WriteLine("}} // namespace {0}", innerInlineNS);

            string[] parts = ns.Split('.');
            string closingBrace = "}";
            if (options.LangStandard < CxxLangStandard.Cxx17)
                closingBrace = new string('}', parts.Length);

            ow.WriteLine("{0} // namespace {1}", closingBrace, string.Join("::", parts));

            ow.WriteLine();
        }

        private string LinkOnceAttribute
        {
            get
            {
                if (options.LangStandard >= CxxLangStandard.Cxx17)
                    return $"inline";
                return $"__declspec(selectany)";
            }
        }

        private string ConstantDecl(string typeName)
        {
            if (options.LangStandard >= CxxLangStandard.Cxx17)
                return $"constexpr {typeName}";
            return $"{typeName} const";
        }

        private string ConstantGlobalDecl(string typeName)
        {
            if (options.LangStandard >= CxxLangStandard.Cxx17)
                return $"inline constexpr {typeName}";
            return $"extern __declspec(selectany) {typeName} const";
        }

        private string GlobalDecl(string typeName, string attributes = null)
        {
            if (attributes is null)
                return $"{LinkOnceAttribute} {typeName}";
            return $"{attributes} {LinkOnceAttribute} {typeName}";
        }

        private void GenerateCore(EventManifest manifest)
        {
            ow.WriteLine("#pragma once");
            ow.WriteLine("// clang-format off");
            WriteSharedDefinitions();
            WriteManifest(manifest);
            ow.WriteLine("// clang-format on");
            ow.Flush();
        }

        private static readonly QName StartOpcode = new QName("Start", "win", WinEventSchema.Namespace);
        private static readonly QName StopOpcode = new QName("Stop", "win", WinEventSchema.Namespace);

        private sealed class Activity
        {
            public Activity(string symbol, Event startEvent, Event stopEvent)
            {
                Symbol = symbol;
                StartEvent = startEvent;
                StopEvent = stopEvent;
            }

            public string Symbol { get; }
            public Event StartEvent { get; }
            public Event StopEvent { get; }
        }

        private static List<Activity> FindActivities(Provider provider)
        {
            var startStopEvents = provider.Events
                .Where(x => !x.NotLogged.GetValueOrDefault())
                .Where(x => x.Opcode?.Name == StartOpcode || x.Opcode?.Name == StopOpcode)
                .OrderBy(x => x.Value)
                .ToList();

            var lookup = startStopEvents.ToDictionary(x => x.Value.Value);

            var activities = new List<Activity>();
            foreach (var stopEvent in startStopEvents.Where(x => x.Opcode.Name == StopOpcode)) {
                uint startValue = stopEvent.Value.Value - 1;

                if (!lookup.TryGetValue(startValue, out var startEvent) ||
                    startEvent.Opcode.Name != StartOpcode)
                    continue;

                // Start and stop event must be similar.
                if (startEvent.Channel != stopEvent.Channel ||
                    startEvent.Level != stopEvent.Level ||
                    startEvent.Task != stopEvent.Task ||
                    startEvent.KeywordMask != stopEvent.KeywordMask)
                    continue;

                // Stop event must not have properties.
                if (stopEvent.Template != null && stopEvent.Template.Properties.Count != 0)
                    continue;

                string symbol = StringExtensions.LongestCommonPrefix(startEvent.Symbol, stopEvent.Symbol);
                activities.Add(new Activity(symbol, startEvent, stopEvent));
            }

            return activities;
        }

        private Activity TryGetActivity(Event stopEvent)
        {
            var activities = activitiesByProvider[stopEvent.Provider];
            return activities.FirstOrDefault(x => x.StopEvent == stopEvent);
        }

        private void WriteManifest(EventManifest manifest)
        {
            activitiesByProvider.Clear();

            foreach (var provider in manifest.Providers) {
                var activities = FindActivities(provider);
                activitiesByProvider.Add(provider, activities);
            }

            WriteNamespaceBegin(options.EtwNamespace);
            WriteTemplates(manifest.Providers);
            WriteActivityTemplates(manifest.Providers);
            WriteNamespaceEnd(options.EtwNamespace);

            foreach (var provider in manifest.Providers)
                WriteProvider(provider);
        }

        private void WriteProvider(Provider provider)
        {
            var ns = GetDeclaredNamespace(provider);
            string inlineNS = null;
            if (ns == null) {
                ns = options.LogNamespace;
                inlineNS = GetInlineNamespace(provider);
            }

            WriteNamespaceBegin(ns, inlineNS);

            ow.WriteLine("//");
            ow.WriteLine("// Provider \"{0}\" {1:B}", provider.Name, provider.Id);
            ow.WriteLine("//");
            ow.WriteLine(
                "{0} {1} = {2};",
                ConstantGlobalDecl("GUID"),
                naming.GetProviderGuidId(provider),
                FormatGuid(provider.Id));
            ow.WriteLine();

            if (options.GenerateDefines) {
                WriteChannels(provider);
                WriteLevels(provider);
                WriteOpcodes(provider);
                WriteTasks(provider);
                WriteKeywords(provider);
            }

            // Maps are usually used as types of user-data, so they must always
            // be included.
            WriteMaps(provider);

            WriteProviderTraits(provider);
            WriteRegistration(provider, inlineNS != null ? ns + "." + inlineNS : ns);
            WriteEventDescriptors(provider);
            WriteEvents(provider);

            WriteNamespaceEnd(ns, inlineNS);
        }

        private static string GetInlineNamespace(Provider provider)
        {
            return CStyleCodeGenNaming.SanitizeIdentifier(provider.Symbol).ToLowerInvariant();
        }

        private static string GetDeclaredNamespace(Provider provider)
        {
            LocatedRef<string> nsAttrib = provider.Namespace;
            if (nsAttrib != null) {
                // A specified namespace-attribute should be honored. So when
                // empty, no namespace.
                if (!string.IsNullOrWhiteSpace(nsAttrib.Value))
                    return nsAttrib.Value;
            }

            return null;
        }

        private void WriteRegistration(Provider provider, string providerNamespace)
        {
            string providerId = naming.GetIdentifier(provider);
            string contextId = naming.GetProviderContextId(provider);
            string controlGuidId = naming.GetProviderGuidId(provider);
            string registerFunction = etwNamespacePrefix + "EmcGenEventRegisterContext";
            string callbackFunction = etwNamespacePrefix + "EmcGenControlCallback";

            if (provider.ControlGuid != null)
                controlGuidId = naming.GetProviderControlGuidId(provider);

            WriteEnableBits(provider);

            ow.WriteLine("/// <summary>");
            ow.WriteLine("///   Registers with ETW using the control GUID specified in the manifest.");
            ow.WriteLine("///   Invoke this macro during module initialization (i.e. program startup,");
            ow.WriteLine("///   DLL process attach, or driver load) to initialize the provider.");
            ow.WriteLine("///   Note that if this function returns an error, the error means that");
            ow.WriteLine("///   will not work, but no action needs to be taken -- even if EventRegister");
            ow.WriteLine("///   returns an error, it is generally safe to use EventWrite and");
            ow.WriteLine("///   EventUnregister macros (they will be no-ops if EventRegister failed).");
            ow.WriteLine("/// </summary>");
            ow.WriteLine("inline ULONG EventRegister{0}() noexcept", providerId);
            ow.WriteLine("{");
            using (ow.IndentScope())
                ow.WriteLine("return {0}({1}, {2}, &{3}, {3});",
                             registerFunction,
                             controlGuidId,
                             callbackFunction,
                             contextId);
            ow.WriteLine("}");
            ow.WriteLine();

            ow.WriteLine("/// <summary>");
            ow.WriteLine("///   Registers with ETW using a specific control GUID (i.e. a GUID other than");
            ow.WriteLine("///   what is specified in the manifest). Advanced scenarios only.");
            ow.WriteLine("/// </summary>");
            ow.WriteLine("inline ULONG EventRegisterByGuid{0}(GUID const& controlId) noexcept", providerId);
            ow.WriteLine("{");
            using (ow.IndentScope())
                ow.WriteLine("return {0}(controlId, {1}, &{2}, {2});",
                             registerFunction,
                             callbackFunction,
                             contextId);
            ow.WriteLine("}");
            ow.WriteLine();

            ow.WriteLine("/// <summary>");
            ow.WriteLine("///   Unregisters with ETW and close the provider.");
            ow.WriteLine("///   Invoke this function during module shutdown (i.e. program exit, DLL");
            ow.WriteLine("///   process detach, or driver unload) to unregister the provider.");
            ow.WriteLine("///   Note that you MUST call EventUnregister before DLL or driver unload");
            ow.WriteLine("///   (not optional): failure to unregister a provider before DLL or driver");
            ow.WriteLine("///   unload will result in crashes.");
            ow.WriteLine("/// </summary>");
            ow.WriteLine("inline ULONG EventUnregister{0}() noexcept", providerId);
            ow.WriteLine("{");
            using (ow.IndentScope())
                ow.WriteLine("return {0}EmcGenEventUnregister(&{1});",
                             etwNamespacePrefix, naming.GetProviderHandleId(provider));
            ow.WriteLine("}");
            ow.WriteLine();

            if (options.IsStaticProvider) {
                string mangledProviderNs = string.Concat(providerNamespace.Split('.').Reverse().Select(x => $"{x}@"));
                ow.WriteLine("EMCGEN_ENLIST_STATIC_PROVIDER({0}, \"{1}\", &EventRegister{0}, &EventUnregister{0}); ", providerId, mangledProviderNs);
                ow.WriteLine();
            }
        }

        private void WriteEnableBits(Provider provider)
        {
            var enableBits = provider.EnableBits;
            int enableByteCount = (enableBits.Count + 31) / 32;

            uint contextFlags = 0;
            if (provider.IncludeProcessName == true)
                contextFlags |= 1;

            if (enableByteCount != 0) {
                ow.WriteLine("// Event Enablement Bits");
                ow.WriteLine(
                    "{0} {1}[{2}] = {{}};",
                    GlobalDecl("std::uint32_t", "DECLSPEC_CACHEALIGN"),
                    naming.GetProviderEnableBitsId(provider),
                    enableByteCount);

                ow.Write(
                    "{0} {1}[{2}] = {{",
                    ConstantGlobalDecl("std::uint64_t"),
                    naming.GetProviderKeywordsId(provider),
                    enableBits.Count);
                WriteList(enableBits, b => $"0x{b.KeywordMask:X8}");
                ow.WriteLine("};");

                ow.Write(
                    "{0} {1}[{2}] = {{",
                    ConstantGlobalDecl("std::uint8_t"),
                    naming.GetProviderLevelsId(provider),
                    enableBits.Count);
                WriteList(enableBits, b => b.Level);
                ow.WriteLine("};");
            }

            ow.WriteLine();
            ow.WriteLine("// Provider context");

            var contextDecl = GlobalDecl($"{etwNamespacePrefix}EmcGenTraceContext");
            if (enableByteCount == 0) {
                ow.WriteLine(
                    "{0} {1} = {{0, {2}, 0, 0, {3}}};",
                    contextDecl,
                    naming.GetProviderContextId(provider),
                    naming.GetProviderTraitsId(provider),
                    contextFlags);
            } else {
                ow.WriteLine(
                    "{0} {1} = {{0, {2}, 0, 0, {3}, false, 0, {4}, {5}, {6}, {7}}};",
                    contextDecl,
                    naming.GetProviderContextId(provider),
                    naming.GetProviderTraitsId(provider),
                    contextFlags,
                    enableBits.Count,
                    naming.GetProviderEnableBitsId(provider),
                    naming.GetProviderKeywordsId(provider),
                    naming.GetProviderLevelsId(provider));
            }

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

        private void WriteProviderTraits(Provider provider)
        {
            var traitsId = naming.GetProviderTraitsId(provider);

            if (provider.ControlGuid != null) {
                ow.WriteLine("// Control GUID = {0:D}", provider.ControlGuid);
                ow.WriteLine(
                    "{0} {1} = {2};",
                    ConstantGlobalDecl("GUID"),
                    naming.GetProviderControlGuidId(provider),
                    FormatGuid(provider.ControlGuid.Value));
                ow.WriteLine();
            }

            if (provider.ControlGuid != null || provider.GroupGuid != null || provider.IncludeNameInTraits == true ||
                provider.IncludeProcessName == true) {
                int totalSize = 2 /*Traits size*/;

                string providerName;
                if (provider.IncludeNameInTraits == true) {
                    providerName = provider.Name.Value.ToCStringLiteral(out var literalLength) + "\\x00";
                    totalSize += literalLength + 1;
                } else {
                    providerName = "\\x00";
                    totalSize += 1;
                }

                string groupGuidTrait = null;
                if (provider.GroupGuid != null) {
                    groupGuidTrait = "\\x13\\x00\\x01" + provider.GroupGuid.Value.ToByteArray().ToCStringLiteral();
                    totalSize += 2 /*Trait size*/ + 1 /*Trait type*/ + 16 /*Guid size*/;
                }

                string decodeGuidTrait = null;
                if (provider.ControlGuid != null) {
                    decodeGuidTrait = "\\x13\\x00\\x02" + provider.Id.Value.ToByteArray().ToCStringLiteral();
                    totalSize += 2 /*Trait size*/ + 1 /*Trait type*/ + 16 /*Guid size*/;
                }

                byte[] sizeBytes = BitConverter.GetBytes((ushort)totalSize);

                ow.WriteLine("{0} {1} =", ConstantGlobalDecl("char const*"), traitsId);
                ow.WriteLine("    \"{0}\" // Total size of traits = {1}", sizeBytes.ToCStringLiteral(), totalSize);
                if (provider.IncludeNameInTraits == true)
                    ow.WriteLine("    \"{0}\" // Provider name", providerName);
                else
                    ow.WriteLine("    \"{0}\" // Provider name omitted", providerName);
                if (groupGuidTrait != null)
                    ow.WriteLine("    \"{0}\" // Group guid", groupGuidTrait);
                if (decodeGuidTrait != null)
                    ow.WriteLine("    \"{0}\" // Decode guid", decodeGuidTrait);
                ow.WriteLine("    ;");
            } else {
                ow.WriteLine("{0} {1} = nullptr;", ConstantGlobalDecl("char const*"), traitsId);
            }
            ow.WriteLine();
        }

        private void WriteChannels(Provider provider)
        {
            if (provider.Channels.Count == 0)
                return;

            ow.WriteLine("// Channels");
            foreach (var channel in provider.Channels) {
                ow.WriteLine(
                    "{0} {1} = 0x{2:X};",
                    ConstantGlobalDecl("std::uint8_t"),
                    naming.GetIdentifier(channel),
                    channel.Value);
            }

            ow.WriteLine();
        }

        private void WriteLevels(Provider provider)
        {
            if (provider.Levels.Count == 0 || provider.Levels.All(t => t.Imported))
                return;

            ow.WriteLine("// Levels");
            foreach (var level in provider.Levels.Where(t => !t.Imported)) {
                ow.WriteLine(
                    "{0} {1} = 0x{2:X};",
                    ConstantGlobalDecl("std::uint8_t"),
                    naming.GetIdentifier(level),
                    level.Value);
            }

            ow.WriteLine();
        }

        private void WriteOpcodes(Provider provider)
        {
            var allOpcodes = provider.GetAllOpcodes();
            if (!allOpcodes.Any() || allOpcodes.All(t => t.Imported))
                return;

            ow.WriteLine("// Opcodes");
            foreach (var opcode in allOpcodes.Where(t => !t.Imported)) {
                ow.WriteLine(
                    "{0} {1} = 0x{2:X};",
                    ConstantGlobalDecl("std::uint8_t"),
                    naming.GetIdentifier(opcode),
                    opcode.Value);
            }

            ow.WriteLine();
        }

        private void WriteTasks(Provider provider)
        {
            if (provider.Tasks.Count == 0 || provider.Tasks.All(t => t.Imported))
                return;

            ow.WriteLine("// Tasks");
            foreach (var task in provider.Tasks.Where(t => !t.Imported)) {
                ow.WriteLine(
                    "{0} {1} = 0x{2:X};",
                    ConstantGlobalDecl("std::uint16_t"),
                    naming.GetIdentifier(task),
                    task.Value);

                if (task.Guid.GetValueOrDefault() != Guid.Empty) {
                    ow.WriteLine(
                        "{0} {1} = {2};",
                        ConstantGlobalDecl("GUID"),
                        naming.GetTaskGuidId(task),
                        FormatGuid(task.Guid.GetValueOrDefault()));
                }
            }
            ow.WriteLine();
        }

        private void WriteKeywords(Provider provider)
        {
            if (provider.Keywords.Count == 0 || provider.Keywords.All(t => t.Imported))
                return;

            ow.WriteLine("// Keywords");
            foreach (var keyword in provider.Keywords.Where(t => !t.Imported)) {
                ow.WriteLine("{0} {1} = 0x{2:X8};",
                             ConstantGlobalDecl("std::uint64_t"),
                             naming.GetIdentifier(keyword),
                             keyword.Mask);
            }

            ow.WriteLine();
        }

        private void WriteMaps(Provider provider)
        {
            if (provider.Maps.Where(x => x.Symbol != null).Sum(x => x.Items.Count) == 0)
                return;

            ow.WriteLine("//");
            ow.WriteLine("// Maps");
            ow.WriteLine("//");
            ow.WriteLine();

            foreach (var map in provider.Maps.Where(m => m.Kind == MapKind.BitMap).Cast<BitMap>()) {
                if (map.Symbol != null && map.Items.Count > 0)
                    WriteMap(map);
            }

            foreach (var map in provider.Maps.Where(m => m.Kind == MapKind.ValueMap).Cast<ValueMap>()) {
                if (map.Symbol != null && map.Items.Count > 0)
                    WriteMap(map);
            }
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
            ow.WriteLine();
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
                "{0} {1} = {{0x{2:X4}, 0x{3:X2}, 0x{4:X2}, 0x{5:X2}, 0x{6:X2}, 0x{7:X4}, 0x{8:X16}{9};",
                ConstantGlobalDecl("EVENT_DESCRIPTOR"),
                naming.GetEventDescriptorId(evt),
                evt.Value, version, channel, level, opcode, task, keywordMask, "}");

            if (options.GenerateDefines) {
                ow.WriteLine(
                    "{0} {1}Id = 0x{2:X};",
                    ConstantGlobalDecl("std::uint16_t"),
                    naming.GetIdentifier(evt),
                    evt.Value);
            }
        }

        private void WriteEvents(Provider provider)
        {
            foreach (Event evt in provider.Events)
                WriteEvent(evt);
        }

        private void WriteActivity(Activity activity)
        {
            var provider = activity.StartEvent.Provider;
            var providerContextId = naming.GetProviderContextId(provider);

            var template = activity.StartEvent.Template ?? DefaultTemplate;
            var symbol = activity.Symbol + "Activity";
            var baseSymbol = etwNamespacePrefix + "Activity" + naming.GetTemplateSuffix(template);
            var properties = template.Properties;
            var parameters = properties.Select(x => CodeParameter.Create(naming, x)).ToList();

            string logParams = FormatAsParams(parameters);
            string logArgs = FormatAsArgs(parameters);

            ow.WriteLine("struct {0} : {1}", symbol, baseSymbol);
            ow.WriteLine("{");
            using (ow.IndentScope()) {
                ow.WriteLine("[[nodiscard]] EMCGEN_FORCEINLINE");
                ow.WriteLine("explicit {0}({1}) noexcept", symbol, logParams);
                ow.WriteLine("    : {0}({1}, {2}, {3}{4})",
                             baseSymbol,
                             providerContextId,
                             naming.GetEventDescriptorId(activity.StartEvent),
                             naming.GetEventDescriptorId(activity.StopEvent),
                             logArgs);
                ow.WriteLine("{}");
            }
            ow.WriteLine("};");
            ow.WriteLine();
        }

        private void WriteEvent(Event evt)
        {
            if (evt.NotLogged.GetValueOrDefault())
                return;

            var template = evt.Template ?? DefaultTemplate;
            var properties = template.Properties;
            var enableFuncId = naming.GetEventFuncId(evt, "EventEnabled");
            var providerContextId = naming.GetProviderContextId(evt.Provider);
            var descriptorId = naming.GetEventDescriptorId(evt);
            var symbol = naming.GetIdentifier(evt);
            var parameters = properties.Select(x => CodeParameter.Create(naming, x)).ToList();

            ow.WriteLine("//");
            ow.WriteLine("// Event {0}", symbol);
            ow.WriteLine("//");
            ow.WriteLine();

            ow.WriteLine("EMCGEN_FORCEINLINE bool {0}() noexcept", enableFuncId);
            ow.WriteLine("{");
            using (ow.IndentScope()) {
                ow.WriteLine(
                    "return EMCGEN_EVENT_BIT_TEST({0}, {1});",
                    naming.GetProviderEnableBitsId(evt.Provider),
                    evt.EnableBit.Bit);
            }
            ow.WriteLine("}");
            ow.WriteLine();

            string logParams = FormatAsParams(parameters);
            string logArgs = FormatAsArgs(parameters);

            if (evt.Message != null)
                ow.WriteLine("// Message: {0}", evt.Message.Value);

            if (parameters.Any(x => x.RequiresPacking)) {
                ow.WriteLine("//");
                ow.WriteLine("// This event contains complex types that require the caller to pack the data.");
                ow.WriteLine("// Refer to the note at the top of this header for additional details.");
                ow.WriteLine("//");
            }

            ow.WriteLine("EMCGEN_FORCEINLINE");
            ow.WriteLine("ULONG {0}({1}) noexcept", naming.GetEventFuncId(evt, loggingPrefix), logParams);
            ow.WriteLine("{");
            using (ow.IndentScope()) {
                ow.WriteLine("return {0}({1}, {2}) ?", "EMCGEN_ENABLE_CHECK", providerContextId, descriptorId);
                ow.WriteLine("       {0}{1}({2}, {3}, nullptr, nullptr{4}) : 0;",
                             etwNamespacePrefix,
                             naming.GetTemplateId(template),
                             providerContextId,
                             descriptorId,
                             logArgs);
            }
            ow.WriteLine("}");
            ow.WriteLine();

            if (evt.Opcode?.Name == StopOpcode) {
                var activity = TryGetActivity(evt);
                if (activity != null)
                    WriteActivity(activity);
            }
        }

        private void WriteTemplates(IEnumerable<Provider> providers)
        {
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

            if (allTemplates.Count == 0)
                return;

            ow.WriteLine("//");
            ow.WriteLine("// Template Functions");
            ow.WriteLine("//");
            ow.WriteLine();

            foreach (var template in allTemplates.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Value)) {
                WriteTemplate(template);
            }

            ow.WriteLine();
        }

        private void WriteTemplate(Template template)
        {
            var properties = template.Properties;
            string templateId = naming.GetTemplateId(template);
            string guardId = naming.GetTemplateGuardId(template);
            string countMacroName = "argCount";
            var parameters = properties.Select(x => CodeParameter.Create(naming, x)).ToList();
            int dataDescriptorCount = parameters.Sum(x => x.DataDescriptorCount);

            ow.WriteLine("//");
            ow.WriteLine("// Template from manifest: {0}", template.Id ?? "(null)");
            ow.WriteLine("//");

            ow.WriteLine("#ifndef {0}", guardId);
            ow.WriteLine("#define {0}", guardId);
            ow.WriteLine("EMCGEN_NOINLINE ULONG");
            ow.WriteLine("{0}(", templateId);
            using (ow.IndentScope()) {
                ow.WriteLine("_In_ EmcGenTraceContext const& {0},", naming.ContextId);
                ow.WriteLine("_In_ EVENT_DESCRIPTOR const& {0},",
                             naming.EventDescriptorId);
                ow.WriteLine("_In_opt_ GUID const* activityId,");
                ow.Write("_In_opt_ GUID const* relatedActivityId");
                AppendArgs(ow, parameters);
                ow.WriteLine(")");
            }
            ow.WriteLine("{");
            using (ow.IndentScope()) {
                ow.WriteLine("{0} {1} = {2};", ConstantDecl("size_t"), countMacroName, dataDescriptorCount);
                ow.WriteLine();
                ow.WriteLine("EVENT_DATA_DESCRIPTOR {0}[{1} + 1];",
                             naming.EventDataDescriptorId, countMacroName);
                ow.WriteLine();
                int index = 0;
                foreach (var property in parameters) {
                    property.AppendDataDescriptor(ref index, ow);
                    ow.WriteLine();
                }

                ow.WriteLine(
                    "return EmcGenEventWrite({0}, {1}, activityId, relatedActivityId, {2} + 1, {3});",
                    naming.ContextId,
                    naming.EventDescriptorId,
                    countMacroName,
                    naming.EventDataDescriptorId);
            }
            ow.WriteLine("}");
            ow.WriteLine("#endif // {0}", guardId);
            ow.WriteLine();
        }

        private void WriteActivityTemplates(IEnumerable<Provider> providers)
        {
            var allTemplates = new Dictionary<string, Template>();
            foreach (var activity in providers.SelectMany(x => activitiesByProvider[x])) {
                Template template = activity.StartEvent.Template ?? DefaultTemplate;
                string name = naming.GetActivityId(template);
                if (allTemplates.ContainsKey(name))
                    continue;
                allTemplates.Add(name, template);
            }

            if (allTemplates.Count == 0)
                return;

            ow.WriteLine("//");
            ow.WriteLine("// Activity templates");
            ow.WriteLine("//");
            ow.WriteLine();

            foreach (var template in allTemplates.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Value)) {
                WriteActivityTemplate(template);
            }

            ow.WriteLine();
        }

        private void WriteActivityTemplate(Template template)
        {
            string templateId = naming.GetActivityId(template);
            string guardId = naming.GetActivityGuardId(template);
            var parameters = template.Properties.Select(x => CodeParameter.Create(naming, x)).ToList();

            ow.WriteLine("#ifndef {0}", guardId);
            ow.WriteLine("#define {0}", guardId);
            ow.WriteLine("struct {0} : ScopedActivityId", templateId);
            ow.WriteLine("{");
            using (ow.IndentScope()) {
                ow.WriteLine("[[nodiscard]] EMCGEN_NOINLINE");
                ow.WriteLine("explicit {0}(", templateId);
                using (ow.IndentScope()) {
                    ow.WriteLine("_In_ EmcGenTraceContext const& context,");
                    ow.WriteLine("_In_ EVENT_DESCRIPTOR const& startEvent,");
                    ow.Write("_In_ EVENT_DESCRIPTOR const& stopEvent");
                    AppendArgs(ow, parameters);
                    ow.WriteLine(") noexcept");
                    ow.WriteLine(": context(context)");
                    ow.WriteLine(", stopEvent(stopEvent)");
                }
                ow.WriteLine("{");
                ow.WriteLine("    {0}({1}, {2}, &activityId, &relatedActivityId{3});",
                    naming.GetTemplateId(template),
                    "context",
                    "startEvent",
                    FormatAsTemplateArgs(parameters));
                ow.WriteLine("}");
                ow.WriteLine();
                ow.WriteLine("~{0}() noexcept", templateId);
                ow.WriteLine("{");
                ow.WriteLine("    {0}({1}, {2}, &activityId, nullptr);",
                    naming.GetTemplateId(DefaultTemplate),
                    "context",
                    "stopEvent");
                ow.WriteLine("}");
                ow.WriteLine();
                ow.WriteLine("EmcGenTraceContext const& context;");
                ow.WriteLine("EVENT_DESCRIPTOR const& stopEvent;");
            }
            ow.WriteLine("};");
            ow.WriteLine("#endif // {0}", guardId);
            ow.WriteLine();
        }

        private static string FormatGuid(Guid guid)
        {
            return guid.ToString("X").Replace(",", ", ");
        }

        private static string FormatAsParams(IEnumerable<CodeParameter> parameters)
        {
            return string.Join(", ", parameters.SelectMany(x => x.Parameters(true, honorOutType: true)));
        }

        private static string FormatAsArgs(IEnumerable<CodeParameter> parameters)
        {
            return string.Concat(
                parameters.SelectMany(x => x.ParameterNames(true, honorOutType: true)).Select(x => $", {x}"));
        }

        private static string FormatAsTemplateArgs(IEnumerable<CodeParameter> parameters)
        {
            return string.Concat(
                parameters.SelectMany(x => x.ParameterNames(false)).Select(x => $", {x}"));
        }

        private static void AppendArgs(TextWriter writer, IReadOnlyList<CodeParameter> properties)
        {
            foreach (var p in properties.SelectMany(x => x.Parameters(false))) {
                writer.WriteLine(',');
                writer.Write(p);
            }
        }

        private void WriteSharedDefinitions()
        {
            string preamble = @"//**********************************************************************
//* This is an include file generated by Event Manifest Compiler.      *
//**********************************************************************

// Notes on the ETW event code generated by EMC:
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
//   size is specified in wchar_t (16-bit) units. In the case of
//   win:CountedAnsiString and win:CountedBinary, the data size is specified in
//   bytes.

#include <windows.h>

#include <evntrace.h>
#include <evntprov.h>

#include <cstdint>
#include <memory>
#include <string>

#pragma comment(lib, ""advapi32.lib"")

#ifndef EMCGEN_NOINLINE
#define EMCGEN_NOINLINE inline DECLSPEC_NOINLINE
#endif // EMCGEN_NOINLINE

#ifndef EMCGEN_FORCEINLINE
#define EMCGEN_FORCEINLINE __forceinline
#endif // EMCGEN_FORCEINLINE

";

            string sharedDefinitions = @"//
// EMCGEN_HAVE_EVENTSETINFORMATION macro:
// Controls how EmcGenEventSetInformation uses the EventSetInformation API.
// - Set to 0 to disable the use of EventSetInformation
//   (EmcGenEventSetInformation will always return an error).
// - Set to 1 to directly invoke EMCGEN_EVENTSETINFORMATION.
// - Set to 2 to to locate EventSetInformation at runtime via GetProcAddress
//   (user-mode) or MmGetSystemRoutineAddress (kernel-mode).
// Default is determined as follows:
// - If EMCGEN_EVENTSETINFORMATION has been customized, set to 1
//   (i.e. use EMCGEN_EVENTSETINFORMATION).
// - Else if the target OS version has EventSetInformation, set to 1
//   (i.e. use EMCGEN_EVENTSETINFORMATION).
// - Else set to 2 (i.e. try to dynamically locate EventSetInformation).
// Note that an EmcGenEventSetInformation function will only be generated if one
// or more provider in a manifest has provider traits.
//
#ifndef EMCGEN_HAVE_EVENTSETINFORMATION
  #ifdef EMCGEN_EVENTSETINFORMATION             // if EMCGEN_EVENTSETINFORMATION has been customized,
    #define EMCGEN_HAVE_EVENTSETINFORMATION   1 //   directly invoke EMCGEN_EVENTSETINFORMATION(...).
  #else                                         //   if target OS and SDK is Windows 8 or later,
    #if WINVER >= 0x0602 && defined(EVENT_FILTER_TYPE_SCHEMATIZED)
      #define EMCGEN_HAVE_EVENTSETINFORMATION 1 //     directly invoke EMCGEN_EVENTSETINFORMATION(...).
    #else                                       //   else
      #define EMCGEN_HAVE_EVENTSETINFORMATION 2 //     find ""EventSetInformation"" via GetModuleHandleExW/GetProcAddress.
    #endif
  #endif
#endif // EMCGEN_HAVE_EVENTSETINFORMATION

// EMCGEN_EVENTWRITEEX macro: Override to use a custom API.
#ifndef EMCGEN_EVENTWRITEEX
#define EMCGEN_EVENTWRITEEX   EventWriteEx
#endif // EMCGEN_EVENTWRITEEX

// EMCGEN_EVENTREGISTER macro: Override to use a custom API.
#ifndef EMCGEN_EVENTREGISTER
#define EMCGEN_EVENTREGISTER        EventRegister
#endif // EMCGEN_EVENTREGISTER

// EMCGEN_EVENTSETINFORMATION macro: Override to use a custom API.
// (EmcGenEventSetInformation also affected by EMCGEN_HAVE_EVENTSETINFORMATION.)
#ifndef EMCGEN_EVENTSETINFORMATION
#define EMCGEN_EVENTSETINFORMATION  EventSetInformation
#endif // EMCGEN_EVENTSETINFORMATION

// EMCGEN_EVENTUNREGISTER macro: Override to use a custom API.
#ifndef EMCGEN_EVENTUNREGISTER
#define EMCGEN_EVENTUNREGISTER      EventUnregister
#endif // EMCGEN_EVENTUNREGISTER

//
// EMCGEN_PENABLECALLBACK macro:
// Override to use a custom function pointer type.
// (Should match the type used by EMCGEN_EVENTREGISTER.)
//
#ifndef EMCGEN_PENABLECALLBACK
#define EMCGEN_PENABLECALLBACK      PENABLECALLBACK
#endif // EMCGEN_PENABLECALLBACK

// EMCGEN_GETLENGTHSID macro: Override to use a custom API.
#ifndef EMCGEN_GETLENGTHSID
#define EMCGEN_GETLENGTHSID(p)      GetLengthSid(reinterpret_cast<PSID>(p))
#endif // EMCGEN_GETLENGTHSID

//
// EMCGEN_EVENT_ENABLED macro:
// Controls how the EventWrite[EventName] macros determine whether an event is
// enabled. The default behavior is for EventWrite[EventName] to use the
// EventEnabled[EventName] macros.
//
#ifndef EMCGEN_EVENT_ENABLED
#define EMCGEN_EVENT_ENABLED(EventName) EventEnabled##EventName()
#endif

//
// EMCGEN_EVENT_BIT_TEST macro:
// Implements testing a bit in an array of ULONG, optimized for CPU type.
//
#ifndef EMCGEN_EVENT_BIT_TEST
#  if defined(_M_IX86) || defined(_M_X64)
#    define EMCGEN_EVENT_BIT_TEST(EnableBits, BitPosition) ((((const unsigned char*)EnableBits)[BitPosition >> 3] & (1u << (BitPosition & 7))) != 0)
#  else
#    define EMCGEN_EVENT_BIT_TEST(EnableBits, BitPosition) ((EnableBits[BitPosition >> 5] & (1u << (BitPosition & 31))) != 0)
#  endif
#endif // EMCGEN_EVENT_BIT_TEST

//
// EMCGEN_ENABLE_CHECK macro:
// Determines whether the specified event would be considered as enabled
// based on the state of the specified context. Slightly faster than calling
// EmcGenEventEnabled directly.
//
#ifndef EMCGEN_ENABLE_CHECK
#define EMCGEN_ENABLE_CHECK(Context, Descriptor) (Context.IsEnabled && EmcGenEventEnabled(Context, Descriptor))
#endif

#ifndef EMCGEN_TRACE_CONTEXT_DEF
#define EMCGEN_TRACE_CONTEXT_DEF
struct EmcGenTraceContext
{
    REGHANDLE              RegistrationHandle;
    void const*            ProviderTraits;
    std::uint64_t          MatchAnyKeyword;
    std::uint64_t          MatchAllKeyword;
    std::uint32_t          Flags;
    bool                   IsEnabled;
    std::uint8_t           Level;
    std::uint16_t          EnableBitsCount;
    std::uint32_t*         EnableBitMask;
    std::uint64_t const*   EnableKeyWords;
    std::uint8_t const*    EnableLevel;
};
#endif // EMCGEN_TRACE_CONTEXT_DEF

#ifndef EMCGEN_LEVEL_KEYWORD_ENABLED_DEF
#define EMCGEN_LEVEL_KEYWORD_ENABLED_DEF
/// <summary>
///   Determines whether an event with a given level and keyword would be
///   considered as enabled based on the state of the specified context.
///   Note that you may want to use EMCGEN_ENABLE_CHECK instead of calling this
///   function directly.
/// </summary>
EMCGEN_FORCEINLINE
bool EmcGenLevelKeywordEnabled(
    _In_ EmcGenTraceContext const& context,
    _In_ std::uint8_t const level,
    _In_ std::uint64_t const keyword) noexcept
{
    // Check if the event level is lower than the level at which the channel is enabled.
    // If the event level is 0 or the channel is enabled at level 0, all levels are enabled.
    if (level <= context.Level || context.Level == 0) {
        // Check if keyword is enabled
        if (keyword == 0 || ((keyword & context.MatchAnyKeyword) &&
                             ((keyword & context.MatchAllKeyword) ==
                              context.MatchAllKeyword))) {
            return true;
        }
    }

    return false;
}
#endif // EMCGEN_LEVEL_KEYWORD_ENABLED_DEF

#ifndef EMCGEN_EVENT_ENABLED_DEF
#define EMCGEN_EVENT_ENABLED_DEF
/// <summary>
///   Determines whether the specified event would be considered as enabled
///   based on the state of the specified context. Note that you may want to use
///   EMCGEN_ENABLE_CHECK instead of calling this function directly.
/// </summary>
EMCGEN_FORCEINLINE
bool EmcGenEventEnabled(
    _In_ EmcGenTraceContext const& enableInfo,
    _In_ EVENT_DESCRIPTOR const& descriptor) noexcept
{
    return EmcGenLevelKeywordEnabled(enableInfo, descriptor.Level, descriptor.Keyword);
}
#endif // EMCGEN_EVENT_ENABLED_DEF

#ifndef EMCGEN_CONTROL_CALLBACK_DEF
#define EMCGEN_CONTROL_CALLBACK_DEF

/// <summary>
///   This is the notification callback for Windows Vista and later.
/// </summary>
/// <param name=""sourceId"">
///   The GUID that identifies the session that enabled the provider.
/// </param>
/// <param name=""controlCode"">
///   The parameter indicates whether the provider is being enabled or disabled.
/// </param>
/// <param name=""level"">
///   The level at which the event is enabled.
/// </param>
/// <param name=""matchAnyKeyword"">
///   The bitmask of keywords that the provider uses to determine the category
///   of events that it writes.
/// </param>
/// <param name=""matchAllKeyword"">
///   This bitmask additionally restricts the category of events that the
///   provider writes.
/// </param>
/// <param name=""filterData"">The provider-defined data.</param>
/// <param name=""callbackContext"">
///   The context of the callback that is defined when the provider called
///   EtwRegister to register itself.
/// </param>
/// <remarks>
///   ETW calls this function to notify provider of enable/disable.
/// </remarks>
EMCGEN_NOINLINE
void __stdcall
EmcGenControlCallback(
    _In_ GUID const* const sourceId,
    _In_ ULONG const controlCode,
    _In_ std::uint8_t const level,
    _In_ std::uint64_t const matchAnyKeyword,
    _In_ std::uint64_t const matchAllKeyword,
    _In_opt_ EVENT_FILTER_DESCRIPTOR* const filterData,
    _Inout_opt_ void* const callbackContext)
{
    if (!callbackContext)
        return;

    EmcGenTraceContext& ctx = *static_cast<EmcGenTraceContext*>(callbackContext);
#ifndef EMCGEN_PRIVATE_ENABLE_CALLBACK
    UNREFERENCED_PARAMETER(sourceId);
    UNREFERENCED_PARAMETER(filterData);
#endif

    switch (controlCode) {
        case EVENT_CONTROL_CODE_ENABLE_PROVIDER:
            ctx.IsEnabled = true;
            ctx.Level = level;
            ctx.MatchAnyKeyword = matchAnyKeyword;
            ctx.MatchAllKeyword = matchAllKeyword;

            for (ULONG b = 0; b < ctx.EnableBitsCount; ++b) {
                if (EmcGenLevelKeywordEnabled(ctx, ctx.EnableLevel[b], ctx.EnableKeyWords[b]))
                    ctx.EnableBitMask[b >> 5] |= (1 << (b % 32));
                else
                    ctx.EnableBitMask[b >> 5] &= ~(1 << (b % 32));
            }
            break;

        case EVENT_CONTROL_CODE_DISABLE_PROVIDER:
            ctx.IsEnabled = true;
            ctx.Level = 0;
            ctx.MatchAnyKeyword = 0;
            ctx.MatchAllKeyword = 0;
            if (ctx.EnableBitsCount > 0) {
                std::memset(ctx.EnableBitMask, 0, (((ctx.EnableBitsCount - 1) / 32) + 1) * sizeof(ctx.EnableBitMask[0]));
            }
            break;

        default:
            break;
    }

#ifdef EMCGEN_PRIVATE_ENABLE_CALLBACK
    // Call user defined callback
    EMCGEN_PRIVATE_ENABLE_CALLBACK(
        sourceId,
        controlCode,
        level,
        matchAnyKeyword,
        matchAllKeyword,
        filterData,
        callbackContext);
#endif // EMCGEN_PRIVATE_ENABLE_CALLBACK
}

#endif // EMCGEN_CONTROL_CALLBACK_DEF

#ifndef EMCGEN_EVENT_WRITE_DEF
#define EMCGEN_EVENT_WRITE_DEF
EMCGEN_NOINLINE
ULONG __stdcall
EmcGenEventWrite(
    _In_ EmcGenTraceContext const& context,
    _In_ EVENT_DESCRIPTOR const& descriptor,
    _In_opt_ GUID const* activityId,
    _In_opt_ GUID const* relatedActivityId,
    _In_range_(1, 128) ULONG eventDataCount,
    _Inout_updates_(eventDataCount) EVENT_DATA_DESCRIPTOR* eventData)
{
    // Some customized EMCGEN_EVENTWRITEEX macros might ignore advanced parameters.
    UNREFERENCED_PARAMETER(activityId);
    UNREFERENCED_PARAMETER(relatedActivityId);

    auto* traits = reinterpret_cast<USHORT UNALIGNED const*>(context.ProviderTraits);

    if (!traits) {
        eventData[0].Ptr = 0;
        eventData[0].Size = 0;
        eventData[0].Type = 0;
    } else {
        eventData[0].Ptr = reinterpret_cast<ULONG_PTR>(traits);
        eventData[0].Size = *traits;
        eventData[0].Type = EVENT_DATA_DESCRIPTOR_TYPE_PROVIDER_METADATA;
    }

    return EMCGEN_EVENTWRITEEX(
        context.RegistrationHandle,
        &descriptor,
        0,
        0,
        activityId,
        relatedActivityId,
        eventDataCount,
        eventData);
}
#endif // EMCGEN_EVENT_WRITE_DEF

#ifndef EMCGEN_REGISTRATION
#define EMCGEN_REGISTRATION

/// <summary>
///   Invokes EventSetInformation to provide additional information to the ETW
///   runtime.
///
///   Note that the implementation of this function depends on the values of
///   the EMCGEN_HAVE_EVENTSETINFORMATION and EMCGEN_EVENTSETINFORMATION macros.
///   Depending on the values of these macros, this function may call
///   EventSetInformation directly, may dynamically-load EventSetInformation
///   via GetProcAddress, or may call a replacement function.
/// </summary>
/// <param name=""regHandle"">Registration handle returned by EventRegister.</param>
/// <param name=""informationClass"">Type of operation to be performed on the registration object.</param>
/// <param name=""eventInformation"">The input buffer.</param>
/// <param name=""informationLength"">Size of the input buffer.</param>
EMCGEN_NOINLINE
ULONG __stdcall
EmcGenEventSetInformation(
    _In_ REGHANDLE regHandle,
    _In_ EVENT_INFO_CLASS informationClass,
    _In_opt_bytecount_(informationLength) void* eventInformation,
    _In_ ULONG informationLength)
{
    ULONG ec;

#if EMCGEN_HAVE_EVENTSETINFORMATION == 1

#pragma warning(suppress: 6387) // It's ok for EventInformation to be null if informationLength is 0.
    ec = EMCGEN_EVENTSETINFORMATION(
        regHandle,
        informationClass,
        eventInformation,
        informationLength);

#elif EMCGEN_HAVE_EVENTSETINFORMATION == 2

    ec = ERROR_NOT_SUPPORTED;
    HMODULE hEventing;
    if (GetModuleHandleExW(0, L""api-ms-win-eventing-provider-l1-1-0"", &hEventing) ||
        GetModuleHandleExW(0, L""advapi32"", &hEventing)) {
        using PFEventSetInformation = ULONG(WINAPI*)(
            _In_ REGHANDLE regHandle,
            _In_ EVENT_INFO_CLASS informationClass,
            _In_opt_bytecount_(informationLength) PVOID eventInformation,
            _In_ ULONG informationLength);
        PFEventSetInformation pfEventSetInformation =
            reinterpret_cast<PFEventSetInformation>(GetProcAddress(hEventing, ""EventSetInformation""));
        if (pfEventSetInformation) {
            ec = pfEventSetInformation(
                regHandle,
                informationClass,
                eventInformation,
                informationLength);
        }

        FreeLibrary(hEventing);
    }

#else // EMCGEN_HAVE_EVENTSETINFORMATION == 0

    (void)regHandle;
    (void)informationClass;
    (void)eventInformation;
    (void)informationLength;

    ec = ERROR_NOT_SUPPORTED;

#endif // EMCGEN_HAVE_EVENTSETINFORMATION

    return ec;
}

namespace emcgen_details
{

inline std::string EmcGenU16To8(wchar_t const* const str, size_t const size)
{
    int ret = WideCharToMultiByte(CP_UTF8, 0, str, size, nullptr, 0, nullptr, nullptr);
    if (ret == 0)
        return std::string();

    std::string buffer(ret, '\0');

    ret = WideCharToMultiByte(CP_UTF8, 0, str, size, &buffer[0],
                              static_cast<int>(buffer.size() + 1), nullptr, nullptr);
    if (ret == 0)
        return std::string();

    return buffer;
}

inline std::string EmcGenGetProcessName()
{
    size_t const MaxPath = MAX_PATH;
    size_t const MaxExtendedPath = 32767;

    std::wstring buffer;
    DWORD ec = ERROR_INSUFFICIENT_BUFFER;

    // GetModuleFileNameW has no way to indicate the required buffer length. So
    // we have to retry with successively larger buffers. Start with the max
    // short path length and double the size until we reach the documented file
    // path limit.
    for (size_t bufferSize = MaxPath;
         bufferSize < MaxExtendedPath && ec == ERROR_INSUFFICIENT_BUFFER;
         bufferSize *= 2) {
        buffer.resize(bufferSize);

        // std::string leaves space for the null terminator. While this is out
        // of range of the string's size, the standard allows accessing and
        // overwriting as long as it is kept a null terminator, and
        // GetModuleFileNameW always writes one.
        size_t const size = GetModuleFileNameW(
            nullptr, &buffer[0], static_cast<DWORD>(buffer.size() + 1));

        // The returned size s with a buffer length l falls into three categories:
        //   s == 0:    Function failed.
        //   s == l:    Success, but truncated path, s is the size with terminator.
        //   0 < s < l: Success, s is the size without terminator.
        if (size > 0 && size <= buffer.size()) {
            // Remove directory and extensions.
            size_t const nameSep = buffer.find_last_of(L""\\/:"");
            size_t const extSep = buffer.find_last_of(L'.');
            size_t const startIdx = nameSep != std::wstring::npos ? nameSep + 1 : 0;
            size_t const endIdx = extSep != std::wstring::npos ? extSep : buffer.size();
            return EmcGenU16To8(&buffer[0] + startIdx, endIdx - startIdx);
        }

        ec = GetLastError();
    }

    // Error other than ERROR_INSUFFICIENT_BUFFER occurred or somehow the path
    // is longer than the maximum documented extended path length.
    return std::string();
}

EMCGEN_NOINLINE
void const* EmcGenAddProcessNameTrait(
    _Inout_ std::unique_ptr<std::uint8_t[]>& buffer, _In_ void const* const providerTraits)
{
    std::string const processName = EmcGenGetProcessName();
    if (processName.empty())
        return providerTraits;

    std::uint16_t const inputTraitsSize = *reinterpret_cast<std::uint16_t const UNALIGNED*>(providerTraits);

    std::uint8_t const EtwProviderTraitTypeProcessName = 128;

    std::uint16_t const traitSize = 2 /*Size*/ + 1 /*Type*/ + static_cast<std::uint16_t>(processName.size()) + 1 /*NUL*/;
    std::uint8_t const traitType = EtwProviderTraitTypeProcessName;
    std::uint16_t const newTraitsSize = inputTraitsSize + traitSize;

    buffer.reset(new std::uint8_t[newTraitsSize]);

    // Copy input traits
    std::memcpy(buffer.get(), providerTraits, inputTraitsSize);

    // Overwrite total traits size
    std::memcpy(buffer.get(), &newTraitsSize, sizeof(newTraitsSize));

    // Write process name trait
    std::uint8_t* trait = buffer.get() + inputTraitsSize;
    std::memcpy(trait, &traitSize, sizeof(traitSize));
    trait += sizeof(traitSize);
    std::memcpy(trait, &traitType, sizeof(traitType));
    trait += sizeof(traitType);
    std::memcpy(trait, processName.data(), processName.size() + 1);

    return buffer.get();
}

} // namespace emcgen_details

/// <summary>
///   Registers the provider with ETW and registers provider traits.
/// </summary>
/// <param name=""providerId"">Provider ID to register with ETW.</param>
/// <param name=""enableCallback"">Callback to be used.</param>
/// <param name=""callbackContext"">Context for the callback.</param>
/// <param name=""context"">Provider context.</param>
/// <remarks>
///   Should not be called if the provider is already registered (i.e. if
///   <c>context.RegistrationHandle != 0</c>). Repeatedly registering a
///   provider is a bug and may indicate a race condition.
/// </remarks>
EMCGEN_NOINLINE
ULONG __stdcall
EmcGenEventRegisterContext(
    _In_ GUID const& providerId,
    _In_opt_ EMCGEN_PENABLECALLBACK enableCallback,
    _In_opt_ void* callbackContext,
    _Inout_ EmcGenTraceContext& context)
{
    if (context.RegistrationHandle != 0)
        return ERROR_INVALID_PARAMETER;

    ULONG const ec = EMCGEN_EVENTREGISTER(
        &providerId,
        enableCallback,
        callbackContext,
        &context.RegistrationHandle);
    if (ec != 0)
        return ec;

    (void)EmcGenEventSetInformation(
        context.RegistrationHandle, EventProviderBinaryTrackInfo, nullptr, 0);

    if (context.ProviderTraits) {
        void const* providerTraits = const_cast<void*>(context.ProviderTraits);

        std::unique_ptr<std::uint8_t[]> providerTraitsBuffer;
        if (context.Flags & 1 /*IncludeProcessName*/) {
            providerTraits = emcgen_details::EmcGenAddProcessNameTrait(
                providerTraitsBuffer, providerTraits);
        }

        (void)EmcGenEventSetInformation(
            context.RegistrationHandle,
            EventProviderSetTraits,
            const_cast<void*>(providerTraits),
            *reinterpret_cast<USHORT const UNALIGNED*>(providerTraits));
    }

    return 0;
}

/// <summary>
///   Unregister from ETW and set *RegHandle = 0.
/// </summary>
/// <param name=""regHandle"">
///   A pointer to the provider registration handle.
/// </param>
/// <remarks>
///   If provider has not been registered (i.e. if *RegHandle == 0),
///   return SUCCESS. It is safe to call EmcGenEventUnregister even if the
///   call to EmcGenEventRegister returned an error.
/// </remarks>
EMCGEN_NOINLINE
ULONG __stdcall
EmcGenEventUnregister(_Inout_ PREGHANDLE regHandle)
{
    if (*regHandle == 0)
        return 0; // ERROR_SUCCESS

    ULONG const ec = EMCGEN_EVENTUNREGISTER(*regHandle);
    *regHandle = REGHANDLE(0);

    return ec;
}

/// <summary>
///   Creates and sets a new ambient thread-local activity id.
/// </summary>
struct ScopedActivityId
{
    EMCGEN_FORCEINLINE ScopedActivityId() noexcept
    {
        // Create a new id for this activity.
        EventActivityIdControl(EVENT_ACTIVITY_CTRL_CREATE_ID, &activityId);

        // Set the new activity id, receiving back the previous activity id.
        relatedActivityId = activityId;
        EventActivityIdControl(EVENT_ACTIVITY_CTRL_GET_SET_ID, &relatedActivityId);
    }

    EMCGEN_FORCEINLINE ~ScopedActivityId() noexcept
    {
        // Restore the parent activity id.
        EventActivityIdControl(EVENT_ACTIVITY_CTRL_SET_ID, &relatedActivityId);
    }

    GUID activityId;
    GUID relatedActivityId;
};

// Linker sections for the list of static providers.
#pragma section(""EMCGEN$__a"", read)
#pragma section(""EMCGEN$__m"", read)
#pragma section(""EMCGEN$__z"", read)

struct EmcGenStaticProviderList
{
    struct Entry
    {
        using FunctionType = ULONG();
        FunctionType* Register;
        FunctionType* Unregister;
    };

    /// <summary>
    ///   Registers all enlisted static providers with ETW.
    /// </summary>
    static inline ULONG RegisterAll()
    {
        ULONG result = 0;
        for (auto it = &first + 1; it != &last; ++it) {
            if (*it) {
                ULONG const ec = (*it)->Register();
                if (ec != 0 && result == 0)
                    result = ec;
            }
        }
        return result;
    }

    /// <summary>
    ///   Unregisters all enlisted static providers from ETW.
    /// </summary>
    static ULONG UnregisterAll()
    {
        ULONG result = 0;
        for (auto it = &first + 1; it != &last; ++it) {
            if (*it) {
                ULONG const ec = (*it)->Unregister();
                if (ec != 0 && result == 0)
                    result = ec;
            }
        }
        return result;
    }

private:
    __declspec(allocate(""EMCGEN$__a"")) static Entry const* first;
    __declspec(allocate(""EMCGEN$__z"")) static Entry const* last;
};

__declspec(selectany) __declspec(allocate(""EMCGEN$__a"")) EmcGenStaticProviderList::Entry const* EmcGenStaticProviderList::first = nullptr;
__declspec(selectany) __declspec(allocate(""EMCGEN$__z"")) EmcGenStaticProviderList::Entry const* EmcGenStaticProviderList::last = nullptr;

";

            ow.WriteLine(preamble);

            ow.WriteLine("// EMCGEN_DISABLE_PROVIDER_CODE_GENERATION macro:");
            ow.WriteLine("// Define this macro to have the compiler skip the generated functions in this");
            ow.WriteLine("// header.");
            ow.WriteLine("#ifndef EMCGEN_DISABLE_PROVIDER_CODE_GENERATION");
            WriteNamespaceBegin(options.EtwNamespace);
            ow.WriteLine(sharedDefinitions);

            string[] nsParts = options.EtwNamespace.Split('.');
            string entryType = "::" + string.Join("::", nsParts) + "::EmcGenStaticProviderList::Entry";
            string mangledEntryType = "Entry@EmcGenStaticProviderList@" + string.Join("@", nsParts.Reverse()) + "@";

            ow.WriteLine("#ifdef _WIN64");
            ow.WriteLine("#define EMCGEN_PROVIDER_ENTRY_PRAGMA(ProviderSymbol, MangledProviderNs) \\");
            ow.WriteLine($"    __pragma(comment(linker, \"/include:?\" #ProviderSymbol \"Slot@\" MangledProviderNs \"@3QEBU{mangledEntryType}@EB\"))");
            ow.WriteLine("#else");
            ow.WriteLine("#define EMCGEN_PROVIDER_ENTRY_PRAGMA(ProviderSymbol, MangledProviderNs) \\");
            ow.WriteLine($"    __pragma(comment(linker, \"/include:?\" #ProviderSymbol \"Slot@\" MangledProviderNs \"@3QBU{mangledEntryType}@B\"))");
            ow.WriteLine("#endif");
            ow.WriteLine();
            ow.WriteLine("#define EMCGEN_ENLIST_STATIC_PROVIDER(ProviderSymbol, MangledProviderNs, RegisterFunc, UnregisterFunc) \\");
            ow.WriteLine("    EMCGEN_PROVIDER_ENTRY_PRAGMA(ProviderSymbol, MangledProviderNs); \\");
            ow.WriteLine($"    __declspec(selectany) {entryType} ProviderSymbol ## Entry = {{RegisterFunc, UnregisterFunc}}; \\");
            ow.WriteLine($"    extern __declspec(allocate(\"EMCGEN$__m\")) __declspec(selectany) {entryType} const* const ProviderSymbol ## Slot = &ProviderSymbol ## Entry");
            ow.WriteLine();
            ow.WriteLine("#endif // EMCGEN_REGISTRATION");
            ow.WriteLine();
            WriteNamespaceEnd(options.EtwNamespace);
            ow.WriteLine("#endif // EMCGEN_DISABLE_PROVIDER_CODE_GENERATION");
            ow.WriteLine();
        }

        private abstract class CodeParameter
        {
            protected readonly ICodeGenNaming naming;

            protected CodeParameter(ICodeGenNaming naming, Property property)
            {
                this.naming = naming;
                Property = property;
            }

            public static CodeParameter Create(ICodeGenNaming naming, Property property)
            {
                if (property.Kind == PropertyKind.Struct) {
                    return new StructParameter(naming, (StructProperty)property);
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
                            return new PrimitiveParameter(naming, dataProperty);

                        case "UnicodeString":
                        case "AnsiString":
                            return new StringParameter(naming, dataProperty);

                        case "CountedUnicodeString":
                        case "CountedAnsiString":
                            return new CountedStringParameter(naming, dataProperty);

                        case "Binary":
                            return new BinaryParameter(naming, dataProperty);

                        case "CountedBinary":
                            return new CountedBinaryParameter(naming, dataProperty);

                        case "GUID":
                            return new RecordParameter(naming, dataProperty, "GUID");
                        case "FILETIME":
                            return new RecordParameter(naming, dataProperty, "FILETIME");
                        case "SYSTEMTIME":
                            return new RecordParameter(naming, dataProperty, "SYSTEMTIME");

                        case "SID":
                            return new SecurityIdParameter(naming, dataProperty);

                        case "Pointer":
                            return new PointerParameter(naming, dataProperty);
                    }
                }

                throw new InternalException("unhandled type '{0}'", property);
            }

            protected Property Property { get; }

            public abstract int DataDescriptorCount { get; }
            public bool RequiresPacking => IsComplex && Property.Count.IsMultiple;
            protected virtual bool IsComplex => false;
            protected virtual bool HasExtraLengthParam => false;
            protected abstract string DataPtrQualifier { get; }
            protected abstract string ParamQualifier { get; }

            public IEnumerable<string> ParameterNames(bool usePropertyName, bool honorOutType = false)
            {
                if (HasExtraLengthParam) {
                    yield return naming.GetLengthArgumentId(Property, usePropertyName);
                }

                if (honorOutType && ArgType != ArgTypeAlt) {
                    yield return "reinterpret_cast<" + ArgType + ">" + "(" + naming.GetArgumentId(Property, usePropertyName) + ")";
                } else {
                    yield return naming.GetArgumentId(Property, usePropertyName);
                }
            }

            public IEnumerable<string> Parameters(bool usePropertyName, bool honorOutType = false)
            {
                if (HasExtraLengthParam) {
                    string lenArgName = naming.GetLengthArgumentId(Property, usePropertyName);
                    yield return $"_In_ ULONG {lenArgName}";
                }

                string argName = naming.GetArgumentId(Property, usePropertyName);
                yield return $"{GetArgSalSpec()} {(honorOutType ? ArgTypeAlt : ArgType)} {ParamQualifier}{argName}";
            }

            public void AppendDataDescriptor(ref int index, TextWriter writer)
            {
                if (RequiresPacking) {
                    AppendPackedDataDescriptor(ref index, writer);
                } else {
                    AppendUnpackedDataDescriptor(ref index, writer);
                }
            }

            protected virtual void AppendUnpackedDataDescriptor(ref int index, TextWriter writer)
            {
                if (HasExtraLengthParam) {
                    writer.WriteLine(
                        "EventDataDescCreate(&{0}[{1}], &{0}[{2}].Size, {3});",
                        naming.EventDataDescriptorId,
                        index + 1,
                        index + 2,
                        "sizeof(USHORT)");
                    ++index;

                    writer.WriteLine();
                }

                writer.WriteLine(
                    "EventDataDescCreate(&{0}[{1}], {3}{2}, {4});{5}",
                    naming.EventDataDescriptorId,
                    index + 1,
                    naming.GetNumberedArgId(Property.Index),
                    DataPtrQualifier,
                    GetDataSizeExpr(),
                    GetDataDescriptorComment());
                ++index;
            }

            protected virtual string GetDataDescriptorComment() => null;

            protected virtual string GetDataDescriptorPackedComment()
            {
                return $" // Blob contains data for {GetCountExpr(string.Empty)} chunks; each chunk is a 16-bit ByteCount followed by ByteCount bytes of data.";
            }

            protected void AppendPackedDataDescriptor(ref int index, TextWriter writer)
            {
                writer.WriteLine(
                    "EventDataDescCreate(&{0}[{1}], {3}{2}, {4});{5}",
                    naming.EventDataDescriptorId,
                    index + 1,
                    naming.GetNumberedArgId(Property.Index),
                    DataPtrQualifier,
                    GetDataSizeExpr(),
                    GetDataDescriptorPackedComment());
                ++index;
            }

            protected abstract string ArgType { get; }
            protected virtual string ArgTypeAlt => ArgType;
            protected abstract string GetArgSalSpec();

            protected abstract string GetDataSizeExpr();

            protected virtual string GetCountExpr(string prefix = "*")
            {
                return GetNumberExpr(Property.Count, prefix);
            }

            protected virtual string GetLengthExpr(string prefix = "*")
            {
                return GetNumberExpr(Property.Length, prefix);
            }

            protected string GetNumberExpr(IPropertyNumber number, string prefix = "*")
            {
                if (number.IsVariable) {
                    int idx = number.DataProperty.Index;
                    return prefix + naming.GetNumberedArgId(idx);
                }
                if (number.IsFixedMultiple)
                    return prefix + number.Value;
                return string.Empty;
            }
        }

        private abstract class DataParameter : CodeParameter
        {
            protected DataParameter(ICodeGenNaming naming, DataProperty property)
                : base(naming, property)
            {
            }

            protected new DataProperty Property => (DataProperty)base.Property;
        }

        private sealed class StructParameter : CodeParameter
        {
            public StructParameter(ICodeGenNaming naming, StructProperty property)
                : base(naming, property)
            {
            }

            private new StructProperty Property => (StructProperty)base.Property;

            public override int DataDescriptorCount => 1;
            protected override bool HasExtraLengthParam => true;

            protected override string ParamQualifier => string.Empty;
            protected override string DataPtrQualifier => string.Empty;

            protected override string GetArgSalSpec()
            {
                return "_In_";
            }

            protected override string ArgType => "void const*";

            protected override string GetDataSizeExpr()
            {
                string countExpr = null;
                if (Property.Count.IsMultiple)
                    countExpr = GetCountExpr(string.Empty) + " * ";
                string lengthExpr = naming.GetLengthArgumentId(Property, false);
                return $"{countExpr}{lengthExpr}";
            }

            protected override void AppendUnpackedDataDescriptor(ref int index, TextWriter writer)
            {
                writer.WriteLine(
                    "EventDataDescCreate(&{0}[{1}],{3}{2}, {4});",
                    naming.EventDataDescriptorId,
                    index + 1,
                    naming.GetNumberedArgId(Property.Index),
                    DataPtrQualifier,
                    GetDataSizeExpr());
                ++index;
            }
        }

        private static class XmlSchemaTypes
        {
            public static QName Long = new QName("long", "xs", XmlSchema.Namespace);
            public static QName UnsignedLong = new QName("unsignedLong", "xs", XmlSchema.Namespace);
        }

        private sealed class PointerParameter : DataParameter
        {
            public PointerParameter(ICodeGenNaming naming, DataProperty property)
                : base(naming, property)
            {
            }

            public override int DataDescriptorCount => 1;
            protected override string ParamQualifier => Property.Count.IsMultiple ? "*" : string.Empty;
            protected override string DataPtrQualifier => Property.Count.IsMultiple ? string.Empty : "&";

            protected override string ArgType => UndecoratedType + " const";
            protected override string ArgTypeAlt => UndecoratedTypeAlt + " const";

            private string UndecoratedType => "void const*";

            private string UndecoratedTypeAlt
            {
                get
                {
                    if (Property.OutType?.Name == XmlSchemaTypes.Long)
                        return "std::intptr_t";
                    if (Property.OutType?.Name == XmlSchemaTypes.UnsignedLong)
                        return "std::uintptr_t";
                    return "void const*";
                }
            }

            protected override string GetArgSalSpec()
            {
                if (Property.Count.IsMultiple)
                    return string.Format("_In_reads_({0})", GetCountExpr(string.Empty));
                return "_In_opt_";
            }

            protected override string GetDataSizeExpr()
            {
                return $"sizeof({UndecoratedType}){GetCountExpr()}";
            }
        }

        private sealed class RecordParameter : DataParameter
        {
            private readonly string structName;

            public RecordParameter(ICodeGenNaming naming, DataProperty property, string structName)
                : base(naming, property)
            {
                this.structName = structName;
            }

            public override int DataDescriptorCount => 1;
            protected override string ParamQualifier => string.Empty;
            protected override string DataPtrQualifier => string.Empty;

            protected override string GetDataSizeExpr()
            {
                return $"sizeof({structName}){GetCountExpr()}";
            }

            protected override string ArgType => "const " + structName + "*";

            protected override string GetArgSalSpec()
            {
                if (Property.Count.IsMultiple)
                    return $"_In_reads_({GetCountExpr(string.Empty)})";
                return "_In_";
            }
        }

        private sealed class PrimitiveParameter : DataParameter
        {
            public PrimitiveParameter(ICodeGenNaming naming, DataProperty property)
                : base(naming, property)
            {
            }

            public override int DataDescriptorCount => 1;
            protected override string ParamQualifier => Property.Count.IsMultiple ? "*" : string.Empty;
            protected override string DataPtrQualifier => Property.Count.IsMultiple ? string.Empty : "&";

            protected override string GetArgSalSpec()
            {
                if (Property.Count.IsMultiple)
                    return $"_In_reads_({GetCountExpr(string.Empty)})";
                return "_In_";
            }

            protected override string ArgType => Property.InType.Name.LocalName switch
            {
                "Int8" => "std::int8_t const",
                "UInt8" => "std::uint8_t const",
                "Int16" => "std::int16_t const",
                "UInt16" => "std::uint16_t const",
                "Int32" => "std::int32_t const",
                "UInt32" => "std::uint32_t const",
                "Int64" => "std::int64_t const",
                "UInt64" => "std::uint64_t const",
                "Float" => "float const",
                "Double" => "double const",
                "Boolean" => "bool const",
                "HexInt32" => "std::int32_t const",
                "HexInt64" => "std::int64_t const",
                _ => throw new InternalException("unhandled type '{0}'", Property.InType),
            };

            protected override string GetDataSizeExpr()
            {
                return $"sizeof({ArgType}){GetCountExpr()}";
            }
        }

        private sealed class SecurityIdParameter : DataParameter
        {
            public SecurityIdParameter(ICodeGenNaming naming, DataProperty property)
                : base(naming, property)
            {
            }

            public override int DataDescriptorCount => 1;
            protected override bool IsComplex => true;
            protected override bool HasExtraLengthParam => RequiresPacking;
            protected override string ParamQualifier => string.Empty;
            protected override string DataPtrQualifier => string.Empty;

            protected override void AppendUnpackedDataDescriptor(ref int index, TextWriter writer)
            {
                writer.WriteLine(
                    "EventDataDescCreate(&{0}[{1}], ({2} != nullptr) ? {2} : static_cast<void const*>(\"\\0\\0\\0\\0\\0\\0\\0\"), {4});",
                    naming.EventDataDescriptorId,
                    index + 1,
                    naming.GetNumberedArgId(Property.Index),
                    "",
                    GetDataSizeExpr());
                ++index;
            }

            protected override string GetDataDescriptorPackedComment()
            {
                return null;
            }

            protected override string ArgType
            {
                get
                {
                    if (RequiresPacking)
                        return "const unsigned char*";
                    return "const SID*";
                }
            }

            protected override string GetArgSalSpec()
            {
                if (RequiresPacking)
                    return $"_In_reads_({GetCountExpr(string.Empty)})";
                return "_In_";
            }

            protected override string GetCountExpr(string prefix = "*")
            {
                return prefix + naming.GetLengthArgumentId(Property, false);
            }

            protected override string GetDataSizeExpr()
            {
                if (RequiresPacking)
                    return naming.GetLengthArgumentId(Property, false);
                return string.Format(
                    "({0} != nullptr) ? EMCGEN_GETLENGTHSID({0}) : 8",
                    naming.GetNumberedArgId(Property.Index));
            }
        }

        private sealed class BinaryParameter : DataParameter
        {
            public BinaryParameter(ICodeGenNaming naming, DataProperty property)
                : base(naming, property)
            {
            }

            public override int DataDescriptorCount => 1;
            protected override string ParamQualifier => string.Empty;
            protected override string DataPtrQualifier => string.Empty;

            protected override string GetArgSalSpec()
            {
                return $"_In_reads_({GetLengthExpr(string.Empty)}{GetCountExpr()})";
            }

            protected override string ArgType => "const unsigned char*";

            protected override string GetDataSizeExpr()
            {
                return $"(ULONG)sizeof(char){GetLengthExpr()}{GetCountExpr()}";
            }

            protected override string GetDataDescriptorComment()
            {
                if (Property.Count.IsMultiple)
                    return string.Format("  // Blob containing {0} concatenated strings; each string has the same length ({1})",
                        GetCountExpr(string.Empty),
                        GetLengthExpr(string.Empty));

                return null;
            }
        }

        private sealed class CountedStringParameter : DataParameter
        {
            public CountedStringParameter(ICodeGenNaming naming, DataProperty property)
                : base(naming, property)
            {
            }

            public override int DataDescriptorCount => RequiresPacking ? 1 : 2;
            protected override bool IsComplex => true;
            protected override bool HasExtraLengthParam => true;
            protected override string ParamQualifier => string.Empty;
            protected override string DataPtrQualifier => string.Empty;

            protected override string ArgType => Property.InType.Name.LocalName switch
            {
                "CountedUnicodeString" => "wchar_t const*",
                "CountedAnsiString" => "char const*",
                _ => throw new InternalException("unhandled type '{0}'", Property.InType),
            };

            protected override string GetArgSalSpec()
            {
                string expr = naming.GetLengthArgumentId(Property, false);
                return $"_In_reads_({expr})";
            }

            protected override string GetDataSizeExpr()
            {
                string lengthExpr = naming.GetLengthArgumentId(Property, false);

                string countExpr = null;
                if (Property.Count.IsSpecified && Property.Length.IsSpecified)
                    countExpr = GetCountExpr();

                string type = Property.InType.Name == WinEventSchema.CountedUnicodeString ? "wchar_t" : "char";

                return $"static_cast<USHORT>(sizeof({type}){countExpr}*{lengthExpr})";
            }
        }

        private sealed class CountedBinaryParameter : DataParameter
        {
            public CountedBinaryParameter(ICodeGenNaming naming, DataProperty property)
                : base(naming, property)
            {
            }

            public override int DataDescriptorCount => RequiresPacking ? 1 : 2;
            protected override bool IsComplex => true;
            protected override bool HasExtraLengthParam => true;
            protected override string ParamQualifier => string.Empty;
            protected override string DataPtrQualifier => string.Empty;

            protected override string ArgType => "const unsigned char*";

            protected override string GetArgSalSpec()
            {
                string expr = naming.GetLengthArgumentId(Property, false);
                return $"_In_reads_({expr})";
            }

            protected override string GetDataSizeExpr()
            {
                if (Property.Count.IsSpecified)
                    return $"static_cast<USHORT>(sizeof(char){GetLengthExpr()})";
                return $"static_cast<USHORT>(sizeof(char){GetLengthExpr()}{GetCountExpr()})";
            }

            protected override string GetLengthExpr(string prefix = "*")
            {
                return prefix + naming.GetLengthArgumentId(Property, false);
            }
        }

        private sealed class StringParameter : DataParameter
        {
            public StringParameter(ICodeGenNaming naming, DataProperty property)
                : base(naming, property)
            {
            }

            public override int DataDescriptorCount => 1;
            protected override bool IsComplex => true;
            protected override bool HasExtraLengthParam => RequiresPacking && !Property.Length.IsSpecified;
            protected override string ParamQualifier => string.Empty;
            protected override string DataPtrQualifier => string.Empty;

            protected override void AppendUnpackedDataDescriptor(ref int index, TextWriter writer)
            {
                if (!Property.Length.IsSpecified) {
                    string argName = naming.GetNumberedArgId(Property.Index);

                    writer.WriteLine(
                        "EventDataDescCreate(&{0}[{1}],",
                        naming.EventDataDescriptorId,
                        index + 1);

                    if (Property.InType.Name == WinEventSchema.UnicodeString) {
                        writer.WriteLine("                    ({0} != nullptr) ? {0} : L\"NULL\",", argName);
                        writer.WriteLine("                    static_cast<ULONG>(({0} != nullptr) ? ((wcslen({0}) + 1) * sizeof(wchar_t)) : sizeof(L\"NULL\")));", argName);
                    } else {
                        writer.WriteLine("                    ({0} != nullptr) ? {0} : \"NULL\",", argName);
                        writer.WriteLine("                    static_cast<ULONG>(({0} != nullptr) ? ((strlen({0}) + 1) * sizeof(char)) : sizeof(\"NULL\")));", argName);
                    }

                    ++index;
                    return;
                }

                base.AppendUnpackedDataDescriptor(ref index, writer);
            }

            protected override string GetDataDescriptorPackedComment()
            {
                string comment;
                if (Property.Length.IsSpecified) {
                    comment = $" // Blob containing {GetCountExpr(string.Empty)} concatenated strings;" +
                              $" each string has the same length ({GetLengthExpr(string.Empty)})";
                } else {
                    comment = $" // Blob containing {GetCountExpr(string.Empty)} concatenated nul-terminated strings";
                }

                return comment;
            }

            protected override string ArgType => Property.InType.Name.LocalName switch
            {
                "UnicodeString" => "wchar_t const*",
                "AnsiString" => "char const*",
                _ => throw new InternalException("unhandled type '{0}'", Property.InType),
            };

            protected override string GetArgSalSpec()
            {
                if (!Property.Count.IsMultiple && !Property.Length.IsSpecified)
                    return "_In_opt_";

                string expr;
                if (!Property.Count.IsSpecified || Property.Length.IsSpecified)
                    expr = $"{GetLengthExpr(string.Empty)}{GetCountExpr()}";
                else if (Property.Count.IsVariable)
                    expr = GetCountExpr(string.Empty);
                else
                    expr = naming.GetLengthArgumentId(Property, false);

                return $"_In_reads_({expr})";
            }

            protected override string GetDataSizeExpr()
            {
                if (!Property.Count.IsSpecified && !Property.Length.IsSpecified)
                    throw new Exception();

                string expr;
                if (!Property.Count.IsSpecified)
                    expr = $"{GetLengthExpr()}";
                else if (Property.Length.IsSpecified)
                    expr = $"{GetCountExpr()}{GetLengthExpr()}";
                else
                    expr = "*" + naming.GetLengthArgumentId(Property, false);

                string type = Property.InType.Name == WinEventSchema.UnicodeString ? "wchar_t" : "char";

                return $"static_cast<ULONG>(sizeof({type}){expr})";
            }
        }
    }
}
