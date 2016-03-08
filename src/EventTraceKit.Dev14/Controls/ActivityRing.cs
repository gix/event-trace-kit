namespace EventTraceKit.Dev14.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    /// <summary>
    ///   Represents a control that indicates that an activity is ongoing. The
    ///   typical visual appearance is a ring-shaped "spinner" that cycles an
    ///   animation as the activity continues.
    /// </summary>
    [TemplateVisualState(Name = "Inactive", GroupName = "ActiveStates")]
    [TemplateVisualState(Name = "Active", GroupName = "ActiveStates")]
    public sealed class ActivityRing : Control
    {
        /// <summary>
        ///   Identifies the <see cref="IsActive"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                "IsActive",
                typeof(bool),
                typeof(ActivityRing),
                new PropertyMetadata(false, IsActiveChanged));

        static ActivityRing()
        {
            DefaultStyleKeyProperty.OverrideMetadata<ActivityRing>();
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ActivityRing"/> class.
        /// </summary>
        public ActivityRing()
        {
            TemplateSettings = new ActivityRingTemplateSettings(this);
        }

        /// <summary>
        ///   Gets or sets a value that indicates whether the <see cref="ActivityRing"/>
        ///   is showing progress.
        /// </summary>
        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        /// <summary>
        ///   Gets an object that provides calculated values that can be referenced
        ///   as <b>TemplateBinding</b> sources when defining templates for a
        ///   <see cref="ActivityRing"/> control.
        /// </summary>
        public ActivityRingTemplateSettings TemplateSettings { get; }

        /// <summary>
        ///   <see cref="IsActiveProperty"/> property changed handler.
        /// </summary>
        /// <param name="d">
        ///   <see cref="ActivityRing"/> that changed its active state.
        /// </param>
        /// <param name="e">Event arguments.</param>
        private static void IsActiveChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (ActivityRing)d;
            source.UpdateActiveState();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            TemplateSettings.InvalidateProperty(ActivityRingTemplateSettings.MaxSideLengthProperty);
            TemplateSettings.InvalidateProperty(ActivityRingTemplateSettings.ArcThicknessProperty);
            TemplateSettings.InvalidateProperty(ActivityRingTemplateSettings.ArcSizeProperty);
            TemplateSettings.InvalidateProperty(ActivityRingTemplateSettings.ArcStartPointProperty);
            TemplateSettings.InvalidateProperty(ActivityRingTemplateSettings.ArcEndPointProperty);
            base.OnRenderSizeChanged(sizeInfo);
        }

        public override void OnApplyTemplate()
        {
            UpdateActiveState();
            base.OnApplyTemplate();
        }

        private void UpdateActiveState()
        {
            string state = IsActive ? "Active" : "Inactive";
            VisualStateManager.GoToState(this, state, true);
        }
    }

    public sealed class ActivityRingTemplateSettings : DependencyObject
    {
        private readonly ActivityRing activityRing;

        public ActivityRingTemplateSettings(ActivityRing activityRing)
        {
            this.activityRing = activityRing;
        }

        private static readonly DependencyPropertyKey MaxSideLengthPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "MaxSideLength",
                typeof(double),
                typeof(ActivityRingTemplateSettings),
                new PropertyMetadata(
                    default(double), null, CoerceMaxSideLength));

        private static readonly DependencyPropertyKey ArcStartPointKey =
            DependencyProperty.RegisterReadOnly(
                "ArcStartPoint",
                typeof(Point),
                typeof(ActivityRingTemplateSettings),
                new PropertyMetadata(default(Point), null, CoerceArcStartPoint));

        private static readonly DependencyPropertyKey ArcEndPointKey =
            DependencyProperty.RegisterReadOnly(
                "ArcEndPoint",
                typeof(Point),
                typeof(ActivityRingTemplateSettings),
                new PropertyMetadata(default(Point), null, CoerceArcEndPoint));

        private static readonly DependencyPropertyKey ArcSizeKey =
            DependencyProperty.RegisterReadOnly(
                "ArcSize",
                typeof(Size),
                typeof(ActivityRingTemplateSettings),
                new PropertyMetadata(default(Size), null, CoerceArcSize));

        private static readonly DependencyPropertyKey ArcThicknessKey =
            DependencyProperty.RegisterReadOnly(
                "ArcThickness",
                typeof(double),
                typeof(ActivityRingTemplateSettings),
                new PropertyMetadata(default(double), null, CoerceArcThickness));

        private static readonly DependencyPropertyKey IsLargeArcKey =
            DependencyProperty.RegisterReadOnly(
                "IsLargeArc",
                typeof(bool),
                typeof(ActivityRingTemplateSettings),
                new PropertyMetadata(true));

        private static readonly DependencyPropertyKey SweepDirectionKey =
            DependencyProperty.RegisterReadOnly(
                "SweepDirection",
                typeof(SweepDirection),
                typeof(ActivityRingTemplateSettings),
                new PropertyMetadata(SweepDirection.Clockwise));

        public static readonly DependencyProperty MaxSideLengthProperty =
            MaxSideLengthPropertyKey.DependencyProperty;

        public static readonly DependencyProperty ArcStartPointProperty =
            ArcStartPointKey.DependencyProperty;

        public static readonly DependencyProperty ArcEndPointProperty =
            ArcEndPointKey.DependencyProperty;

        public static readonly DependencyProperty ArcSizeProperty =
            ArcSizeKey.DependencyProperty;

        public static readonly DependencyProperty ArcThicknessProperty =
            ArcThicknessKey.DependencyProperty;

        public static readonly DependencyProperty IsLargeArcProperty =
            IsLargeArcKey.DependencyProperty;

        public static readonly DependencyProperty SweepDirectionProperty =
            SweepDirectionKey.DependencyProperty;

        public double MaxSideLength => (double)GetValue(MaxSideLengthProperty);
        public Point ArcStartPoint => (Point)GetValue(ArcStartPointProperty);
        public Point ArcEndPoint => (Point)GetValue(ArcEndPointProperty);
        public Size ArcSize => (Size)GetValue(ArcSizeProperty);
        public double ArcThickness => (double)GetValue(ArcThicknessProperty);
        public bool IsLargeArc => (bool)GetValue(IsLargeArcProperty);
        public SweepDirection SweepDirection => (SweepDirection)GetValue(SweepDirectionProperty);

        private static object CoerceMaxSideLength(
            DependencyObject d, object baseValue)
        {
            var source = (ActivityRingTemplateSettings)d;
            return Math.Min(source.activityRing.ActualWidth,
                            source.activityRing.ActualHeight);
        }

        private static object CoerceArcThickness(
            DependencyObject d, object basevalue)
        {
            var source = (ActivityRingTemplateSettings)d;
            return source.MaxSideLength / 5;
        }

        private static object CoerceArcSize(
            DependencyObject d, object basevalue)
        {
            var source = (ActivityRingTemplateSettings)d;
            var length = (source.MaxSideLength - source.ArcThickness) / 2;
            return new Size(length, length);
        }

        private static object CoerceArcStartPoint(
            DependencyObject d, object basevalue)
        {
            var source = (ActivityRingTemplateSettings)d;
            double y = (source.MaxSideLength / 2) - source.ArcSize.Height;
            return new Point(source.MaxSideLength / 2, y);
        }

        private static object CoerceArcEndPoint(
            DependencyObject d, object basevalue)
        {
            var source = (ActivityRingTemplateSettings)d;
            double y = (source.MaxSideLength / 2) - source.ArcSize.Height;
            return new Point(source.MaxSideLength / 2, source.MaxSideLength - y);
        }
    }
}
