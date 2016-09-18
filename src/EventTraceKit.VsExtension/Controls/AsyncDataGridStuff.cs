namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using EventTraceKit.VsExtension.Collections;
    using EventTraceKit.VsExtension.Windows;

    public class ItemEventArgs<T> : EventArgs
    {
        public ItemEventArgs(T item)
        {
            Item = item;
        }

        public T Item { get; }
    }

    public delegate void ItemEventHandler<T>(object sender, ItemEventArgs<T> e);

    public interface IDependencyObjectCustomSerializerAccess
    {
        object GetValue(DependencyProperty dp);
        bool ShouldSerializeProperty(DependencyProperty dp);
    }

    public abstract class FreezableCustomSerializerAccessBase
        : Freezable, IDependencyObjectCustomSerializerAccess
    {
        object IDependencyObjectCustomSerializerAccess.GetValue(DependencyProperty dp)
        {
            return GetValue(dp);
        }

        bool IDependencyObjectCustomSerializerAccess.ShouldSerializeProperty(DependencyProperty dp)
        {
            return ShouldSerializeProperty(dp);
        }
    }

    public class SerializePropertyInProfileAttribute : Attribute
    {
        public SerializePropertyInProfileAttribute(string name)
        {
        }
    }

    public sealed class HdvViewModelPreset
        : FreezableCustomSerializerAccessBase
        , IComparable<HdvViewModelPreset>
        , IEquatable<HdvViewModelPreset>
        , ICloneable
        , ISupportInitialize
    {
        public HdvViewModelPreset()
        {
            ConfigurableColumns = new FreezableCollection<HdvColumnViewModelPreset>();
        }

        #region public string Name

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register(
                nameof(Name),
                typeof(string),
                typeof(HdvViewModelPreset),
                PropertyMetadataUtils.DefaultNull);

        [SerializePropertyInProfile("Name")]
        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        #endregion

        #region public bool IsModified

        public static readonly DependencyProperty IsModifiedProperty =
            DependencyProperty.Register(
                nameof(IsModified),
                typeof(bool),
                typeof(HdvViewModelPreset),
                new PropertyMetadata(Boxed.Bool(false)));

        public bool IsModified
        {
            get { return (bool)GetValue(IsModifiedProperty); }
            set { SetValue(IsModifiedProperty, Boxed.Bool(value)); }
        }

        #endregion

        #region public bool IsModifiedFromUI

        public static readonly DependencyProperty IsUIModifiedProperty =
            DependencyProperty.Register(
                nameof(IsUIModified),
                typeof(bool),
                typeof(HdvViewModelPreset),
                new PropertyMetadata(Boxed.Bool(false)));

        public bool IsUIModified
        {
            get { return (bool)GetValue(IsUIModifiedProperty); }
            set { SetValue(IsUIModifiedProperty, Boxed.Bool(value)); }
        }

        #endregion

        #region public int LeftFrozenColumnCount

        public static readonly DependencyProperty LeftFrozenColumnCountProperty =
            DependencyProperty.Register(
                nameof(LeftFrozenColumnCount),
                typeof(int),
                typeof(HdvViewModelPreset),
                new PropertyMetadata(Boxed.Int32(0)));

        [SerializePropertyInProfile("LeftFrozenColumnCount")]
        public int LeftFrozenColumnCount
        {
            get { return (int)GetValue(LeftFrozenColumnCountProperty); }
            set { SetValue(LeftFrozenColumnCountProperty, Boxed.Int32(value)); }
        }

        #endregion

        #region public int RightFrozenColumnCount

        public static readonly DependencyProperty RightFrozenColumnCountProperty =
            DependencyProperty.Register(
                nameof(RightFrozenColumnCount),
                typeof(int),
                typeof(HdvViewModelPreset),
                new PropertyMetadata(Boxed.Int32(0)));

        [SerializePropertyInProfile("RightFrozenColumnCount")]
        public int RightFrozenColumnCount
        {
            get { return (int)GetValue(RightFrozenColumnCountProperty); }
            set { SetValue(RightFrozenColumnCountProperty, Boxed.Int32(value)); }
        }

        #endregion

        #region public FreezableCollection<HdvColumnViewModelPreset> ConfigurableColumns

        public static readonly DependencyProperty ConfigurableColumnsProperty =
            DependencyProperty.Register(
                nameof(ConfigurableColumns),
                typeof(FreezableCollection<HdvColumnViewModelPreset>),
                typeof(HdvViewModelPreset),
                PropertyMetadataUtils.DefaultNull);

        [SerializePropertyInProfile("Columns")]
        public FreezableCollection<HdvColumnViewModelPreset> ConfigurableColumns
        {
            get
            {
                return
                    (FreezableCollection<HdvColumnViewModelPreset>)GetValue(ConfigurableColumnsProperty);
            }
            private set { SetValue(ConfigurableColumnsProperty, value); }
        }

        #endregion

        public new HdvViewModelPreset Clone()
        {
            return (HdvViewModelPreset)base.Clone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        void ISupportInitialize.BeginInit()
        {
        }

        void ISupportInitialize.EndInit()
        {
        }

        public bool Equals(HdvViewModelPreset other)
        {
            return CompareTo(other) == 0;
        }

        public int CompareTo(HdvViewModelPreset other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (ReferenceEquals(this, other))
                return 0;

            int cmp;
            bool dummy =
                ComparisonUtils.CompareT(out cmp, Name, other.Name) &&
                ComparisonUtils.CompareValueT(
                    out cmp, LeftFrozenColumnCount, other.LeftFrozenColumnCount) &&
                ComparisonUtils.CompareValueT(
                    out cmp, RightFrozenColumnCount, other.RightFrozenColumnCount) &&
                ComparisonUtils.CombineSequenceComparisonT(
                    out cmp, ConfigurableColumns.OrderBySelf(), other.ConfigurableColumns.OrderBySelf());
            return cmp;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new HdvViewModelPreset();
        }

        public bool GetColumnVisibility(int configurableColumnIndex)
        {
            if (configurableColumnIndex >= ConfigurableColumns.Count ||
                configurableColumnIndex < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(configurableColumnIndex), configurableColumnIndex,
                    "Value should be between 0 and " + ConfigurableColumns.Count);

            return ConfigurableColumns[configurableColumnIndex].IsVisible;
        }

        public HdvViewModelPreset SetColumnVisibility(
            int configurableColumnIndex, bool visibility)
        {
            if (configurableColumnIndex >= ConfigurableColumns.Count ||
                configurableColumnIndex < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(configurableColumnIndex), configurableColumnIndex,
                    "Value should be between 0 and " + ConfigurableColumns.Count);

            if (ConfigurableColumns[configurableColumnIndex].IsVisible == visibility)
                return this;

            HdvViewModelPreset preset = CreatePresetThatHasBeenModified();
            preset.ConfigurableColumns[configurableColumnIndex].IsVisible = visibility;
            return preset;
        }

        public HdvViewModelPreset CreatePresetThatHasBeenModified()
        {
            HdvViewModelPreset preset = Clone();
            preset.IsModified = true;
            return preset;
        }
    }

    internal class ComparisonUtils
    {
        public static bool CompareValueT<T>(
            out int cmp, T first, T second) where T : struct, IComparable<T>
        {
            cmp = first.CompareTo(second);
            return cmp == 0;
        }

        public static bool Compare<T>(out int cmp, T first, T second)
            where T : IComparable
        {
            cmp = first.CompareTo(second);
            return cmp == 0;
        }

        public static bool CompareT<T>(
            out int cmp, T x, T y) where T : class, IComparable<T>
        {
            if (x == null || y == null)
                cmp = (x == null ? 1 : 0) - (y == null ? 1 : 0);
            else
                cmp = x.CompareTo(y);

            return cmp == 0;
        }

        public static bool CombineSequenceComparisonT<T>(
            out int cmp, IEnumerable<T> first, IEnumerable<T> second)
            where T : IComparable<T>
        {
            cmp = first.SequenceCompare(second);
            return cmp == 0;
        }
    }

    public interface IDataView
    {
        DataViewColumnsCollection Columns { get; }
        DataViewColumnsCollection VisibleColumns { get; }
        int RowCount { get; }
        event EventHandler RowCountChanged;

        CellValue GetCellValue(int rowIndex, int columnIndex);
        void UpdateRowCount(int rows);

        void BeginDataUpdate();
        bool EndDataUpdate();
        void ApplyColumnView(DataColumnViewInfo[] dataColumnViewInfos);
        object DataValidityToken { get; }
        bool IsValidDataValidityToken(object dataValidityToken);
    }

    public sealed class DataColumnViewInfo
    {
        public IDataView View { get; set; }

        public Guid ColumnId { get; set; }
        public string Name { get; set; }
        public string HelpText { get; set; }
        public bool IsVisible { get; set; }
        public string Format { get; set; }
        public IFormatProvider FormatProvider { get; set; }
    }

    public abstract class DataColumnView
    {
        protected DataColumnView(DataColumn column, DataColumnViewInfo info)
        {
            Column = column;
            IsVisible = info.IsVisible;
            Format = info.Format;
            FormatProvider = info.FormatProvider;
        }

        public DataColumn Column { get; }

        public Guid ColumnId => Column.Id;
        public string Name => Column.Name;
        public string HelpText { get; set; }
        public bool IsVisible { get; set; }
        public string Format { get; set; }
        public IFormatProvider FormatProvider { get; set; }

        public CellValue GetCellValue(int index)
        {
            return GetCellValueCore(index);
        }

        protected abstract CellValue GetCellValueCore(int index);

        public object UntypedGetValue(int index)
        {
            return Column.UntypedGetValue(index);
        }
    }

    public sealed class DataColumnView<T> : DataColumnView
    {
        public DataColumnView(DataColumn column, DataColumnViewInfo info)
            : base(column, info)
        {
        }

        protected sealed override CellValue GetCellValueCore(int index)
        {
            object value = UntypedGetValue(index);
            return new CellValue(value, FormatProvider, Format);
        }
    }

    public abstract class DataColumn
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public double Width { get; set; }
        public bool IsVisible { get; set; }
        public bool IsResizable { get; set; }
        public TextAlignment TextAlignment { get; set; }

        public DataColumnView CreateView(DataColumnViewInfo info)
        {
            return CreateViewCore(info);
        }

        public object UntypedGetValue(int index)
        {
            return UntypedGetValueCore(index);
        }

        protected abstract DataColumnView CreateViewCore(DataColumnViewInfo info);
        protected abstract object UntypedGetValueCore(int index);

        public static DataColumn Create<T>()
        {
            return new DataColumn<T>();
        }

        public static DataColumn Create<T>(Func<int, T> generator)
        {
            return new DataColumn<T>(generator);
        }
    }

    public class DataColumn<T> : DataColumn
    {
        private readonly Func<int, T> generator;

        public DataColumn(Func<int, T> generator)
        {
            this.generator = generator;
        }

        public DataColumn()
        {
            generator = _ => default(T);
        }

        public new DataColumnView<T> CreateView(DataColumnViewInfo info)
        {
            return new DataColumnView<T>(this, info);
        }

        protected sealed override DataColumnView CreateViewCore(DataColumnViewInfo info)
        {
            return CreateView(info);
        }

        public T this[int index] => generator(index);

        protected sealed override object UntypedGetValueCore(int index)
        {
            return this[index];
        }
    }
}
