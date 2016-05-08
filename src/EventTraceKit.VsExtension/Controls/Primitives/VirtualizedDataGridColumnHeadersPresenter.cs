namespace EventTraceKit.VsExtension.Controls.Primitives
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using Microsoft.VisualStudio.PlatformUI;

    public class VirtualizedDataGridColumnHeadersPresenter : ItemsControl
    {
        private VirtualizedDataGrid parentGrid;
        private HeaderDragContext headerDragCtx;

        static VirtualizedDataGridColumnHeadersPresenter()
        {
            Type forType = typeof(VirtualizedDataGridColumnHeadersPresenter);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(forType));
        }

        #region public ReadOnlyObservableCollection<VirtualizedDataGridColumnViewModel> VisibleColumns { get; set; }

        /// <summary>
        ///   Identifies the <see cref="VisibleColumns"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VisibleColumnsProperty =
            DependencyProperty.Register(
                nameof(VisibleColumns),
                typeof(ReadOnlyObservableCollection<VirtualizedDataGridColumnViewModel>),
                typeof(VirtualizedDataGridColumnHeadersPresenter),
                new PropertyMetadata(
                    CollectionDefaults<VirtualizedDataGridColumnViewModel>.ReadOnlyObservable));

        /// <summary>
        ///   Gets or sets the visible columns.
        /// </summary>
        public ReadOnlyObservableCollection<VirtualizedDataGridColumnViewModel> VisibleColumns
        {
            get
            {
                return (ReadOnlyObservableCollection<VirtualizedDataGridColumnViewModel>)
                  GetValue(VisibleColumnsProperty);
            }
            set { SetValue(VisibleColumnsProperty, value); }
        }

        #endregion

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
        ///   Identifies the <see cref="ExpanderHeaderViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpanderHeaderViewModelProperty =
            DependencyProperty.Register(
                nameof(ExpanderHeaderViewModel),
                typeof(VirtualizedDataGridColumnViewModel),
                typeof(VirtualizedDataGridColumnHeadersPresenter),
                PropertyMetadataUtils.DefaultNull);

        /// <summary>
        ///   Gets or sets the expander header column.
        /// </summary>
        public VirtualizedDataGridColumnViewModel ExpanderHeaderViewModel
        {
            get { return (VirtualizedDataGridColumnViewModel)GetValue(ExpanderHeaderViewModelProperty); }
            set { SetValue(ExpanderHeaderViewModelProperty, value); }
        }

        #endregion

        internal VirtualizedDataGrid ParentGrid =>
            parentGrid ?? (parentGrid = this.FindParent<VirtualizedDataGrid>());

        //protected override Size ArrangeOverride(Size arrangeBounds)
        //{
        //    UIElement element = VisualTreeHelper.GetChildrenCount(this) > 0
        //        ? VisualTreeHelper.GetChild(this, 0) as UIElement : null;

        //    if (element != null) {
        //        var finalRect = new Rect(arrangeBounds);
        //        var parent = ParentGrid;
        //        if (parent != null) {
        //            finalRect.X = 0; //-parent.HorizontalScrollOffset;
        //            finalRect.Width = Math.Max(arrangeBounds.Width, parent.ActualWidth);
        //        }

        //        element.Arrange(finalRect);
        //    }

        //    return arrangeBounds;
        //}

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new VirtualizedDataGridColumnHeader();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is VirtualizedDataGridColumnHeader;
        }

        internal void OnHeaderMouseLeftButtonDown(
            MouseButtonEventArgs e, VirtualizedDataGridColumnHeader header)
        {
            if (ParentGrid == null)
                return;

            if (header != null) {
                headerDragCtx.PrepareDrag(header, e.GetPosition(this));
            } else
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
            if (e.LeftButton != MouseButtonState.Pressed ||
                !headerDragCtx.PrepareDragging)
                return;

            headerDragCtx.CurrentPosition = e.GetPosition(this);

            if (!headerDragCtx.IsDragging) {
                if (ShouldStartColumnHeaderDrag(headerDragCtx.CurrentPosition,
                                                headerDragCtx.StartPosition))
                    StartColumnHeaderDrag();
                return;
            }

            GetTranslation(headerDragCtx.Header).X =
                headerDragCtx.CurrentPosition.X - headerDragCtx.StartPosition.X;

            int sourceIndex = headerDragCtx.HeaderIndex;
            int targetIndex = FindHeaderIndex(headerDragCtx.CurrentPosition);

            if (targetIndex != -1) {
                for (int i = 0; i < headerDragCtx.Headers.Length; ++i) {
                    if (i == sourceIndex)
                        continue;

                    var animation = headerDragCtx.Animations[i];
                    if ((i <= sourceIndex && i < targetIndex) ||
                        (i >= sourceIndex && i > targetIndex)) {
                        animation?.MoveBack();
                        continue;
                    }

                    if (animation == null)
                        animation = headerDragCtx.CreateAnimation(i);
                    animation.MoveAside();
                }
            }
        }

        internal void OnHeaderLostMouseCapture(MouseEventArgs e)
        {
            if (headerDragCtx.IsDragging &&
                Mouse.LeftButton == MouseButtonState.Pressed) {
                FinishColumnHeaderDrag(true);
            }
        }

        private void StartColumnHeaderDrag()
        {
            var reorderingEventArgs = new VirtualizedDataGridColumnReorderingEventArgs(
                headerDragCtx.Header.ViewModel);

            ParentGrid.OnColumnReordering(reorderingEventArgs);
            if (reorderingEventArgs.Cancel) {
                FinishColumnHeaderDrag(true);
                return;
            }

            var dragStartedEventArgs = new DragStartedEventArgs(
                headerDragCtx.StartPosition.X,
                headerDragCtx.StartPosition.Y);
            ParentGrid.OnColumnHeaderDragStarted(dragStartedEventArgs);

            Panel.SetZIndex(headerDragCtx.Header.FindAncestor<ContentPresenter>(), 1);

            var generator = headerDragCtx.Header.FindAncestor<ItemsControl>()?.ItemContainerGenerator;
            if (generator == null)
                return;

            headerDragCtx.HeaderIndex = VisibleColumns.IndexOf(headerDragCtx.Header.ViewModel);
            headerDragCtx.Headers = new VirtualizedDataGridColumnHeader[VisibleColumns.Count];
            headerDragCtx.Animations = new HeaderAnimation[VisibleColumns.Count];
            for (int i = 0; i < VisibleColumns.Count; ++i) {
                var c = (ContentPresenter)generator.ContainerFromIndex(i);
                headerDragCtx.Headers[i] = c.FindDescendant<VirtualizedDataGridColumnHeader>();
            }

            headerDragCtx.IsDragging = true;
        }

        private static bool ShouldStartColumnHeaderDrag(
            Point currentPos, Point originalPos)
        {
            double delta = Math.Abs(currentPos.X - originalPos.X);
            return delta.GreaterThan(
                SystemParameters.MinimumHorizontalDragDistance);
        }

        private void FinishColumnHeaderDrag(bool isCancel)
        {
            int targetIndex = FindHeaderIndex(headerDragCtx.CurrentPosition);

            var delta = headerDragCtx.CurrentPosition - headerDragCtx.StartPosition;
            var dragCompletedEventArgs = new DragCompletedEventArgs(
                delta.X, delta.Y, isCancel);
            ParentGrid.OnColumnHeaderDragCompleted(dragCompletedEventArgs);

            headerDragCtx.Header.RenderTransform = null;
            headerDragCtx.Header.FindAncestor<ContentPresenter>().ClearValue(Panel.ZIndexProperty);

            foreach (var animation in headerDragCtx.Animations)
                animation?.Reset();

            if (!isCancel && targetIndex != headerDragCtx.HeaderIndex) {
                var srcColumn = headerDragCtx.Header.ViewModel;
                var dstColumn = headerDragCtx.Headers[targetIndex].ViewModel;
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
            if (headerDragCtx.Headers == null)
                return -1;

            for (int index = 0; index < headerDragCtx.Headers.Length; ++index) {
                var header = headerDragCtx.Headers[index];
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

        private struct HeaderDragContext
        {
            public bool PrepareDragging;
            public bool IsDragging;
            public VirtualizedDataGridColumnHeader Header;
            public int HeaderIndex;
            public Point StartPosition;
            public Point CurrentPosition;
            public VirtualizedDataGridColumnHeader[] Headers;
            public HeaderAnimation[] Animations;

            public void PrepareDrag(
                VirtualizedDataGridColumnHeader header, Point position)
            {
                PrepareDragging = true;
                Header = header;
                StartPosition = position;
            }

            public HeaderAnimation CreateAnimation(int index)
            {
                double distance = Header.ActualWidth;
                double to = index > HeaderIndex ? -distance : distance;
                var animation = new HeaderAnimation(Headers[index], to);
                Animations[index] = animation;
                return animation;
            }
        }

        private sealed class HeaderAnimation
        {
            private readonly VirtualizedDataGridColumnHeader header;
            private readonly DoubleAnimation animation;
            private AnimationClock clock;
            private State state = State.Origin;
            private bool needsReverse;

            private enum State
            {
                Origin,
                MovingToAlternate,
                Alternate,
                MovingToOrigin
            }

            public HeaderAnimation(
                VirtualizedDataGridColumnHeader header, double to)
            {
                this.header = header;

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
                    case State.Origin:
                        state = State.MovingToAlternate;
                        ReverseIfNeeded();
                        Run();
                        break;
                    case State.MovingToOrigin:
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
                        state = State.MovingToOrigin;
                        ReverseIfNeeded();
                        Run();
                        break;
                    case State.MovingToAlternate:
                        state = State.MovingToOrigin;
                        Reverse();
                        Run();
                        break;
                }
            }

            private void Run()
            {
                GetTranslation(header).ApplyAnimationClock(
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
                    case State.MovingToOrigin:
                        needsReverse = true;
                        state = State.Origin;
                        break;
                    default:
                        Debug.Fail("Wrong state in OnCompleted callback " + state + ".");
                        break;
                }
            }

            public void Reset()
            {
                header.RenderTransform = null;
            }
        }
    }
}
