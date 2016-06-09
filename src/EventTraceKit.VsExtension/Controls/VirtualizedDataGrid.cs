namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using EventTraceKit.VsExtension.Collections;
    using Primitives;

    [TemplatePart(Name = PART_CancelButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_CellsScrollViewer, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = PART_CellsPresenter, Type = typeof(VirtualizedDataGridCellsPresenter))]
    [TemplatePart(Name = PART_ColumnHeadersPresenter, Type = typeof(VirtualizedDataGridColumnHeadersPresenter))]
    [TemplateVisualState(Name = STATE_Ready, GroupName = STATE_GROUP_Responsiveness)]
    [TemplateVisualState(Name = STATE_Processing, GroupName = STATE_GROUP_Responsiveness)]
    public class VirtualizedDataGrid : Control
    {
        private const string PART_CancelButton = "PART_CancelButton";
        private const string PART_CellsPresenter = "PART_CellsPresenter";
        private const string PART_CellsScrollViewer = "PART_CellsScrollViewer";
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

        #region public FontFamily RowFontFamily { get; set; }

        /// <summary>
        ///   Identifies the <see cref="RowFontFamily"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowFontFamilyProperty =
            DependencyProperty.Register(
                nameof(RowFontFamily),
                typeof(FontFamily),
                typeof(VirtualizedDataGrid),
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
                typeof(VirtualizedDataGrid),
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
                nameof(CellsScrollViewer),
                typeof(ScrollViewer),
                typeof(VirtualizedDataGrid),
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
                nameof(CellsPresenter), typeof(VirtualizedDataGridCellsPresenter),
                typeof(VirtualizedDataGrid), PropertyMetadataUtils.DefaultNull);

        /// <summary>
        ///   Identifies the <see cref="CellsPresenter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellsPresenterProperty =
            CellsPresenterPartCenterPropertyKey.DependencyProperty;

        public VirtualizedDataGridCellsPresenter CellsPresenter
        {
            get { return (VirtualizedDataGridCellsPresenter)GetValue(CellsPresenterProperty); }
            private set { SetValue(CellsPresenterPartCenterPropertyKey, value); }
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ColumnHeadersPresenter = GetTemplateChild(PART_ColumnHeadersPresenter) as VirtualizedDataGridColumnHeadersPresenter;
            CellsScrollViewer = GetTemplateChild(PART_CellsScrollViewer) as ScrollViewer;
            CellsPresenter = GetTemplateChild(PART_CellsPresenter) as VirtualizedDataGridCellsPresenter;
            CancelButtonPart = GetTemplateChild(PART_CancelButton) as Button;

            if (CellsScrollViewer != null) {
                CellsScrollViewer.SizeChanged += CenterScrollViewerSizeChanged;

                var horizontalOffsetBinding = new Binding(nameof(CellsScrollViewer.HorizontalOffset));
                horizontalOffsetBinding.Source = CellsScrollViewer;
                SetBinding(HorizontalScrollOffsetProperty, horizontalOffsetBinding);
            }

            if (CellsPresenter != null)
                CellsPresenter.MouseUp += OnCellsPresenterMouseUp;

            cellsPresenter = CellsPresenter;
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
            source.CellsPresenter?.OnIsSelectionActiveChanged();
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

        #region internal double HorizontalScrollOffset { get; }

        internal static readonly DependencyProperty HorizontalScrollOffsetProperty =
            DependencyProperty.Register(
                nameof(HorizontalScrollOffset),
                typeof(double),
                typeof(VirtualizedDataGrid),
                new FrameworkPropertyMetadata(0.0, OnHorizontalOffsetChanged));

        internal double HorizontalScrollOffset => (double)GetValue(HorizontalScrollOffsetProperty);

        private static void OnHorizontalOffsetChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var source = (VirtualizedDataGrid)d;
            var columnHeadersPresenter = source.ColumnHeadersPresenter;
            if (columnHeadersPresenter != null) {
                columnHeadersPresenter.InvalidateArrange();

                var itemsHost = columnHeadersPresenter.InternalItemsHost;
                itemsHost?.InvalidateMeasure();
                itemsHost?.InvalidateArrange();
            }
        }

        #endregion

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
            VirtualizedDataGridColumnReorderingEventArgs e)
        {
            ColumnReordering?.Invoke(this, e);
        }

        protected internal virtual void OnColumnReordered(VirtualizedDataGridColumnEventArgs e)
        {
            ColumnReordered?.Invoke(this, e);
        }
    }

    [TemplatePart(Name = PART_ColumnHeadersPresenter, Type = typeof(VirtualizedDataGridColumnHeadersPresenter))]
    public class VirtualizedDataGridScrollViewer : ScrollViewer
    {
        private const string PART_ColumnHeadersPresenter = "PART_ColumnHeadersPresenter";

        static VirtualizedDataGridScrollViewer()
        {
            Type forType = typeof(VirtualizedDataGridScrollViewer);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(forType));
        }

        #region public VirtualizedDataGridColumnHeadersPresenter ColumnHeadersPresenter { get; private set; }

        private static readonly DependencyPropertyKey ColumnHeadersPresenterPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ColumnHeadersPresenter),
                typeof(VirtualizedDataGridColumnHeadersPresenter),
                typeof(VirtualizedDataGridScrollViewer),
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

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ColumnHeadersPresenter = GetTemplateChild(PART_ColumnHeadersPresenter) as VirtualizedDataGridColumnHeadersPresenter;
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

    public interface IDependencyObjectCustomSerializerAccess
    {
        object GetValue(DependencyProperty dp);
        bool ShouldSerializeProperty(DependencyProperty dp);
    }

    public abstract class FreezableCustomSerializerAccessBase
        : Freezable, IDependencyObjectCustomSerializerAccess
    {
        protected FreezableCustomSerializerAccessBase()
        {
        }

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

        protected override void CloneCore(Freezable sourceFreezable)
        {
            base.CloneCore(sourceFreezable);
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

    internal class ComparisonUtils
    {
        public static bool CompareValueT<T>(
            out int cmp, T first, T second) where T : struct, IComparable<T>
        {
            cmp = first.CompareTo(second);
            return cmp == 0;
        }

        public static bool Compare<T>(out int cmp, T first, T second)
            where T : IComparable
        {
            cmp = first.CompareTo(second);
            return cmp == 0;
        }

        public static bool CompareT<T>(
            out int cmp, T x, T y) where T : class, IComparable<T>
        {
            if (x == null || y == null)
                cmp = (x == null ? 1 : 0) - (y == null ? 1 : 0);
            else
                cmp = x.CompareTo(y);

            return cmp == 0;
        }

        public static bool CombineSequenceComparisonT<T>(
            out int cmp, IEnumerable<T> first, IEnumerable<T> second)
            where T : IComparable<T>
        {
            cmp = first.SequenceCompare(second);
            return cmp == 0;
        }
    }

    public interface IDataView
    {
        IDataViewColumnsCollection Columns { get; }
        IDataViewColumnsCollection VisibleColumns { get; }
        bool IsReady { get; }
        HdvViewModelPreset HdvViewModelPreset { get; set; }
        VirtualizedDataGridColumnsViewModel ColumnsViewModel { set; }

        CellValue GetCellValue(int rowIndex, int columnIndex);
        void UpdateRowCount(int rows);
        event ItemEventHandler<bool> Updated;
        bool RequestUpdate(bool updateFromViewModel);
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
}
