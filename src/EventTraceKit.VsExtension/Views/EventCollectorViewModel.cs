namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Input;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Serialization;
    using Microsoft.Win32;
    using Task = System.Threading.Tasks.Task;

    [SerializedShape(typeof(Settings.Persistence.EventCollector))]
    public class EventCollectorViewModel : CollectorViewModel
    {
        private ICommand newProviderCommand;
        private ICommand importProvidersCommand;
        private ICommand removeProviderCommand;
        private ICommand toggleProvidersCommand;
        private ICommand cutProvidersCommand;
        private ICommand copyProvidersCommand;
        private ICommand pasteProvidersCommand;
        private ICommand addManifestCommand;
        private ICommand browseLogFileCommand;

        private string name;
        private string logFileName;
        private uint? flushPeriod;
        private uint? bufferSize;
        private uint? minimumBuffers;
        private uint? maximumBuffers;
        private EventProviderViewModel selectedProvider;
        private ITraceSettingsContext context;

        public EventCollectorViewModel()
        {
            Providers = new AcqRelObservableCollection<EventProviderViewModel>(
                x => x.Context = null, x => x.Context = Context);
        }

        public override ITraceSettingsContext Context
        {
            get => context;
            set
            {
                context = value;
                foreach (var provider in Providers)
                    provider.Context = value;
            }
        }

        public override CollectorViewModel DeepClone()
        {
            var clone = new EventCollectorViewModel {
                Name = Name,
                LogFileName = LogFileName,
                BufferSize = BufferSize,
                MinimumBuffers = MinimumBuffers,
                MaximumBuffers = MaximumBuffers,
                FlushPeriod = FlushPeriod
            };
            clone.Providers.AddRange(Providers.Select(x => x.DeepClone()));
            return clone;
        }

        public ICommand NewProviderCommand =>
            newProviderCommand ??
            (newProviderCommand = new AsyncDelegateCommand(NewProvider));

        public ICommand ImportProvidersCommand =>
            importProvidersCommand ??
            (importProvidersCommand = new AsyncDelegateCommand(ImportProviders));

        public ICommand RemoveProvidersCommand =>
            removeProviderCommand ??
            (removeProviderCommand = new AsyncDelegateCommand(RemoveProviders));

        public ICommand ToggleProvidersCommand =>
            toggleProvidersCommand ??
            (toggleProvidersCommand = new AsyncDelegateCommand(ToggleProviders));

        public ICommand CutProvidersCommand =>
            cutProvidersCommand ??
            (cutProvidersCommand = new AsyncDelegateCommand(CutProviders));

        public ICommand CopyProvidersCommand =>
            copyProvidersCommand ??
            (copyProvidersCommand = new AsyncDelegateCommand(CopyProviders));

        public ICommand PasteProvidersCommand =>
            pasteProvidersCommand ??
            (pasteProvidersCommand = new AsyncDelegateCommand(PasteProviders));

        public ICommand AddManifestCommand =>
            addManifestCommand ??
            (addManifestCommand = new AsyncDelegateCommand(AddManifest));

        public ICommand BrowseLogFileCommand =>
            browseLogFileCommand ??
            (browseLogFileCommand = new AsyncDelegateCommand(BrowseLogFile));

        public ObservableCollection<EventProviderViewModel> Providers { get; }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public string LogFileName
        {
            get => logFileName;
            set => SetProperty(ref logFileName, value);
        }

        public uint? FlushPeriod
        {
            get => flushPeriod;
            set => SetProperty(ref flushPeriod, value);
        }

        public uint? BufferSize
        {
            get => bufferSize;
            set => SetProperty(ref bufferSize, value);
        }

        public uint? MinimumBuffers
        {
            get => minimumBuffers;
            set => SetProperty(ref minimumBuffers, value);
        }

        public uint? MaximumBuffers
        {
            get => maximumBuffers;
            set => SetProperty(ref maximumBuffers, value);
        }

        public EventProviderViewModel SelectedProvider
        {
            get => selectedProvider;
            set => SetProperty(ref selectedProvider, value);
        }

        public IEnumerable<EventProviderViewModel> SelectedProviders =>
            Providers.Where(x => x.IsSelected);

        private Task NewProvider()
        {
            var provider = new EventProviderViewModel(Guid.Empty, null);
            Providers.Add(provider);
            SelectedProvider = provider;
            return Task.CompletedTask;
        }

        private Task RemoveProviders()
        {
            Providers.RemoveRange(SelectedProviders.ToList());
            return Task.CompletedTask;
        }

        private Task ToggleProviders()
        {
            var providers = SelectedProviders.ToList();
            bool enabled = !providers.All(x => x.IsEnabled);
            foreach (var provider in providers)
                provider.IsEnabled = enabled;

            return Task.CompletedTask;
        }

        private Task CutProviders()
        {
            var providers = SelectedProviders.ToList();
            var serializer = new SettingsSerializer();
            try {
                var text = serializer.SaveToString(providers);
                ClipboardUtils.SetText(text);
            } catch (Exception ex) {
                MessageHelper.ReportException(ex, "Failed to cut providers.");
                return Task.CompletedTask;
            }

            Providers.RemoveRange(providers);
            return Task.CompletedTask;
        }

        private Task CopyProviders()
        {
            var providers = SelectedProviders.ToList();
            var serializer = new SettingsSerializer();
            try {
                var text = serializer.SaveToString(providers);
                ClipboardUtils.SetText(text);
            } catch (Exception ex) {
                MessageHelper.ReportException(ex, "Failed to copy providers.");
            }

            return Task.CompletedTask;
        }

        private Task PasteProviders()
        {
            if (!ClipboardUtils.TryGetText(out var text))
                return Task.CompletedTask;

            IReadOnlyList<EventProviderViewModel> newProviders;
            var serializer = new SettingsSerializer();
            try {
                newProviders = serializer.LoadFromStringMultiple<EventProviderViewModel>(text);
            } catch (Exception ex) {
                MessageHelper.ReportDebugException(ex, "Failed to paste providers.");
                return Task.CompletedTask;
            }

            if (newProviders.Count != 0) {
                var usedNames = Providers.Select(x => x.Name).ToHashSet();
                foreach (var newProvider in newProviders) {
                    var newName = string.IsNullOrEmpty(newProvider.Name) ? newProvider.Id.ToString() : newProvider.Name;
                    newProvider.Name = newName.MakeNumberedCopy(usedNames);
                    usedNames.Add(newProvider.Name);
                    Providers.Add(newProvider);
                }

                SelectedProvider = newProviders[0];
            }

            return Task.CompletedTask;
        }

        private Task ImportProviders()
        {
            string declarations = ImportProvidersDialog.Prompt();
            if (string.IsNullOrWhiteSpace(declarations))
                return Task.CompletedTask;

            var newProviders = ParseProviderDeclarations(declarations);
            var existingIds = Providers.Select(x => x.Id).ToHashSet();

            foreach (var p in newProviders.Where(x => !existingIds.Contains(x.Key)))
                Providers.Add(new EventProviderViewModel(p.Key, p.Value));

            return Task.CompletedTask;
        }

        private static Dictionary<Guid, string> ParseProviderDeclarations(string declarations)
        {
            var newProviders = new Dictionary<Guid, string>();
            foreach (Match match in Regex.Matches(declarations, @"^ *(?<id>\S+):?[ \t]+(?<name>.+?) *$", RegexOptions.Multiline)) {
                if (Guid.TryParse(match.Groups["id"].Value, out var providerId) &&
                    !newProviders.ContainsKey(providerId)) {
                    newProviders.Add(providerId, match.Groups["name"].Value);
                }
            }

            return newProviders;
        }

        private async Task AddManifest()
        {
            string manifestPath = PromptManifest();
            if (manifestPath == null)
                return;

            List<EventProviderViewModel> newProviders;
            try {
                newProviders = await Task.Run(() => {
                    var parser = new SimpleEventManifestParser(manifestPath);
                    return parser.ReadProviders().ToList();
                });
            } catch (Exception ex) {
                MessageHelper.ShowErrorMessage("Failed to add manifest", exception: ex);
                return;
            }

            foreach (var provider in newProviders) {
                var existingProvider = Providers.FirstOrDefault(x => x.Id == provider.Id);
                if (existingProvider == null)
                    Providers.Add(provider);
            }
        }

        private Task BrowseLogFile()
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "Log File";
            dialog.Filter = "Event Trace Logs (*.etl)|*.etl|" +
                            "All Files (*.*)|*.*";
            dialog.DefaultExt = ".etl";
            dialog.OverwritePrompt = true;
            dialog.AddExtension = true;
            if (dialog.ShowModal() != true)
                return Task.CompletedTask;

            LogFileName = dialog.FileName;
            return Task.CompletedTask;
        }

        private string PromptManifest()
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Add Manifest";
            dialog.Filter = "Manifest Files (*.man)|*.man|" +
                            "Binary Providers (*.dll, *.exe)|*.dll, *.exe|" +
                            "All Files (*.*)|*.*";
            dialog.DefaultExt = ".man";
            if (dialog.ShowModal() != true)
                return null;

            return dialog.FileName;
        }

        public override CollectorDescriptor CreateDescriptor()
        {
            var descriptor = new EventCollectorDescriptor();
            descriptor.LogFileName = LogFileName;
            descriptor.BufferSize = BufferSize;
            descriptor.MinimumBuffers = MinimumBuffers;
            descriptor.MaximumBuffers = MaximumBuffers;
            descriptor.Providers.AddRange(
                from x in Providers where x.IsEnabled select x.CreateDescriptor());
            if (FlushPeriod != null)
                descriptor.FlushPeriod = TimeSpan.FromMilliseconds(FlushPeriod.Value);
            return descriptor;
        }
    }
}
