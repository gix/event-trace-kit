namespace EventManifestCompiler
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using EventTraceKit.EventTracing;
    using EventTraceKit.EventTracing.Compilation.ResGen;
    using EventTraceKit.EventTracing.Internal.Extensions;
    using EventTraceKit.EventTracing.Internal.Native;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Schema.Base;
    using EventTraceKit.EventTracing.Support;

    public class DecompilationOptions
    {
        public string InputModule { get; set; }
        public string InputEventTemplate { get; set; }
        public string InputMessageTable { get; set; }
        public string OutputManifest { get; set; }
    }

    public sealed class EventTemplateDecompiler
    {
        private readonly IDiagnostics diags;
        private readonly DecompilationOptions opts;

        public EventTemplateDecompiler(IDiagnostics diags, DecompilationOptions opts)
        {
            this.diags = diags ?? throw new ArgumentNullException(nameof(diags));
            this.opts = opts ?? throw new ArgumentNullException(nameof(opts));

            if (opts.InputModule is null && (opts.InputEventTemplate is null || opts.InputMessageTable is null))
                throw new ArgumentException("No input files", nameof(opts));
        }

        public bool Run()
        {
            string manifestFile = opts.OutputManifest;

            var metadata = EventManifestParser.LoadWinmeta(diags);
            var reader = new EventTemplateReader(diags, metadata);

            EventManifest manifest;
            IEnumerable<Message> messages;
            if (opts.InputModule != null) {
                string providerBinary = Path.GetFullPath(opts.InputModule);

                using var module = SafeModuleHandle.LoadImageResource(providerBinary);
                if (module.IsInvalid)
                    throw new Win32Exception();

                using (var stream = module.OpenResource(UnsafeNativeMethods.RT_MESSAGETABLE, 1))
                    messages = reader.ReadMessageTable(stream);

                using (var stream = module.OpenResource("WEVT_TEMPLATE", 1))
                    manifest = reader.ReadWevtTemplate(stream, messages);

                foreach (var provider in manifest.Providers) {
                    provider.ResourceFileName = providerBinary;
                    provider.MessageFileName = providerBinary;
                }
            } else {
                using (var stream = File.OpenRead(opts.InputMessageTable))
                    messages = reader.ReadMessageTable(stream);

                using (var stream = File.OpenRead(opts.InputEventTemplate))
                    manifest = reader.ReadWevtTemplate(stream, messages);
            }

            StripReservedMetadata(manifest, metadata);
            InferSymbols(manifest);
            StripDefaultMessageIds(manifest);

            XDocument doc = manifest.ToXml();
            var settings = new XmlWriterSettings {
                Indent = true,
                IndentChars = "  "
            };
            using var output = File.Create(manifestFile);
            using var writer = XmlWriter.Create(output, settings);
            doc.WriteTo(writer);

            return true;
        }

        private void StripReservedMetadata(EventManifest manifest, IEventManifestMetadata metadata)
        {
            foreach (var provider in manifest.Providers) {
                var channelIds = new HashSet<byte>(metadata.Channels.Select(x => x.Value.Value));
                var levelNames = new HashSet<QName>(metadata.Levels.Select(x => x.Name.Value));
                var taskNames = new HashSet<QName>(metadata.Tasks.Select(x => x.Name.Value));
                var opcodeNames = new HashSet<QName>(metadata.Opcodes.Select(x => x.Name.Value));
                var keywordNames = new HashSet<QName>(metadata.Keywords.Select(x => x.Name.Value));

                var messageSet = new HashSet<LocalizedString>();
                messageSet.UnionWith(from x in provider.Channels
                                     where x.Message != null && channelIds.Contains(x.Value.Value)
                                     select x.Message);
                messageSet.UnionWith(from x in provider.Levels
                                     where x.Message != null && levelNames.Contains(x.Name)
                                     select x.Message);
                messageSet.UnionWith(from x in provider.Tasks
                                     where x.Message != null && taskNames.Contains(x.Name)
                                     select x.Message);
                messageSet.UnionWith(from x in provider.Opcodes
                                     where x.Task == null && x.Message != null && opcodeNames.Contains(x.Name)
                                     select x.Message);
                messageSet.UnionWith(from x in provider.Keywords
                                     where x.Message != null && keywordNames.Contains(x.Name)
                                     select x.Message);

                foreach (var resourceSet in manifest.Resources)
                    resourceSet.Strings.RemoveAll(x => messageSet.Contains(x));

                provider.Channels.RemoveAll(x => channelIds.Contains(x.Value.Value));
                provider.Levels.RemoveAll(x => levelNames.Contains(x.Name));
                provider.Tasks.RemoveAll(x => taskNames.Contains(x.Name));
                provider.Opcodes.RemoveAll(x => opcodeNames.Contains(x.Name));
                provider.Keywords.RemoveAll(x => keywordNames.Contains(x.Name));
            }
        }

        private static Dictionary<TKey, int> GetCountLookup<T, TKey>(IEnumerable<T> collection, Func<T, TKey> keySelector)
        {
            return collection.GroupBy(keySelector).ToDictionary(x => x.Key, x => x.Count());
        }

        private void InferSymbols(EventManifest manifest)
        {
            foreach (var provider in manifest.Providers)
                InferSymbols(provider);
        }

        private void InferSymbols(Provider provider)
        {
            var symbolTable = new HashSet<string>();

            var taskCounts = GetCountLookup(
                provider.Events.Where(x => x.Task != null),
                x => x.Task.Name.Value.LocalName);

            var opcodeCounts = GetCountLookup(
                provider.Events.Where(x => x.Task == null && x.Opcode != null),
                x => x.Opcode.Name.Value.LocalName ?? string.Empty);

            foreach (var evt in provider.Events) {
                if (evt.Name != null) {
                    evt.Symbol = GetUniqueSymbol(evt.Name, symbolTable);
                    continue;
                }

                if (evt.Task != null) {
                    string taskName = evt.Task.Name.Value.LocalName;
                    if (taskCounts[taskName] == 1) {
                        evt.Symbol = GetUniqueSymbol(taskName, symbolTable);
                        continue;
                    }

                    if (evt.Opcode != null) {
                        string opcodeName = evt.Opcode.Name.Value.LocalName;
                        evt.Symbol = GetUniqueSymbol(taskName + "_" + opcodeName, symbolTable);
                        continue;
                    }
                } else if (evt.Opcode != null) {
                    string opcodeName = evt.Opcode.Name.Value.LocalName;
                    if (opcodeCounts[opcodeName] == 1) {
                        evt.Symbol = GetUniqueSymbol(opcodeName, symbolTable);
                        continue;
                    }
                }

                var symbol = "Event" + evt.Value.Value;
                if (symbolTable.Contains(symbol) && evt.Version.Value != 0)
                    symbol += $"_v{evt.Version.Value}";
                evt.Symbol = GetUniqueSymbol(symbol, symbolTable);
            }
        }

        private static string GetUniqueSymbol(string symbol, HashSet<string> symbolTable)
        {
            symbol = SanitizeSymbol(symbol);

            if (symbolTable.Add(symbol))
                return symbol;

            for (int suffix = 2; ; ++suffix) {
                string suffixed = symbol + "_" + suffix;
                if (symbolTable.Add(suffixed))
                    return suffixed;
            }
        }

        private static string SanitizeSymbol(string symbol)
        {
            symbol = symbol.Replace('-', '_');
            if (symbol.Length > 0 && char.IsDigit(symbol[0]))
                symbol = "_" + symbol;
            return symbol;
        }

        private void StripDefaultMessageIds(EventManifest manifest)
        {
            var idGenerator = new StableMessageIdGenerator(new NullDiagnostics());
            foreach (var provider in manifest.Providers) {
                StripMessageId(provider, x => x.Message, x => idGenerator.CreateId(provider));
                foreach (var item in provider.Events)
                    StripMessageId(item, x => x.Message, x => idGenerator.CreateId(x, provider));
                foreach (var item in provider.Levels)
                    StripMessageId(item, x => x.Message, x => idGenerator.CreateId(x, provider));
                foreach (var item in provider.Channels)
                    StripMessageId(item, x => x.Message, x => idGenerator.CreateId(x, provider));
                foreach (var item in provider.Tasks)
                    StripMessageId(item, x => x.Message, x => idGenerator.CreateId(x, provider));
                foreach (var item in provider.Opcodes)
                    StripMessageId(item, x => x.Message, x => idGenerator.CreateId(x, provider));
                foreach (var item in provider.Keywords)
                    StripMessageId(item, x => x.Message, x => idGenerator.CreateId(x, provider));
                foreach (var item in provider.Filters)
                    StripMessageId(item, x => x.Message, x => idGenerator.CreateId(x, provider));
                foreach (var item in provider.Maps)
                    foreach (var mapItem in item.Items)
                        StripMessageId(mapItem, x => x.Message, x => idGenerator.CreateId(x, item, provider));
            }
        }

        private void StripMessageId<T>(
            T entity, Func<T, LocalizedString> messageSelector, Func<T, uint> createId)
        {
            if (entity == null)
                return;

            LocalizedString message = messageSelector(entity);
            if (message == null)
                return;

            if (message.Id != LocalizedString.UnusedId && message.Id == createId(entity))
                message.Id = LocalizedString.UnusedId;
        }
    }
}
