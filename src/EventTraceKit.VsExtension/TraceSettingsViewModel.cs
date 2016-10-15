namespace EventTraceKit.VsExtension
{
    using System;
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

    public class TraceSettingsDesignTimeModel : TraceSettingsViewModel
    {
        public TraceSettingsDesignTimeModel()
        {
            var provider = new TraceProviderDescriptorViewModel();
            provider.Id = new Guid("9ED16FBE-E642-4D9F-B425-E339FEDC91F8");
            provider.Name = "Design Provider";
            provider.IncludeStackTrace = true;
            provider.Events.Add(new TraceEventDescriptorViewModel {
                Id = 23,
                Version = 0,
                IsEnabled = false,
                Channel = "Debug"
            });
            provider.Events.Add(new TraceEventDescriptorViewModel {
                Id = 42,
                Version = 1,
                IsEnabled = true
            });

            var preset = new TraceSessionSettingsViewModel();
            preset.Name = "Design Preset";
            preset.Id = new Guid("7DB6B9B1-9ACF-42C8-B6B1-CEEB6F783689");
            preset.Providers.Add(provider);
            preset.SelectedProvider = provider;

            SessionPresets.Add(preset);
            SelectedSessionPreset = preset;
        }
    }
}
