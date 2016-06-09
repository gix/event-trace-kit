namespace EventTraceKit.VsExtension.Controls.Primitives
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;

    public class BindableScrollBar : ScrollBar
    {
        private readonly ScrollEventHandler scrollBarScrollHandler;
        private readonly ScrollChangedEventHandler scrollViewerScrollChangedHandler;

        public BindableScrollBar()
        {
            scrollBarScrollHandler = OnScrollBarScroll;
            scrollViewerScrollChangedHandler = OnScrollViewerScrollChanged;
        }

        #region public ScrollViewer ScrollViewer { get; set; }

        /// <summary>
        ///   Identifies the <see cref="ScrollViewer"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScrollViewerProperty =
            DependencyProperty.Register(
                nameof(ScrollViewer),
                typeof(ScrollViewer),
                typeof(BindableScrollBar),
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
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var source = (BindableScrollBar)d;
            source.OnScrollViewerChanged(
                (ScrollViewer)e.OldValue, (ScrollViewer)e.NewValue);
        }

        /// <summary>
        ///   <see cref="ScrollViewerProperty"/> property changed handler.
        /// </summary>
        /// <param name="oldValue">Old <see cref="ScrollViewer"/> value.</param>
        /// <param name="newValue">New <see cref="ScrollViewer"/> value.</param>
        protected virtual void OnScrollViewerChanged(
            ScrollViewer oldValue,
            ScrollViewer newValue)
        {
            if (oldValue != null)
                UnbindFromScrollViewer(oldValue);
            if (newValue != null)
                BindToScrollViewer(newValue);
        }

        private void UnbindFromScrollViewer(ScrollViewer oldViewer)
        {
            ClearValue(MaximumProperty);
            ClearValue(ViewportSizeProperty);
            ClearValue(ValueProperty);
            ClearValue(VisibilityProperty);

            oldViewer.RemoveHandler(ScrollViewer.ScrollChangedEvent, scrollViewerScrollChangedHandler);
            RemoveHandler(ScrollEvent, scrollBarScrollHandler);
        }

        private void BindToScrollViewer(ScrollViewer newViewer)
        {
            AddHandler(ScrollEvent, scrollBarScrollHandler);
            newViewer.AddHandler(ScrollViewer.ScrollChangedEvent, scrollViewerScrollChangedHandler);

            Binding maximumBinding;
            Binding viewportSizeBinding;
            Binding valueBinding;
            Binding visibilityBinding;
            switch (Orientation) {
                case Orientation.Horizontal:
                    maximumBinding = new Binding(nameof(newViewer.ScrollableWidth));
                    viewportSizeBinding = new Binding(nameof(newViewer.ViewportWidth));
                    valueBinding = new Binding(nameof(newViewer.HorizontalOffset));
                    visibilityBinding = new Binding(nameof(newViewer.ComputedHorizontalScrollBarVisibility));
                    break;
                case Orientation.Vertical:
                    maximumBinding = new Binding(nameof(newViewer.ScrollableHeight));
                    viewportSizeBinding = new Binding(nameof(newViewer.ViewportHeight));
                    valueBinding = new Binding(nameof(newViewer.VerticalOffset));
                    visibilityBinding = new Binding(nameof(newViewer.ComputedVerticalScrollBarVisibility));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            maximumBinding.Source = newViewer;
            viewportSizeBinding.Source = newViewer;
            valueBinding.Source = newViewer;
            visibilityBinding.Source = newViewer;

            maximumBinding.Mode = BindingMode.OneWay;
            viewportSizeBinding.Mode = BindingMode.OneWay;
            valueBinding.Mode = BindingMode.OneWay;
            visibilityBinding.Mode = BindingMode.OneWay;

            SetBinding(MaximumProperty, maximumBinding);
            SetBinding(ViewportSizeProperty, viewportSizeBinding);
            SetBinding(ValueProperty, valueBinding);
            SetBinding(VisibilityProperty, visibilityBinding);
        }

        #endregion

        private void OnScrollBarScroll(object sender, ScrollEventArgs e)
        {
            switch (Orientation) {
                case Orientation.Horizontal:
                    ScrollViewer.ScrollToHorizontalOffset(e.NewValue);
                    break;
                case Orientation.Vertical:
                    ScrollViewer.ScrollToVerticalOffset(e.NewValue);
                    break;
            }
        }

        private void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            switch (Orientation) {
                case Orientation.Horizontal:
                    Value = e.HorizontalOffset;
                    break;
                case Orientation.Vertical:
                    Value = e.VerticalOffset;
                    break;
            }
        }
    }
}