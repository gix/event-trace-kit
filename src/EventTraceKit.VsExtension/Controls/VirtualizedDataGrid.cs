namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using Primitives;

    [TemplatePart(Name = PART_CenterCellsScrollViewer, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = PART_CellsPresenterCenter, Type = typeof(VirtualizedDataGridCellsPresenter))]
    [TemplatePart(Name = PART_ColumnHeadersPresenter, Type = typeof(VirtualizedDataGridColumnHeadersPresenter))]
    [TemplatePart(Name = PART_CancelButton, Type = typeof(Button))]
    [TemplateVisualState(Name = STATE_Ready, GroupName = STATE_GROUP_Responsiveness)]
    [TemplateVisualState(Name = STATE_Processing, GroupName = STATE_GROUP_Responsiveness)]
    public class VirtualizedDataGrid : Control
    {
        private const string PART_CancelButton = "PART_CancelButton";
        private const string PART_CellsPresenterCenter = "PART_CellsPresenterCenter";
        private const string PART_CenterCellsScrollViewer = "PART_CenterCellsScrollViewer";
        private const string PART_ColumnHeadersPresenter = "PART_ColumnHeadersPresenter";
        private const string STATE_GROUP_Responsiveness = "ResponsivenessStates";
        private const string STATE_Processing = "Processing";
        private const string STATE_Ready = "Ready";

        private VirtualizedDataGridCellsPresenter cellsPresenter;
        private VirtualizedDataGridViewModel viewModel;

        private Action<bool> updateStatesAction;

        static VirtualizedDataGrid()
        {
            Type forType = typeof(VirtualizedDataGrid);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(forType));
            IsEnabledProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(
                    (d, e) => ((VirtualizedDataGrid)d).OnIsEnabledChanged(e)));

            var commandBinding = new CommandBinding(
                CopyCell, OnCopyCellExecuted,
                OnCopyCellCanExecute);
            var gesture = new KeyGesture(Key.C, ModifierKeys.Control);
            CommandManager.RegisterClassInputBinding(
                typeof(VirtualizedDataGrid), new InputBinding(CopyCell, gesture));
            CommandManager.RegisterClassCommandBinding(
                typeof(VirtualizedDataGrid), commandBinding);
        }

        public VirtualizedDataGrid()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public event EventHandler<DragStartedEventArgs> ColumnHeaderDragStarted;
        public event EventHandler<DragDeltaEventArgs> ColumnHeaderDragDelta;
        public event EventHandler<DragCompletedEventArgs> ColumnHeaderDragCompleted;
        public event EventHandler<VirtualizedDataGridColumnReorderingEventArgs> ColumnReordering;
        public event EventHandler<VirtualizedDataGridColumnEventArgs> ColumnReordered;

        public static readonly RoutedCommand CopyCell =
            new RoutedCommand(nameof(CopyCell), typeof(VirtualizedDataGrid));

        #region public Brush RowBackground { get; set; }

        /// <summary>
        ///   Identifies the <see cref="RowBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowBackgroundProperty =
            DependencyProperty.Register(
                nameof(RowBackground),
                typeof(Brush),
                typeof(VirtualizedDataGrid),
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
                typeof(VirtualizedDataGrid),
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
                typeof(VirtualizedDataGrid),
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
                typeof(VirtualizedDataGrid),
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
                typeof(VirtualizedDataGrid),
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
                typeof(VirtualizedDataGrid),
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
                typeof(VirtualizedDataGrid),
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

        #region public IVirtualizedDataGridViewModel ViewModel { get; set; }

        /// <summary>
        ///   Identifies the <see cref="ViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(VirtualizedDataGridViewModel),
                typeof(VirtualizedDataGrid),
                new PropertyMetadata(null, OnViewModelChanged));

        public VirtualizedDataGridViewModel ViewModel
        {
            get { return (VirtualizedDataGridViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        private static void OnViewModelChanged(
            DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var source = (VirtualizedDataGrid)s;
            source.CoerceValue(ViewModelEventSourceProperty);
        }

        #endregion

        #region private IVirtualizedDataGridViewModel ViewModelEventSource { get; }

        private static readonly DependencyPropertyKey ViewModelEventSourcePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ViewModelEventSource),
                typeof(VirtualizedDataGridViewModel),
                typeof(VirtualizedDataGrid),
                new PropertyMetadata(
                    null,
                    (s, e) => ((VirtualizedDataGrid)s).ViewModelEventSourcePropertyChanged(e),
                    (d, v) => ((VirtualizedDataGrid)d).CoerceViewModelEventSourceProperty(v)));

        /// <summary>
        ///   Identifies the <see cref="ViewModelEventSource"/> dependency property.
        /// </summary>
        private static readonly DependencyProperty ViewModelEventSourceProperty =
            ViewModelEventSourcePropertyKey.DependencyProperty;

        private VirtualizedDataGridViewModel ViewModelEventSource =>
            (VirtualizedDataGridViewModel)GetValue(ViewModelEventSourceProperty);

        private object CoerceViewModelEventSourceProperty(object baseValue)
        {
            return IsLoaded ? ViewModel : null;
        }

        private void ViewModelEventSourcePropertyChanged(
            DependencyPropertyChangedEventArgs e)
        {
            var oldValue = (VirtualizedDataGridViewModel)e.OldValue;
            if (oldValue != null) {
                viewModel = null;
                oldValue.Updated -= OnViewModelUpdated;
                //oldValue.PropertyChanged -= OnViewModelPropertyChanged;
                //BindingOperations.ClearBinding(oldValue, VirtualizedDataGridColumnsViewModel.ActualWidthProperty);
            }

            var newValue = (VirtualizedDataGridViewModel)e.NewValue;
            if (newValue != null) {
                viewModel = newValue;
                newValue.Updated += OnViewModelUpdated;
                //newValue.PropertyChanged += OnViewModelPropertyChanged;
                var binding = new Binding {
                    Source = this,
                    Path = new PropertyPath(ActualWidthProperty.Name),
                    Mode = BindingMode.OneWay
                };
                //BindingOperations.SetBinding(newValue.ColumnsViewModel, VirtualizedDataGridColumnsViewModel.ActualWidthProperty, binding);
                var templateChild = GetTemplateChild(PART_CenterCellsScrollViewer) as ScrollViewer;
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
                typeof(VirtualizedDataGrid),
                new PropertyMetadata(
                    null,
                    (s, e) => ((VirtualizedDataGrid)s).OnCancelButtonPartChanged(e)));

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
                nameof(CenterCellsScrollViewer),
                typeof(ScrollViewer),
                typeof(VirtualizedDataGrid),
                PropertyMetadataUtils.DefaultNull);

        /// <summary>
        ///   Identifies the <see cref="CenterCellsScrollViewer"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CenterCellsScrollViewerProperty =
            CenterCellsScrollViewerPropertyKey.DependencyProperty;

        public ScrollViewer CenterCellsScrollViewer
        {
            get { return (ScrollViewer)GetValue(CenterCellsScrollViewerProperty); }
            private set { SetValue(CenterCellsScrollViewerPropertyKey, value); }
        }

        #endregion

        #region public VirtualizedDataGridColumnHeadersPresenter ColumnHeadersPresenter { get; private set; }

        private static readonly DependencyPropertyKey ColumnHeadersPresenterPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ColumnHeadersPresenter),
                typeof(VirtualizedDataGridColumnHeadersPresenter),
                typeof(VirtualizedDataGrid),
                PropertyMetadataUtils.DefaultNull);

        /// <summary>
        ///   Identifies the <see cref="ColumnHeadersPresenter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnHeadersPresenterProperty =
            ColumnHeadersPresenterPropertyKey.DependencyProperty;

        public VirtualizedDataGridColumnHeadersPresenter ColumnHeadersPresenter
        {
            get { return (VirtualizedDataGridColumnHeadersPresenter)GetValue(ColumnHeadersPresenterProperty); }
            private set { SetValue(ColumnHeadersPresenterPropertyKey, value); }
        }

        #endregion

        #region public VirtualizedDataGridCellsPresenter CellsPresenterPartCenter { get; private set; }

        private static readonly DependencyPropertyKey CellsPresenterPartCenterPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(CellsPresenterPartCenter), typeof(VirtualizedDataGridCellsPresenter),
                typeof(VirtualizedDataGrid), PropertyMetadataUtils.DefaultNull);

        /// <summary>
        ///   Identifies the <see cref="CellsPresenterPartCenter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellsPresenterPartCenterProperty =
            CellsPresenterPartCenterPropertyKey.DependencyProperty;

        public VirtualizedDataGridCellsPresenter CellsPresenterPartCenter
        {
            get { return (VirtualizedDataGridCellsPresenter)GetValue(CellsPresenterPartCenterProperty); }
            private set { SetValue(CellsPresenterPartCenterPropertyKey, value); }
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ColumnHeadersPresenter = GetTemplateChild(PART_ColumnHeadersPresenter) as VirtualizedDataGridColumnHeadersPresenter;
            CenterCellsScrollViewer = GetTemplateChild(PART_CenterCellsScrollViewer) as ScrollViewer;
            CellsPresenterPartCenter = GetTemplateChild(PART_CellsPresenterCenter) as VirtualizedDataGridCellsPresenter;
            CancelButtonPart = GetTemplateChild(PART_CancelButton) as Button;

            var scrollViewer = GetTemplateChild(PART_CenterCellsScrollViewer) as ScrollViewer;
            if (scrollViewer != null)
                scrollViewer.SizeChanged += CenterScrollViewerSizeChanged;

            if (CellsPresenterPartCenter != null)
                CellsPresenterPartCenter.MouseUp += OnCellsPresenterMouseUp;

            cellsPresenter = CellsPresenterPartCenter;
            UpdateStates(false);
        }

        protected virtual void OnIsEnabledChanged(
            DependencyPropertyChangedEventArgs e)
        {
        }

        #region public bool IsSelectionActive { get; set; }

        /// <summary>
        ///   Identifies the <see cref="IsSelectionActive"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSelectionActiveProperty =
            DependencyProperty.Register(
                nameof(IsSelectionActive),
                typeof(bool),
                typeof(VirtualizedDataGrid),
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
            var source = (VirtualizedDataGrid)d;
            source.CellsPresenterPartCenter?.OnIsSelectionActiveChanged();
        }

        #endregion

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

            IsSelectionActive = isSelectionActive;
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

            //var relativeTo = sender as VirtualizedDataGridCellsPresenter;
            //if (relativeTo != null) {
            //    VirtualizedDataGridColumnViewModel columnFromPosition = relativeTo.GetColumnFromPosition(e.GetPosition(relativeTo).X);
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
            CellsPresenterPartCenter?.PostUpdateRendering();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (cellsPresenter == null || viewModel == null)
                return;

            if (e.PropertyName == nameof(VirtualizedDataGridViewModel.RowCount)) {
                cellsPresenter.PostUpdateRendering();
            }
        }

        private static void OnCopyCellCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var grid = (VirtualizedDataGrid)sender;
            //grid.ViewModel.DoCopyToClipboard(HierarchicalDataGridViewModel.CopyBehavior.Cell);
            e.Handled = true;
        }

        private static void OnCopyCellExecuted(object sender, ExecutedRoutedEventArgs e)
        {
        }

        public double AutoSize(VirtualizedDataGridColumn column)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            if (!column.IsSafeToReadCellValuesFromUIThread)
                return 0;

            double newWidth = 0;
            if (CellsPresenterPartCenter != null)
                newWidth = CellsPresenterPartCenter.GetColumnAutoSize(column);

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
            VirtualizedDataGridColumnReorderingEventArgs e)
        {
            ColumnReordering?.Invoke(this, e);
        }

        protected internal virtual void OnColumnReordered(VirtualizedDataGridColumnEventArgs e)
        {
            ColumnReordered?.Invoke(this, e);
        }
    }

    public class VirtualizedDataGridColumnEventArgs : EventArgs
    {
        public VirtualizedDataGridColumnEventArgs(VirtualizedDataGridColumn column)
        {
            Column = column;
        }

        public VirtualizedDataGridColumn Column { get; }
    }

    public class VirtualizedDataGridColumnReorderingEventArgs
        : VirtualizedDataGridColumnEventArgs
    {
        public VirtualizedDataGridColumnReorderingEventArgs(
            VirtualizedDataGridColumn column)
            : base(column)
        {
        }

        public bool Cancel { get; set; }
    }

    public class ItemEventArgs<T> : EventArgs
    {
        public ItemEventArgs(T item)
        {
            Item = item;
        }

        public T Item { get; }
    }

    public delegate void ItemEventHandler<T>(object sender, ItemEventArgs<T> e);

    public interface IDataView
    {
        IDataViewColumnsCollection Columns { get; }
        IDataViewColumnsCollection VisibleColumns { get; }
        CellValue GetCellValue(int rowIndex, int columnIndex);
        void UpdateRowCount(int rows);
    }

    public interface IDataViewColumnsCollection
        : IReadOnlyList<IDataColumn>
    {
        int IndexOf(IDataColumn column);
    }

    public interface IDataColumn
    {
        string Name { get; }
        double Width { get; }
        bool IsVisible { get; }
        bool IsResizable { get; }
        TextAlignment TextAlignment { get; }
    }

    public class DataColumn : IDataColumn
    {
        public string Name { get; set; }
        public double Width { get; set; }
        public bool IsVisible { get; set; }
        public bool IsResizable { get; set; }
        public TextAlignment TextAlignment { get; set; }
    }

    public sealed class ScrollOffsetAccessor : FrameworkElement
    {
        #region public ScrollViewer ScrollViewer { get; set; }

        /// <summary>
        ///   Identifies the <see cref="ScrollViewer"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScrollViewerProperty =
            DependencyProperty.Register(
                nameof(ScrollViewer),
                typeof(ScrollViewer),
                typeof(ScrollOffsetAccessor),
                new PropertyMetadata(null, OnScrollViewerChanged));

        /// <summary>
        ///   Gets or sets the scroll viewer.
        /// </summary>
        public ScrollViewer ScrollViewer
        {
            get { return (ScrollViewer)GetValue(ScrollViewerProperty); }
            set { SetValue(ScrollViewerProperty, value); }
        }

        private static void OnScrollViewerChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (ScrollOffsetAccessor)d;
            source.OnScrollViewerChanged((ScrollViewer)e.NewValue);
        }

        private void OnScrollViewerChanged(ScrollViewer newValue)
        {
            BindingOperations.ClearBinding(this, HorizontalOffsetProperty);
            BindingOperations.ClearBinding(this, VerticalOffsetProperty);
            if (newValue == null)
                return;

            var horzBinding = new Binding("HorizontalOffset") {
                Source = newValue,
                Mode = BindingMode.OneWay
            };
            var vertBinding = new Binding("VerticalOffset") {
                Source = newValue,
                Mode = BindingMode.OneWay
            };
            SetBinding(HorizontalOffsetProperty, horzBinding);
            SetBinding(VerticalOffsetProperty, vertBinding);
        }

        #endregion

        #region public double HorizontalOffset { get; set; }

        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register(
                nameof(HorizontalOffset),
                typeof(double),
                typeof(ScrollOffsetAccessor),
                new PropertyMetadata(0.0, OnHorizontalOffsetChanged));

        public double HorizontalOffset
        {
            get { return (double)GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }

        private static void OnHorizontalOffsetChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (ScrollOffsetAccessor)d;
            source.OnHorizontalOffsetChanged((double)e.NewValue);
        }

        private void OnHorizontalOffsetChanged(double value)
        {
            if (ScrollViewer != null && value != ScrollViewer.HorizontalOffset)
                ScrollViewer.ScrollToHorizontalOffset(value);
        }

        #endregion

        #region public double VerticalOffset { get; set; }

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.Register(
                nameof(VerticalOffset),
                typeof(double),
                typeof(ScrollOffsetAccessor),
                new PropertyMetadata(0.0, OnVerticalOffsetChanged));

        public double VerticalOffset
        {
            get { return (double)GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }

        private static void OnVerticalOffsetChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (ScrollOffsetAccessor)d;
            source.OnVerticalOffsetChanged((double)e.NewValue);
        }

        private void OnVerticalOffsetChanged(double value)
        {
            if (ScrollViewer != null && value != ScrollViewer.VerticalOffset)
                ScrollViewer.ScrollToVerticalOffset(value);
        }

        #endregion
    }
}
