namespace EventTraceKit.VsExtension.Controls.Primitives
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using Extensions;
    using Windows;

    public class AsyncDataGridColumnHeadersPresenter : ItemsControl
    {
        private AsyncDataGrid parentGrid;
        private HeaderDragContext headerDragCtx;

        static AsyncDataGridColumnHeadersPresenter()
        {
            Type forType = typeof(AsyncDataGridColumnHeadersPresenter);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(forType));

            // Arrange the headers in reverse order, i.e. from right to left,
            // to make the left header's gripper overlay the right header.
            var panelFactory = new FrameworkElementFactory(
                typeof(AsyncDataGridColumnHeadersPanel));
            panelFactory.SetValue(
                StackPanel.OrientationProperty,
                Orientation.Horizontal);

            var panelTemplate = new ItemsPanelTemplate(panelFactory);
            panelTemplate.Seal();

            ItemsPanelProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(panelTemplate));
        }

        #region public AsyncDataGridColumnsViewModel ViewModel { get; set; }

        /// <summary>
        ///   Identifies the <see cref="ViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(AsyncDataGridColumnsViewModel),
                typeof(AsyncDataGridColumnHeadersPresenter),
                PropertyMetadataUtils.DefaultNull);

        /// <summary>
        ///   Gets or sets the columns view model.
        /// </summary>
        public AsyncDataGridColumnsViewModel ViewModel
        {
            get { return (AsyncDataGridColumnsViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        #endregion

        #region public AsyncDataGridColumnViewModel ExpanderHeaderViewModel { get; set; }

        /// <summary>
        ///   Identifies the <see cref="ExpanderHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpanderHeaderProperty =
            DependencyProperty.Register(
                nameof(ExpanderHeader),
                typeof(AsyncDataGridColumn),
                typeof(AsyncDataGridColumnHeadersPresenter),
                PropertyMetadataUtils.DefaultNull);

        /// <summary>
        ///   Gets or sets the expander header column.
        /// </summary>
        public AsyncDataGridColumn ExpanderHeader
        {
            get { return (AsyncDataGridColumn)GetValue(ExpanderHeaderProperty); }
            set { SetValue(ExpanderHeaderProperty, value); }
        }

        #endregion

        internal AsyncDataGrid ParentGrid =>
            parentGrid ?? (parentGrid = this.FindParent<AsyncDataGrid>());

        internal Panel InternalItemsHost { get; set; }

        internal void NotifyHorizontalOffsetChanged()
        {
            InvalidateArrange();
            InternalItemsHost?.InvalidateArrange();
        }

        public override void OnApplyTemplate()
        {
            if (InternalItemsHost != null && !IsAncestorOf(InternalItemsHost))
                InternalItemsHost = null;

            base.OnApplyTemplate();
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            if (oldValue is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= OnItemsSourceCollectionChanged;

            if (newValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += OnItemsSourceCollectionChanged;

            RefreshEnabled();
        }

        private void OnItemsSourceCollectionChanged(
            object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            RefreshEnabled();
        }

        private void RefreshEnabled()
        {
            IsEnabled = ItemsSource?.Any() == true;
        }

        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            if (!e.Handled) {
                using (ParentGrid.EnterContextMenuVisualState()) {
                    var chooser = new ColumnChooser(ViewModel.ConfigurableColumns, ViewModel.Model) {
                        Placement = PlacementMode.MousePoint,
                        PlacementTarget = VisualTreeHelper.GetParent(this) as UIElement
                    };
                    chooser.IsOpen = true;
                }

                e.Handled = true;
            }

            base.OnContextMenuOpening(e);
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is AsyncDataGridColumnHeader;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new AsyncDataGridColumnHeader();
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

            if (!(item is AsyncDataGridColumn column))
                throw new InvalidOperationException("Invalid item type.");

            var header = (AsyncDataGridColumnHeader)element;
            header.SnapsToDevicePixels = SnapsToDevicePixels;
            header.UseLayoutRounding = UseLayoutRounding;
            header.Column = column;

            var widthBinding = new Binding(nameof(Width)) {
                Source = column,
                Mode = BindingMode.TwoWay
            };
            header.SetBinding(WidthProperty, widthBinding);

            var contentBinding = new Binding(nameof(column.ColumnName)) {
                Source = column
            };
            header.SetBinding(ContentControl.ContentProperty, contentBinding);
        }

        internal void OnHeaderMouseLeftButtonDown(
            MouseButtonEventArgs e, AsyncDataGridColumnHeader header)
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
                if (headerDragCtx.ShouldStartDrag())
                    StartColumnHeaderDrag();
                return;
            }

            headerDragCtx.DoDrag(FindHeaderIndex);

            // For unknown reasons we often get duplicate mouse-move events so
            // only report non-zero deltas.
            // For reference: Thumb's mouse-move handling has the same check for
            // its delta event. DataGridColumnHeader does not but also does not
            // get zero deltas.
            var delta = headerDragCtx.LastDelta;
            if (delta != new Vector()) {
                var dragDeltaEventArgs = new DragDeltaEventArgs(delta.X, delta.Y);
                ParentGrid.OnColumnHeaderDragDelta(dragDeltaEventArgs);
            }
        }

        internal void OnHeaderLostMouseCapture(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed &&
                headerDragCtx.IsDragging)
                FinishColumnHeaderDrag(true);
        }

        internal void OnHeaderKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape && headerDragCtx.IsDragging)
                FinishColumnHeaderDrag(true);
        }

        private void StartColumnHeaderDrag()
        {
            var reorderingEventArgs = new AsyncDataGridColumnReorderingEventArgs(
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

        private AsyncDataGridColumnHeader HeaderFromIndex(int index)
        {
            var container = ItemContainerGenerator.ContainerFromIndex(index);
            return (AsyncDataGridColumnHeader)container;
        }

        private void FinishColumnHeaderDrag(bool canceled)
        {
            int targetIndex = FindHeaderIndex(headerDragCtx.CurrentPosition);

            var totalDelta = headerDragCtx.TotalDelta;
            var dragCompletedEventArgs = new DragCompletedEventArgs(
                totalDelta.X, totalDelta.Y, canceled);
            ParentGrid.OnColumnHeaderDragCompleted(dragCompletedEventArgs);

            headerDragCtx.ResetDrag();

            if (!canceled && targetIndex != -1 &&
                targetIndex != headerDragCtx.DraggedHeaderIndex) {
                var srcColumn = headerDragCtx.DraggedHeader.Column;
                var dstColumn = HeaderFromIndex(targetIndex).Column;
                ViewModel.TryMoveColumn(srcColumn, dstColumn);

                var columnEventArgs = new AsyncDataGridColumnEventArgs(srcColumn);
                ParentGrid.OnColumnReordered(columnEventArgs);
            }

            ClearColumnHeaderDragInfo();

            ParentGrid?.CellsPresenter?.Focus();
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
                if (header.RenderTransform is TranslateTransform translation)
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
            Dragging
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
            private Point lastPosition;
            private Point currentPosition;

            public AsyncDataGridColumnHeader DraggedHeader { get; private set; }
            public int DraggedHeaderIndex { get; private set; }
            public Point StartPosition { get; private set; }

            public Point CurrentPosition
            {
                get => currentPosition;
                set
                {
                    lastPosition = currentPosition;
                    currentPosition = value;
                }
            }

            public Vector TotalDelta => CurrentPosition - StartPosition;
            public Vector LastDelta => CurrentPosition - lastPosition;

            public bool IsActive => state != DragState.None;
            public bool IsPreparing => state == DragState.Preparing;
            public bool IsDragging => state == DragState.Dragging;

            public void PrepareDrag(
                AsyncDataGridColumnHeader draggedHeader, Point position)
            {
                state = DragState.Preparing;
                DraggedHeader = draggedHeader;
                StartPosition = position;
                lastPosition = position;
                currentPosition = position;
            }

            public bool ShouldStartDrag()
            {
                double totalDeltaX = Math.Abs(CurrentPosition.X - StartPosition.X);
                return totalDeltaX.GreaterThan(
                    SystemParameters.MinimumHorizontalDragDistance);
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

            public void ResetDrag()
            {
                DraggedHeader.RenderTransform = null;
                DraggedHeader.ClearValue(Panel.ZIndexProperty);

                foreach (var animation in animations)
                    animation?.Reset();
            }

            public void DoDrag(Func<Point, int> findHeaderIndex)
            {
                GetTranslation(DraggedHeader).X = TotalDelta.X;

                int targetIndex = findHeaderIndex(CurrentPosition);
                if (targetIndex != -1)
                    AnimateDrag(targetIndex);
            }

            private void AnimateDrag(int targetIndex)
            {
                for (int i = 0; i < animations.Length; ++i) {
                    if (i == DraggedHeaderIndex)
                        continue;

                    var animation = animations[i];
                    if ((i <= DraggedHeaderIndex && i < targetIndex) ||
                        (i >= DraggedHeaderIndex && i > targetIndex)) {
                        animation?.MoveToInitial();
                        continue;
                    }

                    if (animation == null)
                        animation = CreateAnimation(i);

                    animation.MoveToAlternate();
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
        ///   When a header still animating has to move back we want to cancel
        ///   the partially completed animation and reverse it. There seems to
        ///   be no built-in way to accomplish that. Just applying an animation
        ///   from the current offset to the new target offset has the wrong
        ///   duration and wrong values due to easing. We have to find out how
        ///   much time passed, reverse the animation and then seek forward
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

            public void MoveToAlternate()
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
                    case State.Alternate:
                    case State.MovingToAlternate:
                        // Nothing to do.
                        break;
                }
            }

            public void MoveToInitial()
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
                    case State.Initial:
                    case State.MovingToInitial:
                        // Nothing to do.
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
                        Debug.Fail("Wrong state in OnCompleted callback: " + state + ".");
                        break;
                }
            }

            public void Reset()
            {
                element.RenderTransform = null;
            }
        }
    }
}
