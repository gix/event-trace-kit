namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;

    [TemplatePart(Name = PART_PreviousContentPresentationSite, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PART_CurrentContentPresentationSite, Type = typeof(ContentPresenter))]
    [TemplateVisualState(Name = NormalState, GroupName = PresentationGroup)]
    public class TransitioningContentControl : ContentControl
    {
        internal const string PresentationGroup = "PresentationStates";
        internal const string NormalState = "Normal";
        internal const string PART_PreviousContentPresentationSite = "PreviousContentPresentationSite";
        internal const string PART_CurrentContentPresentationSite = "CurrentContentPresentationSite";

        private ContentPresenter currentContentPresentationSite;
        private ContentPresenter previousContentPresentationSite;
        private bool allowIsTransitioningPropertyWrite;
        private Storyboard currentTransition;

        public const TransitionType DefaultTransitionState = TransitionType.Default;

        public TransitioningContentControl()
        {
            DefaultStyleKey = typeof(TransitioningContentControl);
            CustomVisualStates = new ObservableCollection<VisualState>();
        }

        public event RoutedEventHandler TransitionCompleted;

        public static readonly DependencyProperty IsTransitioningProperty =
            DependencyProperty.Register(
                nameof(IsTransitioning),
                typeof(bool),
                typeof(TransitioningContentControl),
                new PropertyMetadata(OnIsTransitioningPropertyChanged));

        public static readonly DependencyProperty TransitionProperty =
            DependencyProperty.Register(
                nameof(Transition),
                typeof(TransitionType),
                typeof(TransitioningContentControl),
                new FrameworkPropertyMetadata(
                    TransitionType.Default,
                    FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.Inherits,
                    OnTransitionPropertyChanged));

        public static readonly DependencyProperty RestartTransitionOnContentChangeProperty =
            DependencyProperty.Register(
                nameof(RestartTransitionOnContentChange),
                typeof(bool),
                typeof(TransitioningContentControl),
                new PropertyMetadata(false, OnRestartTransitionOnContentChangePropertyChanged));

        public static readonly DependencyProperty CustomVisualStatesProperty =
            DependencyProperty.Register(
                nameof(CustomVisualStates),
                typeof(ObservableCollection<VisualState>),
                typeof(TransitioningContentControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty CustomVisualStatesNameProperty =
            DependencyProperty.Register(
                nameof(CustomVisualStatesName),
                typeof(string),
                typeof(TransitioningContentControl),
                new PropertyMetadata("CustomTransition"));

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
            var source = (TransitioningContentControl)d;
            if (!source.allowIsTransitioningPropertyWrite)
                source.IsTransitioning = (bool)e.OldValue;
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
            var source = (TransitioningContentControl)d;
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
        }

        private static void OnRestartTransitionOnContentChangePropertyChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = ((TransitioningContentControl)d);
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

            previousContentPresentationSite = GetTemplateChild(PART_PreviousContentPresentationSite) as ContentPresenter;
            currentContentPresentationSite = GetTemplateChild(PART_CurrentContentPresentationSite) as ContentPresenter;

            if (currentContentPresentationSite != null) {
                currentContentPresentationSite.ContentTemplate =
                    ContentTemplateSelector != null
                        ? ContentTemplateSelector.SelectTemplate(Content, this)
                        : ContentTemplate;
                currentContentPresentationSite.Content = Content;
            }

            Storyboard storyboard = GetStoryboard(Transition);
            CurrentTransition = storyboard;
            if (storyboard == null) {
                TransitionType transition = Transition;
                Transition = DefaultTransitionState;
                throw new ArgumentException($"'{transition}' transition not found.", nameof(Transition));
            }

            VisualStateManager.GoToState(this, NormalState, false);
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            StartTransition(oldContent, newContent);
        }

        private void StartTransition(object oldContent, object newContent)
        {
            if (currentContentPresentationSite == null ||
                previousContentPresentationSite == null)
                return;

            if (RestartTransitionOnContentChange)
                CurrentTransition.Completed -= OnTransitionCompleted;

            if (ContentTemplateSelector != null) {
                previousContentPresentationSite.ContentTemplate = ContentTemplateSelector.SelectTemplate(oldContent, this);
                currentContentPresentationSite.ContentTemplate = ContentTemplateSelector.SelectTemplate(newContent, this);
            } else {
                previousContentPresentationSite.ContentTemplate = ContentTemplate;
                currentContentPresentationSite.ContentTemplate = ContentTemplate;
            }

            previousContentPresentationSite.Content = oldContent;
            currentContentPresentationSite.Content = newContent;

            if (IsTransitioning && !RestartTransitionOnContentChange)
                return;

            if (RestartTransitionOnContentChange)
                CurrentTransition.Completed += OnTransitionCompleted;
            IsTransitioning = true;

            VisualStateManager.GoToState(this, NormalState, false);
            VisualStateManager.GoToState(this, GetTransitionName(Transition), true);
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
            AbortTransition();
            if (!(sender is ClockGroup clockGroup) || clockGroup.CurrentState == ClockState.Stopped)
                TransitionCompleted?.Invoke(this, new RoutedEventArgs());
        }

        public void AbortTransition()
        {
            VisualStateManager.GoToState(this, NormalState, false);
            IsTransitioning = false;

            if (previousContentPresentationSite != null) {
                //previousContentPresentationSite.ContentTemplate = null;
                //previousContentPresentationSite.Content = null;
            }
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
            return transition switch
            {
                TransitionType.Normal => NormalState,
                TransitionType.Up => "UpTransition",
                TransitionType.Down => "DownTransition",
                TransitionType.Right => "RightTransition",
                TransitionType.RightReplace => "RightReplaceTransition",
                TransitionType.Left => "LeftTransition",
                TransitionType.LeftReplace => "LeftReplaceTransition",
                TransitionType.Custom => CustomVisualStatesName,
                _ => "DefaultTransition",
            };
        }
    }

    public enum TransitionType
    {
        Default,
        Normal,
        Up,
        Down,
        Right,
        RightReplace,
        Left,
        LeftReplace,
        Custom,
    }
}
