namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using Primitives;
    using Windows;

    [TemplatePart(Name = PART_CancelButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_CellsScrollViewer, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = PART_CellsPresenter, Type = typeof(AsyncDataGridCellsPresenter))]
    [TemplatePart(Name = PART_ColumnHeadersPresenter, Type = typeof(AsyncDataGridColumnHeadersPresenter))]
    [TemplateVisualState(Name = STATE_Ready, GroupName = STATE_GROUP_Responsiveness)]
    [TemplateVisualState(Name = STATE_Processing, GroupName = STATE_GROUP_Responsiveness)]
    public class AsyncDataGrid : Control
    {
        private const string PART_CancelButton = "PART_CancelButton";
        private const string PART_CellsPresenter = "PART_CellsPresenter";
        private const string PART_CellsScrollViewer = "PART_CellsScrollViewer";
        private const string PART_ColumnHeadersPresenter = "PART_ColumnHeadersPresenter";
        private const string STATE_GROUP_Responsiveness = "ResponsivenessStates";
        private const string STATE_Processing = "Processing";
        private const string STATE_Ready = "Ready";

        private Action<bool> updateStatesAction;

        static AsyncDataGrid()
        {
            Type forType = typeof(AsyncDataGrid);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(forType));
            IsEnabledProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(
                    (d, e) => ((AsyncDataGrid)d).OnIsEnabledChanged(e)));

            CommandManager.RegisterClassInputBinding(
                typeof(AsyncDataGrid),
                new InputBinding(
                    CopyCell, new KeyGesture(Key.C, ModifierKeys.Control)));
            CommandManager.RegisterClassCommandBinding(
                typeof(AsyncDataGrid),
                new CommandBinding(
                    CopyCell, OnCopyCellExecuted, OnCopyCellCanExecute));
        }

        public AsyncDataGrid()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public event EventHandler<DragStartedEventArgs> ColumnHeaderDragStarted;
        public event EventHandler<DragDeltaEventArgs> ColumnHeaderDragDelta;
        public event EventHandler<DragCompletedEventArgs> ColumnHeaderDragCompleted;
        public event EventHandler<AsyncDataGridColumnReorderingEventArgs> ColumnReordering;
        public event EventHandler<AsyncDataGridColumnEventArgs> ColumnReordered;

        public static readonly RoutedCommand CopyCell =
            new RoutedCommand(nameof(CopyCell), typeof(AsyncDataGrid));

        #region public FontFamily RowFontFamily { get; set; }

        /// <summary>
        ///   Identifies the <see cref="RowFontFamily"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowFontFamilyProperty =
            DependencyProperty.Register(
                nameof(RowFontFamily),
                typeof(FontFamily),
                typeof(AsyncDataGrid),
                new PropertyMetadata(SystemFonts.MessageFontFamily));

        /// <summary>
        ///   Gets or sets the row font family.
        /// </summary>
        public FontFamily RowFontFamily
        {
            get { return (FontFamily)GetValue(RowFontFamilyProperty); }
            set { SetValue(RowFontFamilyProperty, value); }
        }

        #endregion

        #region public double RowFontSize { get; set; }

        /// <summary>
        ///   Identifies the <see cref="RowFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowFontSizeProperty =
            DependencyProperty.Register(
                nameof(RowFontSize),
                typeof(double),
                typeof(AsyncDataGrid),
                new PropertyMetadata(SystemFonts.MessageFontSize));

        /// <summary>
        ///   Gets or sets the row font size.
        /// </summary>
        [TypeConverter(typeof(FontSizeConverter))]
        public double RowFontSize
        {
            get { return (double)GetValue(RowFontSizeProperty); }
            set { SetValue(RowFontSizeProperty, value); }
        }

        #endregion

        #region public Brush RowBackground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="RowBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowBackgroundProperty =
            DependencyProperty.Register(
                nameof(RowBackground),
                typeof(Brush),
                typeof(AsyncDataGrid),
                new PropertyMetadata(Brushes.White));

        /// <summary>
        ///   Gets or sets the row background.
        /// </summary>
        public Brush RowBackground
        {
            get { return (Brush)GetValue(RowBackgroundProperty); }
            set { SetValue(RowBackgroundProperty, value); }
        }

        #endregion

        #region public Brush AlternatingRowBackground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="AlternatingRowBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AlternatingRowBackgroundProperty =
            DependencyProperty.Register(
                nameof(AlternatingRowBackground),
                typeof(Brush),
                typeof(AsyncDataGrid),
                new PropertyMetadata(Brushes.WhiteSmoke));

        /// <summary>
        ///   Gets or sets the alternating row background.
        /// </summary>
        public Brush AlternatingRowBackground
        {
            get { return (Brush)GetValue(AlternatingRowBackgroundProperty); }
            set { SetValue(AlternatingRowBackgroundProperty, value); }
        }

        #endregion

        #region public Brush RowSelectionForeground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="RowSelectionForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowSelectionForegroundProperty =
            DependencyProperty.Register(
                nameof(RowSelectionForeground),
                typeof(Brush),
                typeof(AsyncDataGrid),
                new PropertyMetadata(SystemColors.HighlightTextBrush));

        /// <summary>
        ///   Gets or sets the row selection Foreground brush.
        /// </summary>
        public Brush RowSelectionForeground
        {
            get { return (Brush)GetValue(RowSelectionForegroundProperty); }
            set { SetValue(RowSelectionForegroundProperty, value); }
        }

        #endregion

        #region public Brush RowSelectionBackground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="RowSelectionBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowSelectionBackgroundProperty =
            DependencyProperty.Register(
                nameof(RowSelectionBackground),
                typeof(Brush),
                typeof(AsyncDataGrid),
                new PropertyMetadata(SystemColors.HighlightBrush));

        /// <summary>
        ///   Gets or sets the row selection background brush.
        /// </summary>
        public Brush RowSelectionBackground
        {
            get { return (Brush)GetValue(RowSelectionBackgroundProperty); }
            set { SetValue(RowSelectionBackgroundProperty, value); }
        }

        #endregion

        #region public Brush RowInactiveSelectionForeground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="RowInactiveSelectionForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowInactiveSelectionForegroundProperty =
            DependencyProperty.Register(
                nameof(RowInactiveSelectionForeground),
                typeof(Brush),
                typeof(AsyncDataGrid),
                new PropertyMetadata(SystemColors.InactiveSelectionHighlightTextBrush));

        /// <summary>
        ///   Gets or sets the row inactive selection Foreground.
        /// </summary>
        public Brush RowInactiveSelectionForeground
        {
            get { return (Brush)GetValue(RowInactiveSelectionForegroundProperty); }
            set { SetValue(RowInactiveSelectionForegroundProperty, value); }
        }

        #endregion

        #region public Brush RowInactiveSelectionBackground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="RowInactiveSelectionBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowInactiveSelectionBackgroundProperty =
            DependencyProperty.Register(
                nameof(RowInactiveSelectionBackground),
                typeof(Brush),
                typeof(AsyncDataGrid),
                new PropertyMetadata(SystemColors.InactiveSelectionHighlightBrush));

        /// <summary>
        ///   Gets or sets the row inactive selection background.
        /// </summary>
        public Brush RowInactiveSelectionBackground
        {
            get { return (Brush)GetValue(RowInactiveSelectionBackgroundProperty); }
            set { SetValue(RowInactiveSelectionBackgroundProperty, value); }
        }

        #endregion

        #region public Brush RowFocusBorderBrush { get; set; }

        /// <summary>
        ///   Identifies the <see cref="RowFocusBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowFocusBorderBrushProperty =
            DependencyProperty.Register(
                nameof(RowFocusBorderBrush),
                typeof(Brush),
                typeof(AsyncDataGrid),
                new PropertyMetadata(SystemColors.ControlTextBrush));

        /// <summary>
        ///   Gets or sets the row focus border brush.
        /// </summary>
        public Brush RowFocusBorderBrush
        {
            get { return (Brush)GetValue(RowFocusBorderBrushProperty); }
            set { SetValue(RowFocusBorderBrushProperty, value); }
        }

        #endregion

        #region public AsyncDataGridViewModel ViewModel { get; set; }

        /// <summary>
        ///   Identifies the <see cref="ViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(AsyncDataGridViewModel),
                typeof(AsyncDataGrid),
                new PropertyMetadata(null, OnViewModelChanged));

        public AsyncDataGridViewModel ViewModel
        {
            get { return (AsyncDataGridViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        private static void OnViewModelChanged(
            DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var source = (AsyncDataGrid)s;
            source.CoerceValue(ViewModelEventSourceProperty);
        }

        #endregion

        #region private AsyncDataGridViewModel ViewModelEventSource { get; }

        private static readonly DependencyPropertyKey ViewModelEventSourcePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ViewModelEventSource),
                typeof(AsyncDataGridViewModel),
                typeof(AsyncDataGrid),
                new PropertyMetadata(
                    null,
                    (s, e) => ((AsyncDataGrid)s).ViewModelEventSourcePropertyChanged(e),
                    (d, v) => ((AsyncDataGrid)d).CoerceViewModelEventSourceProperty(v)));

        /// <summary>
        ///   Identifies the <see cref="ViewModelEventSource"/> dependency property.
        /// </summary>
        private static readonly DependencyProperty ViewModelEventSourceProperty =
            ViewModelEventSourcePropertyKey.DependencyProperty;

        private AsyncDataGridViewModel ViewModelEventSource =>
            (AsyncDataGridViewModel)GetValue(ViewModelEventSourceProperty);

        private object CoerceViewModelEventSourceProperty(object baseValue)
        {
            return IsLoaded ? ViewModel : null;
        }

        private void ViewModelEventSourcePropertyChanged(
            DependencyPropertyChangedEventArgs e)
        {
            var oldValue = (AsyncDataGridViewModel)e.OldValue;
            if (oldValue != null) {
                oldValue.Updated -= OnViewModelUpdated;
            }

            var newValue = (AsyncDataGridViewModel)e.NewValue;
            if (newValue != null) {
                newValue.Updated += OnViewModelUpdated;
                var templateChild = GetTemplateChild(PART_CellsScrollViewer) as ScrollViewer;
                if (templateChild != null)
                    CenterScrollWidthChanged(templateChild.ActualWidth);
            }
        }

        #endregion

        #region public Button CancelButtonPart { get; private set; }

        private static readonly DependencyPropertyKey CancelButtonPartPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(CancelButtonPart),
                typeof(Button),
                typeof(AsyncDataGrid),
                new PropertyMetadata(
                    null,
                    (s, e) => ((AsyncDataGrid)s).OnCancelButtonPartChanged(e)));

        /// <summary>
        ///   Identifies the <see cref="CancelButtonPart"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CancelButtonPartProperty =
            CancelButtonPartPropertyKey.DependencyProperty;

        public Button CancelButtonPart
        {
            get { return (Button)GetValue(CancelButtonPartProperty); }
            private set { SetValue(CancelButtonPartPropertyKey, value); }
        }

        #endregion

        #region public ScrollViewer CenterCellsScrollViewer { get; private set; }

        private static readonly DependencyPropertyKey CenterCellsScrollViewerPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(CellsScrollViewer),
                typeof(ScrollViewer),
                typeof(AsyncDataGrid),
                PropertyMetadataUtils.DefaultNull);

        /// <summary>
        ///   Identifies the <see cref="CellsScrollViewer"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellsScrollViewerProperty =
            CenterCellsScrollViewerPropertyKey.DependencyProperty;

        public ScrollViewer CellsScrollViewer
        {
            get { return (ScrollViewer)GetValue(CellsScrollViewerProperty); }
            private set { SetValue(CenterCellsScrollViewerPropertyKey, value); }
        }

        #endregion

        #region public AsyncDataGridColumnHeadersPresenter ColumnHeadersPresenter { get; private set; }

        private static readonly DependencyPropertyKey ColumnHeadersPresenterPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ColumnHeadersPresenter),
                typeof(AsyncDataGridColumnHeadersPresenter),
                typeof(AsyncDataGrid),
                PropertyMetadataUtils.DefaultNull);

        /// <summary>
        ///   Identifies the <see cref="ColumnHeadersPresenter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnHeadersPresenterProperty =
            ColumnHeadersPresenterPropertyKey.DependencyProperty;

        public AsyncDataGridColumnHeadersPresenter ColumnHeadersPresenter
        {
            get { return (AsyncDataGridColumnHeadersPresenter)GetValue(ColumnHeadersPresenterProperty); }
            private set { SetValue(ColumnHeadersPresenterPropertyKey, value); }
        }

        #endregion

        #region public AsyncDataGridCellsPresenter CellsPresenterPartCenter { get; private set; }

        private static readonly DependencyPropertyKey CellsPresenterPartCenterPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(CellsPresenter), typeof(AsyncDataGridCellsPresenter),
                typeof(AsyncDataGrid), PropertyMetadataUtils.DefaultNull);

        /// <summary>
        ///   Identifies the <see cref="CellsPresenter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellsPresenterProperty =
            CellsPresenterPartCenterPropertyKey.DependencyProperty;

        public AsyncDataGridCellsPresenter CellsPresenter
        {
            get { return (AsyncDataGridCellsPresenter)GetValue(CellsPresenterProperty); }
            private set { SetValue(CellsPresenterPartCenterPropertyKey, value); }
        }

        #endregion

        #region public bool IsSelectionActive { get; set; }

        /// <summary>
        ///   Identifies the <see cref="IsSelectionActive"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSelectionActiveProperty =
            DependencyProperty.Register(
                nameof(IsSelectionActive),
                typeof(bool),
                typeof(AsyncDataGrid),
                new PropertyMetadata(Boxed.False, OnIsSelectionActiveChanged));

        /// <summary>
        ///   Gets or sets a value indicating whether the selection is active.
        /// </summary>
        public bool IsSelectionActive
        {
            get { return (bool)GetValue(IsSelectionActiveProperty); }
            set { SetValue(IsSelectionActiveProperty, Boxed.Bool(value)); }
        }

        private static void OnIsSelectionActiveChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (AsyncDataGrid)d;
            source.CellsPresenter?.OnIsSelectionActiveChanged();
        }

        #endregion

        #region internal double HorizontalScrollOffset { get; }

        internal static readonly DependencyProperty HorizontalScrollOffsetProperty =
            DependencyProperty.Register(
                nameof(HorizontalScrollOffset),
                typeof(double),
                typeof(AsyncDataGrid),
                new FrameworkPropertyMetadata(0.0, OnHorizontalOffsetChanged));

        internal double HorizontalScrollOffset => (double)GetValue(HorizontalScrollOffsetProperty);

        private static void OnHorizontalOffsetChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var source = (AsyncDataGrid)d;
            var columnHeadersPresenter = source.ColumnHeadersPresenter;
            if (columnHeadersPresenter != null) {
                columnHeadersPresenter.InvalidateArrange();

                var itemsHost = columnHeadersPresenter.InternalItemsHost;
                itemsHost?.InvalidateMeasure();
                itemsHost?.InvalidateArrange();
            }
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ColumnHeadersPresenter = GetTemplateChild(PART_ColumnHeadersPresenter) as AsyncDataGridColumnHeadersPresenter;
            CellsScrollViewer = GetTemplateChild(PART_CellsScrollViewer) as ScrollViewer;
            CellsPresenter = GetTemplateChild(PART_CellsPresenter) as AsyncDataGridCellsPresenter;
            CancelButtonPart = GetTemplateChild(PART_CancelButton) as Button;

            if (CellsScrollViewer != null) {
                CellsScrollViewer.SizeChanged += CenterScrollViewerSizeChanged;

                var horizontalOffsetBinding = new Binding(nameof(CellsScrollViewer.HorizontalOffset));
                horizontalOffsetBinding.Source = CellsScrollViewer;
                SetBinding(HorizontalScrollOffsetProperty, horizontalOffsetBinding);
            }

            if (CellsPresenter != null)
                CellsPresenter.MouseUp += OnCellsPresenterMouseUp;

            UpdateStates(false);
        }

        protected override void OnIsKeyboardFocusWithinChanged(
            DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);

            bool isSelectionActive = false;
            if ((bool)e.NewValue) {
                isSelectionActive = true;
            } else {
                var currentFocus = Keyboard.FocusedElement as DependencyObject;
                var root = GetVisualRoot(this) as UIElement;
                if (currentFocus != null && root != null && root.IsKeyboardFocusWithin) {
                    if (FocusManager.GetFocusScope(currentFocus) !=
                        FocusManager.GetFocusScope(this))
                        isSelectionActive = true;
                }
            }

            IsSelectionActive = isSelectionActive || GetIsContextMenuOpen(this);
        }

        #region attached bool IsContextMenuOpen

        private static readonly DependencyPropertyKey IsContextMenuOpenPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "IsContextMenuOpen",
                typeof(bool),
                typeof(AsyncDataGrid),
                new FrameworkPropertyMetadata(
                    Boxed.False, FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty IsContextMenuOpenProperty =
            IsContextMenuOpenPropertyKey.DependencyProperty;

        public static bool GetIsContextMenuOpen(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            return (bool)element.GetValue(IsContextMenuOpenProperty);
        }

        private static void SetIsContextMenuOpen(UIElement element, bool value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            element.SetValue(IsContextMenuOpenPropertyKey, Boxed.Bool(value));
        }

        #endregion

        public IDisposable EnterContextMenuVisualState()
        {
            return new ContextMenuScope(this);
        }

        private sealed class ContextMenuScope : IDisposable
        {
            private readonly AsyncDataGrid owner;

            public ContextMenuScope(AsyncDataGrid owner)
            {
                this.owner = owner;
                SetIsContextMenuOpen(owner, true);

            }

            public void Dispose()
            {
                SetIsContextMenuOpen(owner, false);
            }
        }

        protected virtual void OnIsEnabledChanged(
            DependencyPropertyChangedEventArgs e)
        {
        }

        internal static Visual GetVisualRoot(DependencyObject d)
        {
            var visual = d as Visual;
            if (visual != null) {
                var source = PresentationSource.FromVisual(visual);
                if (source != null)
                    return source.RootVisual;
            } else {
                var element = d as FrameworkContentElement;
                if (element != null)
                    return GetVisualRoot(element.Parent);
            }

            return null;
        }

        private void OnCellsPresenterMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled)
                return;

            //var relativeTo = sender as AsyncDataGridCellsPresenter;
            //if (relativeTo != null) {
            //    AsyncDataGridColumnViewModel columnFromPosition = relativeTo.GetColumnFromPosition(e.GetPosition(relativeTo).X);
            //    ViewModel.ColumnsViewModel.SetClickedColumn(columnFromPosition);
            //}
        }

        private void UpdateStates(bool useTransitions)
        {
            if (true/*IsReadyInternal*/) {
                VisualStateManager.GoToState(this, "Ready", useTransitions);
            } else {
                VisualStateManager.GoToState(this, "Processing", useTransitions);
            }
        }

        private void CenterScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void OnCancelButtonPartChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CoerceValue(ViewModelEventSourceProperty);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CoerceValue(ViewModelEventSourceProperty);
        }

        private void CenterScrollWidthChanged(double width)
        {
            //if (ViewModel != null)
            //    ViewModel.ColumnsViewModel.CenterScrollViewerWidth = width;
        }

        private void OnViewModelUpdated(object sender, ItemEventArgs<bool> e)
        {
            CellsPresenter?.PostUpdateRendering();
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            //CellsPresenter?.Focus();
        }

        private static void OnCopyCellCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var grid = (AsyncDataGrid)sender;
            //grid.ViewModel.DoCopyToClipboard(HierarchicalDataGridViewModel.CopyBehavior.Cell);
            e.Handled = true;
        }

        private static void OnCopyCellExecuted(object sender, ExecutedRoutedEventArgs e)
        {
        }

        public double AutoSize(AsyncDataGridColumn column)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            if (!column.IsSafeToReadCellValuesFromUIThread)
                return 0;

            double newWidth = 0;
            if (CellsPresenter != null)
                newWidth = CellsPresenter.GetColumnAutoSize(column);

            double adjustment = newWidth - column.Width;
            column.Width = newWidth;

            return adjustment;
        }

        protected internal virtual void OnColumnHeaderDragStarted(DragStartedEventArgs e)
        {
            ColumnHeaderDragStarted?.Invoke(this, e);
        }

        protected internal virtual void OnColumnHeaderDragDelta(DragDeltaEventArgs e)
        {
            ColumnHeaderDragDelta?.Invoke(this, e);
        }

        protected internal virtual void OnColumnHeaderDragCompleted(DragCompletedEventArgs e)
        {
            ColumnHeaderDragCompleted?.Invoke(this, e);
        }

        protected internal virtual void OnColumnReordering(
            AsyncDataGridColumnReorderingEventArgs e)
        {
            ColumnReordering?.Invoke(this, e);
        }

        protected internal virtual void OnColumnReordered(AsyncDataGridColumnEventArgs e)
        {
            ColumnReordered?.Invoke(this, e);
        }
    }

    public class AsyncDataGridColumnEventArgs : EventArgs
    {
        public AsyncDataGridColumnEventArgs(AsyncDataGridColumn column)
        {
            Column = column;
        }

        public AsyncDataGridColumn Column { get; }
    }

    public class AsyncDataGridColumnReorderingEventArgs
        : AsyncDataGridColumnEventArgs
    {
        public AsyncDataGridColumnReorderingEventArgs(
            AsyncDataGridColumn column)
            : base(column)
        {
        }

        public bool Cancel { get; set; }
    }
}
