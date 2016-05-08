namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
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

        private Action<bool> updateStatesAction;

        private VirtualizedDataGridCellsPresenter cellsPresenter;
        private IVirtualizedDataGridViewModel viewModel;

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

        public static readonly RoutedCommand CopyCell =
            new RoutedCommand(nameof(CopyCell), typeof(VirtualizedDataGrid));

        #region public IVirtualizedDataGridViewModel ViewModel { get; set; }

        /// <summary>
        ///   Identifies the <see cref="ViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(IVirtualizedDataGridViewModel),
                typeof(VirtualizedDataGrid),
                new PropertyMetadata(
                    null,
                    (s, e) => ((VirtualizedDataGrid)s).OnViewModelChanged(e)));

        public IVirtualizedDataGridViewModel ViewModel
        {
            get { return (IVirtualizedDataGridViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        private void OnViewModelChanged(DependencyPropertyChangedEventArgs e)
        {
            CoerceValue(ViewModelEventSourceProperty);
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

        #region private IVirtualizedDataGridViewModel ViewModelEventSource { get; }

        private static readonly DependencyPropertyKey ViewModelEventSourcePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ViewModelEventSource),
                typeof(IVirtualizedDataGridViewModel),
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

        private IVirtualizedDataGridViewModel ViewModelEventSource =>
            (IVirtualizedDataGridViewModel)GetValue(ViewModelEventSourceProperty);

        private object CoerceViewModelEventSourceProperty(object baseValue)
        {
            return IsLoaded ? ViewModel : null;
        }

        private void ViewModelEventSourcePropertyChanged(
            DependencyPropertyChangedEventArgs e)
        {
            var oldValue = (IVirtualizedDataGridViewModel)e.OldValue;
            if (oldValue != null) {
                viewModel = null;
                oldValue.Updated -= OnViewModelUpdated;
                oldValue.PropertyChanged -= OnViewModelPropertyChanged;
                //BindingOperations.ClearBinding(oldValue, VirtualizedDataGridColumnsViewModel.ActualWidthProperty);
            }

            var newValue = (IVirtualizedDataGridViewModel)e.NewValue;
            if (newValue != null) {
                viewModel = newValue;
                newValue.Updated += OnViewModelUpdated;
                newValue.PropertyChanged += OnViewModelPropertyChanged;
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
            source.CellsPresenterPartCenter?.PostUpdateRendering();
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
            Loaded -= OnLoaded;
            CoerceValue(ViewModelEventSourceProperty);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;
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

            if (e.PropertyName == nameof(IVirtualizedDataGridViewModel.RowCount)) {
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

        public double AutoSize(VirtualizedDataGridColumnViewModel column)
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

        public event EventHandler<DragStartedEventArgs> ColumnHeaderDragStarted;
        public event EventHandler<DragDeltaEventArgs> ColumnHeaderDragDelta;
        public event EventHandler<DragCompletedEventArgs> ColumnHeaderDragCompleted;
        public event EventHandler<VirtualizedDataGridColumnReorderingEventArgs> ColumnReordering;
        public event EventHandler<VirtualizedDataGridColumnEventArgs> ColumnReordered;

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
        public VirtualizedDataGridColumnEventArgs(VirtualizedDataGridColumnViewModel column)
        {
            Column = column;
        }

        public VirtualizedDataGridColumnViewModel Column { get; }
    }

    public class VirtualizedDataGridColumnReorderingEventArgs
        : VirtualizedDataGridColumnEventArgs
    {
        public VirtualizedDataGridColumnReorderingEventArgs(
            VirtualizedDataGridColumnViewModel column)
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

    public interface IVirtualizedDataGridViewModel : INotifyPropertyChanged
    {
        event ItemEventHandler<bool> Updated;

        IVirtualizedDataGridCellsPresenterViewModel CellsPresenterViewModel { get; }
        VirtualizedDataGridColumnsViewModel ColumnsViewModel { get; }
        int RowCount { get; }

        void UpdateRowCount(int newCount);

        void RaiseUpdate(bool refreshViewModelFromModel);

        bool RequestUpdate(bool updateFromViewModel);
    }

    public class CellsPresenterRowSelection
    {
        private readonly IVirtualizedDataGridCellsPresenterViewModel cellsViewModel;
        private int selectionAnchorRowIndex;
        private readonly List<int> selectionChanges = new List<int>();
        private bool[] selectedIndices = new bool[0];
        private bool selectionRemoves;

        public CellsPresenterRowSelection(
            IVirtualizedDataGridCellsPresenterViewModel cellsViewModel)
        {
            this.cellsViewModel = cellsViewModel;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void ToggleSingle(int rowIndex, bool extend)
        {
            lock (selectedIndices) {
                NotifyCollectionChangedAction reset;
                if (!extend) {
                    Reset();
                    SetSelection(rowIndex);
                    reset = NotifyCollectionChangedAction.Add;
                    selectionRemoves = false;
                } else {
                    selectionRemoves = IsSelectedAtRow(rowIndex);
                    if (selectionRemoves) {
                        ClearSelection(rowIndex);
                        reset = NotifyCollectionChangedAction.Remove;
                    } else {
                        SetSelection(rowIndex);
                        reset = NotifyCollectionChangedAction.Add;
                    }
                }

                selectionAnchorRowIndex = rowIndex;
                cellsViewModel.FocusIndex = rowIndex;

                if (reset != NotifyCollectionChangedAction.Reset)
                    OnCollectionChanged(reset, rowIndex);
            }
        }

        public void ToggleExtent(int rowIndex, bool extend)
        {
            lock (selectedIndices) {
                int min = Math.Min(selectionAnchorRowIndex, rowIndex);
                int max = Math.Max(selectionAnchorRowIndex, rowIndex);
                ToggleRange(min, max, !extend, selectionRemoves, !selectionRemoves);
                cellsViewModel.FocusIndex = rowIndex;
            }
        }

        public bool Contains(int row)
        {
            lock (selectedIndices)
                return IsSelectedAtRow(row);
        }

        private bool IsSelectedAtRow(int row)
        {
            return row >= 0 && row < selectedIndices.Length && selectedIndices[row];
        }

        private void ClearSelection(int row)
        {
            SetSelection(row, false);
        }

        private void SetSelection(int row)
        {
            SetSelection(row, true);
        }

        private void SetSelection(int row, bool selected)
        {
            EnsureCapacity(row + 1);
            selectedIndices[row] = selected;
        }

        private void EnsureCapacity(int count)
        {
            if (selectedIndices.Length < count)
                Array.Resize(ref selectedIndices, count);
        }

        private void ToggleRange(
            int min, int max, bool clearFirst = false, bool shouldRemove = true,
            bool shouldAdd = true)
        {
            min = Math.Max(0, min);
            max = Math.Min(max, cellsViewModel.RowCount - 1);

            lock (selectedIndices) {
                if (clearFirst)
                    Reset();

                selectionChanges.Clear();
                for (int i = min; i <= max; ++i) {
                    bool flag = IsSelectedAtRow(i);

                    if (shouldRemove && flag) {
                        ClearSelection(i);
                        selectionChanges.Add(i);
                    }

                    if (shouldAdd && !flag) {
                        SetSelection(i);
                        selectionChanges.Add(i);
                    }
                }

                if (selectionChanges.Count > 0)
                    OnCollectionChanged(
                        NotifyCollectionChangedAction.Remove, selectionChanges);

                selectionChanges.Clear();
            }
        }

        public void ClearAll()
        {
            for (int i = 0; i < selectedIndices.Length; ++i)
                selectedIndices[i] = false;
        }

        private void Reset()
        {
            ClearAll();
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action)
        {
            CollectionChanged?.Invoke(
                this, new NotifyCollectionChangedEventArgs(action));
        }

        private void OnCollectionChanged(
            NotifyCollectionChangedAction action, int item)
        {
            CollectionChanged?.Invoke(
                this, new NotifyCollectionChangedEventArgs(action, item));
        }

        private void OnCollectionChanged(
            NotifyCollectionChangedAction action, IList<int> changes)
        {
            CollectionChanged?.Invoke(
                this, new NotifyCollectionChangedEventArgs(action, changes));
        }

        public void SelectRange(int min, int max)
        {
            ToggleRange(min, max, true, true, true);
        }
    }

    public interface IVirtualizedDataGridCellsPresenterViewModel
    {
        int FocusIndex { get; set; }
        int RowCount { get; }
        CellsPresenterRowSelection RowSelection { get; }
        void RequestUpdate(bool updateFromViewModel);
    }

    public interface IVirtualizedDataGridViewColumn
    {
        string Name { get; }
        double Width { get; }
        bool IsVisible { get; }
        bool IsResizable { get; }
    }

    public class VirtualizedDataGridCellsPresenterViewModel
        : DependencyObject, IVirtualizedDataGridCellsPresenterViewModel
    {
        private readonly IVirtualizedDataGridViewModel parent;

        public VirtualizedDataGridCellsPresenterViewModel(IVirtualizedDataGridViewModel parent)
        {
            this.parent = parent;

            RowSelection = new CellsPresenterRowSelection(this);
        }

        public event EventHandler FocusIndexChanged;

        #region public int FocusIndex { get; set; }

        /// <summary>
        ///   Identifies the <see cref="FocusIndex"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusIndexProperty =
            DependencyProperty.Register(
                nameof(FocusIndex),
                typeof(int),
                typeof(VirtualizedDataGridCellsPresenterViewModel),
                new PropertyMetadata(
                    Boxed.Int32Zero,
                    (d, e) => ((VirtualizedDataGridCellsPresenterViewModel)d).OnFocusIndexChanged(e)));

        /// <summary>
        ///   Gets or sets the focux index.
        /// </summary>
        public int FocusIndex
        {
            get { return (int)GetValue(FocusIndexProperty); }
            set { SetValue(FocusIndexProperty, value); }
        }

        private void OnFocusIndexChanged(DependencyPropertyChangedEventArgs e)
        {
            FocusIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        public int RowCount => parent.RowCount;

        public CellsPresenterRowSelection RowSelection { get; }

        public void RequestUpdate(bool updateFromViewModel)
        {
            VerifyAccess();
            //base.ValidateIsReady();
            parent.RequestUpdate(updateFromViewModel);
            //base.hdvViewModel.RequestUpdate(updateFromViewModel);
        }
    }

    public class VirtualizedDataGridViewColumn : IVirtualizedDataGridViewColumn
    {
        public VirtualizedDataGridViewColumn(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public double Width { get; set; }
        public bool IsResizable { get; set; }
        public bool IsVisible { get; set; }
    }

    public class NegativeDoubleValueConverter : IValueConverter
    {
        public static NegativeDoubleValueConverter Instance { get; } =
            new NegativeDoubleValueConverter();

        public object Convert(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            double? nullable = value as double?;
            return nullable.HasValue ? -nullable.GetValueOrDefault() : value;

        }

        public object ConvertBack(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            double? nullable = value as double?;
            return nullable.HasValue ? -nullable.GetValueOrDefault() : value;
        }
    }
}
