namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Threading;

    public partial class ProvidersListControl
    {
        private INotifyPropertyChanged observedItem;

        public ProvidersListControl()
        {
            InitializeComponent();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            foreach (EventProviderViewModel removedItem in args.RemovedItems)
                removedItem.DiscardAndSwitchFromRenamingModeCommand.Execute(null);

            if (observedItem != null)
                observedItem.PropertyChanged -= OnSelectedItemPropertyChanged;

            observedItem = ProvidersListBox.SelectedItem as INotifyPropertyChanged;

            if (observedItem != null)
                observedItem.PropertyChanged += OnSelectedItemPropertyChanged;
        }

        private void OnSelectedItemPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(EventProviderViewModel.IsInRenamingingMode)) {
                var list = ProvidersListBox;
                var item = list.SelectedItem;
                list.Dispatcher.InvokeAsync(
                    () => list.ScrollIntoView(item),
                    DispatcherPriority.ContextIdle);
            }
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
