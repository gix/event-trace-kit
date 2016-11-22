namespace InstrManifestCompiler
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using InstrManifestCompiler.EventManifestSchema;
    using InstrManifestCompiler.Extensions;
    using InstrManifestCompiler.Native;
    using InstrManifestCompiler.ResGen;

    internal sealed class GenerateManifestFromProviderAction : IAction
    {
        private readonly XNamespace ns = EventManifestSchema.EventManifestSchema.Namespace;
        private readonly IDiagnostics diags;
        private readonly ImcOpts opts;

        public GenerateManifestFromProviderAction(IDiagnostics diags, ImcOpts opts)
        {
            Contract.Requires<ArgumentNullException>(diags != null);
            Contract.Requires<ArgumentNullException>(opts != null);
            this.diags = diags;
            this.opts = opts;
        }

        public int Execute()
        {
            if (opts.Inputs.Count == 0) {
                diags.ReportError("No input provider specified.");
                Program.ShowBriefHelp();
                return ExitCode.UserError;
            }
            if (opts.Inputs.Count > 1) {
                diags.ReportError("Too many input providers specified.");
                Program.ShowBriefHelp();
                return ExitCode.UserError;
            }

            string providerBinary = opts.Inputs[0];
            string manifestFile = opts.OutputManifest;

            var module = SafeModuleHandle.LoadImageResource(providerBinary);
            if (module.IsInvalid)
                throw new Win32Exception();

            using (module) {
                var metadata = EventManifestParser.LoadWinmeta(diags);
                var templateReader = new EventTemplateReader(diags, metadata);

                IEnumerable<Message> messages;
                using (var stream = module.OpenResource(UnsafeNativeMethods.RT_MESSAGETABLE, 1))
                    messages = templateReader.ReadMessageTable(stream);

                EventManifest manifest;
                using (var stream = module.OpenResource("WEVT_TEMPLATE", 1))
                    manifest = templateReader.ReadWevtTemplate(stream, messages);

                XDocument doc = ToXml(manifest);
                var settings = new XmlWriterSettings {
                    Indent = true,
                    IndentChars = "  "
                };
                using (var output = File.Create(manifestFile))
                using (var writer = XmlWriter.Create(output, settings))
                    doc.WriteTo(writer);
            }

            return ExitCode.Success;
        }

        private XDocument ToXml(EventManifest manifest)
        {
            var providersElem = new XElement(ns + "events");
            foreach (var provider in manifest.Providers)
                providersElem.Add(ToXml(provider));
            providersElem.Add(ToMessageTableXml(manifest.Resources));

            XElement localizationElem = null;
            if (manifest.Resources.Count > 0)
                localizationElem = new XElement(ns + "localization", manifest.Resources.Select(ToXml));

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement(
                    ns + "instrumentationManifest",
                    new XAttribute(XNamespace.Xmlns + "win", WinEventSchema.Namespace),
                    new XAttribute(XNamespace.Xmlns + "xs", "http://www.w3.org/2001/XMLSchema"),
                    new XElement(ns + "instrumentation", providersElem),
                    localizationElem));

            return doc;
        }

        private XElement ToMessageTableXml(IEnumerable<LocalizedResourceSet> resources)
        {
            var elem = new XElement(ns + "messageTable");
            foreach (var resourceSet in resources)
                elem.Add(resourceSet.Strings.Select(ToMessageXml));
            return elem;
        }

        private XElement ToXml(LocalizedResourceSet resourceSet)
        {
            if (resourceSet.Strings.Count == 0)
                return null;

            return new XElement(
                ns + "resources",
                new XAttribute("culture", resourceSet.Culture.Name),
                new XElement(ns + "stringTable", resourceSet.Strings.Select(ToXml)));
        }

        private XElement ToXml(LocalizedString ls)
        {
            return new XElement(
                ns + "string",
                new XAttribute("id", ls.Name.Value),
                new XAttribute("value", ls.Value.Value));
        }

        private XElement ToMessageXml(LocalizedString ls)
        {
            if (ls.Id == LocalizedString.UnusedId)
                return null;

            var elem = new XElement(
                ns + "message",
                new XAttribute("value", ls.Id),
                new XAttribute("message", "$(string." + ls.Name + ")"));
            if (ls.Symbol != null)
                elem.Add(new XAttribute("symbol", ls.Symbol));
            return elem;
        }

        private XElement ToXml(Provider provider)
        {
            var elem = new XElement(
                ns + "provider",
                new XAttribute("name", provider.Name.Value),
                new XAttribute("guid", provider.Id.Value.ToString("B")),
                new XAttribute("symbol", provider.Symbol.Value),
                new XAttribute("resourceFileName", "{missing}"),
                new XAttribute("messageFileName", "{missing}"));
            AddOptionalMessage(elem, provider.Message);

            if (provider.Events.Count > 0)
                elem.Add(new XElement(ns + "events", provider.Events.Select(ToXml)));
            if (provider.Channels.Count > 0)
                elem.Add(new XElement(ns + "channels", provider.Channels.Select(ToXml)));
            if (provider.Levels.Count > 0)
                elem.Add(new XElement(ns + "levels", provider.Levels.Select(ToXml)));
            if (provider.Tasks.Count > 0)
                elem.Add(new XElement(ns + "tasks", provider.Tasks.Select(ToXml)));
            if (provider.Opcodes.Count > 0)
                elem.Add(new XElement(ns + "opcodes", provider.Opcodes.Select(ToXml)));
            if (provider.Keywords.Count > 0)
                elem.Add(new XElement(ns + "keywords", provider.Keywords.Select(ToXml)));
            if (provider.Maps.Count > 0)
                elem.Add(new XElement(ns + "maps", provider.Maps.Select(ToXml)));
            if (provider.Templates.Count > 0)
                elem.Add(new XElement(ns + "templates", provider.Templates.Select(ToXml)));
            if (provider.Filters.Count > 0)
                elem.Add(new XElement(ns + "filters", provider.Filters.Select(ToXml)));

            return elem;
        }

        private XElement ToXml(Event @event)
        {
            var elem = new XElement(
                ns + "event",
                new XAttribute("value", @event.Value));
            if (@event.Version != 0)
                elem.Add(new XAttribute("version", @event.Version));
            if (@event.Symbol != null)
                elem.Add(new XAttribute("symbol", @event.Symbol));
            if (@event.Channel != null)
                elem.Add(new XAttribute("channel", @event.Channel.Name.Value));
            if (@event.Level != null)
                elem.Add(new XAttribute("level", @event.Level.Name.Value.ToPrefixedString()));
            if (@event.Task != null)
                elem.Add(new XAttribute("task", @event.Task.Name.Value.ToPrefixedString()));
            if (@event.Opcode != null)
                elem.Add(new XAttribute("opcode", @event.Opcode.Name.Value.ToPrefixedString()));
            if (@event.Keywords.Count > 0)
                elem.Add(new XAttribute(
                    "keywords",
                    string.Join(" ", @event.Keywords.Select(k => k.Name.Value.ToPrefixedString()))));
            if (@event.Template != null)
                elem.Add(new XAttribute("template", @event.Template.Id.Value));
            if (@event.NotLogged != null)
                elem.Add(new XAttribute("notLogged", @event.NotLogged.GetValueOrDefault() ? "true" : "false"));
            AddOptionalMessage(elem, @event.Message);
            return elem;
        }

        private XElement ToXml(Channel channel)
        {
            var elem = new XElement(
                ns + "channel",
                new XAttribute("name", channel.Name),
                new XAttribute("type", ToXml(channel.Type)));
            if (channel.Value != null)
                elem.Add(new XAttribute("value", channel.Value));
            if (channel.Symbol != null)
                elem.Add(new XAttribute("symbol", channel.Symbol));
            AddOptionalMessage(elem, channel.Message);
            return elem;
        }

        private XElement ToXml(Level level)
        {
            var elem = new XElement(
                ns + "level",
                new XAttribute("name", level.Name.Value.ToPrefixedString()),
                new XAttribute("value", level.Value));
            if (level.Symbol != null)
                elem.Add(new XAttribute("symbol", level.Symbol));
            AddOptionalMessage(elem, level.Message);
            return elem;
        }

        private XElement ToXml(Task task)
        {
            var elem = new XElement(
                ns + "task",
                new XAttribute("name", task.Name.Value.ToPrefixedString()),
                new XAttribute("value", task.Value));
            if (task.Guid != null)
                elem.Add(new XAttribute("eventGUID", task.Guid.Value.ToString("B")));
            if (task.Symbol != null)
                elem.Add(new XAttribute("symbol", task.Symbol));
            AddOptionalMessage(elem, task.Message);
            return elem;
        }

        private XElement ToXml(Opcode opcode)
        {
            var elem = new XElement(
                ns + "opcode",
                new XAttribute("name", opcode.Name.Value.ToPrefixedString()),
                new XAttribute("value", opcode.Value));
            if (opcode.Symbol != null)
                elem.Add(new XAttribute("symbol", opcode.Symbol));
            AddOptionalMessage(elem, opcode.Message);
            return elem;
        }

        private XElement ToXml(Keyword keyword)
        {
            var elem = new XElement(
                ns + "keyword",
                new XAttribute("name", keyword.Name.Value.ToPrefixedString()),
                new XAttribute("mask", string.Format(CultureInfo.InvariantCulture, "0x{0:X16}", keyword.Mask.Value)));
            if (keyword.Symbol != null)
                elem.Add(new XAttribute("symbol", keyword.Symbol));
            AddOptionalMessage(elem, keyword.Message);
            return elem;
        }

        private XElement ToXml(IMap map)
        {
            if (map.Kind == MapKind.BitMap)
                return ToXml((BitMap)map);
            if (map.Kind == MapKind.ValueMap)
                return ToXml((ValueMap)map);
            throw new ArgumentException("map");
        }

        private XElement ToXml(BitMap map)
        {
            var elem = new XElement(
                ns + "bitMap",
                new XAttribute("name", map.Name));
            if (map.Symbol != null)
                elem.Add(new XAttribute("symbol", map.Symbol));
            elem.Add(map.Items.Select(i => ToXml((BitMapItem)i)));
            return elem;
        }

        private XElement ToXml(ValueMap map)
        {
            var elem = new XElement(
                ns + "valueMap",
                new XAttribute("name", map.Name));
            if (map.Symbol != null)
                elem.Add(new XAttribute("symbol", map.Symbol));
            elem.Add(map.Items.Select(i => ToXml((ValueMapItem)i)));
            return elem;
        }

        private XElement ToXml(BitMapItem item)
        {
            var elem = new XElement(
                ns + "map",
                new XAttribute("value", string.Format(CultureInfo.InvariantCulture, "{0:X}", item.Value)));
            if (item.Symbol != null)
                elem.Add(new XAttribute("symbol", item.Symbol));
            AddOptionalMessage(elem, item.Message);
            return elem;
        }

        private XElement ToXml(ValueMapItem item)
        {
            var elem = new XElement(
                ns + "map",
                new XAttribute("value", item.Value));
            if (item.Symbol != null)
                elem.Add(new XAttribute("symbol", item.Symbol));
            AddOptionalMessage(elem, item.Message);
            return elem;
        }

        private XElement ToXml(Template template)
        {
            var elem = new XElement(
                ns + "template",
                new XAttribute("tid", template.Id));
            if (template.Name != null)
                elem.Add(new XAttribute("name", template.Name));
            elem.Add(template.Properties.Select(ToXml));
            return elem;
        }

        private XElement ToXml(Property property)
        {
            if (property.Kind == PropertyKind.Struct)
                return ToXml((StructProperty)property);
            return ToXml((DataProperty)property);
        }

        private XElement ToXml(StructProperty property)
        {
            var elem = new XElement(
                ns + "struct",
                new XAttribute("name", property.Name));
            elem.Add(property.Properties.Select(ToXml));
            return elem;
        }

        private XElement ToXml(DataProperty property)
        {
            var elem = new XElement(
                ns + "data",
                new XAttribute("name", property.Name),
                new XAttribute("inType", property.InType.Name.ToPrefixedString()));
            if (property.OutType != null)
                elem.Add(new XAttribute("outType", property.OutType.Name.ToPrefixedString()));
            if (property.Length.IsSpecified)
                elem.Add(new XAttribute("length", property.Length));
            if (property.Count.IsSpecified)
                elem.Add(new XAttribute("count", property.Count));
            return elem;
        }

        private XElement ToXml(Filter filter)
        {
            var elem = new XElement(
                ns + "filter",
                new XAttribute("name", filter.Name.Value.ToPrefixedString()),
                new XAttribute("value", filter.Value),
                new XAttribute("version", filter.Version));
            if (filter.Symbol != null)
                elem.Add(new XAttribute("symbol", filter.Symbol));
            AddOptionalMessage(elem, filter.Message);
            return elem;
        }

        private string ToXml(ChannelType type)
        {
            switch (type) {
                case ChannelType.Admin: return "Admin";
                case ChannelType.Operational: return "Operational";
                case ChannelType.Analytic: return "Analytic";
                case ChannelType.Debug: return "Debug";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static void AddOptionalMessage(XElement elem, LocalizedString message)
        {
            if (message != null)
                elem.Add(new XAttribute("message", "$(string." + message.Name + ")"));
        }
    }
}
