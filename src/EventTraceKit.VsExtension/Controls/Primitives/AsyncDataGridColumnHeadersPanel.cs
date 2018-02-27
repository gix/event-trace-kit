namespace EventTraceKit.VsExtension.Controls.Primitives
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using Windows;

    public class AsyncDataGridColumnHeadersPanel : StackPanel
    {
        private AsyncDataGrid parentGrid;

        static AsyncDataGridColumnHeadersPanel()
        {
            OrientationProperty.OverrideMetadata(
                typeof(AsyncDataGridColumnHeadersPanel),
                new FrameworkPropertyMetadata(Orientation.Horizontal));
            ClipToBoundsProperty.OverrideMetadata(
                typeof(AsyncDataGridColumnHeadersPanel),
                new FrameworkPropertyMetadata(true));
        }

        private AsyncDataGridColumnHeadersPresenter ParentPresenter =>
            this.FindParent<AsyncDataGridColumnHeadersPresenter>();

        private AsyncDataGrid ParentGrid =>
            parentGrid ?? (parentGrid = ParentPresenter?.ParentGrid);

        #region public int LeftFrozenColumnCount { get; set; }

        /// <summary>
        ///   Identifies the <see cref="LeftFrozenColumnCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LeftFrozenColumnCountProperty =
            DependencyProperty.Register(
                nameof(LeftFrozenColumnCount),
                typeof(int),
                typeof(AsyncDataGridColumnHeadersPanel),
                new FrameworkPropertyMetadata(
                    0,
                    FrameworkPropertyMetadataOptions.AffectsArrange,
                    null,
                    (d, v) => ((AsyncDataGridColumnHeadersPanel)d).CoerceLeftFrozenColumnCount(v)));

        /// <summary>
        ///   Gets or sets the left frozen column count.
        /// </summary>
        public int LeftFrozenColumnCount
        {
            get { return (int)GetValue(LeftFrozenColumnCountProperty); }
            set { SetValue(LeftFrozenColumnCountProperty, value); }
        }

        private object CoerceLeftFrozenColumnCount(object baseValue)
        {
            if ((int)baseValue < 0)
                return Boxed.Int32Zero;
            return baseValue;
        }

        #endregion

        #region public int RightFrozenColumnCount { get; set; }

        /// <summary>
        ///   Identifies the <see cref="RightFrozenColumnCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RightFrozenColumnCountProperty =
            DependencyProperty.Register(
                nameof(RightFrozenColumnCount),
                typeof(int),
                typeof(AsyncDataGridColumnHeadersPanel),
                new FrameworkPropertyMetadata(
                    0,
                    FrameworkPropertyMetadataOptions.AffectsArrange,
                    null,
                    (d, v) => ((AsyncDataGridColumnHeadersPanel)d).CoerceRightFrozenColumnCount(v)));

        /// <summary>
        ///   Gets or sets the right frozen column count.
        /// </summary>
        public int RightFrozenColumnCount
        {
            get { return (int)GetValue(RightFrozenColumnCountProperty); }
            set { SetValue(RightFrozenColumnCountProperty, value); }
        }

        private object CoerceRightFrozenColumnCount(object baseValue)
        {
            if ((int)baseValue < 0)
                return Boxed.Int32Zero;
            return baseValue;
        }

        #endregion

        protected override Visual GetVisualChild(int index)
        {
            index = MapVisualIndex(index);
            return base.GetVisualChild(index);
        }

        private int MapVisualIndex(int index)
        {
            // The column range [b, e) we get from the generator is divided into
            // three subranges:
            //   [b, L): Left-frozen columns
            //   [L, R): Non-frozen columns
            //   [R, e): Right-frozen colums
            //
            // All columns have the same Z-index and are drawn in visual order.
            // To draw the right-aligned gripper *over* the following column
            // header we have to reverse the order of the visual children. In
            // addition we have to ensure that frozen columns are drawn after
            // non-frozen columns.
            //
            // We therefore map the visual child index to get the following
            // modified visual child range:
            //   (R, L] followed by (e, R], then (L, b].

            Debug.Assert(InternalChildren.Count == VisualChildrenCount);

            int columnCount = InternalChildren.Count;
            int leftFrozenColumnCount = Math.Min(LeftFrozenColumnCount, columnCount);
            int rightFrozenColumnCount = Math.Min(RightFrozenColumnCount, columnCount - leftFrozenColumnCount);

            int nonFrozenColumnCount = columnCount - leftFrozenColumnCount - rightFrozenColumnCount;
            int firstRightFrozenIndex = leftFrozenColumnCount + nonFrozenColumnCount;

            int end;
            if (index < nonFrozenColumnCount)
                end = firstRightFrozenIndex;
            else if (index < nonFrozenColumnCount + rightFrozenColumnCount)
                end = nonFrozenColumnCount + firstRightFrozenIndex + rightFrozenColumnCount;
            else
                end = columnCount;

            return end - index - 1;
        }

        protected override void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost)
        {
            base.OnIsItemsHostChanged(oldIsItemsHost, newIsItemsHost);

            var headersPresenter = ParentPresenter;
            if (headersPresenter == null)
                return;

            if (newIsItemsHost) {
                IItemContainerGenerator generator = headersPresenter.ItemContainerGenerator;
                if (generator != null
                    && generator == generator.GetItemContainerGeneratorForPanel(this))
                    headersPresenter.InternalItemsHost = this;
            } else {
                if (ReferenceEquals(headersPresenter.InternalItemsHost, this))
                    headersPresenter.InternalItemsHost = null;
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Size measureSize = base.MeasureOverride(constraint);
            if (!double.IsInfinity(constraint.Width))
                measureSize.Width = constraint.Width;
            return measureSize;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            bool isHorizontal = Orientation == Orientation.Horizontal;
            if (!isHorizontal)
                return base.ArrangeOverride(arrangeSize);

            var children = InternalChildren;
            double arrangeLength = Math.Max(arrangeSize.Height, 0);

            int leftFrozenColumnCount = Math.Min(LeftFrozenColumnCount, children.Count);
            int rightFrozenColumnCount = Math.Min(RightFrozenColumnCount, children.Count - leftFrozenColumnCount);

            var childRect = new Rect();

            // Arrange left frozen columns.
            childRect.X = 0;
            childRect.Y = 0;

            for (int i = 0; i < leftFrozenColumnCount; ++i) {
                var child = children[i];
                var childDesiredSize = child.DesiredSize;

                childRect.Width = childDesiredSize.Width;
                childRect.Height = Math.Max(childDesiredSize.Height, arrangeLength);

                child.Arrange(childRect);

                childRect.X += childRect.Width;
            }

            double firstNonFrozenX = childRect.X;

            // Arrange right frozen columns.
            childRect.X = arrangeSize.Width;
            childRect.Y = 0;

            for (int i = children.Count - 1; i >= children.Count - rightFrozenColumnCount; --i) {
                var child = children[i];
                var childDesiredSize = child.DesiredSize;

                childRect.Width = childDesiredSize.Width;
                childRect.Height = Math.Max(childDesiredSize.Height, arrangeLength);
                childRect.X -= childRect.Width;

                child.Arrange(childRect);
            }

            // Arrange non-frozen columns.
            childRect.X = firstNonFrozenX - (ParentGrid?.HorizontalScrollOffset ?? 0);
            childRect.Y = 0;

            for (int i = leftFrozenColumnCount; i < children.Count - rightFrozenColumnCount; ++i) {
                var child = children[i];
                var childDesiredSize = child.DesiredSize;

                childRect.Width = childDesiredSize.Width;
                childRect.Height = Math.Max(childDesiredSize.Height, arrangeLength);

                child.Arrange(childRect);

                childRect.X += childRect.Width;
            }

            return arrangeSize;
        }
    }
}
