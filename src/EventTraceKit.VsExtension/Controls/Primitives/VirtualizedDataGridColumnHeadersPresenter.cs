namespace EventTraceKit.VsExtension.Controls.Primitives
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;

    public class VirtualizedDataGridColumnHeadersPresenter : ItemsControl
    {
        private VirtualizedDataGrid parentGrid;
        private HeaderDragContext headerDragCtx;

        static VirtualizedDataGridColumnHeadersPresenter()
        {
            Type forType = typeof(VirtualizedDataGridColumnHeadersPresenter);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(forType));

            // Arrange the headers in reverse order, i.e. from right to left,
            // to make the left header's gripper overlay the right header.
            var panelFactory = new FrameworkElementFactory(typeof(ReversedStackPanel));
            panelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            var panelTemplate = new ItemsPanelTemplate(panelFactory);
            panelTemplate.Seal();
            ItemsPanelProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(panelTemplate));
        }

        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            if (!e.Handled) {
                var chooser = new ColumnChooser(ViewModel.ConfigurableColumns, ViewModel.DataView) {
                    Placement = PlacementMode.MousePoint,
                    PlacementTarget = VisualTreeHelper.GetParent(this) as UIElement
                };
                chooser.IsOpen = true;
                e.Handled = true;
            }

            base.OnContextMenuOpening(e);
        }

        #region public VirtualizedDataGridColumnsViewModel ViewModel { get; set; }

        /// <summary>
        ///   Identifies the <see cref="ViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(VirtualizedDataGridColumnsViewModel),
                typeof(VirtualizedDataGridColumnHeadersPresenter),
                PropertyMetadataUtils.DefaultNull);

        /// <summary>
        ///   Gets or sets the columns view model.
        /// </summary>
        public VirtualizedDataGridColumnsViewModel ViewModel
        {
            get { return (VirtualizedDataGridColumnsViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        #endregion

        #region public VirtualizedDataGridColumnViewModel ExpanderHeaderViewModel { get; set; }

        /// <summary>
        ///   Identifies the <see cref="ExpanderHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpanderHeaderProperty =
            DependencyProperty.Register(
                nameof(ExpanderHeader),
                typeof(VirtualizedDataGridColumn),
                typeof(VirtualizedDataGridColumnHeadersPresenter),
                PropertyMetadataUtils.DefaultNull);

        /// <summary>
        ///   Gets or sets the expander header column.
        /// </summary>
        public VirtualizedDataGridColumn ExpanderHeader
        {
            get { return (VirtualizedDataGridColumn)GetValue(ExpanderHeaderProperty); }
            set { SetValue(ExpanderHeaderProperty, value); }
        }

        #endregion

        internal VirtualizedDataGrid ParentGrid =>
            parentGrid ?? (parentGrid = this.FindParent<VirtualizedDataGrid>());

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new VirtualizedDataGridColumnHeader();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is VirtualizedDataGridColumnHeader;
        }

        protected override void ClearContainerForItemOverride(
            DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);
            element.ClearValue(WidthProperty);
            element.ClearValue(ContentControl.ContentProperty);
        }

        protected override void PrepareContainerForItemOverride(
            DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            var viewModel = item as VirtualizedDataGridColumn;
            if (viewModel == null)
                throw new InvalidOperationException("Invalid item type.");

            var header = (VirtualizedDataGridColumnHeader)element;
            header.SnapsToDevicePixels = SnapsToDevicePixels;
            header.UseLayoutRounding = UseLayoutRounding;
            header.Column = viewModel;

            var widthBinding = new Binding(nameof(Width)) {
                Source = viewModel,
                Mode = BindingMode.TwoWay
            };
            header.SetBinding(WidthProperty, widthBinding);

            var contentBinding = new Binding(nameof(viewModel.ColumnName)) {
                Source = viewModel
            };
            header.SetBinding(ContentControl.ContentProperty, contentBinding);
        }

        internal void OnHeaderMouseLeftButtonDown(
            MouseButtonEventArgs e, VirtualizedDataGridColumnHeader header)
        {
            if (ParentGrid == null)
                return;

            if (header != null)
                headerDragCtx.PrepareDrag(header, e.GetPosition(this));
            else
                ClearColumnHeaderDragInfo();
        }

        internal void OnHeaderMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (headerDragCtx.IsDragging) {
                headerDragCtx.CurrentPosition = e.GetPosition(this);
                FinishColumnHeaderDrag(false);
            } else
                ClearColumnHeaderDragInfo();
        }

        internal void OnHeaderMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !headerDragCtx.IsActive)
                return;

            headerDragCtx.CurrentPosition = e.GetPosition(this);

            if (headerDragCtx.IsPreparing) {
                if (ShouldStartColumnHeaderDrag(headerDragCtx.CurrentPosition,
                                                headerDragCtx.StartPosition))
                    StartColumnHeaderDrag();
                return;
            }

            var deltaX = headerDragCtx.CurrentPosition.X - headerDragCtx.StartPosition.X;
            GetTranslation(headerDragCtx.DraggedHeader).X = deltaX;

            int targetIndex = FindHeaderIndex(headerDragCtx.CurrentPosition);
            if (targetIndex != -1)
                headerDragCtx.AnimateDrag(targetIndex);
        }

        internal void OnHeaderLostMouseCapture(MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed &&
                headerDragCtx.IsDragging)
                FinishColumnHeaderDrag(true);
        }

        private void StartColumnHeaderDrag()
        {
            var reorderingEventArgs = new VirtualizedDataGridColumnReorderingEventArgs(
                headerDragCtx.DraggedHeader.Column);

            ParentGrid.OnColumnReordering(reorderingEventArgs);
            if (reorderingEventArgs.Cancel) {
                FinishColumnHeaderDrag(true);
                return;
            }

            var dragStartedEventArgs = new DragStartedEventArgs(
                headerDragCtx.StartPosition.X,
                headerDragCtx.StartPosition.Y);
            ParentGrid.OnColumnHeaderDragStarted(dragStartedEventArgs);

            headerDragCtx.StartDrag(ItemContainerGenerator);
        }

        private VirtualizedDataGridColumnHeader HeaderFromIndex(int index)
        {
            var container = ItemContainerGenerator.ContainerFromIndex(index);
            return (VirtualizedDataGridColumnHeader)container;
        }

        private static bool ShouldStartColumnHeaderDrag(
            Point currentPos, Point originalPos)
        {
            double deltaX = Math.Abs(currentPos.X - originalPos.X);
            return deltaX.GreaterThan(
                SystemParameters.MinimumHorizontalDragDistance);
        }

        private void FinishColumnHeaderDrag(bool isCancel)
        {
            int targetIndex = FindHeaderIndex(headerDragCtx.CurrentPosition);

            var delta = headerDragCtx.CurrentPosition - headerDragCtx.StartPosition;
            var dragCompletedEventArgs = new DragCompletedEventArgs(
                delta.X, delta.Y, isCancel);
            ParentGrid.OnColumnHeaderDragCompleted(dragCompletedEventArgs);

            headerDragCtx.Reset();

            if (!isCancel && targetIndex != headerDragCtx.DraggedHeaderIndex) {
                var srcColumn = headerDragCtx.DraggedHeader.Column;
                var dstColumn = HeaderFromIndex(targetIndex).Column;
                ViewModel.TryMoveColumn(srcColumn, dstColumn);

                dragCompletedEventArgs.Handled = true;
                var columnEventArgs = new VirtualizedDataGridColumnEventArgs(srcColumn);
                ParentGrid.OnColumnReordered(columnEventArgs);
            }

            ClearColumnHeaderDragInfo();
        }

        private void ClearColumnHeaderDragInfo()
        {
            headerDragCtx = new HeaderDragContext();
        }

        private static TranslateTransform GetTranslation(UIElement element)
        {
            var translation = element.RenderTransform as TranslateTransform;
            if (translation == null)
                element.RenderTransform = translation = new TranslateTransform();
            return translation;
        }

        private int FindHeaderIndex(Point startPos)
        {
            if (ItemContainerGenerator == null)
                return -1;

            for (int index = 0; index < ItemContainerGenerator.Items.Count; ++index) {
                var header = HeaderFromIndex(index);
                if (header == null)
                    continue;

                var leftEdge = header.TransformToAncestor(this).Transform(new Point()).X;
                var translation = header.RenderTransform as TranslateTransform;
                if (translation != null)
                    leftEdge -= translation.X;
                var rightEdge = leftEdge + header.RenderSize.Width;

                if (startPos.X.GreaterThanOrClose(leftEdge) &&
                    startPos.X.LessThanOrClose(rightEdge))
                    return index;
            }

            return -1;
        }

        private enum DragState
        {
            None,
            Preparing,
            Dragging,
        }

        /// <devdoc>
        /// <para>
        ///   There are a lot of options to realize header dragging. Many use
        ///   ghost elements duplicating the dragged header and showing a drop
        ///   indicator. These elements can be placed in an adorner layer or
        ///   arranged explicitly as part of the normal visual tree.
        /// </para>
        /// <para>
        ///   The ghost element is usually rendered using a visual brush of the
        ///   dragged header. This has the disadvantage of losing the pressed
        ///   state once the mouse cursor leaves the bounds of the original
        ///   header.
        /// </para>
        /// <para>
        ///   We want to have Explorer-like real dragging of the header with
        ///   dragged over columns moving aside. Using ghost elements for this
        ///   is not practicable. Instead we simply apply a translate transform
        ///   to the real header while dragging. This also preserves the pressed
        ///   appearance. While dragging, the other headers are rearranged
        ///   on-the-fly to show the potential new column order.
        /// </para>
        /// </devdoc>
        private struct HeaderDragContext
        {
            private DragState state;
            private HeaderAnimation[] animations;
            private ItemContainerGenerator generator;

            public VirtualizedDataGridColumnHeader DraggedHeader { get; private set; }
            public int DraggedHeaderIndex { get; private set; }
            public Point StartPosition { get; private set; }
            public Point CurrentPosition { get; set; }

            public bool IsActive => state != DragState.None;
            public bool IsPreparing => state == DragState.Preparing;
            public bool IsDragging => state == DragState.Dragging;

            public void PrepareDrag(
                VirtualizedDataGridColumnHeader draggedHeader, Point position)
            {
                state = DragState.Preparing;
                DraggedHeader = draggedHeader;
                StartPosition = position;
            }

            public void StartDrag(ItemContainerGenerator generator)
            {
                state = DragState.Dragging;
                this.generator = generator;
                DraggedHeaderIndex = generator.IndexFromContainer(DraggedHeader);
                animations = new HeaderAnimation[generator.Items.Count];

                // Headers by default have no explicit Z-index thus they are
                // rendered in the order in which they appear in the visual tree.
                // To ensure that the dragged header is rendered on top of all
                // other headers we have to raise its Z-index above the default.
                // The raised Z-index is negative because the panel used to
                // arrange the headers has its visual child collection reversed.
                Panel.SetZIndex(DraggedHeader, -1);
            }

            public void Reset()
            {
                DraggedHeader.RenderTransform = null;
                DraggedHeader.ClearValue(Panel.ZIndexProperty);

                foreach (var animation in animations)
                    animation?.Reset();
            }

            public void AnimateDrag(int targetIndex)
            {
                for (int i = 0; i < animations.Length; ++i) {
                    if (i == DraggedHeaderIndex)
                        continue;

                    var animation = animations[i];
                    if ((i <= DraggedHeaderIndex && i < targetIndex) ||
                        (i >= DraggedHeaderIndex && i > targetIndex)) {
                        animation?.MoveBack();
                        continue;
                    }

                    if (animation == null)
                        animation = CreateAnimation(i);

                    animation.MoveAside();
                }
            }

            private HeaderAnimation CreateAnimation(int index)
            {
                var element = (UIElement)generator.ContainerFromIndex(index);
                double distance = DraggedHeader.ActualWidth;
                double to = index > DraggedHeaderIndex ? -distance : distance;
                var animation = new HeaderAnimation(element, to);
                animations[index] = animation;
                return animation;
            }
        }

        /// <devdoc>
        /// <para>
        ///   Encapsulates the animation of a header moving aside (or back) to
        ///   make room for the dragged header. Headers have just two resting
        ///   places: the initial position, or the alternate position. The
        ///   latter only depends depends on the actual width of the dragged
        ///   column.
        ///
        ///   Initial position:
        ///     | Header1   | Dragged | Header3       | Header4     |
        ///       +0                    +0              +0
        ///                      ------>
        ///
        ///   Alternate positions:
        ///     | Header1   | Header3       | Dragged | Header4     |
        ///       +0          -10                       +0
        ///                                      ------>
        ///
        ///     | Header1   | Header3       | Header4     | Dragged |
        ///       +0          -10             -10
        ///
        ///     | Dragged | Header1   | Header3       | Header4     |
        ///                 +10         +0              +0
        /// </para>
        /// <para>
        ///   When a header is still animating has to move back we want to
        ///   cancel the partially completed animation and reverse it. There
        ///   seems to be no built-in way to accomplish that. Just applying a an
        ///   animation from the current offset to the new target offset has the
        ///   wrong duration and wrong values due to easing. We have to find out
        ///   how much time passed, reverse the animation and then seek forward
        ///   before continuing.
        /// </para>
        /// </devdoc>
        private sealed class HeaderAnimation
        {
            private readonly UIElement element;
            private readonly DoubleAnimation animation;
            private AnimationClock clock;
            private State state = State.Initial;
            private bool needsReverse;

            private enum State
            {
                Initial,
                MovingToAlternate,
                Alternate,
                MovingToInitial
            }

            public HeaderAnimation(UIElement element, double to)
            {
                this.element = element;

                var duration = TimeSpan.FromMilliseconds(333);
                var easingFunction = new QuadraticEase {
                    EasingMode = EasingMode.EaseInOut
                };

                animation = new DoubleAnimation {
                    Duration = duration,
                    From = 0,
                    To = to,
                    EasingFunction = easingFunction
                };
                animation.Completed += OnCompleted;

                clock = animation.CreateClock();
            }

            public void MoveAside()
            {
                switch (state) {
                    case State.Initial:
                        state = State.MovingToAlternate;
                        ReverseIfNeeded();
                        Run();
                        break;
                    case State.MovingToInitial:
                        state = State.MovingToAlternate;
                        Reverse();
                        Run();
                        break;
                }
            }

            public void MoveBack()
            {
                switch (state) {
                    case State.Alternate:
                        state = State.MovingToInitial;
                        ReverseIfNeeded();
                        Run();
                        break;
                    case State.MovingToAlternate:
                        state = State.MovingToInitial;
                        Reverse();
                        Run();
                        break;
                }
            }

            private void Run()
            {
                GetTranslation(element).ApplyAnimationClock(
                    TranslateTransform.XProperty, clock);
            }

            private void ReverseIfNeeded()
            {
                if (needsReverse)
                    Reverse();
                needsReverse = false;
            }

            private void Reverse()
            {
                var from = animation.From;
                var to = animation.To;
                animation.From = to;
                animation.To = from;

                var offset = TimeSpan.Zero;
                if (clock.CurrentTime != null)
                    offset = animation.Duration.TimeSpan - clock.CurrentTime.Value;

                clock.Controller.Stop();
                clock = animation.CreateClock();
                clock.Controller.Seek(offset, TimeSeekOrigin.BeginTime);
            }

            private void OnCompleted(object sender, EventArgs eventArgs)
            {
                switch (state) {
                    case State.MovingToAlternate:
                        needsReverse = true;
                        state = State.Alternate;
                        break;
                    case State.MovingToInitial:
                        needsReverse = true;
                        state = State.Initial;
                        break;
                    default:
                        Debug.Fail("Wrong state in OnCompleted callback " + state + ".");
                        break;
                }
            }

            public void Reset()
            {
                element.RenderTransform = null;
            }
        }
    }

    public class ColumnChooser : ContextMenu
    {
        private ReadOnlyObservableCollection<VirtualizedDataGridColumn> columns;
        private VirtualizedDataGridColumn[] sortedColumns;

        static ColumnChooser()
        {
            Type forType = typeof(ColumnChooser);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(typeof(ContextMenu)));
        }

        public ColumnChooser(
            ReadOnlyObservableCollection<VirtualizedDataGridColumn> columns,
            IDataView viewModel)
        {
            if (columns == null)
                throw new ArgumentNullException(nameof(columns));

            this.columns = columns;

            ViewModel = viewModel;
            //this.InitializeComponent();
            //this.columnTemplate = FindResource("columnTemplate") as DataTemplate;
            sortedColumns = new VirtualizedDataGridColumn[columns.Count];
            this.columns.CopyTo(sortedColumns, 0);
            Array.Sort(sortedColumns, CompareColumnNames);

            for (int i = 0; i < sortedColumns.Length; ++i)
                Items.Insert(i, CreateContainer(sortedColumns[i]));

            //this.RefreshColumns();
            //this.columns.CollectionChanged += new NotifyCollectionChangedEventHandler(this.ColumnsCollectionChangedHandler);

            CommandBindings.Add(
                new CommandBinding(ApplicationCommands.Close, CloseCommandExecuted));
        }

        public IDataView ViewModel { get; }

        private void CloseCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private MenuItem CreateContainer(VirtualizedDataGridColumn column)
        {
            var item = new MenuItem();
            item.IsCheckable = true;
            item.StaysOpenOnClick = true;
            item.SetBinding(MenuItem.IsCheckedProperty, new Binding(nameof(column.IsVisible)) {
                Mode = BindingMode.TwoWay
            });
            item.SetBinding(HeaderedItemsControl.HeaderProperty, new Binding(nameof(column.ColumnName)) {
                Mode = BindingMode.OneWay
            });
            item.DataContext = column;
            return item;
        }

        private static int CompareColumnNames(
            VirtualizedDataGridColumn lhs, VirtualizedDataGridColumn rhs)
        {
            var comparisonType = StringComparison.CurrentCultureIgnoreCase;

            int cmp = String.Compare(lhs.ColumnName, rhs.ColumnName, comparisonType);
            if (cmp != 0)
                return cmp;

            return lhs.ModelVisibleColumnIndex.CompareTo(rhs.ModelVisibleColumnIndex);
        }
    }
}
