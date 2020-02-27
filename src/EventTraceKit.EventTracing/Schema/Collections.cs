namespace EventTraceKit.EventTracing.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EventTraceKit.EventTracing.Collections;
    using EventTraceKit.EventTracing.Internal;
    using EventTraceKit.EventTracing.Internal.Extensions;
    using EventTraceKit.EventTracing.Schema.Base;

    public sealed class ProviderCollection : UniqueCollection<Provider>
    {
        private readonly EventManifest manifest;

        public ProviderCollection(EventManifest manifest)
        {
            this.manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));

            this.UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate provider name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Id)
                .WithMessage("Duplicate provider id: '{0:B}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Symbol)
                .WithMessage("Duplicate provider symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        protected override void InsertItem(int index, Provider item)
        {
            base.InsertItem(index, item);
            item.Manifest = manifest;
            item.Index = index;
        }

        protected override void SetItem(int index, Provider newItem)
        {
            Provider oldItem = this[index];
            base.SetItem(index, newItem);
            oldItem.Manifest = null;
            oldItem.Index = -1;
            newItem.Manifest = manifest;
            newItem.Index = index;
        }

        protected override void RemoveItem(int index)
        {
            Provider item = this[index];
            base.RemoveItem(index);
            item.Manifest = null;
            item.Index = -1;
        }
    }

    public abstract class ProviderItemCollection<T> : UniqueCollection<T>
        where T : ProviderItem
    {
        private readonly Provider provider;

        private protected ProviderItemCollection(Provider provider)
        {
            this.provider = provider;
        }

        protected override void InsertItem(int index, T item)
        {
            if (item.Provider != null)
                throw new InvalidOperationException("Provider already set");
            base.InsertItem(index, item);
            item.Provider = provider;
        }

        protected override void SetItem(int index, T newItem)
        {
            if (newItem.Provider != null)
                throw new InvalidOperationException("Provider already set");
            T oldItem = this[index];
            base.SetItem(index, newItem);
            oldItem.Provider = null;
            newItem.Provider = provider;
        }

        protected override void RemoveItem(int index)
        {
            T item = this[index];
            base.RemoveItem(index);
            item.Provider = null;
        }

        protected override void ClearItems()
        {
            foreach (var item in Items)
                item.Provider = null;

            base.ClearItems();
        }
    }

    public sealed class EventCollection : ProviderItemCollection<Event>
    {
        public EventCollection(Provider provider)
            : base(provider)
        {
            this.UniqueConstraintFor(e => Tuple.Create(e.Value, e.Version))
                .WithMessage("Duplicate event with value {0} (0x{0:X}) and version {1} (0x{1:X})",
                             e => e.Value,
                             e => e.Version)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate event symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class ChannelCollection : ProviderItemCollection<Channel>
    {
        public ChannelCollection(Provider provider)
            : base(provider)
        {
            this.UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate channel name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Value)
                .WithMessage("Duplicate channel value: {0} (0x{0:X})", e => e.Value)
                .IfNotNull()
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate channel symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Id)
                .IfNotNull()
                .WithMessage("Duplicate channel id: '{0}'", e => e.Id)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class LevelCollection : ProviderItemCollection<Level>
    {
        public LevelCollection(Provider provider)
            : base(provider)
        {
            this.UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate level name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Value)
                .WithMessage("Duplicate level value: {0} (0x{0:X})", e => e.Value)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate level symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class OpcodeCollection : ProviderItemCollection<Opcode>
    {
        public OpcodeCollection(Provider provider)
            : base(provider)
        {
            this.UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate opcode name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Value)
                .WithMessage("Duplicate opcode value: {0} (0x{0:X})", e => e.Value)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate opcode symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class TaskOpcodeCollection : UniqueCollection<Opcode>
    {
        public TaskOpcodeCollection()
        {
            this.UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate opcode name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Value)
                .WithMessage("Duplicate opcode value: {0} (0x{0:X})", e => e.Value)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate opcode symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class TaskCollection : ProviderItemCollection<Task>
    {
        public TaskCollection(Provider provider)
            : base(provider)
        {
            this.UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate task name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Value)
                .WithMessage("Duplicate task value: {0} (0x{0:X})", e => e.Value)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate task symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Guid)
                .IfNotNull()
                .WithMessage("Duplicate task event guid: '{0:B}'", e => e.Guid)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class KeywordCollection : ProviderItemCollection<Keyword>
    {
        public KeywordCollection(Provider provider)
            : base(provider)
        {
            this.UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate keyword name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Mask)
                .WithMessage("Duplicate keyword mask: 0x{0:X}", e => e.Mask)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate task symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class MapCollection : ProviderItemCollection<Map>
    {
        public MapCollection(Provider provider)
            : base(provider)
        {
            this.UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate map name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate map symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        public Map GetByName(string name)
        {
            return this.FirstOrDefault(e => e.Name == name);
        }
    }

    public sealed class MapItemCollection<T> : UniqueCollection<T>
        where T : MapItem
    {
        public MapItemCollection()
        {
            this.UniqueConstraintFor(e => e.Value)
                .WithMessage("Map '{0}' has item with duplicate value: {1} (0x{1:X})",
                             e => e.Map.Name,
                             e => e.Value)
                .DiagnoseUsing(DiagUtils.ReportError);

            this.UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Map '{0}' has item with duplicate symbol: '{1}'",
                             e => e.Map.Name,
                             e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class PatternMapCollection : ProviderItemCollection<PatternMap>
    {
        public PatternMapCollection(Provider provider)
            : base(provider)
        {
            this.UniqueConstraintFor(e => e.Name).WithMessage("Duplicate pattern map name: '{0}'", e => e.Name);
            this.UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate pattern map symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class PatternMapItemCollection : UniqueCollection<PatternMapItem>
    {
        public PatternMapItemCollection()
        {
            this.UniqueConstraintFor(e => e.Value)
                .WithMessage("Pattern map '{0}' has item with duplicate value: '{1}'",
                             e => e.Map.Name,
                             e => e.Value)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class TemplateCollection : ProviderItemCollection<Template>
    {
        public TemplateCollection(Provider provider)
            : base(provider)
        {
            this.UniqueConstraintFor(e => e.Id)
                .WithMessage("Duplicate template id: '{0}'", e => e.Id)
                .DiagnoseUsing(DiagUtils.ReportError);

            // Name doesn't seem to be used.
            this.UniqueConstraintFor(e => e.Name)
                .IfNotNull()
                .WithMessage("Duplicate template name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class PropertyCollection : UniqueCollection<Property>
    {
        public PropertyCollection()
        {
            this.UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate property name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class FilterCollection : ProviderItemCollection<Filter>
    {
        public FilterCollection(Provider provider)
            : base(provider)
        {
            this.UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate filter name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => Tuple.Create(e.Value, e.Version))
                .WithMessage("Duplicate filter with value {0} (0x{0:X}) and version {1} (0x{1:X})",
                             e => e.Value,
                             e => e.Version)
                .DiagnoseUsing(DiagUtils.ReportError);
            this.UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate filter symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class LocalizedStringCollection : UniqueCollection<LocalizedString>
    {
        public LocalizedStringCollection()
        {
            this.UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate string id: '{0}'", e => e.Id)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        public IEnumerable<LocalizedString> Used()
        {
            return this.Where(s => s.Id != uint.MaxValue);
        }

        public LocalizedString Import(LocalizedString str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            var @string = this.GetByName(str.Name);
            if (@string == null) {
                @string = new LocalizedString(str.Name, str.Value);
                @string.Imported = true;
                Add(@string);
            }

            return @string;
        }
    }

    internal static class CollectionExtensions
    {
        public static Channel GetByIdOrName(this IEnumerable<Channel> collection, string str)
        {
            return collection.FirstOrDefault(e => e.Id == str || e.Name == str);
        }

        public static Channel GetById(this IEnumerable<Channel> collection, string id)
        {
            return collection.FirstOrDefault(e => e.Id == id);
        }

        public static Channel GetByName(this IEnumerable<Channel> collection, string name)
        {
            return collection.FirstOrDefault(e => e.Name == name);
        }

        public static Level GetByName(this IEnumerable<Level> collection, QName name)
        {
            return collection.FirstOrDefault(e => e.Name == name);
        }

        public static Opcode GetByName(this IEnumerable<Opcode> collection, QName name)
        {
            return collection.FirstOrDefault(e => e.Name == name);
        }

        public static Task GetByName(this IEnumerable<Task> collection, QName name)
        {
            return collection.FirstOrDefault(e => e.Name == name);
        }

        public static Keyword GetByName(this IEnumerable<Keyword> collection, QName name)
        {
            return collection.FirstOrDefault(e => e.Name == name);
        }

        public static PatternMap GetByName(this IEnumerable<PatternMap> collection, string name)
        {
            return collection.FirstOrDefault(e => e.Name == name);
        }

        public static Template GetById(this IEnumerable<Template> collection, string id)
        {
            return collection.FirstOrDefault(e => e.Id == id);
        }

        public static int GetIndexByName(this IReadOnlyList<Property> collection, string name)
        {
            return collection.FindIndex(e => e.Name == name);
        }

        public static Filter GetByName(this IEnumerable<Filter> collection, QName name)
        {
            return collection.FirstOrDefault(e => e.Name == name);
        }

        public static LocalizedString GetByName(this IEnumerable<LocalizedString> collection, string name)
        {
            return collection.FirstOrDefault(e => e.Name == name);
        }
    }
}
