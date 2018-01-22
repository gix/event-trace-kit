namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;
    using Collections;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.Win32;
    using Microsoft.Windows.TaskDialogs;
    using Microsoft.Windows.TaskDialogs.Controls;
    using Serialization;
    using UI;
    using Task = System.Threading.Tasks.Task;

    [SerializedShape(typeof(Settings.Persistence.TraceSession))]
    public class TraceSessionSettingsViewModel : ViewModel
    {
        private ICommand newProviderCommand;
        private ICommand importProvidersCommand;
        private ICommand removeProviderCommand;
        private ICommand toggleSelectedProvidersCommand;
        private ICommand addManifestCommand;
        private ICommand browseLogFileCommand;

        private Guid id = Guid.NewGuid();
        private string name;
        private string logFileName;
        private uint? bufferSize;
        private uint? minimumBuffers;
        private uint? maximumBuffers;
        private TraceProviderDescriptorViewModel selectedProvider;

        public IDialogService DialogService { get; set; }

        public TraceSessionSettingsViewModel DeepClone()
        {
            var clone = new TraceSessionSettingsViewModel();
            clone.Name = Name;
            clone.LogFileName = LogFileName;
            clone.BufferSize = BufferSize;
            clone.MinimumBuffers = MinimumBuffers;
            clone.MaximumBuffers = MaximumBuffers;
            clone.Providers.AddRange(Providers.Select(x => x.DeepClone()));
            return clone;
        }

        public ICommand NewProviderCommand =>
            newProviderCommand ??
            (newProviderCommand = new AsyncDelegateCommand(NewProvider));

        public ICommand ImportProvidersCommand =>
            importProvidersCommand ??
            (importProvidersCommand = new AsyncDelegateCommand(ImportProviders));

        public ICommand RemoveProviderCommand =>
            removeProviderCommand ??
            (removeProviderCommand = new AsyncDelegateCommand<IList>(RemoveProviders));

        public ICommand ToggleSelectedProvidersCommand =>
            toggleSelectedProvidersCommand ??
            (toggleSelectedProvidersCommand = new AsyncDelegateCommand<IList>(ToggleSelectedProviders));

        public ICommand AddManifestCommand =>
            addManifestCommand ??
            (addManifestCommand = new AsyncDelegateCommand(AddManifest));

        public ICommand BrowseLogFileCommand =>
            browseLogFileCommand ??
            (browseLogFileCommand = new AsyncDelegateCommand(BrowseLogFile));

        public ObservableCollection<TraceProviderDescriptorViewModel> Providers { get; }
            = new ObservableCollection<TraceProviderDescriptorViewModel>();

        public Guid Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

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

        public TraceProviderDescriptorViewModel SelectedProvider
        {
            get => selectedProvider;
            set => SetProperty(ref selectedProvider, value);
        }

        public ObservableCollection<TraceProviderDescriptorViewModel> SelectedProviders { get; } =
            new ObservableCollection<TraceProviderDescriptorViewModel>();

        private Task NewProvider()
        {
            var provider = new TraceProviderDescriptorViewModel(Guid.Empty, null);
            Providers.Add(provider);
            SelectedProvider = provider;
            return Task.CompletedTask;
        }

        private Task RemoveProviders(IList providers)
        {
            Providers.RemoveRange(providers.OfType<TraceProviderDescriptorViewModel>().ToList());
            return Task.CompletedTask;
        }

        private static Task ToggleSelectedProviders(IList selectedObjects)
        {
            var selectedEvents = selectedObjects.Cast<TraceProviderDescriptorViewModel>().ToList();
            bool enabled = !selectedEvents.All(x => x.IsEnabled);
            foreach (var evt in selectedEvents)
                evt.IsEnabled = enabled;

            return Task.CompletedTask;
        }

        private async Task Foo()
        {
            var vstwdf = (IVsThreadedWaitDialogFactory)ServiceProvider.GlobalProvider.GetService(
                typeof(SVsThreadedWaitDialogFactory));

            ManifestInfo manifestInfo;
            //using (var wds = vstwdf.StartWaitDialog("Enumerating Providers")) {
            //    manifestInfo = await Task.Run(
            //        () => ManifestInfo.EnumerateAsync(
            //            wds.UserCancellationToken,
            //            new Progress<ManifestInfoProcess>(
            //                mip => wds.Progress.Report(new ThreadedWaitDialogProgressData(
            //                    "Enumerating", "X", "Y", true, mip.Processed, mip.TotalProviders)))),
            //        wds.UserCancellationToken);
            //}
            manifestInfo = await Task.Run(
                () => ManifestInfo.Enumerate(
                    new CancellationToken(),
                    new Progress<ManifestInfoProcess>()));

            //foreach (var providerInfo in manifestInfo.Providers ?? Enumerable.Empty<ProviderInfo>()) {
            //    var p = new TraceProviderDescriptorViewModel(providerInfo.Id, providerInfo.Name);
            //    p.IsMOF = providerInfo.IsMOF;
            //    foreach (var evtDesc in providerInfo.Events ?? Enumerable.Empty<ProviderEventInfo>())
            //        p.Events.Add(new TraceEventDescriptorViewModel(evtDesc));

            //    Providers.Add(p);
            //}
        }

        private Task ImportProviders()
        {
            string declarations = ImportProvidersDialog.Prompt(DialogService.Owner);
            if (string.IsNullOrWhiteSpace(declarations))
                return Task.CompletedTask;

            var newProviders = ParseProviderDeclarations(declarations);
            var existingIds = Providers.Select(x => x.Id).ToHashSet();

            foreach (var p in newProviders.Where(x => !existingIds.Contains(x.Key)))
                Providers.Add(new TraceProviderDescriptorViewModel(p.Key, p.Value));

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

            List<TraceProviderDescriptorViewModel> newProviders;
            try {
                newProviders = await Task.Run(() => {
                    var parser = new SimpleInstrumentationManifestParser(manifestPath);
                    return parser.ReadProviders().ToList();
                });
            } catch (Exception ex) {
                ShowErrorDialog("Failed to add manifest", ex);
                return;
            }

            MergeStrategy? pinnedMergeStrategy = null;

            foreach (var provider in newProviders) {
                var existingProvider = Providers.FirstOrDefault(x => x.Id == provider.Id);
                if (existingProvider == null) {
                    Providers.Add(provider);
                    continue;
                }

                MergeStrategy? mergeStrategy = pinnedMergeStrategy;
                if (mergeStrategy == null) {
                    var result = PromptConflictResolution(provider);
                    mergeStrategy = (MergeStrategy)(int)result.Button;
                    if (result.VerificationFlagChecked)
                        pinnedMergeStrategy = mergeStrategy;
                }

                switch (mergeStrategy) {
                    case MergeStrategy.Merge:
                        MergeProviders(existingProvider, provider);
                        break;
                    case MergeStrategy.Overwrite:
                        Providers.Remove(existingProvider);
                        Providers.Add(provider);
                        break;
                }
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
            if (dialog.ShowDialog(DialogService.Owner) != true)
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
            if (dialog.ShowDialog(DialogService.Owner) != true)
                return null;

            return dialog.FileName;
        }

        private void ShowErrorDialog(string message, Exception exception)
        {
            var dialog = new TaskDialog();
            dialog.Caption = "Error";
            dialog.Content = message;
            dialog.ExpandedInfo = exception.Message;
            dialog.ExpandedInfoLocation = TaskDialogExpandedInfoLocation.Footer;
            dialog.ExpandedButtonLabel = "Details";
            dialog.Show(DialogService.Owner);
        }

        private enum MergeStrategy
        {
            Merge = TaskDialog.MinimumCustomButtonId,
            Keep = TaskDialog.MinimumCustomButtonId + 1,
            Overwrite = TaskDialog.MinimumCustomButtonId + 2
        }

        private TaskDialogResult PromptConflictResolution(
                TraceProviderDescriptorViewModel duplicateProvider)
        {
            var dialog = new TaskDialog();
            dialog.Caption = "Provider Conflict";
            dialog.Instruction = $"Provider {duplicateProvider.DisplayName} exists already";
            dialog.Controls.Add(new TaskDialogCommandLink((int)MergeStrategy.Merge, "Merge both"));
            dialog.Controls.Add(new TaskDialogCommandLink((int)MergeStrategy.Keep, "Keep existing provider"));
            dialog.Controls.Add(new TaskDialogCommandLink((int)MergeStrategy.Overwrite, "Overwrite existing provider"));
            dialog.VerificationText = "Do this for the next conflicts";
            return dialog.Show(DialogService.Owner);
        }

        private void MergeProviders(
            TraceProviderDescriptorViewModel target,
            TraceProviderDescriptorViewModel source)
        {
            if (target.Name == null)
                target.Name = source.Name;

            var targetEvents = target.Events.ToDictionary(x => x.CreateKey());
            var sourceEvents = source.Events.ToDictionary(x => x.CreateKey());

            var newKeys = sourceEvents.Keys.Except(targetEvents.Keys);
            var mergeKeys = sourceEvents.Keys.Intersect(targetEvents.Keys);

            foreach (var key in mergeKeys) {
                var targetEvent = targetEvents[key];
                var sourceEvent = sourceEvents[key];
                MergeEvent(targetEvent, sourceEvent);
            }

            foreach (var key in newKeys)
                targetEvents.Add(key, sourceEvents[key]);

            target.Events.Clear();
            target.Events.AddRange(targetEvents.OrderBy(x => x.Key).Select(x => x.Value));
        }

        private void MergeEvent(
            TraceEventDescriptorViewModel target,
            TraceEventDescriptorViewModel source)
        {
            target.Symbol = target.Symbol ?? source.Symbol;
            target.Level = target.Level ?? source.Level;
            target.Channel = target.Channel ?? source.Channel;
            target.Task = target.Task ?? source.Task;
            target.Opcode = target.Opcode ?? source.Opcode;
            target.Keywords = target.Keywords ?? source.Keywords;
        }

        public TraceSessionDescriptor CreateDescriptor()
        {
            var descriptor = new TraceSessionDescriptor();
            descriptor.LogFileName = LogFileName;
            descriptor.BufferSize = BufferSize;
            descriptor.MinimumBuffers = MinimumBuffers;
            descriptor.MaximumBuffers = MaximumBuffers;
            descriptor.Providers.AddRange(
                from x in Providers where x.IsEnabled select x.CreateDescriptor());
            return descriptor;
        }

        public Dictionary<EventKey, string> GetEventSymbols()
        {
            var symbols = new Dictionary<EventKey, string>();
            foreach (var provider in Providers.Where(x => x.IsEnabled)) {
                foreach (var evt in provider.Events) {
                    if (evt.Symbol != null)
                        symbols.Add(new EventKey(provider.Id, evt.Id, evt.Version), evt.Symbol);
                }
            }

            return symbols;
        }
    }

    public static class TaskDialogExtensions
    {
        public static TaskDialogResult Show(this TaskDialog dialog, Window owner)
        {
            var wih = new WindowInteropHelper(owner);
            dialog.OwnerWindow = wih.Handle;
            return dialog.Show();
        }
    }
}
