namespace EventTraceKit.VsExtension
{
    using System;
    using System.Windows;
    using EventTraceKit.VsExtension.Controls;
    using EventTraceKit.VsExtension.Windows;

    public class HdvColumnViewModelPreset :
        FreezableCustomSerializerAccessBase
        , IComparable<HdvColumnViewModelPreset>
        , ICloneable
    {
        #region public Guid Id

        public static readonly DependencyProperty IdProperty =
            DependencyProperty.Register(
                nameof(Id),
                typeof(Guid),
                typeof(HdvColumnViewModelPreset),
                new PropertyMetadata(Guid.Empty));

        [SerializePropertyInProfile("Id")]
        public Guid Id
        {
            get { return (Guid)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        #endregion

        #region public string Name

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register(
                nameof(Name),
                typeof(string),
                typeof(HdvColumnViewModelPreset),
                PropertyMetadataUtils.DefaultNull);

        [SerializePropertyInProfile("Name")]
        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        #endregion

        #region public string HelpText

        public static readonly DependencyProperty HelpTextProperty =
            DependencyProperty.Register(
                nameof(HelpText),
                typeof(string),
                typeof(HdvColumnViewModelPreset),
                PropertyMetadataUtils.DefaultNull);

        [SerializePropertyInProfile("HelpText")]
        public string HelpText
        {
            get { return (string)GetValue(HelpTextProperty); }
            set { SetValue(HelpTextProperty, value); }
        }

        #endregion

        #region public int Width

        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(
                nameof(Width),
                typeof(int),
                typeof(HdvColumnViewModelPreset),
                new PropertyMetadata(
                    0, null, CoerceWidth));

        [SerializePropertyInProfile("Width")]
        public int Width
        {
            get { return (int)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        private static object CoerceWidth(DependencyObject d, object baseValue)
        {
            int width = (int)baseValue;
            return Boxed.Int32(width.Clamp(0, 10000));
        }

        #endregion

        #region public bool IsVisible

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register(
                nameof(IsVisible),
                typeof(bool),
                typeof(HdvColumnViewModelPreset),
                new PropertyMetadata(Boxed.False));

        [SerializePropertyInProfile("IsVisible")]
        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, Boxed.Bool(value)); }
        }

        #endregion

        #region public TextAlignment TextAlignment

        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(
                nameof(TextAlignment),
                typeof(TextAlignment),
                typeof(HdvColumnViewModelPreset),
                new PropertyMetadata(TextAlignment.Left));

        [SerializePropertyInProfile("TextAlignment")]
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        #endregion

        #region public string CellFormat

        public static readonly DependencyProperty CellFormatProperty =
            DependencyProperty.Register(
                nameof(CellFormat),
                typeof(string),
                typeof(HdvColumnViewModelPreset),
                new PropertyMetadata(null));

        [SerializePropertyInProfile("CellFormat")]
        public string CellFormat
        {
            get { return (string)GetValue(CellFormatProperty); }
            set { SetValue(CellFormatProperty, value); }
        }

        #endregion

        protected override Freezable CreateInstanceCore()
        {
            return new HdvColumnViewModelPreset();
        }

        public new HdvColumnViewModelPreset Clone()
        {
            return (HdvColumnViewModelPreset)base.Clone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public int CompareTo(HdvColumnViewModelPreset other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (ReferenceEquals(this, other))
                return 0;

            int cmp;
            bool dummy =
                ComparisonUtils.CompareValueT(out cmp, IsVisible, other.IsVisible) &&
                ComparisonUtils.Compare(out cmp, Id, Id) &&
                ComparisonUtils.Compare(out cmp, TextAlignment, other.TextAlignment) &&
                ComparisonUtils.CompareT(out cmp, CellFormat, other.CellFormat) &&
                ComparisonUtils.CompareValueT(out cmp, Width, other.Width);

            return cmp;
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            base.CloneCore(sourceFreezable);
        }
    }
}
