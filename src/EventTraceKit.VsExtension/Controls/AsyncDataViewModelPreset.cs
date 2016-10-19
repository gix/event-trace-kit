namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using Collections;
    using Windows;
    using Serialization;

    [SerializedShape(typeof(Settings.ViewPreset))]
    public sealed class AsyncDataViewModelPreset
        : FreezableCustomSerializerAccessBase
        , IComparable<AsyncDataViewModelPreset>
        , IEquatable<AsyncDataViewModelPreset>
        , ICloneable
        , ISupportInitialize
    {
        public AsyncDataViewModelPreset()
        {
            ConfigurableColumns = new FreezableCollection<ColumnViewModelPreset>();
        }

        #region public string Name

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register(
                nameof(Name),
                typeof(string),
                typeof(AsyncDataViewModelPreset),
                PropertyMetadataUtils.DefaultNull);

        [Serialize]
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
                typeof(AsyncDataViewModelPreset),
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
                typeof(AsyncDataViewModelPreset),
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
                typeof(AsyncDataViewModelPreset),
                new PropertyMetadata(Boxed.Int32(0)));

        [Serialize]
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
                typeof(AsyncDataViewModelPreset),
                new PropertyMetadata(Boxed.Int32(0)));

        [Serialize]
        public int RightFrozenColumnCount
        {
            get { return (int)GetValue(RightFrozenColumnCountProperty); }
            set { SetValue(RightFrozenColumnCountProperty, Boxed.Int32(value)); }
        }

        #endregion

        #region public FreezableCollection<ColumnViewModelPreset> ConfigurableColumns

        public static readonly DependencyProperty ConfigurableColumnsProperty =
            DependencyProperty.Register(
                nameof(ConfigurableColumns),
                typeof(FreezableCollection<ColumnViewModelPreset>),
                typeof(AsyncDataViewModelPreset),
                PropertyMetadataUtils.DefaultNull);

        [Serialize(serializedName: nameof(Settings.ViewPreset.Columns))]
        public FreezableCollection<ColumnViewModelPreset> ConfigurableColumns
        {
            get
            {
                return
                    (FreezableCollection<ColumnViewModelPreset>)GetValue(ConfigurableColumnsProperty);
            }
            private set { SetValue(ConfigurableColumnsProperty, value); }
        }

        #endregion

        public new AsyncDataViewModelPreset Clone()
        {
            return (AsyncDataViewModelPreset)base.Clone();
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

        public bool Equals(AsyncDataViewModelPreset other)
        {
            return CompareTo(other) == 0;
        }

        public int CompareTo(AsyncDataViewModelPreset other)
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
            return new AsyncDataViewModelPreset();
        }

        public string GetDisplayName()
        {
            string displayName = Name;
            if (IsModified)
                displayName += "*";
            return displayName;
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

        public AsyncDataViewModelPreset SetColumnVisibility(
            int configurableColumnIndex, bool visibility)
        {
            if (configurableColumnIndex >= ConfigurableColumns.Count ||
                configurableColumnIndex < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(configurableColumnIndex), configurableColumnIndex,
                    "Value should be between 0 and " + ConfigurableColumns.Count);

            if (ConfigurableColumns[configurableColumnIndex].IsVisible == visibility)
                return this;

            AsyncDataViewModelPreset preset = CreateModifiedPreset();
            preset.ConfigurableColumns[configurableColumnIndex].IsVisible = visibility;
            return preset;
        }

        public AsyncDataViewModelPreset CreateModifiedPreset()
        {
            AsyncDataViewModelPreset preset = Clone();
            preset.IsModified = true;
            return preset;
        }

        public AsyncDataViewModelPreset CreateCompatiblePreset(AsyncDataViewModelPreset template)
        {
            if (template == null)
                return this;

            var compatiblePreset = Clone();
            var templateColumnsMap = template.GetConfigurableColumnsMap();
            var canonicalList = compatiblePreset.GetCanonicalList();
            canonicalList = PruneList(canonicalList, templateColumnsMap);
            compatiblePreset.ApplyCanonicalList(canonicalList);
            return compatiblePreset;
        }

        private static readonly Guid SentinelId = new Guid("D6986295-540C-4A2D-B0B8-CFEBF58323C3");
        private static readonly ColumnViewModelPreset SentinelLeftFreezePreset =
            new ColumnViewModelPreset { Id = SentinelId }.EnsureFrozen();
        private static readonly ColumnViewModelPreset SentinelRightFreezePreset =
            new ColumnViewModelPreset { Id = SentinelId }.EnsureFrozen();

        private IList<ColumnViewModelPreset> GetCanonicalList()
        {
            var canonicalList = CopyCollection(ConfigurableColumns);
            PlaceSeparatorsInList(canonicalList, SentinelLeftFreezePreset, SentinelRightFreezePreset);
            return canonicalList;
        }

        private static FreezableCollection<T> CopyCollection<T>(
            FreezableCollection<T> collection) where T : DependencyObject
        {
            return new FreezableCollection<T>(collection);
        }

        public void PlaceSeparatorsInList<T>(
            IList<T> columns,
            T leftFreezableAreaSeparatorColumn,
            T rightFreezableAreaSeparatorColumn)
        {
            int leftIndex = LeftFrozenColumnCount.Clamp(0, columns.Count);
            int rightIndex = columns.Count - RightFrozenColumnCount.Clamp(0, columns.Count - leftIndex);
            columns.Insert(rightIndex, rightFreezableAreaSeparatorColumn);
            columns.Insert(leftIndex, leftFreezableAreaSeparatorColumn);
        }

        private void ApplyCanonicalList(IList<ColumnViewModelPreset> canonicalList)
        {
            ConfigurableColumns = new FreezableCollection<ColumnViewModelPreset>(
                from column in canonicalList
                where !IsSentinelColumn(column)
                select column);

            int leftIndex = GetSentinelColumnIndex(canonicalList, SentinelLeftFreezePreset);
            int rightIndex = GetSentinelColumnIndex(canonicalList, SentinelRightFreezePreset);
            LeftFrozenColumnCount = leftIndex != -1 ? leftIndex : 0;
            RightFrozenColumnCount = rightIndex != -1 ? canonicalList.Count - rightIndex - 1 : 0;
        }

        private static int GetSentinelColumnIndex(
            IList<ColumnViewModelPreset> canonicalList, ColumnViewModelPreset sentinel)
        {
            return canonicalList.IndexOf(sentinel);
        }

        private static bool IsSentinelColumn(ColumnViewModelPreset column)
        {
            return column.Id == SentinelId;
        }

        private static List<ColumnViewModelPreset> PruneList(
            IList<ColumnViewModelPreset> canonicalColumns,
            Dictionary<Guid, ColumnViewModelPreset> templateColumnsMap)
        {
            return canonicalColumns.Where(
                x => templateColumnsMap.ContainsKey(x.Id) || IsSentinelColumn(x)).ToList();
        }

        private Dictionary<Guid, ColumnViewModelPreset> GetConfigurableColumnsMap()
        {
            return ConfigurableColumns.ToDictionary(x => x.Id);
        }
    }
}
