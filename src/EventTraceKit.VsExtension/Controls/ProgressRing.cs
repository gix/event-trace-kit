namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using Windows;

    /// <summary>
    ///   Represents a control that indicates that an operation is ongoing. The
    ///   typical visual appearance is a ring-shaped "spinner" that cycles an
    ///   animation as progress continues.
    /// </summary>
    [TemplateVisualState(Name = InactiveState, GroupName = ActiveStatesGroup)]
    [TemplateVisualState(Name = ActiveState, GroupName = ActiveStatesGroup)]
    [TemplateVisualState(Name = SmallSize, GroupName = SizeStatesGroup)]
    [TemplateVisualState(Name = LargeSize, GroupName = SizeStatesGroup)]
    public sealed class ProgressRing : Control
    {
        private const string ActiveStatesGroup = "ActiveStates";
        private const string ActiveState = "Active";
        private const string InactiveState = "Inactive";

        private const string SizeStatesGroup = "SizeStates";
        private const string SmallSize = "Small";
        private const string LargeSize = "Large";

        /// <summary>
        ///   Identifies the <see cref="IsActive"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(ProgressRing),
                new PropertyMetadata(false, IsActiveChanged));

        static ProgressRing()
        {
            DefaultStyleKeyProperty.OverrideMetadata<ProgressRing>();
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ProgressRing"/> class.
        /// </summary>
        public ProgressRing()
        {
            TemplateSettings = new ProgressRingTemplateSettings(this);
        }

        /// <summary>
        ///   Gets or sets a value that indicates whether the <see cref="ProgressRing"/>
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
        ///   <see cref="ProgressRing"/> control.
        /// </summary>
        public ProgressRingTemplateSettings TemplateSettings { get; }

        /// <summary>
        ///   <see cref="IsActiveProperty"/> property changed handler.
        /// </summary>
        /// <param name="d">
        ///   <see cref="ProgressRing"/> that changed its active state.
        /// </param>
        /// <param name="e">Event arguments.</param>
        private static void IsActiveChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (ProgressRing)d;
            source.UpdateActiveState();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            TemplateSettings.InvalidateProperty(ProgressRingTemplateSettings.MaxSideLengthProperty);
            TemplateSettings.InvalidateProperty(ProgressRingTemplateSettings.EllipseDiameterProperty);
            TemplateSettings.InvalidateProperty(ProgressRingTemplateSettings.EllipseOffsetProperty);
            UpdateLargeState();
            base.OnRenderSizeChanged(sizeInfo);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateActiveState();
            UpdateLargeState();
        }

        private void UpdateActiveState()
        {
            string state = IsActive ? ActiveState : InactiveState;
            VisualStateManager.GoToState(this, state, true);
        }

        private void UpdateLargeState()
        {
            string state = TemplateSettings.MaxSideLength >= 60 ? LargeSize : SmallSize;
            VisualStateManager.GoToState(this, state, true);
        }
    }

    public sealed class ProgressRingTemplateSettings : DependencyObject
    {
        private readonly ProgressRing progressRing;

        public ProgressRingTemplateSettings(ProgressRing progressRing)
        {
            this.progressRing = progressRing;
        }

        private static readonly DependencyPropertyKey MaxSideLengthPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(MaxSideLength),
                typeof(double),
                typeof(ProgressRingTemplateSettings),
                new PropertyMetadata(
                    default(double), null, CoerceMaxSideLength));

        private static readonly DependencyPropertyKey EllipseDiameterPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(EllipseDiameter),
                typeof(double),
                typeof(ProgressRingTemplateSettings),
                new PropertyMetadata(default(double), null, CoerceEllipseDiameter));

        private static readonly DependencyPropertyKey EllipseOffsetPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(EllipseOffset),
                typeof(Thickness),
                typeof(ProgressRingTemplateSettings),
                new PropertyMetadata(default(Thickness), null, CoerceEllipseOffset));

        public static readonly DependencyProperty MaxSideLengthProperty =
            MaxSideLengthPropertyKey.DependencyProperty;

        public static readonly DependencyProperty EllipseDiameterProperty =
            EllipseDiameterPropertyKey.DependencyProperty;

        public static readonly DependencyProperty EllipseOffsetProperty =
            EllipseOffsetPropertyKey.DependencyProperty;

        public double MaxSideLength => (double)GetValue(MaxSideLengthProperty);

        public double EllipseDiameter => (double)GetValue(EllipseDiameterProperty);

        public Thickness EllipseOffset => (Thickness)GetValue(EllipseOffsetProperty);

        private static object CoerceMaxSideLength(
            DependencyObject d, object baseValue)
        {
            var source = (ProgressRingTemplateSettings)d;
            return Math.Min(source.progressRing.ActualWidth,
                            source.progressRing.ActualHeight);
        }

        private static object CoerceEllipseDiameter(
            DependencyObject d, object baseValue)
        {
            var source = (ProgressRingTemplateSettings)d;
            double length = source.MaxSideLength;
            if (length <= 40)
                length += 10;

            return length / 10;
        }

        private static object CoerceEllipseOffset(
            DependencyObject d, object baseValue)
        {
            var source = (ProgressRingTemplateSettings)d;

            double top = 0.4 * source.MaxSideLength;
            if (source.MaxSideLength <= 40)
                top -= 1;

            return new Thickness(0, top, 0, 0);
        }
    }
}
