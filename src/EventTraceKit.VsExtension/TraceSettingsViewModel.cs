namespace EventTraceKit.VsExtension
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public class TraceSettingsViewModel : ViewModel
    {
        private bool? dialogResult;
        private TraceSessionSettingsViewModel selectedSessionPreset;
        private ICommand newPresetCommand;
        private ICommand copyPresetCommand;
        private ICommand deletePresetCommand;

        public TraceSettingsViewModel()
        {
            AcceptCommand = new AsyncDelegateCommand(Accept);
        }

        public ICommand AcceptCommand { get; }

        public bool? DialogResult
        {
            get { return dialogResult; }
            set { SetProperty(ref dialogResult, value); }
        }

        public ICommand NewPresetCommand =>
            newPresetCommand ?? (newPresetCommand = new AsyncDelegateCommand(NewPreset));

        public ICommand CopyPresetCommand =>
            copyPresetCommand ?? (copyPresetCommand = new AsyncDelegateCommand(CopyPreset, CanCopyPreset));

        public ICommand DeletePresetCommand =>
            deletePresetCommand ?? (deletePresetCommand = new AsyncDelegateCommand(DeletePreset, CanDeletePreset));

        public TraceSessionSettingsViewModel SelectedSessionPreset
        {
            get { return selectedSessionPreset; }
            set { SetProperty(ref selectedSessionPreset, value); }
        }

        public ObservableCollection<TraceSessionSettingsViewModel> SessionPresets { get; }
            = new ObservableCollection<TraceSessionSettingsViewModel>();

        private Task NewPreset()
        {
            var newPreset = new TraceSessionSettingsViewModel();
            newPreset.Name = "Unnamed";
            SessionPresets.Add(newPreset);
            SelectedSessionPreset = newPreset;
            return Task.CompletedTask;
        }

        private bool CanCopyPreset()
        {
            return SelectedSessionPreset != null;
        }

        private Task CopyPreset()
        {
            if (SelectedSessionPreset != null) {
                var newPreset = SelectedSessionPreset.DeepClone();
                newPreset.Name = "Copy of " + SelectedSessionPreset.Name;
                SessionPresets.Add(newPreset);
                SelectedSessionPreset = newPreset;
            }

            return Task.CompletedTask;
        }

        private bool CanDeletePreset()
        {
            return SelectedSessionPreset != null;
        }

        private Task DeletePreset()
        {
            if (SelectedSessionPreset != null) {
                SessionPresets.Remove(SelectedSessionPreset);
                SelectedSessionPreset = null;
            }

            return Task.CompletedTask;
        }

        private Task Accept()
        {
            DialogResult = true;
            return Task.CompletedTask;
        }

        public TraceSessionDescriptor GetDescriptor()
        {
            return SelectedSessionPreset?.GetDescriptor();
        }

        public Dictionary<EventKey, string> GetEventSymbols()
        {
            return SelectedSessionPreset?.GetEventSymbols() ?? new Dictionary<EventKey, string>();
        }
    }
}
