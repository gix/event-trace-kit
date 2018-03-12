namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;
    using EventTraceKit.VsExtension.Native;
    using EventTraceKit.VsExtension.Windows;

    [TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
    public class DropDownTextBox : Control
    {
        private Popup dropDownPopup;

        static DropDownTextBox()
        {
            var thisType = typeof(DropDownTextBox);
            DefaultStyleKeyProperty.OverrideMetadata(
                thisType, new FrameworkPropertyMetadata(thisType));

            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(
                thisType, new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
            KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(
                thisType, new FrameworkPropertyMetadata(KeyboardNavigationMode.None));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(
                thisType, new FrameworkPropertyMetadata(KeyboardNavigationMode.None));
            ToolTipService.IsEnabledProperty.OverrideMetadata(
                thisType, new FrameworkPropertyMetadata(null, CoerceToolTipIsEnabled));

            ItemsControl.IsTextSearchEnabledProperty.OverrideMetadata(thisType, new FrameworkPropertyMetadata(Boxed.True));
            EventManager.RegisterClassHandler(thisType, Mouse.LostMouseCaptureEvent, new MouseEventHandler(OnLostMouseCapture));
            EventManager.RegisterClassHandler(thisType, Mouse.MouseDownEvent, new MouseButtonEventHandler(OnMouseButtonDown), true);
            EventManager.RegisterClassHandler(thisType, Mouse.MouseMoveEvent, new MouseEventHandler(OnMouseMove));
            EventManager.RegisterClassHandler(thisType, Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(OnPreviewMouseButtonDown));
            EventManager.RegisterClassHandler(thisType, GotFocusEvent, new RoutedEventHandler(OnGotFocus));
            EventManager.RegisterClassHandler(thisType, ContextMenuService.ContextMenuOpeningEvent, new ContextMenuEventHandler(OnContextMenuOpen), true);
            EventManager.RegisterClassHandler(thisType, ContextMenuService.ContextMenuClosingEvent, new ContextMenuEventHandler(OnContextMenuClose), true);
        }

        public DropDownTextBox()
        {
            SetBinding(DropDownContentProperty, new Binding());
        }

        #region public bool IsDropDownOpen { get; set; }

        /// <summary>
        ///   Identifies the <see cref="IsDropDownOpen"/>  dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(
                nameof(IsDropDownOpen),
                typeof(bool),
                typeof(DropDownTextBox),
                new FrameworkPropertyMetadata(
                    Boxed.False,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsDropDownOpenChanged,
                    CoerceIsDropDownOpen));

        /// <summary>
        ///   Gets or sets a value that indicates whether the drop-down is currently
        ///   open.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if the drop-down is open; otherwise,
        ///   <see langword="false"/>. The default is <see langword="false"/>.
        /// </returns>
        [Bindable(true)]
        [Browsable(false)]
        [Category("Appearance")]
        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, Boxed.Bool(value));
        }

        private static object CoerceIsDropDownOpen(DependencyObject d, object value)
        {
            if ((bool)value) {
                var textBox = (DropDownTextBox)d;
                if (!textBox.IsLoaded) {
                    textBox.RegisterToOpenOnLoad();
                    return Boxed.False;
                }
            }
            return value;
        }

        private void RegisterToOpenOnLoad()
        {
            Loaded += OpenOnLoad;
        }

        private void OpenOnLoad(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => CoerceValue(IsDropDownOpenProperty), DispatcherPriority.Input);
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBox = (DropDownTextBox)d;
            textBox.HasMouseEnteredItemsHost = false;

            bool newValue = (bool)e.NewValue;
            bool oldValue = !newValue;

            if (UIElementAutomationPeer.FromElement(textBox) is DropDownTextBoxAutomationPeer peer)
                peer.RaiseExpandCollapseAutomationEvent(oldValue, newValue);

            if (newValue) {
                Mouse.Capture(textBox, CaptureMode.SubTree);
                textBox.TextBoxSite?.SelectAll();

                //if (textBox._clonedElement != null && VisualTreeHelper.GetParent((DependencyObject)textBox._clonedElement) == null) {
                //    textBox.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Delegate)(arg => {
                //        DropDownTextBox comboBox2 = (DropDownTextBox)arg;
                //        comboBox2.UpdateSelectionBoxItem();
                //        if (comboBox2._clonedElement != null)
                //            comboBox2._clonedElement.CoerceValue(FlowDirectionProperty);
                //        return (object)null;
                //    }), (object)textBox);
                //}

                //textBox.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action<object>(arg => {
                //    var comboBox2 = (DropDownTextBox)arg;
                //    return (object)null;
                //}), textBox);

                textBox.OnDropDownOpened(EventArgs.Empty);
            } else {
                if (textBox.IsKeyboardFocusWithin) {
                    if (textBox.TextBoxSite != null && !textBox.TextBoxSite.IsKeyboardFocusWithin)
                        textBox.Focus();
                }

                if (ReferenceEquals(Mouse.Captured, textBox))
                    Mouse.Capture(null);

                if (textBox.dropDownPopup == null)
                    textBox.OnDropDownClosed(EventArgs.Empty);
            }

            //textBox.CoerceValue(DropDownTextBox.IsSelectionBoxHighlightedProperty);
            textBox.CoerceValue(ToolTipService.IsEnabledProperty);
            //textBox.UpdateVisualState();
        }

        #endregion

        #region public object DropDownContent { get; set; }

        /// <summary>
        ///   Identifies the <see cref="DropDownContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DropDownContentProperty =
            DependencyProperty.Register(
                nameof(DropDownContent),
                typeof(object),
                typeof(DropDownTextBox),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets the content displayed in the drop-down.
        /// </summary>
        [Bindable(true)]
        [Category("Content")]
        [Localizability(LocalizationCategory.Label)]
        public object DropDownContent
        {
            get => GetValue(DropDownContentProperty);
            set => SetValue(DropDownContentProperty, value);
        }

        #endregion

        #region public string DropDownContentStringFormat { get; set; }

        /// <summary>
        ///   Identifies the <see cref="DropDownContentTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DropDownContentStringFormatProperty =
            DependencyProperty.Register(
                nameof(DropDownContentStringFormat),
                typeof(string),
                typeof(DropDownTextBox),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets the drop-down content string format.
        /// </summary>
        [Bindable(true)]
        [Category("Content")]
        public string DropDownContentStringFormat
        {
            get => (string)GetValue(DropDownContentStringFormatProperty);
            set => SetValue(DropDownContentStringFormatProperty, value);
        }

        #endregion

        #region public DataTemplate DropDownContentTemplate { get; set; }

        /// <summary>
        ///   Identifies the <see cref="DropDownContentTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DropDownContentTemplateProperty =
            DependencyProperty.Register(
                nameof(DropDownContentTemplate),
                typeof(DataTemplate),
                typeof(DropDownTextBox),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets the drop-down content template.
        /// </summary>
        [Bindable(true)]
        [Category("Content")]
        public DataTemplate DropDownContentTemplate
        {
            get => (DataTemplate)GetValue(DropDownContentTemplateProperty);
            set => SetValue(DropDownContentTemplateProperty, value);
        }

        #endregion

        #region public DataTemplateSelector DropDownContentTemplateSelector { get; set; }

        /// <summary>
        ///   Identifies the <see cref="DropDownContentTemplateSelector"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DropDownContentTemplateSelectorProperty =
            DependencyProperty.Register(
                nameof(DropDownContentTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(DropDownTextBox),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets the drop-down content template selector.
        /// </summary>
        [Bindable(true)]
        [Category("Content")]
        public DataTemplateSelector DropDownContentTemplateSelector
        {
            get => (DataTemplateSelector)GetValue(DropDownContentTemplateSelectorProperty);
            set => SetValue(DropDownContentTemplateSelectorProperty, value);
        }

        #endregion

        #region public string Text { get; set; }

        /// <summary>Gets or sets the text contents of the text box.</summary>
        /// <returns>
        ///   A string containing the text contents of the text box. The default
        ///   is an empty string ("").
        /// </returns>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        ///   Identifies the <see cref="Text"/>  dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(DropDownTextBox),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault |
                    FrameworkPropertyMetadataOptions.Journal,
                    OnTextChanged));

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (DropDownTextBox)d;
            if (UIElementAutomationPeer.FromElement(source) is DropDownTextBoxAutomationPeer peer)
                peer.RaiseValuePropertyChangedEvent((string)e.OldValue, (string)e.NewValue);
            source.TextUpdated((string)e.NewValue, false);
        }

        #endregion

        #region public bool IsReadOnly { get; set; }

        /// <summary>
        ///   Identifies the <see cref="IsReadOnly"/>
        ///    dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            TextBoxBase.IsReadOnlyProperty.AddOwner(typeof(DropDownTextBox));

        /// <summary>
        ///   Gets or sets a value that enables selection-only mode, in which
        ///   the contents of the text box are selectable but not editable.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="DropDownTextBox"/> is
        ///   read-only; otherwise, <see langword="false"/>. The default is
        ///   <see langword="false"/>.
        /// </returns>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, Boxed.Bool(value));
        }

        #endregion

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new DropDownTextBoxAutomationPeer(this);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (TextBoxSite != null) {
                TextBoxSite.TextChanged -= OnTextBoxTextChanged;
                TextBoxSite.SelectionChanged -= OnTextBoxSelectionChanged;
                TextBoxSite.PreviewTextInput -= OnTextBoxPreviewTextInput;
            }

            if (dropDownPopup != null)
                dropDownPopup.Closed -= OnPopupClosed;

            TextBoxSite = GetTemplateChild("PART_TextBox") as TextBox;
            dropDownPopup = GetTemplateChild("PART_Popup") as Popup;

            if (TextBoxSite != null) {
                TextBoxSite.TextChanged += OnTextBoxTextChanged;
                TextBoxSite.SelectionChanged += OnTextBoxSelectionChanged;
                TextBoxSite.PreviewTextInput += OnTextBoxPreviewTextInput;
            }

            if (dropDownPopup != null)
                dropDownPopup.Closed += OnPopupClosed;

            Update();
        }

        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs args)
        {
            TextUpdated(TextBoxSite.Text, true);
        }

        private void OnTextBoxSelectionChanged(object sender, RoutedEventArgs args)
        {
        }

        private void OnTextBoxPreviewTextInput(object sender, TextCompositionEventArgs args)
        {
            //if (!IsWaitingForTextComposition || args.TextComposition.Source != TextBoxSite || args.TextComposition.Stage != TextCompositionStage.Done)
            //    return;
            //
            //IsWaitingForTextComposition = false;
            //TextUpdated(TextBoxSite.Text, true);
            //TextBoxSite.RaiseCourtesyTextChangedEvent();
        }

        private void TextUpdated(string newText, bool textBoxUpdated)
        {
            if (UpdatingText)
                return;

            try {
                UpdatingText = true;
                if (textBoxUpdated) {
                    SetCurrentValue(TextProperty, newText);
                } else {
                    if (TextBoxSite != null)
                        TextBoxSite.Text = newText;
                }
            } finally {
                UpdatingText = false;
            }
        }

        private BitVector32 _cacheValid = new BitVector32(0);

        private bool UpdatingText
        {
            get => _cacheValid[(int)CacheBits.UpdatingText];
            set => _cacheValid[(int)CacheBits.UpdatingText] = value;
        }

        private bool HasMouseEnteredItemsHost
        {
            get => _cacheValid[(int)CacheBits.HasMouseEnteredItemsHost];
            set => _cacheValid[(int)CacheBits.HasMouseEnteredItemsHost] = value;
        }

        private bool IsContextMenuOpen
        {
            get => _cacheValid[(int)CacheBits.IsContextMenuOpen];
            set => _cacheValid[(int)CacheBits.IsContextMenuOpen] = value;
        }

        private bool IsMouseOverItemsHost
        {
            get => _cacheValid[(int)CacheBits.IsMouseOverItemsHost];
            set => _cacheValid[(int)CacheBits.IsMouseOverItemsHost] = value;
        }

        private enum CacheBits
        {
            IsMouseOverItemsHost = 0x1,
            HasMouseEnteredItemsHost = 0x2,
            IsContextMenuOpen = 0x4,
            UpdatingText = 0x8,
            IsWaitingForTextComposition = 0x10,
        }

        private void UpdateTextBox()
        {
            if (UpdatingText)
                return;

            try {
                UpdatingText = true;
                string text = Text;
                if (TextBoxSite != null && TextBoxSite.Text != text) {
                    TextBoxSite.Text = text;
                    TextBoxSite.SelectAll();
                }
            } finally {
                UpdatingText = false;
            }
        }

        private void Update()
        {
            UpdateTextBox();
        }

        private void OnPopupClosed(object source, EventArgs args)
        {
            OnDropDownClosed(EventArgs.Empty);
        }

        internal TextBox TextBoxSite { get; set; }

        /// <summary>Occurs when the drop-down opens.</summary>
        public event EventHandler DropDownOpened;

        /// <summary>Occurs when the drop-down closes.</summary>
        public event EventHandler DropDownClosed;

        /// <summary>Reports when a drop-down text box's popup opens.</summary>
        /// <param name="args">
        ///   The event data for the <see cref="DropDownOpened"/> event.
        /// </param>
        protected virtual void OnDropDownOpened(EventArgs args)
        {
            DropDownOpened?.Invoke(this, args);
        }

        /// <summary>Reports when a drop-down text box's popup closes.</summary>
        /// <param name="args">
        ///   The event data for the <see cref="DropDownClosed"/> event.
        /// </param>
        protected virtual void OnDropDownClosed(EventArgs args)
        {
            DropDownClosed?.Invoke(this, args);
        }

        private static void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            var source = (DropDownTextBox)sender;
            if (Mouse.Captured == source)
                return;

            if (e.OriginalSource == source) {
                if (Mouse.Captured == null || !FrameworkExtensions.IsDescendant(source, Mouse.Captured as DependencyObject))
                    source.Close();
            } else if (FrameworkExtensions.IsDescendant(source, e.OriginalSource as DependencyObject)) {
                if (source.IsDropDownOpen && Mouse.Captured == null && NativeMethods.GetCapture() == IntPtr.Zero) {
                    Mouse.Capture(source, CaptureMode.SubTree);
                    e.Handled = true;
                }
            } else
                source.Close();
        }

        private static void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            var source = (DropDownTextBox)sender;
            if (!source.IsContextMenuOpen && !source.IsKeyboardFocusWithin)
                source.Focus();

            e.Handled = true;

            if (Mouse.Captured == source && e.OriginalSource == source)
                source.Close();
        }

        private static void OnPreviewMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            var source = (DropDownTextBox)sender;

            var originalSource = e.OriginalSource as Visual;
            var textBox = source.TextBoxSite;
            if (originalSource == null || textBox == null || !textBox.IsAncestorOf(originalSource))
                return;

            if (source.IsDropDownOpen) {
                source.Close();
            } else if (!source.IsContextMenuOpen && !source.IsKeyboardFocusWithin) {
                source.Focus();
                e.Handled = true;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (HasMouseEnteredItemsHost && !IsMouseOverItemsHost && IsDropDownOpen) {
                Close();
                e.Handled = true;
            }
            base.OnMouseLeftButtonUp(e);
        }

        protected override bool HandlesScrolling => true;

        protected override bool HasEffectiveKeyboardFocus
        {
            get
            {
                if (TextBoxSite != null)
                    return TextBoxSite.IsKeyboardFocused;
                return base.HasEffectiveKeyboardFocus;
            }
        }

        private static object CoerceToolTipIsEnabled(DependencyObject d, object value)
        {
            return ((DropDownTextBox)d).IsDropDownOpen ? Boxed.False : value;
        }

        private Point _lastMousePosition;
        private UIElement ItemsHost => dropDownPopup;

        internal void SetInitialMousePosition()
        {
            this._lastMousePosition = Mouse.GetPosition(this);
        }

        internal void ResetLastMousePosition()
        {
            this._lastMousePosition = new Point();
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            var source = (DropDownTextBox)sender;
            if (source.IsDropDownOpen) {
                bool flag = source.ItemsHost != null && source.ItemsHost.IsMouseOver;
                if (flag && !source.HasMouseEnteredItemsHost)
                    source.SetInitialMousePosition();
                source.IsMouseOverItemsHost = flag;
                source.HasMouseEnteredItemsHost |= flag;
            }

            if (Mouse.LeftButton == MouseButtonState.Pressed && source.HasMouseEnteredItemsHost &&
                Mouse.Captured == source) {
                if (Mouse.LeftButton == MouseButtonState.Pressed) {
                    //source.DoAutoScroll(source.HighlightedInfo);
                } else {
                    source.ReleaseMouseCapture();
                    //source.ResetLastMousePosition();
                }

                e.Handled = true;
            }
        }

        private void KeyboardToggleDropDown(bool commitSelection)
        {
            KeyboardToggleDropDown(!IsDropDownOpen, commitSelection);
        }

        private void KeyboardCloseDropDown(bool commitSelection)
        {
            KeyboardToggleDropDown(false, commitSelection);
        }

        private void KeyboardToggleDropDown(bool openDropDown, bool commitSelection)
        {
            SetCurrentValue(IsDropDownOpenProperty, Boxed.Bool(openDropDown));
        }

        private static void OnGotFocus(object sender, RoutedEventArgs e)
        {
            var source = (DropDownTextBox)sender;
            if (e.Handled || source.TextBoxSite == null)
                return;

            if (e.OriginalSource == source) {
                source.TextBoxSite.Focus();
                e.Handled = true;
            } else if (e.OriginalSource == source.TextBoxSite) {
                source.TextBoxSite.SelectAll();
            }
        }

        private void Close()
        {
            if (IsDropDownOpen)
                SetCurrentValue(IsDropDownOpenProperty, Boxed.False);
        }

        private static void OnContextMenuOpen(object sender, ContextMenuEventArgs e)
        {
            ((DropDownTextBox)sender).IsContextMenuOpen = true;
        }

        private static void OnContextMenuClose(object sender, ContextMenuEventArgs e)
        {
            ((DropDownTextBox)sender).IsContextMenuOpen = false;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.OriginalSource == TextBoxSite)
                KeyDownHandler(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            KeyDownHandler(e);
        }

        private void KeyDownHandler(KeyEventArgs e)
        {
            var key = e.Key;
            if (key == Key.System)
                key = e.SystemKey;

            bool handled = false;
            switch (key) {
                case Key.Return:
                    if (IsDropDownOpen) {
                        KeyboardCloseDropDown(true);
                        handled = true;
                    }

                    break;
                case Key.Escape:
                    if (IsDropDownOpen) {
                        KeyboardCloseDropDown(false);
                        handled = true;
                    }

                    break;

                case Key.Down:
                    handled = true;
                    if ((e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                        KeyboardToggleDropDown(true);
                    break;

                case Key.F4:
                    if ((e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == ModifierKeys.None) {
                        KeyboardToggleDropDown(true);
                        handled = true;
                    }
                    break;
            }

            if (handled)
                e.Handled = true;
        }
    }
}
