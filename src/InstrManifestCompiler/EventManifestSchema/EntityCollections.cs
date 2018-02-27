namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using InstrManifestCompiler.Collections;
    using InstrManifestCompiler.EventManifestSchema.Base;
    using InstrManifestCompiler.Extensions;
    using InstrManifestCompiler.Support;

    internal static class DiagUtils
    {
        public static void ReportError<T, TProperty>(
            T entity,
            RefValue<TProperty> value,
            IDiagnostics diags,
            IUniqueConstraint<T> constraint) where TProperty : class
        {
            diags.ReportError(value.Location, constraint.FormatMessage(entity));
        }

        public static void ReportError<T, TProperty>(
            T entity,
            StructValue<TProperty> value,
            IDiagnostics diags,
            IUniqueConstraint<T> constraint)
            where TProperty : struct
        {
            diags.ReportError(value.Location, constraint.FormatMessage(entity));
        }

        public static void ReportError<T, TProperty>(
            T entity,
            NullableValue<TProperty> value,
            IDiagnostics diags,
            IUniqueConstraint<T> constraint)
            where TProperty : struct
        {
            diags.ReportError(value.Location, constraint.FormatMessage(entity));
        }

        public static void ReportError<T, TProperty>(
            T entity,
            TProperty value,
            IDiagnostics diags,
            IUniqueConstraint<T> constraint)
        {
            diags.ReportError(
                entity is ISourceItem sourceItem ? sourceItem.Location : new SourceLocation(),
                constraint.FormatMessage(entity));
        }
    }

    internal sealed class ProviderCollection : ConstrainedEntityCollection<Provider>
    {
        private readonly EventManifest manifest;

        public ProviderCollection(EventManifest manifest)
        {
            Contract.Requires<ArgumentNullException>(manifest != null);
            this.manifest = manifest;
            UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate provider name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Id)
                .WithMessage("Duplicate provider id: '{0:B}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Symbol)
                .WithMessage("Duplicate provider symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        protected override void InsertItem(int index, Provider item)
        {
            //Contract.Requires<InvalidOperationException>(item.Manifest == null);
            base.InsertItem(index, item);
            item.Manifest = manifest;
            item.Index = index;
        }

        protected override void SetItem(int index, Provider newItem)
        {
            //Contract.Requires<InvalidOperationException>(newItem.Manifest == null);
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

    internal abstract class ProviderItemCollection<T> : ConstrainedEntityCollection<T>
        where T : ProviderItem
    {
        private readonly Provider provider;

        protected ProviderItemCollection(Provider provider)
        {
            Contract.Requires<ArgumentNullException>(provider != null);
            this.provider = provider;
        }

        protected override void InsertItem(int index, T item)
        {
            //Contract.Requires<InvalidOperationException>(item.Provider == null);
            base.InsertItem(index, item);
            item.Provider = provider;
        }

        protected override void SetItem(int index, T newItem)
        {
            //Contract.Requires<InvalidOperationException>(newItem.Provider == null);
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
    }

    public sealed class EventCollection : ConstrainedEntityCollection<Event>
    {
        public EventCollection()
        {
            UniqueConstraintFor(e => Tuple.Create(e.Value, e.Version))
                .WithMessage("Duplicate event with value {0} (0x{0:X}) and version {1} (0x{1:X})",
                             e => e.Value,
                             e => e.Version)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate event symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class ChannelCollection : ConstrainedEntityCollection<Channel>
    {
        public ChannelCollection()
        {
            UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate channel name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Value)
                .WithMessage("Duplicate channel value: {0} (0x{0:X})", e => e.Value)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate channel symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Id)
                .IfNotNull()
                .WithMessage("Duplicate channel id: '{0}'", e => e.Id)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        public Channel GetById(string id)
        {
            return this.FirstOrDefault(e => e.Id == id);
        }

        public Channel GetByName(string name)
        {
            return this.FirstOrDefault(e => e.Name == name);
        }
    }

    public sealed class LevelCollection : ConstrainedEntityCollection<Level>
    {
        public LevelCollection()
        {
            UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate level name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Value)
                .WithMessage("Duplicate level value: {0} (0x{0:X})", e => e.Value)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate level symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        public Level GetByName(QName name)
        {
            return this.FirstOrDefault(e => e.Name == name);
        }
    }

    public sealed class OpcodeCollection : ConstrainedEntityCollection<Opcode>
    {
        public OpcodeCollection()
        {
            UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate opcode name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Value)
                .WithMessage("Duplicate opcode value: {0} (0x{0:X})", e => e.Value)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate opcode symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        public Opcode GetByName(QName name)
        {
            return this.FirstOrDefault(e => e.Name == name);
        }
    }

    public sealed class TaskCollection : ConstrainedEntityCollection<Task>
    {
        public TaskCollection()
        {
            UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate task name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Value)
                .WithMessage("Duplicate task value: {0} (0x{0:X})", e => e.Value)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate task symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Guid)
                .IfNotNull()
                .WithMessage("Duplicate task event guid: '{0:B}'", e => e.Guid)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        public Task GetByName(QName name)
        {
            return this.FirstOrDefault(e => e.Name == name);
        }
    }

    public sealed class KeywordCollection : ConstrainedEntityCollection<Keyword>
    {
        public KeywordCollection()
        {
            UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate keyword name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Mask)
                .WithMessage("Duplicate keyword mask: 0x{0:X}", e => e.Mask)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate task symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        public Keyword GetByName(QName name)
        {
            return this.FirstOrDefault(e => e.Name == name);
        }
    }

    public sealed class MapCollection : ConstrainedEntityCollection<IMap>
    {
        public MapCollection()
        {
            UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate map name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate map symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        public IMap GetByName(string name)
        {
            return this.FirstOrDefault(e => e.Name == name);
        }
    }

    public sealed class MapItemCollection<T> : ConstrainedEntityCollection<T>
        where T : IMapItem
    {
        public MapItemCollection()
        {
            UniqueConstraintFor(e => e.Value)
                .WithMessage("Map '{0}' has item with duplicate value: {1} (0x{1:X})",
                             e => e.Map.Name,
                             e => e.Value)
                .DiagnoseUsing(DiagUtils.ReportError);

            UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Map '{0}' has item with duplicate symbol: '{1}'",
                             e => e.Map.Name,
                             e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class PatternMapCollection : ConstrainedEntityCollection<PatternMap>
    {
        public PatternMapCollection()
        {
            UniqueConstraintFor(e => e.Name).WithMessage("Duplicate pattern map name: '{0}'", e => e.Name);
            UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate pattern map symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        public PatternMap GetByName(string name)
        {
            return this.FirstOrDefault(e => e.Name == name);
        }
    }

    public sealed class PatternMapItemCollection : ConstrainedEntityCollection<PatternMapItem>
    {
        public PatternMapItemCollection()
        {
            UniqueConstraintFor(e => e.Value)
                .WithMessage("Pattern map '{0}' has item with duplicate value: '{1}'",
                             e => e.Map.Name,
                             e => e.Value)
                .DiagnoseUsing(DiagUtils.ReportError);
        }
    }

    public sealed class TemplateCollection : ConstrainedEntityCollection<Template>
    {
        public TemplateCollection()
        {
            UniqueConstraintFor(e => e.Id)
                .WithMessage("Duplicate template id: '{0}'", e => e.Id)
                .DiagnoseUsing(DiagUtils.ReportError);

            // Name doesn't seem to be used.
            UniqueConstraintFor(e => e.Name)
                .IfNotNull()
                .WithMessage("Duplicate template name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        public Template GetById(string id)
        {
            return this.FirstOrDefault(e => e.Id == id);
        }
    }

    public sealed class PropertyCollection : ConstrainedEntityCollection<Property>
    {
        public PropertyCollection()
        {
            UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate property name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        public int GetIndexByName(string name)
        {
            return this.FindIndex(e => e.Name == name);
        }
    }

    public sealed class FilterCollection : ConstrainedEntityCollection<Filter>
    {
        public FilterCollection()
        {
            UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate filter name: '{0}'", e => e.Name)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => Tuple.Create(e.Value, e.Version))
                .WithMessage("Duplicate filter with value {0} (0x{0:X}) and version {1} (0x{1:X})",
                             e => e.Value,
                             e => e.Version)
                .DiagnoseUsing(DiagUtils.ReportError);
            UniqueConstraintFor(e => e.Symbol)
                .IfNotNull()
                .WithMessage("Duplicate filter symbol: '{0}'", e => e.Symbol)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        public Filter GetByName(QName name)
        {
            return this.FirstOrDefault(e => e.Name == name);
        }
    }

    public sealed class LocalizedStringCollection : ConstrainedEntityCollection<LocalizedString>
    {
        public LocalizedStringCollection()
        {
            UniqueConstraintFor(e => e.Name)
                .WithMessage("Duplicate string id: '{0}'", e => e.Id)
                .DiagnoseUsing(DiagUtils.ReportError);
        }

        public LocalizedString GetByName(string name)
        {
            return this.FirstOrDefault(e => e.Name == name);
        }

        public IEnumerable<LocalizedString> Used()
        {
            return this.Where(s => s.Id != uint.MaxValue);
        }

        public LocalizedString Import(LocalizedString str)
        {
            Contract.Requires<ArgumentNullException>(str != null);

            var @string = GetByName(str.Name);
            if (@string == null) {
                @string = new LocalizedString(str.Name, str.Value);
                Add(@string);
            }

            return @string;
        }
    }
}
