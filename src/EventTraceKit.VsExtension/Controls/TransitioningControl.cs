namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;

    [TemplatePart(Name = PART_PreviousContentPresentationSite, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PART_CurrentContentPresentationSite, Type = typeof(ContentPresenter))]
    [TemplateVisualState(Name = NormalState, GroupName = PresentationGroup)]
    public class TransitioningControl : ContentControl
    {
        internal const string PresentationGroup = "PresentationStates";
        internal const string NormalState = "Normal";
        internal const string PART_PreviousContentPresentationSite = "PreviousContentPresentationSite";
        internal const string PART_CurrentContentPresentationSite = "CurrentContentPresentationSite";

        private bool allowIsTransitioningPropertyWrite;
        private Storyboard currentTransition;
        private Storyboard normalTransition;

        private ContentPresenter previousContentPresentationSite;
        private ContentPresenter currentContentPresentationSite;

        public const TransitionType DefaultTransitionState = TransitionType.Default;

        public TransitioningControl()
        {
            DefaultStyleKey = typeof(TransitioningControl);
            CustomVisualStates = new ObservableCollection<VisualState>();
        }

        public event RoutedEventHandler TransitionCompleted;

        public static readonly DependencyProperty IsTransitioningProperty =
            DependencyProperty.Register(
                nameof(IsTransitioning),
                typeof(bool),
                typeof(TransitioningControl),
                new PropertyMetadata(OnIsTransitioningPropertyChanged));

        public static readonly DependencyProperty TransitionProperty =
            DependencyProperty.Register(
                nameof(Transition),
                typeof(TransitionType),
                typeof(TransitioningControl),
                new FrameworkPropertyMetadata(
                    TransitionType.Default,
                    FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.Inherits,
                    OnTransitionPropertyChanged));

        public static readonly DependencyProperty RestartTransitionOnContentChangeProperty =
            DependencyProperty.Register(
                nameof(RestartTransitionOnContentChange),
                typeof(bool),
                typeof(TransitioningControl),
                new PropertyMetadata(false, OnRestartTransitionOnContentChangePropertyChanged));

        public static readonly DependencyProperty CustomVisualStatesProperty =
            DependencyProperty.Register(
                nameof(CustomVisualStates),
                typeof(ObservableCollection<VisualState>),
                typeof(TransitioningControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty CustomVisualStatesNameProperty =
            DependencyProperty.Register(
                nameof(CustomVisualStatesName),
                typeof(string),
                typeof(TransitioningControl),
                new PropertyMetadata("CustomTransition"));

        private readonly DataTemplate nullTemplate = new DataTemplate();

        public ObservableCollection<VisualState> CustomVisualStates
        {
            get => (ObservableCollection<VisualState>)GetValue(CustomVisualStatesProperty);
            set => SetValue(CustomVisualStatesProperty, value);
        }

        /// <summary>
        ///   Gets or sets the name of the custom transition visual state.
        /// </summary>
        public string CustomVisualStatesName
        {
            get => (string)GetValue(CustomVisualStatesNameProperty);
            set => SetValue(CustomVisualStatesNameProperty, value);
        }

        /// <summary>
        ///   Gets a value indicating whether the content is transitioning.
        /// </summary>
        public bool IsTransitioning
        {
            get => (bool)GetValue(IsTransitioningProperty);
            private set
            {
                allowIsTransitioningPropertyWrite = true;
                SetValue(IsTransitioningProperty, value);
                allowIsTransitioningPropertyWrite = false;
            }
        }

        public TransitionType Transition
        {
            get => (TransitionType)GetValue(TransitionProperty);
            set => SetValue(TransitionProperty, value);
        }

        public bool RestartTransitionOnContentChange
        {
            get => (bool)GetValue(RestartTransitionOnContentChangeProperty);
            set => SetValue(RestartTransitionOnContentChangeProperty, value);
        }

        private static void OnIsTransitioningPropertyChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (TransitioningControl)d;
            if (!source.allowIsTransitioningPropertyWrite) {
                source.IsTransitioning = (bool)e.OldValue;
                throw new InvalidOperationException();
            }
        }

        private Storyboard CurrentTransition
        {
            get => currentTransition;
            set
            {
                if (currentTransition != null)
                    currentTransition.Completed -= OnTransitionCompleted;

                currentTransition = value;

                if (currentTransition != null)
                    currentTransition.Completed += OnTransitionCompleted;
            }
        }

        private static void OnTransitionPropertyChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (TransitioningControl)d;
            var oldValue = (TransitionType)e.OldValue;
            var newValue = (TransitionType)e.NewValue;

            if (source.IsTransitioning)
                source.AbortTransition();

            Storyboard storyboard = source.GetStoryboard(newValue);
            if (storyboard != null) {
                source.CurrentTransition = storyboard;
                return;
            }

            if (source.TryGetVisualStateGroup(PresentationGroup) == null) {
                source.CurrentTransition = null;
                return;
            }

            source.SetValue(TransitionProperty, oldValue);
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                "Temporary removed exception message", newValue));
        }

        private static void OnRestartTransitionOnContentChangePropertyChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = ((TransitioningControl)d);
            source.OnRestartTransitionOnContentChangeChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        protected virtual void OnRestartTransitionOnContentChangeChanged(bool oldValue, bool newValue)
        {
        }

        public override void OnApplyTemplate()
        {
            if (IsTransitioning)
                AbortTransition();

            if (CustomVisualStates != null && CustomVisualStates.Any()) {
                var stateGroup = this.TryGetVisualStateGroup(PresentationGroup);
                if (stateGroup != null) {
                    foreach (VisualState state in CustomVisualStates)
                        stateGroup.States.Add(state);
                }
            }

            base.OnApplyTemplate();

            previousContentPresentationSite = GetTemplateChild("PreviousContentPresentationSite") as ContentPresenter;
            currentContentPresentationSite = GetTemplateChild("CurrentContentPresentationSite") as ContentPresenter;

            if (currentContentPresentationSite != null)
                currentContentPresentationSite.ContentTemplate = ContentTemplate;

            normalTransition = GetStoryboard(TransitionType.Normal);

            Storyboard storyboard = GetStoryboard(Transition);
            CurrentTransition = storyboard;
            if (storyboard == null) {
                TransitionType transition = Transition;
                Transition = DefaultTransitionState;
                throw new ArgumentException($"'{transition}' transition not found.", nameof(Transition));
            }

            VisualStateManager.GoToState(this, NormalState, false);
        }

        protected override void OnContentTemplateChanged(
            DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
        {
            base.OnContentTemplateChanged(oldContentTemplate, newContentTemplate);
            StartTransition(oldContentTemplate, newContentTemplate);
        }

        private void StartTransition(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
        {
            if (currentContentPresentationSite == null ||
                previousContentPresentationSite == null)
                return;

            if (RestartTransitionOnContentChange)
                CurrentTransition.Completed -= OnTransitionCompleted;

            Debug.Assert(currentContentPresentationSite.ContentTemplate == oldContentTemplate);

            var temp = previousContentPresentationSite;
            previousContentPresentationSite = currentContentPresentationSite;
            currentContentPresentationSite = temp;

            currentContentPresentationSite.ContentTemplate = newContentTemplate;

            //PreviousContentPresentationSite.ContentTemplate = oldContentTemplate;
            //CurrentContentPresentationSite.ContentTemplate = newContentTemplate;

            SetTransitionTargets();

            if (IsTransitioning && !RestartTransitionOnContentChange)
                return;

            if (RestartTransitionOnContentChange)
                CurrentTransition.Completed += OnTransitionCompleted;
            IsTransitioning = true;

            VisualStateManager.GoToState(this, NormalState, false);
            VisualStateManager.GoToState(this, GetTransitionName(Transition), true);
        }

        private void SetTransitionTargets()
        {
            foreach (var timeline in CurrentTransition.Children) {
                switch (Storyboard.GetTargetName(timeline)) {
                    case PART_PreviousContentPresentationSite:
                        Storyboard.SetTarget(timeline, previousContentPresentationSite);
                        break;
                    case PART_CurrentContentPresentationSite:
                        Storyboard.SetTarget(timeline, currentContentPresentationSite);
                        break;
                }
            }

            foreach (var timeline in normalTransition.Children) {
                switch (Storyboard.GetTargetName(timeline)) {
                    case PART_PreviousContentPresentationSite:
                        Storyboard.SetTarget(timeline, previousContentPresentationSite);
                        break;
                    case PART_CurrentContentPresentationSite:
                        Storyboard.SetTarget(timeline, currentContentPresentationSite);
                        break;
                }
            }
        }

        /// <summary>
        ///   Reloads the current transition if the content is the same.
        /// </summary>
        public void ReloadTransition()
        {
            if (currentContentPresentationSite == null ||
                previousContentPresentationSite == null)
                return;

            if (RestartTransitionOnContentChange)
                CurrentTransition.Completed -= OnTransitionCompleted;

            if (IsTransitioning && !RestartTransitionOnContentChange)
                return;

            if (RestartTransitionOnContentChange)
                CurrentTransition.Completed += OnTransitionCompleted;

            IsTransitioning = true;

            VisualStateManager.GoToState(this, NormalState, false);
            VisualStateManager.GoToState(this, GetTransitionName(Transition), true);
        }

        private void OnTransitionCompleted(object sender, EventArgs e)
        {
            var clockGroup = sender as ClockGroup;
            AbortTransition();
            if (clockGroup == null || clockGroup.CurrentState == ClockState.Stopped)
                TransitionCompleted?.Invoke(this, new RoutedEventArgs());
        }

        public void AbortTransition()
        {
            VisualStateManager.GoToState(this, NormalState, false);
            IsTransitioning = false;

            if (previousContentPresentationSite != null)
                previousContentPresentationSite.ContentTemplate = nullTemplate;
        }

        private Storyboard GetStoryboard(TransitionType newTransition)
        {
            var stateGroup = this.TryGetVisualStateGroup(PresentationGroup);
            if (stateGroup == null)
                return null;

            string name = GetTransitionName(newTransition);

            return (from x in stateGroup.States.OfType<VisualState>()
                    where x.Name == name
                    select x.Storyboard).FirstOrDefault();
        }

        private string GetTransitionName(TransitionType transition)
        {
            switch (transition) {
                case TransitionType.Normal:
                    return NormalState;
                case TransitionType.Up:
                    return "UpTransition";
                case TransitionType.Down:
                    return "DownTransition";
                case TransitionType.Right:
                    return "RightTransition";
                case TransitionType.RightReplace:
                    return "RightReplaceTransition";
                case TransitionType.Left:
                    return "LeftTransition";
                case TransitionType.LeftReplace:
                    return "LeftReplaceTransition";
                case TransitionType.Custom:
                    return CustomVisualStatesName;
                default:
                    return "DefaultTransition";
            }
        }
    }
}
