namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Extensions;
    using Serialization;

    [SerializedShape(typeof(Settings.Persistence.TraceSettings))]
    public class TraceSettingsViewModel : ViewModel, IDialogService
    {
        private bool? dialogResult;
        private TraceSessionSettingsViewModel activeSession;
        private ICommand newPresetCommand;
        private ICommand copyPresetCommand;
        private ICommand deletePresetCommand;

        public TraceSettingsViewModel()
        {
            AcceptCommand = new AsyncDelegateCommand(Accept);
            Sessions.CollectionChanged += OnSessionsChanged;
        }

        private void OnSessionsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            Sessions.HandleChanges(args, x => x.DialogService = null, x => x.DialogService = this);
        }

        public ICommand AcceptCommand { get; }

        public bool? DialogResult
        {
            get => dialogResult;
            set => SetProperty(ref dialogResult, value);
        }

        public ICommand NewPresetCommand =>
            newPresetCommand ?? (newPresetCommand = new AsyncDelegateCommand(NewPreset));

        public ICommand CopyPresetCommand =>
            copyPresetCommand ?? (copyPresetCommand = new AsyncDelegateCommand(CopyPreset, CanCopyPreset));

        public ICommand DeletePresetCommand =>
            deletePresetCommand ?? (deletePresetCommand = new AsyncDelegateCommand(DeletePreset, CanDeletePreset));

        public TraceSessionSettingsViewModel ActiveSession
        {
            get => activeSession;
            set => SetProperty(ref activeSession, value);
        }

        public ObservableCollection<TraceSessionSettingsViewModel> Sessions { get; }
            = new ObservableCollection<TraceSessionSettingsViewModel>();

        Window IDialogService.Owner => Window;
        public Window Window { get; set; }

        private Task NewPreset()
        {
            var newPreset = new TraceSessionSettingsViewModel();
            newPreset.Name = "Unnamed";
            Sessions.Add(newPreset);
            ActiveSession = newPreset;
            return Task.CompletedTask;
        }

        private bool CanCopyPreset()
        {
            return ActiveSession != null;
        }

        private Task CopyPreset()
        {
            if (ActiveSession != null) {
                var newPreset = ActiveSession.DeepClone();
                newPreset.Name = "Copy of " + ActiveSession.Name;
                Sessions.Add(newPreset);
                ActiveSession = newPreset;
            }

            return Task.CompletedTask;
        }

        private bool CanDeletePreset()
        {
            return ActiveSession != null;
        }

        private Task DeletePreset()
        {
            if (ActiveSession != null) {
                Sessions.Remove(ActiveSession);
                ActiveSession = null;
            }

            return Task.CompletedTask;
        }

        private Task Accept()
        {
            DialogResult = true;
            return Task.CompletedTask;
        }

        public EventSessionDescriptor GetDescriptor()
        {
            return ActiveSession?.CreateDescriptor();
        }

        public Dictionary<EventKey, string> GetEventSymbols()
        {
            return ActiveSession?.GetEventSymbols() ?? new Dictionary<EventKey, string>();
        }
    }

    public class TraceSettingsDesignTimeModel : TraceSettingsViewModel
    {
        public TraceSettingsDesignTimeModel()
        {
            var provider = new EventProviderViewModel();
            provider.Id = new Guid("9ED16FBE-E642-4D9F-B425-E339FEDC91F8");
            provider.Name = "Design Provider";
            provider.IncludeStackTrace = true;
            provider.Events.Add(new EventViewModel {
                Id = 23,
                Version = 0,
                IsEnabled = false,
                Channel = "Debug"
            });
            provider.Events.Add(new EventViewModel {
                Id = 42,
                Version = 1,
                IsEnabled = true
            });

            var preset = new TraceSessionSettingsViewModel();
            preset.Name = "Design Preset";
            preset.Id = new Guid("7DB6B9B1-9ACF-42C8-B6B1-CEEB6F783689");
            preset.Providers.Add(provider);
            preset.SelectedProvider = provider;

            Sessions.Add(preset);
            ActiveSession = preset;
        }
    }
}
