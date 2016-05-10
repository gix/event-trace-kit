namespace EventTraceKit.VsExtension.Controls.Primitives
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;

    public class VirtualizedDataGridCellsPresenter : FrameworkElement, IScrollInfo
    {
        private readonly VirtualizedDataGridCellsDrawingVisual cellsDrawingVisual;
        private readonly ValueCache<Typeface> typefaceCache;
        private readonly ValueCache<Pen> horizontalGridLinesPenCache;
        private readonly ValueCache<Pen> verticalGridLinesPenCache;
        private readonly ValueCache<Pen> selectionBorderPenCache;
        private readonly ValueCache<Pen> inactiveSelectionBorderPenCache;
        private readonly ValueCache<Pen> focusBorderPenCache;

        private readonly Action<bool> updateRenderingAction;
        private DispatcherOperation updateRenderingActionOperation;
        private bool postedUpdate;
        private bool postUpdateWhenVisible;
        private Point computedOffset;

        public VirtualizedDataGridCellsPresenter()
        {
            typefaceCache = new ValueCache<Typeface>(CreateTypeFace);
            horizontalGridLinesPenCache = new ValueCache<Pen>(
                CreateHorizontalGridLinesPen);
            verticalGridLinesPenCache = new ValueCache<Pen>(
                CreateVerticalGridLinesPen);
            selectionBorderPenCache = new ValueCache<Pen>(
                CreateSelectionBorderPen);
            inactiveSelectionBorderPenCache = new ValueCache<Pen>(
                CreateInactiveSelectionBorderPen);
            focusBorderPenCache = new ValueCache<Pen>(
                CreateFocusBorderPen);

            FocusVisualStyle = null;
            RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);
            cellsDrawingVisual = new VirtualizedDataGridCellsDrawingVisual(this);
            AddVisualChild(cellsDrawingVisual);

            CoerceValue(FontSizeProperty);
            CoerceValue(RowHeightProperty);

            postUpdateWhenVisible = true;
            SizeChanged += OnSizeChanged;
            IsVisibleChanged += OnIsVisibleChanged;
            updateRenderingAction = UpdateRendering;
        }

        #region public VirtualizedDataGridCellsPresenterViewModel ViewModel { get; set; }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel),
            typeof(VirtualizedDataGridCellsPresenterViewModel),
            typeof(VirtualizedDataGridCellsPresenter),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                FrameworkPropertyMetadataOptions.AffectsRender,
                (s, e) => ((VirtualizedDataGridCellsPresenter)s).OnViewModelChanged(e),
                null));

        public VirtualizedDataGridCellsPresenterViewModel ViewModel
        {
            get { return (VirtualizedDataGridCellsPresenterViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        private void OnViewModelChanged(DependencyPropertyChangedEventArgs e)
        {
            //CoerceValue(ViewModelEventSourceProperty);
        }

        #endregion

        #region public ReadOnlyObservableCollection<VirtualizedDataGridColumnViewModel> VisibleColumns { get; set; }

        /// <summary>
        ///   Identifies the <see cref="VisibleColumns"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VisibleColumnsProperty =
            DependencyProperty.Register(
                nameof(VisibleColumns),
                typeof(ReadOnlyObservableCollection<VirtualizedDataGridColumn>),
                typeof(VirtualizedDataGridCellsPresenter),
                new PropertyMetadata(
                    CollectionDefaults<VirtualizedDataGridColumn>.ReadOnlyObservable));

        /// <summary>
        ///   Gets or sets the visible columns.
        /// </summary>
        public ReadOnlyObservableCollection<VirtualizedDataGridColumn> VisibleColumns
        {
            get
            {
                return (ReadOnlyObservableCollection<VirtualizedDataGridColumn>)GetValue(
                    VisibleColumnsProperty);
            }
            set { SetValue(VisibleColumnsProperty, value); }
        }

        #endregion

        //public static readonly DependencyProperty HighlightBackgroundProperty =
        //    DependencyProperty.Register(
        //        "HighlightBackground",
        //        typeof(Brush),
        //        typeof(VirtualizedDataGridCellsPresenter),
        //        new FrameworkPropertyMetadata(
        //            SolidColorBrushCache.GetBrush(Colors.Green),
        //            FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
        //            FrameworkPropertyMetadataOptions.AffectsRender));

        //public static readonly DependencyProperty PITHighlightBackgroundProperty =
        //    DependencyProperty.Register(
        //        "PITHighlightBackground",
        //        typeof(Brush),
        //        typeof(VirtualizedDataGridCellsPresenter),
        //        new FrameworkPropertyMetadata(
        //            SolidColorBrushCache.GetBrush(Colors.Purple),
        //            FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
        //            FrameworkPropertyMetadataOptions.AffectsRender));

        #region public double RowHeight { get; set; }

        public static readonly DependencyProperty RowHeightProperty =
            DependencyProperty.Register(
                nameof(RowHeight),
                typeof(double),
                typeof(VirtualizedDataGridCellsPresenter),
                new PropertyMetadata(
                    1.0,
                    (d, e) => ((VirtualizedDataGridCellsPresenter)d).OnRowHeightChanged(
                        (double)e.OldValue, (double)e.NewValue),
                    (d, v) => ((VirtualizedDataGridCellsPresenter)d).CoerceRowHeight((double)v)));

        [TypeConverter(typeof(LengthConverter))]
        public double RowHeight
        {
            get { return (double)GetValue(RowHeightProperty); }
            set { SetValue(RowHeightProperty, value); }
        }

        private void OnRowHeightChanged(double oldValue, double newValue)
        {
            if (!DoubleUtils.AreClose(oldValue, newValue))
                InvalidateVisual();

            //if (this.ViewModel != null)
            //    this.ViewModel.RowHeightReflected = newValue;
        }

        private double CoerceRowHeight(double baseValue)
        {
            double rowHeight = DoubleUtils.Max(FontSize * 1.2, 12.0, baseValue);
            return Math.Ceiling(rowHeight);
        }

        #endregion

        #region public FontFamily FontFamily { get; set; }

        public static readonly DependencyProperty FontFamilyProperty =
            TextElement.FontFamilyProperty.AddOwner(
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemFonts.MessageFontFamily,
                    FrameworkPropertyMetadataOptions.Inherits,
                    ClearTypefaceCache));

        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        #endregion

        #region public double FontSize { get; set; }

        public static readonly DependencyProperty FontSizeProperty =
            TextElement.FontSizeProperty.AddOwner(
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemFonts.MessageFontSize,
                    FrameworkPropertyMetadataOptions.Inherits,
                    OnFontSizeChanged));

        [TypeConverter(typeof(FontSizeConverter))]
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        private static void OnFontSizeChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (VirtualizedDataGridCellsPresenter)d;
            source.CoerceValue(RowHeightProperty);
        }

        #endregion

        #region public FontStretch FontStretch { get; set; }

        public static readonly DependencyProperty FontStretchProperty =
            TextElement.FontStretchProperty.AddOwner(
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    TextElement.FontStretchProperty.DefaultMetadata.DefaultValue,
                    FrameworkPropertyMetadataOptions.Inherits,
                    ClearTypefaceCache));

        public FontStretch FontStretch
        {
            get { return (FontStretch)GetValue(FontStretchProperty); }
            set { SetValue(FontStretchProperty, value); }
        }

        #endregion

        #region public FontStyle FontStyle { get; set; }

        public static readonly DependencyProperty FontStyleProperty =
            TextElement.FontStyleProperty.AddOwner(
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemFonts.MessageFontStyle,
                    FrameworkPropertyMetadataOptions.Inherits,
                    ClearTypefaceCache));

        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        #endregion

        #region public FontWeight FontWeight { get; set; }

        public static readonly DependencyProperty FontWeightProperty =
            TextElement.FontWeightProperty.AddOwner(
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemFonts.MessageFontWeight,
                    FrameworkPropertyMetadataOptions.Inherits,
                    ClearTypefaceCache));

        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        #endregion

        #region public Brush Foreground { get; set; }

        public static readonly DependencyProperty ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner(
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.ControlTextBrush,
                    FrameworkPropertyMetadataOptions.Inherits));

        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        #endregion

        #region public Brush PrimaryBackground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="PrimaryBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PrimaryBackgroundProperty =
            DependencyProperty.Register(
                nameof(PrimaryBackground),
                typeof(Brush),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Brushes.White,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnPrimaryBackgroundChanged));

        private static void OnPrimaryBackgroundChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        /// <summary>
        ///   Gets or sets the primary background.
        /// </summary>
        public Brush PrimaryBackground
        {
            get { return (Brush)GetValue(PrimaryBackgroundProperty); }
            set { SetValue(PrimaryBackgroundProperty, value); }
        }

        #endregion

        #region public Brush SecondaryBackground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="SecondaryBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SecondaryBackgroundProperty =
            DependencyProperty.Register(
                nameof(SecondaryBackground),
                typeof(Brush),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Brushes.WhiteSmoke,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the secondary background.
        /// </summary>
        public Brush SecondaryBackground
        {
            get { return (Brush)GetValue(SecondaryBackgroundProperty); }
            set { SetValue(SecondaryBackgroundProperty, value); }
        }

        #endregion

        #region public Visibility HorizontalScrollVisibility { get; internal set; }

        private static readonly DependencyPropertyKey HorizontalScrollVisibilityPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(HorizontalScrollVisibility),
                typeof(Visibility),
                typeof(VirtualizedDataGridCellsPresenter),
                new PropertyMetadata(Visibility.Hidden));

        public static readonly DependencyProperty HorizontalScrollVisibilityProperty =
            HorizontalScrollVisibilityPropertyKey.DependencyProperty;

        public Visibility HorizontalScrollVisibility
        {
            get { return (Visibility)GetValue(HorizontalScrollVisibilityProperty); }
            internal set { SetValue(HorizontalScrollVisibilityPropertyKey, value); }
        }

        #endregion

        #region public Visibility VerticalScrollVisibility { get; internal set; }

        private static readonly DependencyPropertyKey VerticalScrollVisibilityPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(VerticalScrollVisibility),
                typeof(Visibility),
                typeof(VirtualizedDataGridCellsPresenter),
                new PropertyMetadata(Visibility.Hidden));

        public static readonly DependencyProperty VerticalScrollVisibilityProperty =
            VerticalScrollVisibilityPropertyKey.DependencyProperty;

        public Visibility VerticalScrollVisibility
        {
            get { return (Visibility)GetValue(VerticalScrollVisibilityProperty); }
            internal set { SetValue(VerticalScrollVisibilityPropertyKey, value); }
        }

        #endregion

        #region public Brush HorizontalGridLinesBrush { get; set; }

        /// <summary>
        ///   Identifies the <see cref="HorizontalGridLinesBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HorizontalGridLinesBrushProperty =
            DependencyProperty.Register(
                nameof(HorizontalGridLinesBrush),
                typeof(Brush),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Brushes.Silver,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    ClearHorizontalGridLinesPen));

        public Brush HorizontalGridLinesBrush
        {
            get { return (Brush)GetValue(HorizontalGridLinesBrushProperty); }
            set { SetValue(HorizontalGridLinesBrushProperty, value); }
        }

        private static void ClearHorizontalGridLinesPen(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (VirtualizedDataGridCellsPresenter)d;
            source.horizontalGridLinesPenCache.Clear();
        }

        #endregion

        #region public double HorizontalGridLinesThickness { get; set; }

        /// <summary>
        ///   Identifies the <see cref="HorizontalGridLinesThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HorizontalGridLinesThicknessProperty =
            DependencyProperty.Register(
                nameof(HorizontalGridLinesThickness),
                typeof(double),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    ClearHorizontalGridLinesPen));

        public double HorizontalGridLinesThickness
        {
            get { return (double)GetValue(HorizontalGridLinesThicknessProperty); }
            set { SetValue(HorizontalGridLinesThicknessProperty, value); }
        }

        #endregion

        #region public Brush VerticalGridLinesBrush { get; set; }

        /// <summary>
        ///   Identifies the <see cref="VerticalGridLinesBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VerticalGridLinesBrushProperty =
            DependencyProperty.Register(
                nameof(VerticalGridLinesBrush),
                typeof(Brush),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Brushes.Silver,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    ClearVerticalGridLinesPen));

        public Brush VerticalGridLinesBrush
        {
            get { return (Brush)GetValue(VerticalGridLinesBrushProperty); }
            set { SetValue(VerticalGridLinesBrushProperty, value); }
        }

        private static void ClearVerticalGridLinesPen(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (VirtualizedDataGridCellsPresenter)d;
            source.verticalGridLinesPenCache.Clear();
        }

        #endregion

        #region public double VerticalGridLinesThickness { get; set; }

        /// <summary>
        ///   Identifies the <see cref="VerticalGridLinesThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VerticalGridLinesThicknessProperty =
            DependencyProperty.Register(
                nameof(VerticalGridLinesThickness),
                typeof(double),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    ClearVerticalGridLinesPen));

        public double VerticalGridLinesThickness
        {
            get { return (double)GetValue(VerticalGridLinesThicknessProperty); }
            set { SetValue(VerticalGridLinesThicknessProperty, value); }
        }

        #endregion

        #region public Brush SeparatorBrush { get; set; }

        /// <summary>
        ///   Identifies the <see cref="SeparatorBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SeparatorBrushProperty =
            DependencyProperty.Register(
                nameof(SeparatorBrush),
                typeof(Brush),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Brushes.Gold,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the separator brush.
        /// </summary>
        public Brush SeparatorBrush
        {
            get { return (Brush)GetValue(SeparatorBrushProperty); }
            set { SetValue(SeparatorBrushProperty, value); }
        }

        #endregion

        #region public Brush FrozenColumnBrush { get; set; }

        /// <summary>
        ///   Identifies the <see cref="FrozenColumnBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FrozenColumnBrushProperty =
            DependencyProperty.Register(
                nameof(FrozenColumnBrush),
                typeof(Brush),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.ControlBrush,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the frozen column brush.
        /// </summary>
        public Brush FrozenColumnBrush
        {
            get { return (Brush)GetValue(FrozenColumnBrushProperty); }
            set { SetValue(FrozenColumnBrushProperty, value); }
        }

        #endregion

        #region public Brush FreezableAreaSeparatorBrush { get; set; }

        /// <summary>
        ///   Identifies the <see cref="FreezableAreaSeparatorBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FreezableAreaSeparatorBrushProperty =
            DependencyProperty.Register(
                nameof(FreezableAreaSeparatorBrush),
                typeof(Brush),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Brushes.Gray,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the freezable area separator brush.
        /// </summary>
        public Brush FreezableAreaSeparatorBrush
        {
            get { return (Brush)GetValue(FreezableAreaSeparatorBrushProperty); }
            set { SetValue(FreezableAreaSeparatorBrushProperty, value); }
        }

        #endregion

        #region public Brush SelectionForeground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="SelectionForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionForegroundProperty =
            DependencyProperty.Register(
                nameof(SelectionForeground),
                typeof(Brush),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.HighlightTextBrush,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the selection foreground brush.
        /// </summary>
        public Brush SelectionForeground
        {
            get { return (Brush)GetValue(SelectionForegroundProperty); }
            set { SetValue(SelectionForegroundProperty, value); }
        }

        #endregion

        #region public Brush SelectionBackground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="SelectionBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionBackgroundProperty =
            DependencyProperty.Register(
                nameof(SelectionBackground),
                typeof(Brush),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.HighlightBrush,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the selection background brush.
        /// </summary>
        public Brush SelectionBackground
        {
            get { return (Brush)GetValue(SelectionBackgroundProperty); }
            set { SetValue(SelectionBackgroundProperty, value); }
        }

        #endregion

        #region public Brush SelectionBorderBrush { get; set; }

        /// <summary>
        ///   Identifies the <see cref="SelectionBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionBorderBrushProperty =
            DependencyProperty.Register(
                nameof(SelectionBorderBrush),
                typeof(Brush),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.HighlightBrush,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    ClearSelectionBorderPenCache));

        /// <summary>
        ///   Gets or sets the selection border brush.
        /// </summary>
        public Brush SelectionBorderBrush
        {
            get { return (Brush)GetValue(SelectionBorderBrushProperty); }
            set { SetValue(SelectionBorderBrushProperty, value); }
        }

        private static void ClearSelectionBorderPenCache(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (VirtualizedDataGridCellsPresenter)d;
            source.selectionBorderPenCache.Clear();
        }

        #endregion

        #region public double SelectionBorderThickness { get; set; }

        /// <summary>
        ///   Identifies the <see cref="SelectionBorderThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionBorderThicknessProperty =
            DependencyProperty.Register(
                nameof(SelectionBorderThickness),
                typeof(double),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    ClearSelectionBorderPenCache));

        /// <summary>
        ///   Gets or sets the selection border thickness.
        /// </summary>
        public double SelectionBorderThickness
        {
            get { return (double)GetValue(SelectionBorderThicknessProperty); }
            set { SetValue(SelectionBorderThicknessProperty, value); }
        }

        #endregion

        #region public Brush InactiveSelectionForeground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="InactiveSelectionForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InactiveSelectionForegroundProperty =
            DependencyProperty.Register(
                nameof(InactiveSelectionForeground),
                typeof(Brush),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.InactiveSelectionHighlightTextBrush,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the selection foreground brush.
        /// </summary>
        public Brush InactiveSelectionForeground
        {
            get { return (Brush)GetValue(InactiveSelectionForegroundProperty); }
            set { SetValue(InactiveSelectionForegroundProperty, value); }
        }

        #endregion

        #region public Brush InactiveSelectionBackground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="InactiveSelectionBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InactiveSelectionBackgroundProperty =
            DependencyProperty.Register(
                nameof(InactiveSelectionBackground),
                typeof(Brush),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.InactiveSelectionHighlightBrush,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the selection background brush.
        /// </summary>
        public Brush InactiveSelectionBackground
        {
            get { return (Brush)GetValue(InactiveSelectionBackgroundProperty); }
            set { SetValue(InactiveSelectionBackgroundProperty, value); }
        }

        #endregion

        #region public Brush InactiveSelectionBorderBrush { get; set; }

        /// <summary>
        ///   Identifies the <see cref="InactiveSelectionBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InactiveSelectionBorderBrushProperty =
            DependencyProperty.Register(
                nameof(InactiveSelectionBorderBrush),
                typeof(Brush),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.InactiveSelectionHighlightBrush,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    ClearInactiveSelectionBorderPenCache));

        /// <summary>
        ///   Gets or sets the selection border brush.
        /// </summary>
        public Brush InactiveSelectionBorderBrush
        {
            get { return (Brush)GetValue(InactiveSelectionBorderBrushProperty); }
            set { SetValue(InactiveSelectionBorderBrushProperty, value); }
        }

        private static void ClearInactiveSelectionBorderPenCache(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (VirtualizedDataGridCellsPresenter)d;
            source.selectionBorderPenCache.Clear();
        }

        #endregion

        #region public Brush FocusBorderBrush { get; set; }

        /// <summary>
        ///   Identifies the <see cref="FocusBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusBorderBrushProperty =
            DependencyProperty.Register(
                nameof(FocusBorderBrush),
                typeof(Brush),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.ControlTextBrush,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    ClearFocusBorderPenCache));

        /// <summary>
        ///   Gets or sets the focus border brush.
        /// </summary>
        public Brush FocusBorderBrush
        {
            get { return (Brush)GetValue(FocusBorderBrushProperty); }
            set { SetValue(FocusBorderBrushProperty, value); }
        }

        private static void ClearFocusBorderPenCache(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (VirtualizedDataGridCellsPresenter)d;
            source.focusBorderPenCache.Clear();
        }

        #endregion

        #region public double FocusBorderThickness { get; set; }

        /// <summary>
        ///   Identifies the <see cref="FocusBorderThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusBorderThicknessProperty =
            DependencyProperty.Register(
                nameof(FocusBorderThickness),
                typeof(double),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    ClearFocusBorderPenCache));

        /// <summary>
        ///   Gets or sets the focus border thickness.
        /// </summary>
        public double FocusBorderThickness
        {
            get { return (double)GetValue(FocusBorderThicknessProperty); }
            set { SetValue(FocusBorderThicknessProperty, value); }
        }

        #endregion

        protected override int VisualChildrenCount => base.VisualChildrenCount + 1;

        public Typeface Typeface => typefaceCache.Value;

        public Pen HorizontalGridLinesPen => horizontalGridLinesPenCache.Value;

        public Pen VerticalGridLinesPen => verticalGridLinesPenCache.Value;

        public Pen SelectionBorderPen => selectionBorderPenCache.Value;

        public Pen InactiveSelectionBorderPen => inactiveSelectionBorderPenCache.Value;

        public Pen FocusBorderPen => focusBorderPenCache.Value;

        internal int FirstVisibleRowIndex =>
            (int)Math.Floor(VerticalOffset / RowHeight);

        internal int LastVisibleRowIndex =>
            Math.Min(
                (int)Math.Ceiling((VerticalOffset + ViewportHeight) / RowHeight) - 1,
                ViewModel?.RowCount - 1 ?? -1);

        internal int LastAvailableRowIndex =>
            (int)Math.Ceiling((VerticalOffset + ViewportHeight) / RowHeight) - 1;

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            arrangeSize = base.ArrangeOverride(arrangeSize);
            VerifyScrollData(arrangeSize, new Size(ExtentWidth, ExtentHeight));
            return arrangeSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0)
                return cellsDrawingVisual;
            return base.GetVisualChild(index - 1);
        }

        #region IScrollInfo

        #region public ScrollViewer ScrollOwner { get; set; }

        public static readonly DependencyProperty ScrollOwnerProperty =
            DependencyProperty.Register(
                nameof(ScrollOwner),
                typeof(ScrollViewer),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(null));

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ScrollViewer ScrollOwner
        {
            get { return (ScrollViewer)GetValue(ScrollOwnerProperty); }
            set { SetValue(ScrollOwnerProperty, value); }
        }

        #endregion

        #region public bool CanHorizontallyScroll { get; set; }

        /// <summary>
        ///   Identifies the <see cref="CanHorizontallyScroll"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanHorizontallyScrollProperty =
            DependencyProperty.Register(
                nameof(CanHorizontallyScroll),
                typeof(bool),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(Boxed.False));

        public bool CanHorizontallyScroll
        {
            get { return (bool)GetValue(CanHorizontallyScrollProperty); }
            set { SetValue(CanHorizontallyScrollProperty, value); }
        }

        #endregion

        #region public bool CanVerticallyScroll { get; set; }

        /// <summary>
        ///   Identifies the <see cref="CanVerticallyScroll"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanVerticallyScrollProperty =
            DependencyProperty.Register(
                nameof(CanVerticallyScroll),
                typeof(bool),
                typeof(VirtualizedDataGridCellsPresenter),
                new PropertyMetadata(Boxed.False));

        public bool CanVerticallyScroll
        {
            get { return (bool)GetValue(CanVerticallyScrollProperty); }
            set { SetValue(CanVerticallyScrollProperty, value); }
        }

        #endregion

        #region public double ExtentWidth { get; private set; }

        public static readonly DependencyProperty ExtentWidthProperty =
            DependencyProperty.Register(
                "ExtentWidth",
                typeof(double),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Boxed.DoubleZero,
                    (d, e) => ((VirtualizedDataGridCellsPresenter)d).OnExtentWidthChanged(e)));

        public double ExtentWidth
        {
            get { return (double)GetValue(ExtentWidthProperty); }
            private set { SetValue(ExtentWidthProperty, value); }
        }

        private void OnExtentWidthChanged(DependencyPropertyChangedEventArgs e)
        {
            CoerceValue(HorizontalOffsetProperty);
        }

        #endregion

        #region public double ExtentHeight { get; private set; }

        public static readonly DependencyProperty ExtentHeightProperty =
            DependencyProperty.Register(
                nameof(ExtentHeight),
                typeof(double),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Boxed.DoubleZero,
                    (d, e) => ((VirtualizedDataGridCellsPresenter)d).OnExtentHeightChanged(e)));

        public double ExtentHeight
        {
            get { return (double)GetValue(ExtentHeightProperty); }
            private set { SetValue(ExtentHeightProperty, value); }
        }

        private void OnExtentHeightChanged(
            DependencyPropertyChangedEventArgs e)
        {
            CoerceValue(VerticalOffsetProperty);
        }

        #endregion

        #region public double ViewportWidth { get; private set; }

        public static readonly DependencyProperty ViewportWidthProperty =
            DependencyProperty.Register(
                "ViewportWidth",
                typeof(double),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Boxed.DoubleZero,
                    (d, e) => ((VirtualizedDataGridCellsPresenter)d).OnViewportWidthChanged(e)));

        public double ViewportWidth
        {
            get { return (double)GetValue(ViewportWidthProperty); }
            private set { SetValue(ViewportWidthProperty, value); }
        }

        private void OnViewportWidthChanged(
            DependencyPropertyChangedEventArgs e)
        {
            CoerceValue(HorizontalOffsetProperty);
        }

        #endregion

        #region public double ViewportHeight { get; private set; }

        public static readonly DependencyProperty ViewportHeightProperty =
            DependencyProperty.Register(
                nameof(ViewportHeight),
                typeof(double),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Boxed.DoubleZero,
                    (d, e) => ((VirtualizedDataGridCellsPresenter)d).OnViewportHeightChanged(e)));

        public double ViewportHeight
        {
            get { return (double)GetValue(ViewportHeightProperty); }
            private set { SetValue(ViewportHeightProperty, value); }
        }

        private void OnViewportHeightChanged(
            DependencyPropertyChangedEventArgs e)
        {
            CoerceValue(VerticalOffsetProperty);
        }

        #endregion

        #region public double HorizontalOffset { get; private set; }

        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register(
                nameof(HorizontalOffset),
                typeof(double),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Boxed.DoubleZero,
                    (d, e) => ((VirtualizedDataGridCellsPresenter)d).OnHorizontalOffsetChanged(e),
                    (d, v) => ((VirtualizedDataGridCellsPresenter)d).CoerceHorizontalOffset(v)),
                ValidateValueCallbacks.IsFiniteDouble);

        public double HorizontalOffset
        {
            get { return (double)GetValue(HorizontalOffsetProperty); }
            private set { SetValue(HorizontalOffsetProperty, value); }
        }

        private object CoerceHorizontalOffset(object baseValue)
        {
            var max = Math.Max(0.0, ExtentWidth - ViewportWidth);
            var offset = DoubleUtils.SafeClamp((double)baseValue, 0, max);
            return Math.Ceiling(offset);
        }

        private void OnHorizontalOffsetChanged(
            DependencyPropertyChangedEventArgs e)
        {
            PostUpdateRendering();
        }

        #endregion

        #region public double VerticalOffset { get; private set; }

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.Register(
                nameof(VerticalOffset),
                typeof(double),
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Boxed.DoubleZero,
                    (d, e) => ((VirtualizedDataGridCellsPresenter)d).OnVerticalOffsetChanged(e),
                    (d, v) => ((VirtualizedDataGridCellsPresenter)d).CoerceVerticalOffset(v)),
                ValidateValueCallbacks.IsFiniteDouble);

        public double VerticalOffset
        {
            get { return (double)GetValue(VerticalOffsetProperty); }
            private set { SetValue(VerticalOffsetProperty, value); }
        }

        private object CoerceVerticalOffset(object baseValue)
        {
            var max = Math.Max(0, ExtentHeight - ViewportHeight);
            var offset = ((double)baseValue).Clamp(0, max);
            return Math.Ceiling(offset);
        }

        private void OnVerticalOffsetChanged(
            DependencyPropertyChangedEventArgs e)
        {
            PostUpdateRendering();
        }

        #endregion

        public void LineUp()
        {
            VerticalOffset -= RowHeight;
        }

        public void LineDown()
        {
            VerticalOffset += RowHeight;
        }

        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset - RowHeight);
        }

        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset + RowHeight);
        }

        public void PageUp()
        {
            VerticalOffset -= ViewportHeight;
        }

        public void PageDown()
        {
            VerticalOffset += ViewportHeight;
        }

        public void PageLeft()
        {
            SetHorizontalOffset(HorizontalOffset - ViewportWidth);
        }

        public void PageRight()
        {
            SetHorizontalOffset(HorizontalOffset + ViewportWidth);
        }

        public void MouseWheelUp()
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) | Keyboard.IsKeyDown(Key.RightShift))
                MouseWheelLeft();
            else
                VerticalOffset -= RowHeight * SystemParameters.WheelScrollLines;
        }

        public void MouseWheelDown()
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) | Keyboard.IsKeyDown(Key.RightShift))
                MouseWheelRight();
            else
                VerticalOffset += RowHeight * SystemParameters.WheelScrollLines;
        }

        public void MouseWheelLeft()
        {
            SetHorizontalOffset(
                HorizontalOffset - RowHeight * SystemParameters.WheelScrollLines);
        }

        public void MouseWheelRight()
        {
            SetHorizontalOffset(
                HorizontalOffset + RowHeight * SystemParameters.WheelScrollLines);
        }

        public void SetHorizontalOffset(double offset)
        {
            HorizontalOffset = offset;
        }

        public void SetVerticalOffset(double offset)
        {
            if (offset != 0 || IsLoaded)
                VerticalOffset = offset;
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            // We can only work on visuals that are us or children.
            // An empty rect has no size or position so we can't meaningfully use it.
            if (visual == null
                || !(ReferenceEquals(visual, this) || IsAncestorOf(visual))
                || rectangle.IsEmpty)
                return Rect.Empty;

            // Compute the child's rect relative to (0,0) in our coordinate space.
            GeneralTransform childTransform = visual.TransformToAncestor(this);
            rectangle = childTransform.TransformBounds(rectangle);

            var viewport = new Rect(
                HorizontalOffset, VerticalOffset, ViewportWidth, ViewportHeight);
            rectangle.X += viewport.X;
            rectangle.Y += viewport.Y;

            // Compute the offsets required to minimally scroll the child maximally into view.
            double minX = ComputeScrollOffsetWithMinimalScroll(
                viewport.Left, viewport.Right, rectangle.Left, rectangle.Right);
            double minY = ComputeScrollOffsetWithMinimalScroll(
                viewport.Top, viewport.Bottom, rectangle.Top, rectangle.Bottom);

            SetHorizontalOffset(minX);
            VerticalOffset = minY;

            // Compute the visible rectangle of the child relative to the viewport.
            viewport.X = minX;
            viewport.Y = minY;
            rectangle.Intersect(viewport);

            if (!rectangle.IsEmpty) {
                rectangle.X -= viewport.X;
                rectangle.Y -= viewport.Y;
            }

            return rectangle;
        }

        #endregion

        private void OnSizeChanged(object sender, EventArgs e)
        {
            PostUpdateRendering();
        }

        private void UpdateScrollInfo()
        {
            if (ViewModel != null) {
                double extentWidth = VisibleColumns.Sum(x => x.Width);
                double extentHeight = ViewModel.RowCount * RowHeight;
                extentHeight += HorizontalGridLinesThickness;

                VerifyScrollData(
                    new Size(ViewportWidth, ViewportHeight),
                    new Size(extentWidth, extentHeight));
            }

            HorizontalScrollVisibility = ViewportWidth < ExtentWidth ?
                Visibility.Visible : Visibility.Collapsed;

            VerticalScrollVisibility = ViewportHeight < ExtentHeight ?
                Visibility.Visible : Visibility.Collapsed;
        }

        private static void ClearTypefaceCache(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (VirtualizedDataGridCellsPresenter)d;
            source.typefaceCache.Clear();
        }

        private void VerifyScrollData(Size viewportSize, Size extentSize)
        {
            if (double.IsInfinity(viewportSize.Width))
                viewportSize.Width = extentSize.Width;
            if (double.IsInfinity(viewportSize.Height))
                viewportSize.Height = extentSize.Height;

            bool valid = true;
            valid &= SizeUtils.AreClose(viewportSize, new Size(ViewportWidth, ViewportHeight));
            valid &= SizeUtils.AreClose(extentSize, new Size(ExtentWidth, ExtentHeight));

            ViewportWidth = viewportSize.Width;
            ViewportHeight = viewportSize.Height;
            ExtentWidth = extentSize.Width;
            ExtentHeight = extentSize.Height;

            //if (this.ViewModel != null)
            //    this.ViewModel.VerticalScrollExtent = extentSize.Height;

            valid &= CoerceOffsets();

            if (!valid)
                ScrollOwner?.InvalidateScrollInfo();
        }

        private bool CoerceOffsets()
        {
            var point = new Point(
                CoerceOffset(HorizontalOffset, ExtentWidth, ViewportWidth),
                CoerceOffset(VerticalOffset, ExtentHeight, ViewportHeight));

            bool valid = PointUtils.AreClose(computedOffset, point);
            computedOffset = point;
            return valid;
        }

        private static double CoerceOffset(double offset, double extent, double viewport)
        {
            offset = Math.Min(offset, extent - viewport);
            return Math.Max(offset, 0);
        }

        private int? prevFirst;
        private int? prevLast;

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            PostUpdateRendering();
        }

        internal void UpdateRendering(bool forceUpdate)
        {
            postedUpdate = false;

            if (ViewModel != null) { // && ViewModel.IsReady
                UpdateScrollInfo();

                bool updateDrawing =
                    prevFirst == null ||
                    prevLast == null ||
                    (ViewModel.RowCount >= prevFirst && ViewModel.RowCount <= prevLast);
                updateDrawing = true;

                if (updateDrawing) {
                    cellsDrawingVisual.Update(
                        new Rect(HorizontalOffset, VerticalOffset, ViewportWidth, ViewportHeight),
                        new Size(ExtentWidth, ExtentHeight),
                        forceUpdate);

                    prevFirst = FirstVisibleRowIndex;
                    prevLast = LastAvailableRowIndex;
                }
            }
        }

        internal void PostUpdateRendering(bool forceUpdate = true)
        {
            if (!IsVisible) {
                postUpdateWhenVisible = true;
                return;
            }

            postUpdateWhenVisible = false;
            if (postedUpdate)
                return;

            postedUpdate = true;
            updateRenderingActionOperation?.Abort();
            updateRenderingActionOperation = Dispatcher.BeginInvoke(
                updateRenderingAction, DispatcherPriority.Render, forceUpdate);
        }

        private void OnIsVisibleChanged(
            object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && postUpdateWhenVisible)
                PostUpdateRendering();
        }

        internal double GetColumnAutoSize(VirtualizedDataGridColumn column)
        {
            return cellsDrawingVisual.GetColumnAutoSize(column);
        }

        public int? GetRowFromPosition(double y)
        {
            if (ViewModel == null)
                return null;

            int row = (int)((y + VerticalOffset) / RowHeight);
            if (row >= 0 && row < ViewModel.RowCount)
                return row;

            return null;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.Handled) // || !this.ViewModel.IsReady)
                return;

            Point position = e.GetPosition(this);
            Keyboard.Focus(this);

            int? rowFromPosition = GetRowFromPosition(position.Y);
            if (rowFromPosition.HasValue) {
                int rowIndex = rowFromPosition.Value;
                EnsureVisible(rowIndex);
                var viewModel = ViewModel;
                if (viewModel != null) {
                    ModifierKeys modifiers = Keyboard.Modifiers;
                    bool extend = (modifiers & ModifierKeys.Control) != ModifierKeys.None;
                    switch (modifiers & ~ModifierKeys.Control) {
                        case ModifierKeys.None:
                            viewModel.RowSelection.ToggleSingle(rowIndex, extend);
                            e.Handled = true;
                            break;

                        case ModifierKeys.Shift:
                            viewModel.RowSelection.ToggleExtent(rowIndex, extend);
                            e.Handled = true;
                            break;
                    }

                    if (e.Handled)
                        viewModel.RequestUpdate(false);
                }
            }

            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel != null && /*viewModel.IsReady &&*/ !e.Handled &&
                (e.KeyboardDevice.Modifiers & (ModifierKeys.Windows | ModifierKeys.Alt)) == ModifierKeys.None) {

                switch (e.Key) {
                    case Key.Prior:
                    case Key.Next:
                        OnPageUpOrDownKeyDown(e);
                        break;

                    case Key.Home:
                    case Key.End:
                        OnHomeOrEndKeyDown(e);
                        break;

                    case Key.Left:
                    case Key.Right:
                    case Key.Up:
                    case Key.Down:
                        OnArrowKeyDown(e);
                        break;

                    case Key.Space:
                        OnSpaceKeyDown(e);
                        break;

                    case Key.A:
                        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
                            viewModel.RowSelection.SelectRange(0, viewModel.RowCount - 1);
                            e.Handled = true;
                        }
                        break;
                }

                if (e.Handled)
                    viewModel.RequestUpdate(false);
            }

            //if (!ViewModel.IsReady)
            //    e.Handled = true;

            base.OnKeyDown(e);
        }

        private void OnSpaceKeyDown(KeyEventArgs e)
        {
            var viewModel = ViewModel;
            var rowSelection = viewModel.RowSelection;

            switch (e.KeyboardDevice.Modifiers) {
                case ModifierKeys.None:
                    rowSelection.ToggleSingle(viewModel.FocusIndex, false);
                    break;
                case ModifierKeys.Shift:
                    rowSelection.ToggleExtent(viewModel.FocusIndex, false);
                    break;
                case ModifierKeys.Control:
                    rowSelection.ToggleSingle(viewModel.FocusIndex, true);
                    break;
                case ModifierKeys.Shift | ModifierKeys.Control:
                    rowSelection.ToggleExtent(viewModel.FocusIndex, true);
                    break;
                default:
                    return;
            }

            e.Handled = true;
        }

        private void OnHomeOrEndKeyDown(KeyEventArgs e)
        {
            var viewModel = ViewModel;
            int firstRow = 0;
            int lastRow = viewModel.RowCount - 1;

            switch (e.Key) {
                case Key.Home:
                    switch (e.KeyboardDevice.Modifiers) {
                        case ModifierKeys.None:
                            viewModel.RowSelection.ToggleSingle(firstRow, false);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift:
                            viewModel.RowSelection.ToggleExtent(firstRow, false);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Control:
                            viewModel.FocusIndex = firstRow;
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift | ModifierKeys.Control:
                            VerticalOffset = 0;
                            e.Handled = true;
                            break;
                    }
                    break;

                case Key.End:
                    switch (e.KeyboardDevice.Modifiers) {
                        case ModifierKeys.None:
                            viewModel.RowSelection.ToggleSingle(lastRow, false);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift:
                            viewModel.RowSelection.ToggleExtent(lastRow, false);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Control:
                            viewModel.FocusIndex = lastRow;
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift | ModifierKeys.Control:
                            VerticalOffset = ExtentHeight;
                            e.Handled = true;
                            break;
                    }
                    break;
            }
        }

        private void OnPageUpOrDownKeyDown(KeyEventArgs e)
        {
            var viewModel = ViewModel;

            int firstRow = 0;
            int lastRow = viewModel.RowCount - 1;

            int visibleRowCount = (int)Math.Floor(ViewportHeight / RowHeight);
            int prevPageRow = Math.Max(viewModel.FocusIndex - visibleRowCount, firstRow);
            int nextPageRow = Math.Min(viewModel.FocusIndex + visibleRowCount, lastRow);

            switch (e.Key) {
                case Key.Prior:
                    switch (e.KeyboardDevice.Modifiers) {
                        case ModifierKeys.None:
                            viewModel.RowSelection.ToggleSingle(prevPageRow, false);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift:
                            viewModel.RowSelection.ToggleExtent(prevPageRow, false);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Control:
                            viewModel.FocusIndex = prevPageRow;
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift | ModifierKeys.Control:
                            PageUp();
                            e.Handled = true;
                            break;
                    }
                    break;

                case Key.Next:
                    switch (e.KeyboardDevice.Modifiers) {
                        case ModifierKeys.None:
                            viewModel.RowSelection.ToggleSingle(nextPageRow, false);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift:
                            viewModel.RowSelection.ToggleExtent(nextPageRow, false);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Control:
                            viewModel.FocusIndex = nextPageRow;
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift | ModifierKeys.Control:
                            PageDown();
                            e.Handled = true;
                            break;
                    }
                    break;
            }
        }

        private void OnArrowKeyDown(KeyEventArgs e)
        {
            var viewModel = ViewModel;

            int firstRow = 0;
            int lastRow = viewModel.RowCount - 1;

            int prevRow = Math.Max(viewModel.FocusIndex - 1, firstRow);
            int nextRow = Math.Min(viewModel.FocusIndex + 1, lastRow);

            switch (e.Key) {
                case Key.Left:
                    switch (e.KeyboardDevice.Modifiers) {
                        case ModifierKeys.Control:
                            LineLeft();
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift | ModifierKeys.Control:
                            PageLeft();
                            e.Handled = true;
                            break;
                    }
                    break;

                case Key.Right:
                    switch (e.KeyboardDevice.Modifiers) {
                        case ModifierKeys.Control:
                            LineRight();
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift | ModifierKeys.Control:
                            PageRight();
                            e.Handled = true;
                            break;
                    }
                    break;

                case Key.Up:
                    switch (e.KeyboardDevice.Modifiers) {
                        case ModifierKeys.None:
                            viewModel.RowSelection.ToggleSingle(prevRow, false);
                            BringRowIntoView(prevRow);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift:
                            viewModel.RowSelection.ToggleExtent(prevRow, false);
                            BringRowIntoView(prevRow);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Control:
                            viewModel.FocusIndex = prevRow;
                            BringRowIntoView(prevRow);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift | ModifierKeys.Control:
                            LineUp();
                            e.Handled = true;
                            break;
                    }
                    break;

                case Key.Down:
                    switch (e.KeyboardDevice.Modifiers) {
                        case ModifierKeys.None:
                            viewModel.RowSelection.ToggleSingle(nextRow, false);
                            BringRowIntoView(nextRow);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift:
                            viewModel.RowSelection.ToggleExtent(nextRow, false);
                            BringRowIntoView(nextRow);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Control:
                            viewModel.FocusIndex = nextRow;
                            BringRowIntoView(nextRow);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift | ModifierKeys.Control:
                            LineDown();
                            e.Handled = true;
                            break;
                    }
                    break;
            }
        }

        private static double ComputeScrollOffsetWithMinimalScroll(
            double topView, double bottomView, double topChild, double bottomChild)
        {
            // # Child Position   Child Size    Scroll   Action
            // --------------------------------------------------------
            // 1 Above viewport   <= viewport   Down     Align top edge of child & viewport
            // 2 Above viewport   >  viewport   Down     Align bottom edge of child & viewport
            // 3 Below viewport   <= viewport   Up       Align bottom edge of child & viewport
            // 4 Below viewport   >  viewport   Up       Align top edge of child & viewport
            // 5 Entirely within viewport       N/A      No scroll.
            // 6 Spanning viewport              N/A      No scroll.
            //
            // Note: "Above viewport" = topChild above topView, bottomChild above bottomView
            //       "Below viewport" = topChild below topView, bottomChild below bottomView
            // The child thus may overlap with the viewport, but will scroll in the same direction.

            double viewHeight = bottomView - topView;
            double childHeight = bottomChild - topChild;
            bool childLarger = childHeight > viewHeight;

            bool above = topChild.LessThan(topView) && bottomChild.LessThan(bottomView);
            bool below = topChild.GreaterThan(topView) && bottomChild.GreaterThan(bottomView);

            // Cases 1 and 4
            if (above && !childLarger || below && childLarger)
                return topChild;

            // Cases 2 and 3
            if (above || below)
                return bottomChild - viewHeight;

            // Cases 5 and 6
            return topView;
        }

        internal void EnsureVisible(int rowIndex)
        {
            if (rowIndex == int.MaxValue)
                throw new ArgumentOutOfRangeException(
                    nameof(rowIndex), "rowIndex must be less than int.MaxValue");

            double offset = ComputeScrollOffsetWithMinimalScroll(
                VerticalOffset, VerticalOffset + ViewportHeight,
                rowIndex * RowHeight, (rowIndex + 1) * RowHeight);

            if (offset != VerticalOffset)
                VerticalOffset = offset;

            var grid = this.FindAncestor<VirtualizedDataGrid>();
            if (grid != null && (grid.IsKeyboardFocused || grid.IsKeyboardFocusWithin))
                BringRowIntoView(rowIndex);
        }

        internal Rect? TryGetViewportBoundsFromRow(int rowIndex)
        {
            VerifyAccess();
            if (rowIndex < 0)
                return null;

            double rowHeight = RowHeight;
            double left = 0;
            double top = (rowIndex * rowHeight) - VerticalOffset;
            return new Rect(left, top, ActualWidth, rowHeight);
        }

        private void BringRowIntoView(int rowIndex)
        {
            VerifyAccess();
            Rect? bounds = TryGetViewportBoundsFromRow(rowIndex);
            if (bounds.HasValue)
                BringIntoView(bounds.Value);
        }

        private Typeface CreateTypeFace()
        {
            if (FontFamily == null)
                return null;
            return new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        }

        private Pen CreateHorizontalGridLinesPen()
        {
            if (HorizontalGridLinesBrush == null ||
                !HorizontalGridLinesThickness.GreaterThan(0))
                return null;

            var pen = new Pen(HorizontalGridLinesBrush, HorizontalGridLinesThickness);
            pen.Freeze();
            return pen;
        }

        private Pen CreateVerticalGridLinesPen()
        {
            if (VerticalGridLinesBrush == null ||
                !VerticalGridLinesThickness.GreaterThan(0))
                return null;

            var pen = new Pen(VerticalGridLinesBrush, VerticalGridLinesThickness);
            pen.Freeze();
            return pen;
        }

        private Pen CreateSelectionBorderPen()
        {
            if (SelectionBorderBrush == null ||
                !SelectionBorderThickness.GreaterThan(0))
                return null;

            var pen = new Pen(SelectionBorderBrush, SelectionBorderThickness);
            pen.Freeze();
            return pen;
        }

        private Pen CreateInactiveSelectionBorderPen()
        {
            if (InactiveSelectionBorderBrush == null ||
                !SelectionBorderThickness.GreaterThan(0))
                return null;

            var pen = new Pen(InactiveSelectionBorderBrush, SelectionBorderThickness);
            pen.Freeze();
            return pen;
        }

        private Pen CreateFocusBorderPen()
        {
            if (FocusBorderBrush == null || !FocusBorderThickness.GreaterThan(0))
                return null;

            var pen = new Pen(FocusBorderBrush, FocusBorderThickness) {
                DashStyle = DashStyles.Dash
            };
            pen.Freeze();
            return pen;
        }

        internal void OnIsSelectionActiveChanged()
        {
            if (ViewModel == null || ViewModel.RowSelection.Count == 0)
                return;

            PostUpdateRendering();
        }
    }
}
