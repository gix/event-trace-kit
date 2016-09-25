namespace EventTraceKit.VsExtension.Controls.Primitives
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;

    public class BindableScrollBar : ScrollBar
    {
        private readonly ScrollEventHandler scrollBarScrollHandler;
        private readonly ScrollChangedEventHandler scrollViewerScrollChangedHandler;

        static BindableScrollBar()
        {
            InitializeCommands();
        }

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

        #endregion

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

        private static void InitializeCommands()
        {
            var executed = new ExecutedRoutedEventHandler(OnScrollCommand);
            var canExecute = new CanExecuteRoutedEventHandler(OnQueryScrollCommand);
            RegisterCommandHandler(LineLeftCommand, executed, canExecute);
            RegisterCommandHandler(LineRightCommand, executed, canExecute);
            RegisterCommandHandler(PageLeftCommand, executed, canExecute);
            RegisterCommandHandler(PageRightCommand, executed, canExecute);
            RegisterCommandHandler(LineUpCommand, executed, canExecute);
            RegisterCommandHandler(LineDownCommand, executed, canExecute);
            RegisterCommandHandler(PageUpCommand, executed, canExecute);
            RegisterCommandHandler(PageDownCommand, executed, canExecute);
            RegisterCommandHandler(ScrollToLeftEndCommand, executed, canExecute);
            RegisterCommandHandler(ScrollToRightEndCommand, executed, canExecute);
            RegisterCommandHandler(ScrollToEndCommand, executed, canExecute);
            RegisterCommandHandler(ScrollToHomeCommand, executed, canExecute);
            RegisterCommandHandler(ScrollToTopCommand, executed, canExecute);
            RegisterCommandHandler(ScrollToBottomCommand, executed, canExecute);
            RegisterCommandHandler(ScrollToHorizontalOffsetCommand, executed, canExecute);
            RegisterCommandHandler(ScrollToVerticalOffsetCommand, executed, canExecute);
            RegisterCommandHandler(DeferScrollToHorizontalOffsetCommand, executed, canExecute);
            RegisterCommandHandler(DeferScrollToVerticalOffsetCommand, executed, canExecute);
        }

        private static void RegisterCommandHandler(
            RoutedCommand command, ExecutedRoutedEventHandler executed,
            CanExecuteRoutedEventHandler canExecute)
        {
            CommandManager.RegisterClassCommandBinding(
                typeof(BindableScrollBar),
                new CommandBinding(command, executed, canExecute));
        }

        private static void OnQueryScrollCommand(
            object target, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
            if (args.Command == DeferScrollToHorizontalOffsetCommand ||
                args.Command == DeferScrollToVerticalOffsetCommand) {
                var viewer = target as ScrollViewer;
                if (viewer != null && !viewer.IsDeferredScrollingEnabled) {
                    args.CanExecute = false;
                    args.Handled = true;
                }
            }
        }

        private static void OnScrollCommand(
            object target, ExecutedRoutedEventArgs args)
        {
            // Forward all scrollbar events to the bound scroll viewer which
            // then can handle them properly using its scroll info. Without
            // forwarding these events will not reach the scroll viewer via
            // bubbling when we are not a descendant element of the viewer.
            var scrollBar = target as BindableScrollBar;
            scrollBar?.ScrollViewer?.RaiseEvent(args);
        }

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
