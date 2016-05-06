namespace EventTraceKit.VsExtension
{
    using System.Collections.ObjectModel;
    using System.Windows.Input;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.Win32;

    public class TraceSessionSettingsWindowViewModel : ViewModel
    {
        public TraceSessionSettingsWindowViewModel()
        {
            AddProviderCommand = new DelegateCommand(AddProvider);
            AddManifestCommand = new DelegateCommand(AddManifest);
            AddBinaryCommand = new DelegateCommand(AddBinary);
            Providers = new ObservableCollection<TraceProviderSpecViewModel>();
        }

        public ICommand AddProviderCommand { get; }
        public ICommand AddManifestCommand { get; }
        public ICommand AddBinaryCommand { get; }
        public ObservableCollection<TraceProviderSpecViewModel> Providers { get; }

        private void AddProvider(object obj)
        {
        }

        private void AddManifest(object obj)
        {
            var dialog = new OpenFileDialog();
            dialog.ShowDialog();
        }

        private void AddBinary(object obj)
        {
        }
    }
}
