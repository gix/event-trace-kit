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

    public class VirtualizedDataGridCellsPresenter : Canvas, IScrollInfo
    {
        private readonly VirtualizedDataGridCellsDrawingVisual cellsDrawingVisual;
        private readonly ValueCache<Typeface> typefaceCache;
        private readonly ValueCache<Pen> horizontalGridLinesPenCache;
        private readonly ValueCache<Pen> verticalGridLinesPenCache;

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

        #region public IVirtualizedDataGridCellsPresenterViewModel ViewModel { get; set; }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel),
            typeof(IVirtualizedDataGridCellsPresenterViewModel),
            typeof(VirtualizedDataGridCellsPresenter),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                FrameworkPropertyMetadataOptions.AffectsRender,
                (s, e) => ((VirtualizedDataGridCellsPresenter)s).ViewModelPropertyChanged(e),
                null));

        public IVirtualizedDataGridCellsPresenterViewModel ViewModel
        {
            get { return (IVirtualizedDataGridCellsPresenterViewModel)GetValue(ViewModelProperty); }
            set { SetValue((DependencyProperty)ViewModelProperty, value); }
        }

        private void ViewModelPropertyChanged(DependencyPropertyChangedEventArgs e)
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
                typeof(ReadOnlyObservableCollection<VirtualizedDataGridColumnViewModel>),
                typeof(VirtualizedDataGridCellsPresenter),
                new PropertyMetadata(
                    CollectionDefaults<VirtualizedDataGridColumnViewModel>.ReadOnlyObservable));

        /// <summary>
        ///   Gets or sets the visible columns.
        /// </summary>
        public ReadOnlyObservableCollection<VirtualizedDataGridColumnViewModel> VisibleColumns
        {
            get { return (ReadOnlyObservableCollection<VirtualizedDataGridColumnViewModel>)GetValue(VisibleColumnsProperty); }
            set { SetValue(VisibleColumnsProperty, value); }
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
                    (d, e) => ((VirtualizedDataGridCellsPresenter)d).OnExtentWidthPropertyChanged(e)));

        public double ExtentWidth
        {
            get { return (double)GetValue(ExtentWidthProperty); }
            private set { SetValue(ExtentWidthProperty, value); }
        }

        private void OnExtentWidthPropertyChanged(DependencyPropertyChangedEventArgs e)
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
                    (d, e) => ((VirtualizedDataGridCellsPresenter)d).OnExtentHeightPropertyChanged(e)));

        public double ExtentHeight
        {
            get { return (double)GetValue(ExtentHeightProperty); }
            private set { SetValue(ExtentHeightProperty, value); }
        }

        private void OnExtentHeightPropertyChanged(
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
                    (d, e) => ((VirtualizedDataGridCellsPresenter)d).OnViewportWidthPropertyChanged(e)));

        public double ViewportWidth
        {
            get { return (double)GetValue(ViewportWidthProperty); }
            private set { SetValue(ViewportWidthProperty, value); }
        }

        private void OnViewportWidthPropertyChanged(
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
                    (d, e) => ((VirtualizedDataGridCellsPresenter)d).OnViewportHeightPropertyChanged(e)));

        public double ViewportHeight
        {
            get { return (double)GetValue(ViewportHeightProperty); }
            private set { SetValue(ViewportHeightProperty, value); }
        }

        private void OnViewportHeightPropertyChanged(
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
                    (d, e) => ((VirtualizedDataGridCellsPresenter)d).OnHorizontalOffsetPropertyChanged(e),
                    (d, v) => ((VirtualizedDataGridCellsPresenter)d).CoerceHorizontalOffsetProperty(v)),
                ValidateValueCallbacks.IsFiniteDouble);

        public double HorizontalOffset
        {
            get { return (double)GetValue(HorizontalOffsetProperty); }
            private set { SetValue(HorizontalOffsetProperty, value); }
        }

        private object CoerceHorizontalOffsetProperty(object baseValue)
        {
            return DoubleUtils.SafeClamp(
                (double)baseValue, 0.0, Math.Max(0.0, ExtentWidth - ViewportWidth));
        }

        private void OnHorizontalOffsetPropertyChanged(
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
                    (d, e) => ((VirtualizedDataGridCellsPresenter)d).OnVerticalOffsetPropertyChanged(e),
                    (d, v) => ((VirtualizedDataGridCellsPresenter)d).CoerceVerticalOffsetProperty(v)),
                ValidateValueCallbacks.IsFiniteDouble);

        public double VerticalOffset
        {
            get { return (double)GetValue(VerticalOffsetProperty); }
            private set { SetValue(VerticalOffsetProperty, value); }
        }

        private void OnVerticalOffsetPropertyChanged(
            DependencyPropertyChangedEventArgs e)
        {
            PostUpdateRendering();
        }

        private object CoerceVerticalOffsetProperty(object baseValue)
        {
            return DoubleUtils.Clamp(
                (double)baseValue, 0.0, Math.Max(0.0, ExtentHeight - ViewportHeight));
        }

        #endregion

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

        #region public double HorizontalGridLinesThickness { get; set; }

        /// <summary>
        ///   Identifies the <see cref="HorizontalGridLinesThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HorizontalGridLinesThicknessProperty =
            DependencyProperty.Register(
                nameof(HorizontalGridLinesThickness),
                typeof(double),
                typeof(VirtualizedDataGridCellsPresenter),
                new PropertyMetadata(Boxed.DoubleZero));

        public double HorizontalGridLinesThickness
        {
            get { return (double)GetValue(HorizontalGridLinesThicknessProperty); }
            set { SetValue(HorizontalGridLinesThicknessProperty, value); }
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
                new PropertyMetadata(Boxed.DoubleZero));

        public double VerticalGridLinesThickness
        {
            get { return (double)GetValue(VerticalGridLinesThicknessProperty); }
            set { SetValue(VerticalGridLinesThicknessProperty, value); }
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
                    OnHorizontalGridLinesBrushChanged));

        public Brush HorizontalGridLinesBrush
        {
            get { return (Brush)GetValue(HorizontalGridLinesBrushProperty); }
            set { SetValue(HorizontalGridLinesBrushProperty, value); }
        }

        private static void OnHorizontalGridLinesBrushChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
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
                    OnVerticalGridLinesBrushChanged));

        public Brush VerticalGridLinesBrush
        {
            get { return (Brush)GetValue(VerticalGridLinesBrushProperty); }
            set { SetValue(VerticalGridLinesBrushProperty, value); }
        }

        private static void OnVerticalGridLinesBrushChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
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

        #region public double RowHeight { get; internal set; }

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
            return DoubleUtils.Max(FontSize * 1.6, 12.0, baseValue);
        }

        #endregion

        #region public FontFamily FontFamily { get; set; }

        public static readonly DependencyProperty FontFamilyProperty =
            TextElement.FontFamilyProperty.AddOwner(
                typeof(VirtualizedDataGridCellsPresenter),
                new FrameworkPropertyMetadata(
                    SystemFonts.MessageFontFamily,
                    FrameworkPropertyMetadataOptions.Inherits,
                    OnClearTypefaceCache));

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
                    OnClearTypefaceCache));

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
                    OnClearTypefaceCache));

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
                    OnClearTypefaceCache));

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
            set { SetValue((DependencyProperty)SeparatorBrushProperty, value); }
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
            set { SetValue((DependencyProperty)FrozenColumnBrushProperty, value); }
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
            set { SetValue((DependencyProperty)FreezableAreaSeparatorBrushProperty, value); }
        }

        #endregion

        protected override int VisualChildrenCount => base.VisualChildrenCount + 1;

        public Typeface Typeface => typefaceCache.Value;
        public Pen HorizontalGridLinesPen => horizontalGridLinesPenCache.Value;
        public Pen VerticalGridLinesPen => verticalGridLinesPenCache.Value;

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
            return new Rect();
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            PostUpdateRendering();
        }

        private void UpdateScrollInfo()
        {
            if (ViewModel != null) {
                double totalWidth = VisibleColumns.Sum(x => x.Width);
                VerifyScrollData(
                    new Size(ViewportWidth, ViewportHeight),
                    new Size(totalWidth, ViewModel.RowCount * RowHeight));
            }

            HorizontalScrollVisibility = ViewportWidth < ExtentWidth ?
                Visibility.Visible : Visibility.Collapsed;

            VerticalScrollVisibility = ViewportHeight < ExtentHeight ?
                Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnClearTypefaceCache(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VirtualizedDataGridCellsPresenter)d).OnClearTypefaceCache();
        }

        private void OnClearTypefaceCache()
        {
            typefaceCache.Clear();
        }

        private void VerifyScrollData(Size viewportSize, Size extentSize)
        {
            if (double.IsInfinity(viewportSize.Width))
                viewportSize.Width = extentSize.Width;
            if (double.IsInfinity(viewportSize.Height))
                viewportSize.Height = extentSize.Height;

            bool flag = true;
            flag &= SizeUtils.AreClose(viewportSize, new Size(ViewportWidth, ViewportHeight));
            flag &= SizeUtils.AreClose(extentSize, new Size(ExtentWidth, ExtentHeight));

            ViewportWidth = viewportSize.Width;
            ViewportHeight = viewportSize.Height;
            ExtentWidth = extentSize.Width;
            ExtentHeight = extentSize.Height;

            //if (this.ViewModel != null)
            //    this.ViewModel.VerticalScrollExtent = extentSize.Height;

            if (!(flag & CoerceOffsets()))
                ScrollOwner?.InvalidateScrollInfo();
        }

        private bool CoerceOffsets()
        {
            Point point = new Point(
                CoerceOffset(HorizontalOffset, ExtentWidth, ViewportWidth),
                CoerceOffset(VerticalOffset, ExtentHeight, ViewportHeight));

            bool flag = PointUtils.AreClose(computedOffset, point);
            computedOffset = point;
            return flag;
        }

        private static double CoerceOffset(double offset, double extent, double viewport)
        {
            offset = Math.Min(offset, extent - viewport);
            return Math.Max(offset, 0.0);
        }

        private int? prevFirst;
        private int? prevLast;

        internal void UpdateRendering(bool forceUpdate)
        {
            postedUpdate = false;

            //if ((this.ViewModel != null) && this.ViewModel.IsReady) {
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
            //}
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

        private Typeface CreateTypeFace()
        {
            if (FontFamily == null)
                return null;
            return new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        }

        private Pen CreateHorizontalGridLinesPen()
        {
            if (HorizontalGridLinesBrush == null ||
                !DoubleUtils.GreaterThan(HorizontalGridLinesThickness, 0))
                return null;

            var pen = new Pen(HorizontalGridLinesBrush, HorizontalGridLinesThickness);
            pen.Freeze();
            return pen;
        }

        private Pen CreateVerticalGridLinesPen()
        {
            if (VerticalGridLinesBrush == null ||
                !DoubleUtils.GreaterThan(VerticalGridLinesThickness, 0))
                return null;

            var pen = new Pen(VerticalGridLinesBrush, VerticalGridLinesThickness);
            pen.Freeze();
            return pen;
        }

        internal double GetColumnAutoSize(VirtualizedDataGridColumnViewModel viewModelColumn)
        {
            return cellsDrawingVisual.GetColumnAutoSize(viewModelColumn);
        }
    }
}
