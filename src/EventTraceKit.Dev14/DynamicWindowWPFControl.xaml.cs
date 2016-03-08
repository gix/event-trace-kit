namespace EventTraceKit.Dev14
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    /// <summary>
    /// Interaction logic for DynamicWindowWPFControl.xaml
    /// </summary>
    public partial class DynamicWindowWPFControl : UserControl
    {
        public DynamicWindowWPFControl()
        {
            InitializeComponent();
        }

        private WindowStatus currentState = null;
        /// <summary>
        /// This is the object that will keep track of the state of the IVsWindowFrame
        /// that is hosting this control. The pane should set this property once
        /// the frame is created to enable us to stay up to date.
        /// </summary>
        public WindowStatus CurrentState
        {
            get { return currentState; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                currentState = value;
                // Subscribe to the change notification so we can update our UI
                currentState.StatusChange += new EventHandler<EventArgs>(RefreshValues);
                // Update the display now
                RefreshValues(this, null);
            }
        }

        /// <summary>
        /// This method is the call back for state changes events
        /// </summary>
        /// <param name="sender">Event senders</param>
        /// <param name="arguments">Event arguments</param>
        private void RefreshValues(object sender, EventArgs arguments)
        {
            xText.Text = currentState.X.ToString(CultureInfo.CurrentCulture);
            yText.Text = currentState.Y.ToString(CultureInfo.CurrentCulture);
            widthText.Text = currentState.Width.ToString(CultureInfo.CurrentCulture);
            heightText.Text = currentState.Height.ToString(CultureInfo.CurrentCulture);
            dockedCheckBox.IsChecked = currentState.IsDockable;
            InvalidateVisual();
        }

        private void InvertColors(object sender, RoutedEventArgs e)
        {
            Brush temp;
            temp = Background;
            Background = Foreground;
            Foreground = temp;
        }
    }
}
