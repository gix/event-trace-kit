namespace EventTraceKit.EventTracing
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;
    using EventTraceKit.EventTracing.Schema;

    public static class EventManifestXmlExtensions
    {
        public static XDocument ToXml(this EventManifest manifest)
        {
            var providersElem = new XElement(EventManifestSchema.Namespace + "events");
            foreach (var provider in manifest.Providers)
                providersElem.Add(provider.ToXml());

            //providersElem.Add(ToMessageTableXml(manifest.Resources));

            XElement localizationElem = null;
            if (manifest.Resources.Count > 0) {
                localizationElem = new XElement(
                    EventManifestSchema.Namespace + "localization",
                    manifest.Resources.Select(ToXml));
            }

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement(
                    EventManifestSchema.Namespace + "instrumentationManifest",
                    new XAttribute(XNamespace.Xmlns + "win", WinEventSchema.Namespace),
                    new XAttribute(XNamespace.Xmlns + "xs", "http://www.w3.org/2001/XMLSchema"),
                    new XElement(EventManifestSchema.Namespace + "instrumentation", providersElem),
                    localizationElem));

            return doc;
        }

        public static XElement ToXml(this LocalizedResourceSet resourceSet)
        {
            if (resourceSet.Strings.Count == 0)
                return null;

            return new XElement(
                EventManifestSchema.Namespace + "resources",
                new XAttribute("culture", resourceSet.Culture.Name),
                new XElement(EventManifestSchema.Namespace + "stringTable", resourceSet.Strings.Select(ToXml)));
        }

        public static XElement ToXml(this LocalizedString ls)
        {
            return new XElement(
                EventManifestSchema.Namespace + "string",
                new XAttribute("id", ls.Name.Value),
                new XAttribute("value", ls.Value.Value));
        }

        public static XElement ToXml(this Provider provider)
        {
            var elem = new XElement(
                EventManifestSchema.Namespace + "provider",
                new XAttribute("name", provider.Name.Value),
                new XAttribute("guid", provider.Id.Value.ToString("B")),
                new XAttribute("symbol", provider.Symbol.Value));
            if (provider.Namespace != null)
                elem.Add(new XAttribute("namespace", provider.Namespace.Value));
            if (provider.ResourceFileName != null)
                elem.Add(new XAttribute("resourceFileName", provider.ResourceFileName.Value));
            if (provider.MessageFileName != null)
                elem.Add(new XAttribute("messageFileName", provider.MessageFileName.Value));
            if (provider.ParameterFileName != null)
                elem.Add(new XAttribute("parameterFileName", provider.ParameterFileName.Value));
            AddOptionalMessage(elem, provider.Message);

            if (provider.Events.Count > 0)
                elem.Add(new XElement(EventManifestSchema.Namespace + "events", provider.Events.Select(ToXml)));
            if (provider.Channels.Count > 0)
                elem.Add(new XElement(EventManifestSchema.Namespace + "channels", provider.Channels.Select(ToXml)));
            if (provider.Levels.Count > 0)
                elem.Add(new XElement(EventManifestSchema.Namespace + "levels", provider.Levels.Select(ToXml)));
            if (provider.Tasks.Count > 0)
                elem.Add(new XElement(EventManifestSchema.Namespace + "tasks", provider.Tasks.Select(ToXml)));
            if (provider.Opcodes.Count > 0)
                elem.Add(new XElement(EventManifestSchema.Namespace + "opcodes", provider.Opcodes.Select(ToXml)));
            if (provider.Keywords.Count > 0)
                elem.Add(new XElement(EventManifestSchema.Namespace + "keywords", provider.Keywords.Select(ToXml)));
            if (provider.Maps.Count > 0)
                elem.Add(new XElement(EventManifestSchema.Namespace + "maps", provider.Maps.Select(ToXml)));
            if (provider.Templates.Count > 0)
                elem.Add(new XElement(EventManifestSchema.Namespace + "templates", provider.Templates.Select(ToXml)));
            if (provider.Filters.Count > 0)
                elem.Add(new XElement(EventManifestSchema.Namespace + "filters", provider.Filters.Select(ToXml)));

            return elem;
        }

        public static XElement ToXml(this Event @event)
        {
            var elem = new XElement(
                EventManifestSchema.Namespace + "event",
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

        public static XElement ToXml(this Channel channel)
        {
            if (channel.Imported) {
                var importedElem = new XElement(
                    EventManifestSchema.Namespace + "importChannel",
                    new XAttribute("name", channel.Name));
                if (channel.Id != null)
                    importedElem.Add(new XAttribute("chid", channel.Id));
                if (channel.Symbol != null)
                    importedElem.Add(new XAttribute("symbol", channel.Symbol));
                return importedElem;
            }

            var elem = new XElement(
                EventManifestSchema.Namespace + "channel",
                new XAttribute("name", channel.Name),
                new XAttribute("type", ToXml(channel.Type)));
            if (channel.Value != null)
                elem.Add(new XAttribute("value", channel.Value));
            if (channel.Id != null)
                elem.Add(new XAttribute("chid", channel.Id));
            if (channel.Symbol != null)
                elem.Add(new XAttribute("symbol", channel.Symbol));
            AddOptionalMessage(elem, channel.Message);
            return elem;
        }

        public static XElement ToXml(this Level level)
        {
            var elem = new XElement(
                EventManifestSchema.Namespace + "level",
                new XAttribute("name", level.Name.Value.ToPrefixedString()),
                new XAttribute("value", level.Value));
            if (level.Symbol != null)
                elem.Add(new XAttribute("symbol", level.Symbol));
            AddOptionalMessage(elem, level.Message);
            return elem;
        }

        public static XElement ToXml(this Task task)
        {
            var elem = new XElement(
                EventManifestSchema.Namespace + "task",
                new XAttribute("name", task.Name.Value.ToPrefixedString()),
                new XAttribute("value", task.Value));
            if (task.Guid != null)
                elem.Add(new XAttribute("eventGUID", task.Guid.Value.ToString("B")));
            if (task.Symbol != null)
                elem.Add(new XAttribute("symbol", task.Symbol));
            AddOptionalMessage(elem, task.Message);
            elem.Add(task.Opcodes.Select(ToXml));
            return elem;
        }

        public static XElement ToXml(this Opcode opcode)
        {
            var elem = new XElement(
                EventManifestSchema.Namespace + "opcode",
                new XAttribute("name", opcode.Name.Value.ToPrefixedString()),
                new XAttribute("value", opcode.Value));
            if (opcode.Symbol != null)
                elem.Add(new XAttribute("symbol", opcode.Symbol));
            AddOptionalMessage(elem, opcode.Message);
            return elem;
        }

        public static XElement ToXml(this Keyword keyword)
        {
            var elem = new XElement(
                EventManifestSchema.Namespace + "keyword",
                new XAttribute("name", keyword.Name.Value.ToPrefixedString()),
                new XAttribute("mask", string.Format(CultureInfo.InvariantCulture, "0x{0:X16}", keyword.Mask.Value)));
            if (keyword.Symbol != null)
                elem.Add(new XAttribute("symbol", keyword.Symbol));
            AddOptionalMessage(elem, keyword.Message);
            return elem;
        }

        public static XElement ToXml(this Map map)
        {
            if (map.Kind == MapKind.BitMap)
                return ((BitMap)map).ToXml();
            if (map.Kind == MapKind.ValueMap)
                return ((ValueMap)map).ToXml();
            throw new ArgumentException("map");
        }

        public static XElement ToXml(this BitMap map)
        {
            var elem = new XElement(
                EventManifestSchema.Namespace + "bitMap",
                new XAttribute("name", map.Name));
            if (map.Symbol != null)
                elem.Add(new XAttribute("symbol", map.Symbol));
            elem.Add(map.Items.Select(i => ((BitMapItem)i).ToXml()));
            return elem;
        }

        public static XElement ToXml(this ValueMap map)
        {
            var elem = new XElement(
                EventManifestSchema.Namespace + "valueMap",
                new XAttribute("name", map.Name));
            if (map.Symbol != null)
                elem.Add(new XAttribute("symbol", map.Symbol));
            elem.Add(map.Items.Select(i => ((ValueMapItem)i).ToXml()));
            return elem;
        }

        public static XElement ToXml(this BitMapItem item)
        {
            var elem = new XElement(
                EventManifestSchema.Namespace + "map",
                new XAttribute("value", string.Format(CultureInfo.InvariantCulture, "{0:X}", item.Value)));
            if (item.Symbol != null)
                elem.Add(new XAttribute("symbol", item.Symbol));
            AddOptionalMessage(elem, item.Message);
            return elem;
        }

        public static XElement ToXml(this ValueMapItem item)
        {
            var elem = new XElement(
                EventManifestSchema.Namespace + "map",
                new XAttribute("value", item.Value));
            if (item.Symbol != null)
                elem.Add(new XAttribute("symbol", item.Symbol));
            AddOptionalMessage(elem, item.Message);
            return elem;
        }

        public static XElement ToXml(this Template template)
        {
            var elem = new XElement(
                EventManifestSchema.Namespace + "template",
                new XAttribute("tid", template.Id));
            if (template.Name != null)
                elem.Add(new XAttribute("name", template.Name));
            elem.Add(template.Properties.Select(ToXml));
            return elem;
        }

        public static XElement ToXml(this Property property)
        {
            if (property.Kind == PropertyKind.Struct)
                return ((StructProperty)property).ToXml();
            return ((DataProperty)property).ToXml();
        }

        public static XElement ToXml(this StructProperty property)
        {
            var elem = new XElement(
                EventManifestSchema.Namespace + "struct",
                new XAttribute("name", property.Name));
            elem.Add(property.Properties.Select(ToXml));
            return elem;
        }

        public static XElement ToXml(this DataProperty property)
        {
            var elem = new XElement(
                EventManifestSchema.Namespace + "data",
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

        public static XElement ToXml(this Filter filter)
        {
            var elem = new XElement(
                EventManifestSchema.Namespace + "filter",
                new XAttribute("name", filter.Name.Value.ToPrefixedString()),
                new XAttribute("value", filter.Value),
                new XAttribute("version", filter.Version));
            if (filter.Symbol != null)
                elem.Add(new XAttribute("symbol", filter.Symbol));
            AddOptionalMessage(elem, filter.Message);
            return elem;
        }

        private static string ToXml(this ChannelType type)
        {
            return type switch
            {
                ChannelType.Admin => "Admin",
                ChannelType.Operational => "Operational",
                ChannelType.Analytic => "Analytic",
                ChannelType.Debug => "Debug",
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };
        }

        private static void AddOptionalMessage(XElement elem, LocalizedString message)
        {
            if (message != null)
                elem.Add(new XAttribute("message", "$(string." + message.Name + ")"));
        }
    }
}
