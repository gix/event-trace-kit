namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using EventTraceKit.VsExtension.Collections;
    using EventTraceKit.VsExtension.Windows;

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
}
