namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Input;
    using AutoMapper;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Serialization;
    using EventTraceKit.VsExtension.Settings;
    using EventTraceKit.VsExtension.Settings.Persistence;
    using EventTraceKit.VsExtension.Threading;
    using Microsoft.VisualStudio.PlatformUI;
    using Task = System.Threading.Tasks.Task;

    [SerializedShape(typeof(TraceSettings))]
    public class TraceSettingsViewModel : ObservableModel, IDataErrorInfo
    {
        private readonly ISettingsStore settingsStore;
        private readonly IMapper mapper = SettingsSerializer.Mapper;
        private readonly ITraceSettingsContext context;

        private bool? dialogResult;
        private ICommand saveCommand;
        private ICommand restoreCommand;
        private ICommand newProfileCommand;
        private ICommand copyProfileCommand;
        private ICommand deleteProfileCommand;
        private TraceProfileViewModel activeProfile;

        private bool isInRenamingMode;
        private ICommand switchToRenamingModeCommand;
        private ICommand saveAndSwitchFromRenamingModeCommand;
        private ICommand discardAndSwitchFromRenamingModeCommand;

        public TraceSettingsViewModel(
            ITraceSettingsContext context, ISettingsStore settingsStore = null)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.settingsStore = settingsStore;

            Profiles = new AcqRelObservableCollection<TraceProfileViewModel>(
                x => x.Context = null, x => x.Context = context);

            if (settingsStore != null) {
                LoadFromSettings();
                Title = $"Trace Settings ({settingsStore.Name})";
            } else {
                Title = "Trace Settings";
            }
        }

        public string Title { get; }

        public bool? DialogResult
        {
            get => dialogResult;
            set => SetProperty(ref dialogResult, value);
        }

        public ICommand SaveCommand =>
            saveCommand ?? (saveCommand = new AsyncDelegateCommand(Save));

        public ICommand RestoreCommand =>
            restoreCommand ?? (restoreCommand = new AsyncDelegateCommand(Restore, () => settingsStore != null));

        public ICommand NewProfileCommand =>
            newProfileCommand ?? (newProfileCommand = new AsyncDelegateCommand(NewProfile));

        public ICommand CopyProfileCommand =>
            copyProfileCommand ?? (copyProfileCommand = new AsyncDelegateCommand(CopyProfile, CanCopyProfile));

        public ICommand DeleteProfileCommand =>
            deleteProfileCommand ?? (deleteProfileCommand = new AsyncDelegateCommand(DeleteProfile, CanDeleteProfile));

        public string NewName { get; set; }

        public bool IsInRenamingMode
        {
            get => isInRenamingMode;
            set => SetProperty(ref isInRenamingMode, value);
        }

        public ICommand SwitchToRenamingModeCommand =>
            switchToRenamingModeCommand ??
            (switchToRenamingModeCommand = new DelegateCommand(o => {
                if (ActiveProfile != null) {
                    NewName = ActiveProfile.Name;
                    IsInRenamingMode = true;
                }
            }));

        public ICommand SaveAndSwitchFromRenamingModeCommand =>
            saveAndSwitchFromRenamingModeCommand ??
            (saveAndSwitchFromRenamingModeCommand = new DelegateCommand(o => {
                if (!IsInRenamingMode)
                    return;

                if (!string.IsNullOrWhiteSpace(NewName)
                    && ActiveProfile != null
                    && NewName != ActiveProfile.Name) {
                    if (!IsUniqueProfileName(NewName))
                        return;
                    ActiveProfile.Name = NewName;
                }

                IsInRenamingMode = false;
            }));

        public ICommand DiscardAndSwitchFromRenamingModeCommand =>
            discardAndSwitchFromRenamingModeCommand ??
            (discardAndSwitchFromRenamingModeCommand = new DelegateCommand(o => {
                if (IsInRenamingMode) {
                    NewName = null;
                    IsInRenamingMode = false;
                }
            }));

        public TraceProfileViewModel ActiveProfile
        {
            get => activeProfile;
            set
            {
                if (!isInRenamingMode)
                    SetProperty(ref activeProfile, value);
            }
        }

        public ObservableCollection<TraceProfileViewModel> Profiles { get; }

        private Task NewProfile()
        {
            var newProfile = new TraceProfileViewModel {
                Name = EnsureUniqueProfileName("Unnamed Profile"),
                Collectors = {
                    new EventCollectorViewModel {
                        Name = "Default Collector"
                    }
                }
            };

            IsInRenamingMode = false;
            Profiles.Add(newProfile);
            ActiveProfile = newProfile;
            return Task.CompletedTask;
        }

        private bool CanCopyProfile()
        {
            return ActiveProfile != null;
        }

        private Task CopyProfile()
        {
            if (ActiveProfile != null) {
                IsInRenamingMode = false;
                var newProfile = ActiveProfile.DeepClone();
                newProfile.Name = ActiveProfile.Name.MakeNumberedCopy(
                    Profiles.Select(x => x.Name).ToHashSet());
                Profiles.Add(newProfile);
                ActiveProfile = newProfile;
            }

            return Task.CompletedTask;
        }

        private bool CanDeleteProfile()
        {
            return ActiveProfile != null;
        }

        private Task DeleteProfile()
        {
            if (ActiveProfile != null) {
                IsInRenamingMode = false;
                Profiles.Remove(ActiveProfile);
                ActiveProfile = null;
            }

            return Task.CompletedTask;
        }

        private async Task Save()
        {
            IsInRenamingMode = false;
            if (settingsStore != null) {
                var settings = settingsStore.GetValue(SettingsKeys.Tracing) ?? new TraceSettings();
                settings.Profiles.SetRange(GetProfiles());
                settings.ActiveProfile = ActiveProfile?.Id;
                settingsStore.SetValue(SettingsKeys.Tracing, settings);

                await ThreadingExtensions.RunWithProgress(() => settingsStore.Save(), "Saving");
            }

            DialogResult = true;
        }

        private Task Restore()
        {
            IsInRenamingMode = false;
            LoadFromSettings();
            return Task.CompletedTask;
        }

        private void LoadFromSettings()
        {
            var traceSettings = settingsStore.GetValue(SettingsKeys.Tracing);
            if (traceSettings != null) {
                Profiles.SetRange(traceSettings.Profiles.Select(
                    x => mapper.Map(x, new TraceProfileViewModel(x.Id))));
                ActiveProfile = Profiles.FirstOrDefault(x => x.Id == traceSettings.ActiveProfile);
            }
        }

        public TraceProfileDescriptor GetDescriptor()
        {
            return ActiveProfile?.CreateDescriptor();
        }

        public IEnumerable<TraceProfile> GetProfiles()
        {
            return Profiles.Select(x => mapper.Map<TraceProfile>(x));
        }

        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(NewName)) {
                    if (ActiveProfile?.Name != NewName && !IsUniqueProfileName(NewName))
                        return "Name is already used";
                }

                return "";
            }
        }

        string IDataErrorInfo.Error { get; }

        private bool IsUniqueProfileName(string name)
        {
            return !Profiles.Select(x => x.Name).Contains(name);
        }

        private string EnsureUniqueProfileName(string name)
        {
            var names = Profiles.Select(x => x.Name).ToHashSet();
            if (!names.Contains(name))
                return name;

            for (int i = 2; ; ++i) {
                var numberedName = $"{name} {i}";
                if (!names.Contains(numberedName))
                    return numberedName;
            }
        }
    }

    public class TraceSettingsDesignTimeModel : TraceSettingsViewModel
    {
        public TraceSettingsDesignTimeModel() : base(null)
        {
            var provider = new EventProviderViewModel();
            provider.Id = new Guid("9ED16FBE-E642-4D9F-B425-E339FEDC91F8");
            provider.Name = "Design Provider";
            provider.IncludeStackTrace = true;

            var collector = new EventCollectorViewModel();
            collector.Name = "Design Collector";
            collector.Providers.Add(provider);

            var preset = new TraceProfileViewModel(
                new Guid("7DB6B9B1-9ACF-42C8-B6B1-CEEB6F783689"));
            preset.Name = "Design Profile";
            preset.Collectors.Add(collector);
            preset.SelectedCollector = collector;

            Profiles.Add(preset);
            ActiveProfile = preset;
        }
    }
}
