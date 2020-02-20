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
    using EventTraceKit.VsExtension.Utilities;
    using Extensions;
    using Windows;

    public class AsyncDataGridCellsPresenter : FrameworkElement, IScrollInfo
    {
        private readonly AsyncDataGridRenderedCellsVisual renderedCellsVisual;
        private readonly ValueCache<Typeface> typefaceCache;
        private readonly ValueCache<Pen> horizontalGridLinesPenCache;
        private readonly ValueCache<Pen> verticalGridLinesPenCache;
        private readonly ValueCache<Pen> selectionBorderPenCache;
        private readonly ValueCache<Pen> inactiveSelectionBorderPenCache;
        private readonly ValueCache<Pen> focusBorderPenCache;

        private readonly QueuedDispatcherAction<int> focusIndexUpdate;

        private readonly Action<bool> renderAction;
        private DispatcherOperation renderOperation;
        private bool renderNeeded;
        private bool queuedRender;
        private bool queueRenderWhenVisible;

        private Point computedOffset;
        private DragSelectionContext dragSelectionCtx;

        public AsyncDataGridCellsPresenter()
        {
            FocusVisualStyle = null;
            RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);

            typefaceCache = new ValueCache<Typeface>(CreateTypeFace);
            horizontalGridLinesPenCache = new ValueCache<Pen>(
                CreateHorizontalGridLinesPen);
            verticalGridLinesPenCache = new ValueCache<Pen>(
                CreateVerticalGridLinesPen);
            selectionBorderPenCache = new ValueCache<Pen>(
                CreateSelectionBorderPen);
            inactiveSelectionBorderPenCache = new ValueCache<Pen>(
                CreateInactiveSelectionBorderPen);
            focusBorderPenCache = new ValueCache<Pen>(CreateFocusBorderPen);

            focusIndexUpdate = new QueuedDispatcherAction<int>(Dispatcher, EnsureVisible);

            renderedCellsVisual = new AsyncDataGridRenderedCellsVisual(this);
            AddVisualChild(renderedCellsVisual);

            CoerceValue(FontSizeProperty);
            CoerceValue(RowHeightProperty);

            SizeChanged += OnSizeChanged;
            IsVisibleChanged += OnIsVisibleChanged;

            queueRenderWhenVisible = true;
            renderAction = PerformQueuedRender;
        }

        #region public AsyncDataGridCellsPresenterViewModel ViewModel { get; set; }

        public static readonly DependencyProperty ViewModelProperty =
                DependencyProperty.Register(
                    nameof(ViewModel),
                    typeof(AsyncDataGridCellsPresenterViewModel),
                    typeof(AsyncDataGridCellsPresenter),
                    new FrameworkPropertyMetadata(
                        null,
                        FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                        FrameworkPropertyMetadataOptions.AffectsRender,
                        (s, e) => ((AsyncDataGridCellsPresenter)s).OnViewModelChanged(e),
                        null));

        public AsyncDataGridCellsPresenterViewModel ViewModel
        {
            get => (AsyncDataGridCellsPresenterViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        private void OnViewModelChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null) {
                var oldValue = (AsyncDataGridCellsPresenterViewModel)e.OldValue;
                oldValue.FocusIndexChanged -= OnViewModelFocusIndexChanged;
            }

            if (e.NewValue != null) {
                var newValue = (AsyncDataGridCellsPresenterViewModel)e.NewValue;
                newValue.FocusIndexChanged += OnViewModelFocusIndexChanged;
            }
        }

        #endregion

        #region public ReadOnlyObservableCollection<AsyncDataGridColumnViewModel> VisibleColumns { get; set; }

        /// <summary>
        ///   Identifies the <see cref="VisibleColumns"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VisibleColumnsProperty =
            DependencyProperty.Register(
                nameof(VisibleColumns),
                typeof(ReadOnlyObservableCollection<AsyncDataGridColumn>),
                typeof(AsyncDataGridCellsPresenter),
                new PropertyMetadata(
                    CollectionDefaults<AsyncDataGridColumn>.ReadOnlyObservable));

        /// <summary>
        ///   Gets or sets the visible columns.
        /// </summary>
        public ReadOnlyObservableCollection<AsyncDataGridColumn> VisibleColumns
        {
            get => (ReadOnlyObservableCollection<AsyncDataGridColumn>)GetValue(
                VisibleColumnsProperty);
            set => SetValue(VisibleColumnsProperty, value);
        }

        #endregion

        #region public double RowHeight { get; set; }

        public static readonly DependencyProperty RowHeightProperty =
            DependencyProperty.Register(
                nameof(RowHeight),
                typeof(double),
                typeof(AsyncDataGridCellsPresenter),
                new PropertyMetadata(
                    1.0,
                    (d, e) => ((AsyncDataGridCellsPresenter)d).OnRowHeightChanged(
                        (double)e.OldValue, (double)e.NewValue),
                    (d, v) => ((AsyncDataGridCellsPresenter)d).CoerceRowHeight((double)v)));

        [TypeConverter(typeof(LengthConverter))]
        public double RowHeight
        {
            get => (double)GetValue(RowHeightProperty);
            set => SetValue(RowHeightProperty, value);
        }

        private double CoerceRowHeight(double baseValue)
        {
            double rowHeight = DoubleUtils.Max(FontSize * 1.2, 12.0, baseValue);
            return Math.Ceiling(rowHeight);
        }

        private void OnRowHeightChanged(double oldValue, double newValue)
        {
            if (!DoubleUtils.AreClose(oldValue, newValue)) {
                InvalidateRowCache();
                InvalidateVisual();
            }
        }

        #endregion

        #region public bool AutoScroll { get; set; }

        /// <summary>
        ///   Identifies the <see cref="AutoScroll"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.Register(
                nameof(AutoScroll),
                typeof(bool),
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    (d, e) => ((AsyncDataGridCellsPresenter)d).OnAutoScrollChanged(
                        (bool)e.OldValue, (bool)e.NewValue)));

        /// <summary>
        ///   Gets or sets whether the view is auto-scrolling to the bottom.
        /// </summary>
        public bool AutoScroll
        {
            get => (bool)GetValue(AutoScrollProperty);
            set => SetValue(AutoScrollProperty, value);
        }

        private void OnAutoScrollChanged(bool oldValue, bool newValue)
        {
            if (newValue)
                ScrollToEnd();
        }

        #endregion

        #region public FontFamily FontFamily { get; set; }

        public static readonly DependencyProperty FontFamilyProperty =
            TextElement.FontFamilyProperty.AddOwner(
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemFonts.MessageFontFamily,
                    FrameworkPropertyMetadataOptions.Inherits,
                    ClearTypefaceCache));

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        #endregion

        #region public double FontSize { get; set; }

        public static readonly DependencyProperty FontSizeProperty =
            TextElement.FontSizeProperty.AddOwner(
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemFonts.MessageFontSize,
                    FrameworkPropertyMetadataOptions.Inherits,
                    OnFontSizeChanged));

        [TypeConverter(typeof(FontSizeConverter))]
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        private static void OnFontSizeChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (AsyncDataGridCellsPresenter)d;
            source.CoerceValue(RowHeightProperty);
            source.typefaceCache.Clear();
        }

        #endregion

        #region public FontStretch FontStretch { get; set; }

        public static readonly DependencyProperty FontStretchProperty =
            TextElement.FontStretchProperty.AddOwner(
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    TextElement.FontStretchProperty.DefaultMetadata.DefaultValue,
                    FrameworkPropertyMetadataOptions.Inherits,
                    ClearTypefaceCache));

        public FontStretch FontStretch
        {
            get => (FontStretch)GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        #endregion

        #region public FontStyle FontStyle { get; set; }

        public static readonly DependencyProperty FontStyleProperty =
            TextElement.FontStyleProperty.AddOwner(
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemFonts.MessageFontStyle,
                    FrameworkPropertyMetadataOptions.Inherits,
                    ClearTypefaceCache));

        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        #endregion

        #region public FontWeight FontWeight { get; set; }

        public static readonly DependencyProperty FontWeightProperty =
            TextElement.FontWeightProperty.AddOwner(
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemFonts.MessageFontWeight,
                    FrameworkPropertyMetadataOptions.Inherits,
                    ClearTypefaceCache));

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        #endregion

        #region public Brush Foreground { get; set; }

        public static readonly DependencyProperty ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner(
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.ControlTextBrush,
                    FrameworkPropertyMetadataOptions.Inherits));

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Brushes.White,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the primary background.
        /// </summary>
        public Brush PrimaryBackground
        {
            get => (Brush)GetValue(PrimaryBackgroundProperty);
            set => SetValue(PrimaryBackgroundProperty, value);
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Brushes.WhiteSmoke,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the secondary background.
        /// </summary>
        public Brush SecondaryBackground
        {
            get => (Brush)GetValue(SecondaryBackgroundProperty);
            set => SetValue(SecondaryBackgroundProperty, value);
        }

        #endregion

        #region public Visibility HorizontalScrollVisibility { get; internal set; }

        private static readonly DependencyPropertyKey HorizontalScrollVisibilityPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(HorizontalScrollVisibility),
                typeof(Visibility),
                typeof(AsyncDataGridCellsPresenter),
                new PropertyMetadata(Visibility.Hidden));

        public static readonly DependencyProperty HorizontalScrollVisibilityProperty =
            HorizontalScrollVisibilityPropertyKey.DependencyProperty;

        public Visibility HorizontalScrollVisibility
        {
            get => (Visibility)GetValue(HorizontalScrollVisibilityProperty);
            internal set => SetValue(HorizontalScrollVisibilityPropertyKey, value);
        }

        #endregion

        #region public Visibility VerticalScrollVisibility { get; internal set; }

        private static readonly DependencyPropertyKey VerticalScrollVisibilityPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(VerticalScrollVisibility),
                typeof(Visibility),
                typeof(AsyncDataGridCellsPresenter),
                new PropertyMetadata(Visibility.Hidden));

        public static readonly DependencyProperty VerticalScrollVisibilityProperty =
            VerticalScrollVisibilityPropertyKey.DependencyProperty;

        public Visibility VerticalScrollVisibility
        {
            get => (Visibility)GetValue(VerticalScrollVisibilityProperty);
            internal set => SetValue(VerticalScrollVisibilityPropertyKey, value);
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Brushes.Silver,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    ClearHorizontalGridLinesPen));

        public Brush HorizontalGridLinesBrush
        {
            get => (Brush)GetValue(HorizontalGridLinesBrushProperty);
            set => SetValue(HorizontalGridLinesBrushProperty, value);
        }

        private static void ClearHorizontalGridLinesPen(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (AsyncDataGridCellsPresenter)d;
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    ClearHorizontalGridLinesPen));

        public double HorizontalGridLinesThickness
        {
            get => (double)GetValue(HorizontalGridLinesThicknessProperty);
            set => SetValue(HorizontalGridLinesThicknessProperty, value);
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Brushes.Silver,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    ClearVerticalGridLinesPen));

        public Brush VerticalGridLinesBrush
        {
            get => (Brush)GetValue(VerticalGridLinesBrushProperty);
            set => SetValue(VerticalGridLinesBrushProperty, value);
        }

        private static void ClearVerticalGridLinesPen(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (AsyncDataGridCellsPresenter)d;
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    ClearVerticalGridLinesPen));

        public double VerticalGridLinesThickness
        {
            get => (double)GetValue(VerticalGridLinesThicknessProperty);
            set => SetValue(VerticalGridLinesThicknessProperty, value);
        }

        #endregion

        #region public Brush KeySeparatorBrush { get; set; }

        /// <summary>
        ///   Identifies the <see cref="KeySeparatorBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty KeySeparatorBrushProperty =
            DependencyProperty.Register(
                nameof(KeySeparatorBrush),
                typeof(Brush),
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Brushes.Gold,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the key separator brush.
        /// </summary>
        public Brush KeySeparatorBrush
        {
            get => (Brush)GetValue(KeySeparatorBrushProperty);
            set => SetValue(KeySeparatorBrushProperty, value);
        }

        #endregion

        #region public Brush FrozenColumnBackground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="FrozenColumnBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FrozenColumnBackgroundProperty =
            DependencyProperty.Register(
                nameof(FrozenColumnBackground),
                typeof(Brush),
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.ControlBrush,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the frozen column background.
        /// </summary>
        public Brush FrozenColumnBackground
        {
            get => (Brush)GetValue(FrozenColumnBackgroundProperty);
            set => SetValue(FrozenColumnBackgroundProperty, value);
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Brushes.Gray,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the freezable area separator brush.
        /// </summary>
        public Brush FreezableAreaSeparatorBrush
        {
            get => (Brush)GetValue(FreezableAreaSeparatorBrushProperty);
            set => SetValue(FreezableAreaSeparatorBrushProperty, value);
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.HighlightTextBrush,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the selection foreground brush.
        /// </summary>
        public Brush SelectionForeground
        {
            get => (Brush)GetValue(SelectionForegroundProperty);
            set => SetValue(SelectionForegroundProperty, value);
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.HighlightBrush,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the selection background brush.
        /// </summary>
        public Brush SelectionBackground
        {
            get => (Brush)GetValue(SelectionBackgroundProperty);
            set => SetValue(SelectionBackgroundProperty, value);
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
                typeof(AsyncDataGridCellsPresenter),
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
            get => (Brush)GetValue(SelectionBorderBrushProperty);
            set => SetValue(SelectionBorderBrushProperty, value);
        }

        private static void ClearSelectionBorderPenCache(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (AsyncDataGridCellsPresenter)d;
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
                typeof(AsyncDataGridCellsPresenter),
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
            get => (double)GetValue(SelectionBorderThicknessProperty);
            set => SetValue(SelectionBorderThicknessProperty, value);
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.InactiveSelectionHighlightTextBrush,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the selection foreground brush.
        /// </summary>
        public Brush InactiveSelectionForeground
        {
            get => (Brush)GetValue(InactiveSelectionForegroundProperty);
            set => SetValue(InactiveSelectionForegroundProperty, value);
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemColors.InactiveSelectionHighlightBrush,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///   Gets or sets the selection background brush.
        /// </summary>
        public Brush InactiveSelectionBackground
        {
            get => (Brush)GetValue(InactiveSelectionBackgroundProperty);
            set => SetValue(InactiveSelectionBackgroundProperty, value);
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
                typeof(AsyncDataGridCellsPresenter),
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
            get => (Brush)GetValue(InactiveSelectionBorderBrushProperty);
            set => SetValue(InactiveSelectionBorderBrushProperty, value);
        }

        private static void ClearInactiveSelectionBorderPenCache(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (AsyncDataGridCellsPresenter)d;
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
                typeof(AsyncDataGridCellsPresenter),
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
            get => (Brush)GetValue(FocusBorderBrushProperty);
            set => SetValue(FocusBorderBrushProperty, value);
        }

        private static void ClearFocusBorderPenCache(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (AsyncDataGridCellsPresenter)d;
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
                typeof(AsyncDataGridCellsPresenter),
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
            get => (double)GetValue(FocusBorderThicknessProperty);
            set => SetValue(FocusBorderThicknessProperty, value);
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
                return renderedCellsVisual;
            return base.GetVisualChild(index - 1);
        }

        #region IScrollInfo

        #region public ScrollViewer ScrollOwner { get; set; }

        public static readonly DependencyProperty ScrollOwnerProperty =
            DependencyProperty.Register(
                nameof(ScrollOwner),
                typeof(ScrollViewer),
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(OnScrollOwnerChanged));

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ScrollViewer ScrollOwner
        {
            get => (ScrollViewer)GetValue(ScrollOwnerProperty);
            set => SetValue(ScrollOwnerProperty, value);
        }

        private static void OnScrollOwnerChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (AsyncDataGridCellsPresenter)d;
            var oldValue = (ScrollViewer)e.OldValue;
            var newValue = (ScrollViewer)e.NewValue;
            if (oldValue != null)
                oldValue.ScrollChanged -= source.OnScrollOwnerScrollChanged;
            if (newValue != null)
                newValue.ScrollChanged += source.OnScrollOwnerScrollChanged;
        }

        private void OnScrollOwnerScrollChanged(object sender, ScrollChangedEventArgs args)
        {
            // Ignore any extend changes so autoscrolling stays enabled when
            // resizing or clearing.
            if (args.ViewportHeightChange != 0 || args.ExtentHeightChange != 0)
                return;

            if (args.VerticalChange < 0) {
                AutoScroll = false;
            }
            //else if (args.VerticalChange > 0) {
            //    const double autoScrollThreshold = 5.0;
            //    double bottomEdge = args.VerticalOffset + args.ViewportHeight;
            //    if (Math.Abs(bottomEdge - args.ExtentHeight) < autoScrollThreshold)
            //        AutoScroll = true;
            //}
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(Boxed.False));

        public bool CanHorizontallyScroll
        {
            get => (bool)GetValue(CanHorizontallyScrollProperty);
            set => SetValue(CanHorizontallyScrollProperty, value);
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
                typeof(AsyncDataGridCellsPresenter),
                new PropertyMetadata(Boxed.False));

        public bool CanVerticallyScroll
        {
            get => (bool)GetValue(CanVerticallyScrollProperty);
            set => SetValue(CanVerticallyScrollProperty, value);
        }

        #endregion

        #region public double ExtentWidth { get; private set; }

        public static readonly DependencyProperty ExtentWidthProperty =
            DependencyProperty.Register(
                nameof(ExtentWidth),
                typeof(double),
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Boxed.DoubleZero,
                    (d, e) => ((AsyncDataGridCellsPresenter)d).OnExtentWidthChanged(e)));

        public double ExtentWidth
        {
            get => (double)GetValue(ExtentWidthProperty);
            private set => SetValue(ExtentWidthProperty, value);
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Boxed.DoubleZero,
                    (d, e) => ((AsyncDataGridCellsPresenter)d).OnExtentHeightChanged(e)));

        public double ExtentHeight
        {
            get => (double)GetValue(ExtentHeightProperty);
            private set => SetValue(ExtentHeightProperty, value);
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
                nameof(ViewportWidth),
                typeof(double),
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Boxed.DoubleZero,
                    (d, e) => ((AsyncDataGridCellsPresenter)d).OnViewportWidthChanged(e)));

        public double ViewportWidth
        {
            get => (double)GetValue(ViewportWidthProperty);
            private set => SetValue(ViewportWidthProperty, value);
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Boxed.DoubleZero,
                    (d, e) => ((AsyncDataGridCellsPresenter)d).OnViewportHeightChanged(e)));

        public double ViewportHeight
        {
            get => (double)GetValue(ViewportHeightProperty);
            private set => SetValue(ViewportHeightProperty, value);
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
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Boxed.DoubleZero,
                    (d, e) => ((AsyncDataGridCellsPresenter)d).OnHorizontalOffsetChanged(e),
                    (d, v) => ((AsyncDataGridCellsPresenter)d).CoerceHorizontalOffset(v)));

        public double HorizontalOffset
        {
            get => (double)GetValue(HorizontalOffsetProperty);
            private set => SetValue(HorizontalOffsetProperty, value);
        }

        private object CoerceHorizontalOffset(object baseValue)
        {
            var max = Math.Max(0.0, ExtentWidth - ViewportWidth);
            var offset = ((double)baseValue).SafeClamp(0, max);
            return Math.Ceiling(offset);
        }

        private void OnHorizontalOffsetChanged(DependencyPropertyChangedEventArgs e)
        {
            // Update immediately to synchronize scrolling with the column
            // headers. Otherwise there is a small but noticable delay.
            PerformRender(true);
        }

        #endregion

        #region public double VerticalOffset { get; private set; }

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.Register(
                nameof(VerticalOffset),
                typeof(double),
                typeof(AsyncDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    Boxed.DoubleZero,
                    (d, e) => ((AsyncDataGridCellsPresenter)d).OnVerticalOffsetChanged(e),
                    (d, v) => ((AsyncDataGridCellsPresenter)d).CoerceVerticalOffset(v)));

        public double VerticalOffset
        {
            get => (double)GetValue(VerticalOffsetProperty);
            private set => SetValue(VerticalOffsetProperty, value);
        }

        private object CoerceVerticalOffset(object baseValue)
        {
            var max = Math.Max(0, ExtentHeight - ViewportHeight);
            var offset = ((double)baseValue).Clamp(0, max);
            return Math.Ceiling(offset);
        }

        private void OnVerticalOffsetChanged(DependencyPropertyChangedEventArgs e)
        {
            QueueRender(true);
        }

        #endregion

        public void LineUp()
        {
            VerticalOffset -= RowHeight;
            AutoScroll = false;
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
            AutoScroll = false;
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
            if (Keyboard.IsKeyDown(Key.LeftShift) | Keyboard.IsKeyDown(Key.RightShift)) {
                MouseWheelLeft();
                return;
            }

            VerticalOffset -= RowHeight * SystemParameters.WheelScrollLines;
            AutoScroll = false;
        }

        public void MouseWheelDown()
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) | Keyboard.IsKeyDown(Key.RightShift)) {
                MouseWheelRight();
                return;
            }

            VerticalOffset += RowHeight * SystemParameters.WheelScrollLines;
            AutoScroll = false;
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

        public AsyncDataGridColumn GetColumnFromPosition(double x)
        {
            // Check right-frozen columns.
            var rx = ViewportWidth;
            int r = VisibleColumns.Count;
            for (; r > 0; --r) {
                var column = VisibleColumns[r - 1];
                if (!column.IsFrozen)
                    break;

                if (x >= rx - column.Width)
                    return column;

                rx -= column.Width;
            }

            // Check left-frozen columns.
            int l = 0;
            for (; l < r; ++l) {
                var column = VisibleColumns[l];
                if (!column.IsFrozen)
                    break;

                if (x <= column.Width)
                    return column;

                x -= column.Width;
            }

            // Check unfrozen columns.
            x += HorizontalOffset;
            for (int i = l; i < r; ++i) {
                var column = VisibleColumns[i];
                if (x <= column.Width)
                    return column;

                x -= column.Width;
            }

            return null;
        }

        public void InvalidateRowCache()
        {
            renderedCellsVisual.InvalidateRowCache();
            QueueRender(true);
        }

        public void QueueRender(bool forceUpdate = true)
        {
            renderNeeded = true;
            if (queuedRender)
                return;

            if (!IsVisible) {
                queueRenderWhenVisible = false;
                return;
            }

            queuedRender = true;
            renderOperation?.Abort();
            renderOperation = Dispatcher.BeginInvoke(
                renderAction, DispatcherPriority.Render, forceUpdate);
        }

        private void PerformQueuedRender(bool forceUpdate)
        {
            queuedRender = false;
            if (renderNeeded && IsVisible)
                PerformRender(forceUpdate);
        }

        public void PerformRender(bool forceUpdate)
        {
            renderNeeded = false;
            if (ViewModel == null || !ViewModel.IsReady)
                return;

            UpdateScrollInfo();

            renderedCellsVisual.Update(
                new Rect(HorizontalOffset, VerticalOffset, ViewportWidth, ViewportHeight),
                new Size(ExtentWidth, ExtentHeight),
                forceUpdate);
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            QueueRender(true);
        }

        private void OnViewModelFocusIndexChanged(object sender, EventArgs e)
        {
            if (ViewModel != null)
                focusIndexUpdate.Queue(ViewModel.FocusIndex);
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
            var source = (AsyncDataGridCellsPresenter)d;
            source.typefaceCache.Clear();
            source.renderedCellsVisual.InvalidateRowCache();
        }

        private void VerifyScrollData(Size newViewportSize, Size newExtentSize)
        {
            if (double.IsInfinity(newViewportSize.Width))
                newViewportSize.Width = newExtentSize.Width;
            if (double.IsInfinity(newViewportSize.Height))
                newViewportSize.Height = newExtentSize.Height;

            bool similar = true;
            similar &= SizeUtils.AreClose(newViewportSize, new Size(ViewportWidth, ViewportHeight));
            similar &= SizeUtils.AreClose(newExtentSize, new Size(ExtentWidth, ExtentHeight));

            ViewportWidth = newViewportSize.Width;
            ViewportHeight = newViewportSize.Height;
            ExtentWidth = newExtentSize.Width;
            ExtentHeight = newExtentSize.Height;

            if (AutoScroll) {
                var newVerticalOffset = ExtentHeight - ViewportHeight;
                similar &= DoubleUtils.AreClose(newVerticalOffset, VerticalOffset);
                VerticalOffset = newVerticalOffset;
            }

            similar &= CoerceOffsets();

            if (!similar)
                ScrollOwner?.InvalidateScrollInfo();
        }

        private bool CoerceOffsets()
        {
            var point = new Point(
                CoerceOffset(HorizontalOffset, ExtentWidth, ViewportWidth),
                CoerceOffset(VerticalOffset, ExtentHeight, ViewportHeight));

            bool similar = PointUtils.AreClose(computedOffset, point);
            computedOffset = point;
            return similar;
        }

        private static double CoerceOffset(double offset, double extent, double viewport)
        {
            offset = Math.Min(offset, extent - viewport);
            return Math.Max(offset, 0);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // Make the presenter hit-testable so any bubbling event reaching
            // the grid has the presenter as original source. Otherwise clicking
            // the empty space between any rows starts at the parent scroll
            // viewer.
            drawingContext.DrawRectangle(
                Brushes.Transparent, null, new Rect(RenderSize));

            QueueRender(true);
        }

        private void OnIsVisibleChanged(
            object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && queueRenderWhenVisible)
                QueueRender(true);
        }

        internal double GetColumnAutoSize(AsyncDataGridColumn column)
        {
            return renderedCellsVisual.GetColumnAutoSize(column);
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
            base.OnMouseLeftButtonDown(e);
            var viewModel = ViewModel;
            if (viewModel == null || e.Handled)
                return;

            Keyboard.Focus(this);

            Point position = e.GetPosition(this);
            int? row = GetRowFromPosition(position.Y);
            if (row != null) {
                int rowIndex = row.Value;
                EnsureVisible(rowIndex);

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
                    viewModel.RequestUpdate();

                BeginDragging(rowIndex);
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (dragSelectionCtx.IsDragging)
                EndDragging();
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            if (dragSelectionCtx.IsDragging)
                CancelDragging();
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (!e.Handled) {
                Focus();

                var viewModel = ViewModel;
                Point position = e.GetPosition(this);
                int? row = GetRowFromPosition(position.Y);
                if (row.HasValue) {
                    int rowIndex = row.Value;
                    EnsureVisible(rowIndex);

                    if (!viewModel.RowSelection.Contains(rowIndex)) {
                        viewModel.RowSelection.ToggleSingle(rowIndex);
                        viewModel.RequestUpdate();
                    }
                } else {
                    viewModel.RowSelection.Clear();
                    viewModel.RequestUpdate();
                }

                e.Handled = true;
            }

            base.OnMouseRightButtonDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null || !dragSelectionCtx.IsDragging)
                return;

            Point position = e.GetPosition(this);
            int? row = GetRowFromPosition(position.Y);
            if (row != null && row != dragSelectionCtx.LastTargetedRow) {
                int rowIndex = row.Value;
                EnsureVisible(rowIndex);

                int min = Math.Min(dragSelectionCtx.AnchorRow, rowIndex);
                int max = Math.Max(dragSelectionCtx.AnchorRow, rowIndex);
                viewModel.RowSelection.RestoreSnapshot(dragSelectionCtx.SelectionSnapshot);
                viewModel.RowSelection.SetRange(min, max, dragSelectionCtx.SelectionRemoves);
                viewModel.RequestUpdate();
            }

            dragSelectionCtx.LastTargetedRow = row;
        }

        private struct DragSelectionContext
        {
            public bool IsDragging;
            public MultiRange SelectionSnapshot;
            public bool SelectionRemoves;
            public int AnchorRow;

            public int? LastTargetedRow;
        }

        private void BeginDragging(int rowIndex)
        {
            if (Mouse.Capture(this, CaptureMode.SubTree)) {
                dragSelectionCtx.IsDragging = true;
                dragSelectionCtx.SelectionSnapshot = ViewModel.RowSelection.GetSnapshot();
                dragSelectionCtx.SelectionRemoves = ViewModel.RowSelection.Contains(rowIndex);
                dragSelectionCtx.AnchorRow = rowIndex;
            }
        }

        private void EndDragging()
        {
            dragSelectionCtx = new DragSelectionContext();
            if (IsMouseCaptured)
                ReleaseMouseCapture();
        }

        private void CancelDragging()
        {
            ViewModel.RowSelection.RestoreSnapshot(dragSelectionCtx.SelectionSnapshot);
            EndDragging();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel != null && !e.Handled &&
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
                            viewModel.RowSelection.SelectAll();
                            e.Handled = true;
                        }
                        break;
                }

                if (e.Handled)
                    viewModel.RequestUpdate();
            }

            if (!ViewModel.IsReady)
                e.Handled = true;

            base.OnKeyDown(e);
        }

        private void OnSpaceKeyDown(KeyEventArgs e)
        {
            var viewModel = ViewModel;
            var rowSelection = viewModel.RowSelection;

            switch (e.KeyboardDevice.Modifiers) {
                case ModifierKeys.None:
                    rowSelection.ToggleSingle(viewModel.FocusIndex);
                    break;
                case ModifierKeys.Shift:
                    rowSelection.ToggleExtent(viewModel.FocusIndex);
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
            var rowSelection = viewModel.RowSelection;
            int firstRow = 0;
            int lastRow = viewModel.RowCount - 1;

            switch (e.Key) {
                case Key.Home:
                    AutoScroll = false;
                    switch (e.KeyboardDevice.Modifiers) {
                        case ModifierKeys.None:
                            rowSelection.ToggleSingle(firstRow);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift:
                            rowSelection.ToggleExtent(firstRow);
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
                            rowSelection.ToggleSingle(lastRow, false);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift:
                            rowSelection.ToggleExtent(lastRow, false);
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
            var rowSelection = viewModel.RowSelection;
            int firstRow = 0;
            int lastRow = viewModel.RowCount - 1;

            int visibleRowCount = (int)Math.Floor(ViewportHeight / RowHeight);
            int prevPageRow = Math.Max(viewModel.FocusIndex - visibleRowCount, firstRow);
            int nextPageRow = Math.Min(viewModel.FocusIndex + visibleRowCount, lastRow);

            switch (e.Key) {
                case Key.Prior:
                    AutoScroll = false;
                    switch (e.KeyboardDevice.Modifiers) {
                        case ModifierKeys.None:
                            rowSelection.ToggleSingle(prevPageRow, false);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift:
                            rowSelection.ToggleExtent(prevPageRow, false);
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
                            rowSelection.ToggleSingle(nextPageRow, false);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift:
                            rowSelection.ToggleExtent(nextPageRow, false);
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
            var rowSelection = viewModel.RowSelection;

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
                    AutoScroll = false;
                    switch (e.KeyboardDevice.Modifiers) {
                        case ModifierKeys.None:
                            rowSelection.ToggleSingle(prevRow, false);
                            BringRowIntoView(prevRow);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift:
                            rowSelection.ToggleExtent(prevRow, false);
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
                            rowSelection.ToggleSingle(nextRow, false);
                            BringRowIntoView(nextRow);
                            e.Handled = true;
                            break;
                        case ModifierKeys.Shift:
                            rowSelection.ToggleExtent(nextRow, false);
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

        internal void OnIsSelectionActiveChanged()
        {
            if (ViewModel == null)
                return;

            QueueRender(true);
        }

        private void ScrollToEnd()
        {
            if (ViewModel != null)
                EnsureVisible(ViewModel.RowCount);
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

            var grid = this.FindAncestor<AsyncDataGrid>();
            if (grid != null && (grid.IsKeyboardFocused || grid.IsKeyboardFocusWithin))
                BringRowIntoView(rowIndex);
        }

        internal Rect? TryGetViewportBoundsForRow(int rowIndex)
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
            Rect? bounds = TryGetViewportBoundsForRow(rowIndex);
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
    }
}
