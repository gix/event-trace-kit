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

    [TemplatePart(Name = PART_CellsScrollViewer, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = PART_CellsPresenter, Type = typeof(AsyncDataGridCellsPresenter))]
    [TemplatePart(Name = PART_ColumnHeadersPresenter, Type = typeof(AsyncDataGridColumnHeadersPresenter))]
    public class AsyncDataGrid : Control
    {
        private const string PART_CellsPresenter = "PART_CellsPresenter";
        private const string PART_CellsScrollViewer = "PART_CellsScrollViewer";
        private const string PART_ColumnHeadersPresenter = "PART_ColumnHeadersPresenter";

        static AsyncDataGrid()
        {
            Type forType = typeof(AsyncDataGrid);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(forType));

            CommandManager.RegisterClassCommandBinding(
                typeof(AsyncDataGrid),
                new CommandBinding(
                    Copy, OnCopyExecuted, OnCopyCanExecute));

            CommandManager.RegisterClassInputBinding(
                typeof(AsyncDataGrid),
                new InputBinding(
                    Copy, new KeyGesture(Key.C, ModifierKeys.Control)) {
                    CommandParameter = CopyBehavior.Selection
                });
            CommandManager.RegisterClassInputBinding(
                typeof(AsyncDataGrid),
                new InputBinding(
                    Copy, new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift)) {
                    CommandParameter = CopyBehavior.Cell
                });
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

        public static readonly RoutedCommand Copy =
            new RoutedCommand(nameof(Copy), typeof(AsyncDataGrid));

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
            get => (FontFamily)GetValue(RowFontFamilyProperty);
            set => SetValue(RowFontFamilyProperty, value);
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
            get => (double)GetValue(RowFontSizeProperty);
            set => SetValue(RowFontSizeProperty, value);
        }

        #endregion

        #region public Brush RowForeground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="RowForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowForegroundProperty =
            DependencyProperty.Register(
                nameof(RowForeground),
                typeof(Brush),
                typeof(AsyncDataGrid),
                new PropertyMetadata(SystemColors.ControlTextBrush));

        /// <summary>
        ///   Gets or sets the row foreground.
        /// </summary>
        public Brush RowForeground
        {
            get => (Brush)GetValue(RowForegroundProperty);
            set => SetValue(RowForegroundProperty, value);
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
            get => (Brush)GetValue(RowBackgroundProperty);
            set => SetValue(RowBackgroundProperty, value);
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
            get => (Brush)GetValue(AlternatingRowBackgroundProperty);
            set => SetValue(AlternatingRowBackgroundProperty, value);
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
                typeof(AsyncDataGrid),
                new PropertyMetadata(Brushes.LightGray));

        /// <summary>
        ///   Gets or sets the frozen column background.
        /// </summary>
        public Brush FrozenColumnBackground
        {
            get => (Brush)GetValue(FrozenColumnBackgroundProperty);
            set => SetValue(FrozenColumnBackgroundProperty, value);
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
            get => (Brush)GetValue(RowSelectionForegroundProperty);
            set => SetValue(RowSelectionForegroundProperty, value);
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
            get => (Brush)GetValue(RowSelectionBackgroundProperty);
            set => SetValue(RowSelectionBackgroundProperty, value);
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
            get => (Brush)GetValue(RowInactiveSelectionForegroundProperty);
            set => SetValue(RowInactiveSelectionForegroundProperty, value);
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
            get => (Brush)GetValue(RowInactiveSelectionBackgroundProperty);
            set => SetValue(RowInactiveSelectionBackgroundProperty, value);
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
            get => (Brush)GetValue(RowFocusBorderBrushProperty);
            set => SetValue(RowFocusBorderBrushProperty, value);
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
            get => (AsyncDataGridViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
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
                    (s, e) => ((AsyncDataGrid)s).OnViewModelEventSourceChanged(e),
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

        private void OnViewModelEventSourceChanged(
            DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is AsyncDataGridViewModel oldValue) {
                oldValue.DataInvalidated -= OnViewModelDataInvalidated;
                oldValue.ColumnsModel.ColumnsChanged -= OnColumnsChanged;
                oldValue.Updated -= OnViewModelUpdated;
            }

            if (e.NewValue is AsyncDataGridViewModel newValue) {
                newValue.Updated += OnViewModelUpdated;
                newValue.ColumnsModel.ColumnsChanged += OnColumnsChanged;
                newValue.DataInvalidated += OnViewModelDataInvalidated;
            }
        }

        #endregion

        #region public ScrollViewer CellsScrollViewer { get; private set; }

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
            get => (ScrollViewer)GetValue(CellsScrollViewerProperty);
            private set => SetValue(CenterCellsScrollViewerPropertyKey, value);
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
            get => (AsyncDataGridColumnHeadersPresenter)GetValue(ColumnHeadersPresenterProperty);
            private set => SetValue(ColumnHeadersPresenterPropertyKey, value);
        }

        #endregion

        #region public AsyncDataGridCellsPresenter CellsPresenter { get; private set; }

        private static readonly DependencyPropertyKey CellsPresenterPartCenterPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(CellsPresenter),
                typeof(AsyncDataGridCellsPresenter),
                typeof(AsyncDataGrid),
                PropertyMetadataUtils.DefaultNull);

        /// <summary>
        ///   Identifies the <see cref="CellsPresenter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellsPresenterProperty =
            CellsPresenterPartCenterPropertyKey.DependencyProperty;

        public AsyncDataGridCellsPresenter CellsPresenter
        {
            get => (AsyncDataGridCellsPresenter)GetValue(CellsPresenterProperty);
            private set => SetValue(CellsPresenterPartCenterPropertyKey, value);
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
            get => (bool)GetValue(IsSelectionActiveProperty);
            set => SetValue(IsSelectionActiveProperty, Boxed.Bool(value));
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
            source.ColumnHeadersPresenter?.NotifyHorizontalOffsetChanged();
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (CellsScrollViewer != null)
                BindingOperations.ClearBinding(this, HorizontalScrollOffsetProperty);

            ColumnHeadersPresenter = GetTemplateChild(PART_ColumnHeadersPresenter) as AsyncDataGridColumnHeadersPresenter;
            CellsScrollViewer = GetTemplateChild(PART_CellsScrollViewer) as ScrollViewer;
            CellsPresenter = GetTemplateChild(PART_CellsPresenter) as AsyncDataGridCellsPresenter;

            if (CellsScrollViewer != null) {
                var binding = new Binding(nameof(CellsScrollViewer.HorizontalOffset));
                binding.Source = CellsScrollViewer;
                SetBinding(HorizontalScrollOffsetProperty, binding);
            }
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
                var root = this.GetRootVisual<UIElement>();
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

        public double AutoSize(AsyncDataGridColumn column)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            double newWidth = 0;
            if (CellsPresenter != null)
                newWidth = CellsPresenter.GetColumnAutoSize(column);

            double adjustment = newWidth - column.Width;
            column.Width = newWidth;

            return adjustment;
        }

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

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CoerceValue(ViewModelEventSourceProperty);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CoerceValue(ViewModelEventSourceProperty);
        }

        private void OnViewModelUpdated(object sender, EventArgs args)
        {
            CellsPresenter?.QueueRender(true);
        }

        private void OnColumnsChanged(object sender, EventArgs args)
        {
            CellsPresenter?.InvalidateRowCache();
        }

        private void OnViewModelDataInvalidated(object sender, EventArgs args)
        {
            CellsPresenter?.InvalidateRowCache();
        }

        private static void OnCopyCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var source = (AsyncDataGrid)sender;
            e.CanExecute = source.ViewModel?.RowSelection.Count != 0;
            e.Handled = true;
        }

        private static void OnCopyExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var source = (AsyncDataGrid)sender;
            source.ViewModel?.CopyToClipboard((CopyBehavior)e.Parameter);
        }

        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            if (!e.Handled) {
                var d = e.OriginalSource as DependencyObject;
                var cellsPresenter = d?.FindAncestorOrSelf<AsyncDataGridCellsPresenter>();
                if (cellsPresenter != null) {
                    var position = new Point(e.CursorLeft, e.CursorTop);
                    var column = cellsPresenter.GetColumnFromPosition(position.X);
                    var rowIndex = cellsPresenter.GetRowFromPosition(position.Y);
                    var columnIndex = column?.ModelColumnIndex;

                    var menu = new ContextMenu();
                    ViewModel.BuildContextMenu(menu, rowIndex, columnIndex);
                    if (menu.Items.Count != 0) {
                        menu.PlacementTarget = this;
                        using (EnterContextMenuVisualState())
                            menu.IsOpen = true;
                        e.Handled = true;
                    }
                }
            }

            base.OnContextMenuOpening(e);
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
