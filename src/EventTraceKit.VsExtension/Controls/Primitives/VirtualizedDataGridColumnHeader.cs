namespace EventTraceKit.VsExtension.Controls.Primitives
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;

    [TemplatePart(Name = PART_RightHeaderGripper, Type = typeof(Thumb))]
    internal class VirtualizedDataGridColumnHeader : ButtonBase
    {
        private const string PART_RightHeaderGripper = "PART_RightHeaderGripper";

        private VirtualizedDataGrid parentGrid;
        private VirtualizedDataGridColumnHeadersPresenter parentPresenter;

        private static readonly Lazy<Cursor> SplitCursorCache;

        static VirtualizedDataGridColumnHeader()
        {
            Type forType = typeof(VirtualizedDataGridColumnHeader);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(forType));

            FocusableProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(Boxed.False));

            ColumnNameMaxWidthConverter = new DelegateValueConverter<double, double>(
                actualWidth => Math.Max(0, actualWidth - 13));

            SplitCursorCache = new Lazy<Cursor>(
                () => ResourceUtils.LoadCursorFromResource(
                    forType, "Split.cur"));
        }

        public static DelegateValueConverter<double, double> ColumnNameMaxWidthConverter { get; }

        public static Cursor SplitCursor => SplitCursorCache.Value;

        #region public VirtualizedDataGridColumnViewModel ViewModel

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(VirtualizedDataGridColumn),
                typeof(VirtualizedDataGridColumnHeader),
                new UIPropertyMetadata(null, OnViewModelChanged));

        public VirtualizedDataGridColumn ViewModel
        {
            get { return (VirtualizedDataGridColumn)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        private static void OnViewModelChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VirtualizedDataGridColumnHeader)d).OnViewModelChanged(
                (VirtualizedDataGridColumn)e.OldValue,
                (VirtualizedDataGridColumn)e.NewValue);
        }

        private void OnViewModelChanged(
            VirtualizedDataGridColumn oldValue,
            VirtualizedDataGridColumn newValue)
        {
            if (newValue != null) {
                IsResizable = newValue.IsResizable;
                IsSeparator = newValue.IsSeparator;
                IsFreezableAreaSeparator = newValue.IsFreezableAreaSeparator;
                IsExpanderHeader = newValue.IsExpanderHeader;
            }

            RightHeaderGripperVisibility =
                newValue?.IsResizable == true ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region public Thumb RightHeaderGripperPart

        private static readonly DependencyPropertyKey RightHeaderGripperPartPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(RightHeaderGripperPart),
                typeof(Thumb),
                typeof(VirtualizedDataGridColumnHeader),
                new UIPropertyMetadata(null));

        public static readonly DependencyProperty RightHeaderGripperPartProperty =
            RightHeaderGripperPartPropertyKey.DependencyProperty;

        public Thumb RightHeaderGripperPart
        {
            get { return (Thumb)GetValue(RightHeaderGripperPartProperty); }
            private set { SetValue(RightHeaderGripperPartPropertyKey, value); }
        }

        #endregion

        #region public Visibility RightHeaderGripperVisibility { get; private set; }

        private static readonly DependencyPropertyKey RightHeaderGripperVisibilityPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(RightHeaderGripperVisibility),
                typeof(Visibility),
                typeof(VirtualizedDataGridColumnHeader),
                new PropertyMetadata(Visibility.Collapsed));

        public static readonly DependencyProperty RightHeaderGripperVisibilityProperty =
            RightHeaderGripperVisibilityPropertyKey.DependencyProperty;

        public Visibility RightHeaderGripperVisibility
        {
            get { return (Visibility)GetValue(RightHeaderGripperVisibilityProperty); }
            private set { SetValue(RightHeaderGripperVisibilityPropertyKey, value); }
        }

        #endregion

        #region public bool IsResizable

        private static readonly DependencyPropertyKey IsResizablePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsResizable),
                typeof(bool),
                typeof(VirtualizedDataGridColumnHeader),
                new PropertyMetadata(Boxed.Bool(true)));

        public static readonly DependencyProperty IsResizableProperty =
            IsResizablePropertyKey.DependencyProperty;

        public bool IsResizable
        {
            get { return (bool)GetValue(IsResizableProperty); }
            private set { SetValue(IsResizablePropertyKey, Boxed.Bool(value)); }
        }

        #endregion

        #region public bool IsSeparator

        private static readonly DependencyPropertyKey IsSeparatorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsSeparator),
                typeof(bool),
                typeof(VirtualizedDataGridColumnHeader),
                new PropertyMetadata(Boxed.False));

        public static readonly DependencyProperty IsSeparatorProperty =
            IsSeparatorPropertyKey.DependencyProperty;

        public bool IsSeparator
        {
            get { return (bool)GetValue(IsSeparatorProperty); }
            private set { SetValue(IsSeparatorPropertyKey, Boxed.Bool(value)); }
        }

        #endregion

        #region public bool IsFreezableAreaSeparator

        private static readonly DependencyPropertyKey IsFreezableAreaSeparatorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsFreezableAreaSeparator),
                typeof(bool),
                typeof(VirtualizedDataGridColumnHeader),
                new PropertyMetadata(Boxed.False));

        public static readonly DependencyProperty IsFreezableAreaSeparatorProperty =
            IsFreezableAreaSeparatorPropertyKey.DependencyProperty;

        public bool IsFreezableAreaSeparator
        {
            get { return (bool)GetValue(IsFreezableAreaSeparatorProperty); }
            private set { SetValue(IsFreezableAreaSeparatorPropertyKey, Boxed.Bool(value)); }
        }

        #endregion

        #region public bool IsExpanderHeader

        private static readonly DependencyPropertyKey IsExpanderHeaderPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsExpanderHeader),
                typeof(bool),
                typeof(VirtualizedDataGridColumnHeader),
                new PropertyMetadata(Boxed.False));

        public static readonly DependencyProperty IsExpanderHeaderProperty =
            IsExpanderHeaderPropertyKey.DependencyProperty;

        public bool IsExpanderHeader
        {
            get { return (bool)GetValue(IsExpanderHeaderProperty); }
            private set { SetValue(IsExpanderHeaderPropertyKey, Boxed.Bool(value)); }
        }

        #endregion

        #region public int SortPriority

        private static readonly DependencyPropertyKey SortPriorityPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(SortPriority),
                typeof(int),
                typeof(VirtualizedDataGridColumnHeader),
                new PropertyMetadata(Boxed.Int32Minus1));

        public static readonly DependencyProperty SortPriorityProperty =
            SortPriorityPropertyKey.DependencyProperty;

        public int SortPriority
        {
            get { return (int)GetValue(SortPriorityProperty); }
            private set { SetValue(SortPriorityPropertyKey, Boxed.Int32(value)); }
        }

        #endregion

        #region public Brush SeparatorBrush

        public static readonly DependencyProperty SeparatorBrushProperty =
            DependencyProperty.Register(
                nameof(SeparatorBrush),
                typeof(Brush),
                typeof(VirtualizedDataGridColumnHeader),
                new FrameworkPropertyMetadata(null));

        public Brush SeparatorBrush
        {
            get { return (Brush)GetValue(SeparatorBrushProperty); }
            set { SetValue(SeparatorBrushProperty, value); }
        }

        #endregion

        #region public Visibility SeparatorVisibility

        public static readonly DependencyProperty SeparatorVisibilityProperty =
            DependencyProperty.Register(
                nameof(SeparatorVisibility),
                typeof(Visibility),
                typeof(VirtualizedDataGridColumnHeader),
                new FrameworkPropertyMetadata(Visibility.Visible));

        public Visibility SeparatorVisibility
        {
            get { return (Visibility)GetValue(SeparatorVisibilityProperty); }
            set { SetValue(SeparatorVisibilityProperty, value); }
        }

        #endregion

        internal VirtualizedDataGrid ParentGrid =>
            parentGrid ?? (parentGrid = this.FindVisualParent<VirtualizedDataGrid>());

        internal VirtualizedDataGridColumnHeadersPresenter ParentPresenter =>
            parentPresenter ??
            (parentPresenter = (VirtualizedDataGridColumnHeadersPresenter)
                ItemsControl.ItemsControlFromItemContainer(this));

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            RightHeaderGripperPart = GetTemplateChild(PART_RightHeaderGripper) as Thumb;
            if (RightHeaderGripperPart != null) {
                RightHeaderGripperPart.DragStarted += OnRightHeaderGripperPartDragStarted;
                RightHeaderGripperPart.DragDelta += OnRightHeaderGripperPartDragDelta;
                RightHeaderGripperPart.DragCompleted += OnRightHeaderGripperPartDragCompleted;
                RightHeaderGripperPart.MouseDoubleClick += OnRightHeaderGripperPartMouseDoubleClick;
            }
        }

        protected override void OnClick()
        {
            //if (!isDragging) {
            base.OnClick();
            //VirtualizedDataGridColumnViewModel viewModel = ViewModel;
            //if (viewModel != null) {
            //    Action method = null;
            //    var grid = this.FindAncestor<VirtualizedDataGrid>();
            //    viewModel.OnClick(this);
            //    if (grid != null) {
            //        if (method == null) {
            //            method = delegate {
            //                VirtualizedDataGridColumnHeader header = grid.TryFindColumnHeaderByOldIdentity(viewModel);
            //                if (header != null) {
            //                    header.Focus();
            //                }
            //            };
            //        }
            //        Dispatcher.BeginInvoke(method, DispatcherPriority.Input, new object[0]);
            //    }
            //}
            //}
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            var parent = ParentPresenter;
            if (parent != null) {
                if (ClickMode == ClickMode.Hover &&
                    e.ButtonState == MouseButtonState.Pressed)
                    CaptureMouse();

                parent.OnHeaderMouseLeftButtonDown(e, this);
                e.Handled = true;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            var parent = ParentPresenter;
            if (parent != null) {
                if (ClickMode == ClickMode.Hover && IsMouseCaptured)
                    ReleaseMouseCapture();

                parent.OnHeaderMouseLeftButtonUp(e);
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var parent = ParentPresenter;
            if (parent != null) {
                parent.OnHeaderMouseMove(e);
                e.Handled = true;
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);

            var parent = ParentPresenter;
            if (parent != null) {
                parent.OnHeaderLostMouseCapture(e);
                e.Handled = true;
            }
        }

        private void OnRightHeaderGripperPartDragStarted(
            object sender, DragStartedEventArgs e)
        {
            if (!e.Handled) {
                ViewModel?.BeginPossiblyResizing();
                e.Handled = true;
            }
        }

        private void OnRightHeaderGripperPartDragDelta(
            object sender, DragDeltaEventArgs e)
        {
            if (!e.Handled && ViewModel != null && ViewModel.IsResizing) {
                ViewModel.Width += e.HorizontalChange;
                e.Handled = true;
            }
        }

        private void OnRightHeaderGripperPartDragCompleted(
            object sender, DragCompletedEventArgs e)
        {
            if (e.Handled)
                return;

            if (ViewModel != null && ViewModel.IsResizing) {
                ViewModel.EndPossiblyResizing(e.HorizontalChange);
                e.Handled = true;
            }

            RightHeaderGripperPart.ClearValue(BackgroundProperty);
        }

        private void OnRightHeaderGripperPartMouseDoubleClick(
            object sender, MouseButtonEventArgs e)
        {
            if (ViewModel == null || e.Handled) // || !ViewModel.HdvViewModel.IsReady)
                return;

            double change = ParentGrid.AutoSize(ViewModel);
            ViewModel.EndPossiblyResizing(change);
            e.Handled = true;
        }
    }
}
