namespace EventTraceKit.VsExtension.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    public partial class TraceSettingsWindow
    {
        public TraceSettingsWindow()
        {
            InitializeComponent();
        }

        private void ProfileNameTextBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            textBox.Focus();
            Keyboard.Focus(textBox);

            textBox.SelectAll();
        }
    }
}
