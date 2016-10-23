namespace EventTraceKit.VsExtension.Controls.Primitives
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
    using Windows;

    [TemplatePart(Name = PART_LeftHeaderGripper, Type = typeof(Thumb))]
    [TemplatePart(Name = PART_RightHeaderGripper, Type = typeof(Thumb))]
    internal class AsyncDataGridColumnHeader : ButtonBase
    {
        private const string PART_LeftHeaderGripper = "PART_LeftHeaderGripper";
        private const string PART_RightHeaderGripper = "PART_RightHeaderGripper";

        private AsyncDataGrid parentGrid;
        private AsyncDataGridColumnHeadersPresenter parentPresenter;

        private static readonly Lazy<Cursor> SplitCursorCache;

        static AsyncDataGridColumnHeader()
        {
            Type forType = typeof(AsyncDataGridColumnHeader);
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

        #region public AsyncDataGridColumn Column

        public static readonly DependencyProperty ColumnProperty =
            DependencyProperty.Register(
                nameof(Column),
                typeof(AsyncDataGridColumn),
                typeof(AsyncDataGridColumnHeader),
                new UIPropertyMetadata(null, OnColumnChanged));

        public AsyncDataGridColumn Column
        {
            get { return (AsyncDataGridColumn)GetValue(ColumnProperty); }
            set { SetValue(ColumnProperty, value); }
        }

        private static void OnColumnChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AsyncDataGridColumnHeader)d).OnColumnChanged(
                (AsyncDataGridColumn)e.OldValue,
                (AsyncDataGridColumn)e.NewValue);
        }

        private void OnColumnChanged(
            AsyncDataGridColumn oldValue,
            AsyncDataGridColumn newValue)
        {
            if (newValue != null) {
                IsResizable = newValue.IsResizable;
                IsKeySeparator = newValue.IsKeySeparator;
                IsFreezableAreaSeparator = newValue.IsFreezableAreaSeparator;
                IsExpanderHeader = newValue.IsExpanderHeader;
            }

            RightHeaderGripperVisibility =
                newValue?.IsResizable == true ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region public Thumb LeftHeaderGripperPart { get; private set; }

        private static readonly DependencyPropertyKey LeftHeaderGripperPartPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(LeftHeaderGripperPart),
                typeof(Thumb),
                typeof(AsyncDataGridColumnHeader),
                new UIPropertyMetadata(null));

        public static readonly DependencyProperty LeftHeaderGripperPartProperty =
            LeftHeaderGripperPartPropertyKey.DependencyProperty;

        public Thumb LeftHeaderGripperPart
        {
            get { return (Thumb)GetValue(LeftHeaderGripperPartProperty); }
            private set { SetValue(LeftHeaderGripperPartPropertyKey, value); }
        }

        #endregion

        #region public Thumb RightHeaderGripperPart { get; private set; }

        private static readonly DependencyPropertyKey RightHeaderGripperPartPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(RightHeaderGripperPart),
                typeof(Thumb),
                typeof(AsyncDataGridColumnHeader),
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
                typeof(AsyncDataGridColumnHeader),
                new PropertyMetadata(Visibility.Collapsed));

        public static readonly DependencyProperty RightHeaderGripperVisibilityProperty =
            RightHeaderGripperVisibilityPropertyKey.DependencyProperty;

        public Visibility RightHeaderGripperVisibility
        {
            get { return (Visibility)GetValue(RightHeaderGripperVisibilityProperty); }
            private set { SetValue(RightHeaderGripperVisibilityPropertyKey, value); }
        }

        #endregion

        #region public bool IsResizable { get; private set; }

        private static readonly DependencyPropertyKey IsResizablePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsResizable),
                typeof(bool),
                typeof(AsyncDataGridColumnHeader),
                new PropertyMetadata(Boxed.Bool(true)));

        public static readonly DependencyProperty IsResizableProperty =
            IsResizablePropertyKey.DependencyProperty;

        public bool IsResizable
        {
            get { return (bool)GetValue(IsResizableProperty); }
            private set { SetValue(IsResizablePropertyKey, Boxed.Bool(value)); }
        }

        #endregion

        #region public bool IsKeySeparator { get; private set; }

        private static readonly DependencyPropertyKey IsSeparatorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsKeySeparator),
                typeof(bool),
                typeof(AsyncDataGridColumnHeader),
                new PropertyMetadata(Boxed.False));

        public static readonly DependencyProperty IsKeySeparatorProperty =
            IsSeparatorPropertyKey.DependencyProperty;

        public bool IsKeySeparator
        {
            get { return (bool)GetValue(IsKeySeparatorProperty); }
            private set { SetValue(IsSeparatorPropertyKey, Boxed.Bool(value)); }
        }

        #endregion

        #region public bool IsFreezableAreaSeparator { get; private set; }

        private static readonly DependencyPropertyKey IsFreezableAreaSeparatorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsFreezableAreaSeparator),
                typeof(bool),
                typeof(AsyncDataGridColumnHeader),
                new PropertyMetadata(Boxed.False));

        public static readonly DependencyProperty IsFreezableAreaSeparatorProperty =
            IsFreezableAreaSeparatorPropertyKey.DependencyProperty;

        public bool IsFreezableAreaSeparator
        {
            get { return (bool)GetValue(IsFreezableAreaSeparatorProperty); }
            private set { SetValue(IsFreezableAreaSeparatorPropertyKey, Boxed.Bool(value)); }
        }

        #endregion

        #region public bool IsExpanderHeader { get; private set; }

        private static readonly DependencyPropertyKey IsExpanderHeaderPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsExpanderHeader),
                typeof(bool),
                typeof(AsyncDataGridColumnHeader),
                new PropertyMetadata(Boxed.False));

        public static readonly DependencyProperty IsExpanderHeaderProperty =
            IsExpanderHeaderPropertyKey.DependencyProperty;

        public bool IsExpanderHeader
        {
            get { return (bool)GetValue(IsExpanderHeaderProperty); }
            private set { SetValue(IsExpanderHeaderPropertyKey, Boxed.Bool(value)); }
        }

        #endregion

        #region public int SortPriority { get; private set; }

        private static readonly DependencyPropertyKey SortPriorityPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(SortPriority),
                typeof(int),
                typeof(AsyncDataGridColumnHeader),
                new PropertyMetadata(Boxed.Int32Minus1));

        public static readonly DependencyProperty SortPriorityProperty =
            SortPriorityPropertyKey.DependencyProperty;

        public int SortPriority
        {
            get { return (int)GetValue(SortPriorityProperty); }
            private set { SetValue(SortPriorityPropertyKey, Boxed.Int32(value)); }
        }

        #endregion

        #region public Brush SeparatorBrush { get; set; }

        public static readonly DependencyProperty SeparatorBrushProperty =
            DependencyProperty.Register(
                nameof(SeparatorBrush),
                typeof(Brush),
                typeof(AsyncDataGridColumnHeader),
                new FrameworkPropertyMetadata(null));

        public Brush SeparatorBrush
        {
            get { return (Brush)GetValue(SeparatorBrushProperty); }
            set { SetValue(SeparatorBrushProperty, value); }
        }

        #endregion

        #region public Visibility SeparatorVisibility { get; set; }

        public static readonly DependencyProperty SeparatorVisibilityProperty =
            DependencyProperty.Register(
                nameof(SeparatorVisibility),
                typeof(Visibility),
                typeof(AsyncDataGridColumnHeader),
                new FrameworkPropertyMetadata(Visibility.Visible));

        public Visibility SeparatorVisibility
        {
            get { return (Visibility)GetValue(SeparatorVisibilityProperty); }
            set { SetValue(SeparatorVisibilityProperty, value); }
        }

        #endregion

        internal AsyncDataGrid ParentGrid =>
            parentGrid ?? (parentGrid = this.FindVisualParent<AsyncDataGrid>());

        internal AsyncDataGridColumnHeadersPresenter ParentPresenter =>
            parentPresenter ??
            (parentPresenter = (AsyncDataGridColumnHeadersPresenter)
                ItemsControl.ItemsControlFromItemContainer(this));

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            LeftHeaderGripperPart = GetTemplateChild(PART_LeftHeaderGripper) as Thumb;
            if (LeftHeaderGripperPart != null) {
                LeftHeaderGripperPart.DragStarted += OnLeftHeaderGripperPartDragStarted;
                LeftHeaderGripperPart.DragDelta += OnLeftHeaderGripperPartDragDelta;
                LeftHeaderGripperPart.DragCompleted += OnLeftHeaderGripperPartDragCompleted;
                LeftHeaderGripperPart.MouseDoubleClick += OnLeftHeaderGripperPartMouseDoubleClick;
            }

            RightHeaderGripperPart = GetTemplateChild(PART_RightHeaderGripper) as Thumb;
            if (RightHeaderGripperPart != null) {
                RightHeaderGripperPart.DragStarted += OnRightHeaderGripperPartDragStarted;
                RightHeaderGripperPart.DragDelta += OnRightHeaderGripperPartDragDelta;
                RightHeaderGripperPart.DragCompleted += OnRightHeaderGripperPartDragCompleted;
                RightHeaderGripperPart.MouseDoubleClick += OnRightHeaderGripperPartMouseDoubleClick;
            }
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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            var parent = ParentPresenter;
            if (parent != null) {
                parent.OnHeaderKeyDown(e);
                e.Handled = true;
            }
        }

        private void OnLeftHeaderGripperPartDragStarted(
            object sender, DragStartedEventArgs e)
        {
            if (!e.Handled) {
                Column?.BeginPossiblyResizing();
                e.Handled = true;
            }
        }

        private void OnLeftHeaderGripperPartDragDelta(
            object sender, DragDeltaEventArgs e)
        {
            if (!e.Handled && Column != null && Column.IsResizing) {
                Column.Width -= e.HorizontalChange;
                e.Handled = true;
            }
        }

        private void OnLeftHeaderGripperPartDragCompleted(
            object sender, DragCompletedEventArgs e)
        {
            if (e.Handled)
                return;

            if (Column != null && Column.IsResizing) {
                Column.EndPossiblyResizing(-e.HorizontalChange);
                e.Handled = true;
            }
        }

        private void OnLeftHeaderGripperPartMouseDoubleClick(
            object sender, MouseButtonEventArgs e)
        {
            if (Column == null || e.Handled) // || !ViewModel.AsyncDataViewModel.IsReady)
                return;

            double change = ParentGrid.AutoSize(Column);
            Column.EndPossiblyResizing(change);
            e.Handled = true;
        }

        private void OnRightHeaderGripperPartDragStarted(
            object sender, DragStartedEventArgs e)
        {
            if (!e.Handled) {
                Column?.BeginPossiblyResizing();
                e.Handled = true;
            }
        }

        private void OnRightHeaderGripperPartDragDelta(
            object sender, DragDeltaEventArgs e)
        {
            if (!e.Handled && Column != null && Column.IsResizing) {
                Column.Width += e.HorizontalChange;
                e.Handled = true;
            }
        }

        private void OnRightHeaderGripperPartDragCompleted(
            object sender, DragCompletedEventArgs e)
        {
            if (e.Handled)
                return;

            if (Column != null && Column.IsResizing) {
                Column.EndPossiblyResizing(e.HorizontalChange);
                e.Handled = true;
            }
        }

        private void OnRightHeaderGripperPartMouseDoubleClick(
            object sender, MouseButtonEventArgs e)
        {
            if (Column == null || e.Handled) // || !ViewModel.AsyncDataViewModel.IsReady)
                return;

            double change = ParentGrid.AutoSize(Column);
            Column.EndPossiblyResizing(change);
            e.Handled = true;
        }
    }
}
