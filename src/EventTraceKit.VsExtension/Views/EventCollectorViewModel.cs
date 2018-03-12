namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Serialization;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.Win32;
    using Microsoft.Windows.TaskDialogs;
    using Microsoft.Windows.TaskDialogs.Controls;
    using Task = System.Threading.Tasks.Task;

    [SerializedShape(typeof(Settings.Persistence.EventCollector))]
    public class EventCollectorViewModel : CollectorViewModel
    {
        private ICommand newProviderCommand;
        private ICommand importProvidersCommand;
        private ICommand removeProviderCommand;
        private ICommand toggleProvidersCommand;
        private ICommand copyProvidersCommand;
        private ICommand pasteProvidersCommand;
        private ICommand addManifestCommand;
        private ICommand browseLogFileCommand;

        private Guid id = Guid.NewGuid();
        private string name;
        private string logFileName;
        private uint? bufferSize;
        private uint? minimumBuffers;
        private uint? maximumBuffers;
        private EventProviderViewModel selectedProvider;

        public EventCollectorViewModel()
        {
            Providers = new AcqRelObservableCollection<EventProviderViewModel>(
                x => x.Context = null, x => x.Context = Context);
        }

        public override CollectorViewModel DeepClone()
        {
            var clone = new EventCollectorViewModel();
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

        public ICommand RemoveProvidersCommand =>
            removeProviderCommand ??
            (removeProviderCommand = new AsyncDelegateCommand(RemoveProviders));

        public ICommand ToggleProvidersCommand =>
            toggleProvidersCommand ??
            (toggleProvidersCommand = new AsyncDelegateCommand(ToggleProviders));

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

        private Task CopyProviders()
        {
            var providers = SelectedProviders.ToList();
            var serializer = new SettingsSerializer();
            try {
                var text = serializer.SaveToString(providers);
                ClipboardUtils.SetText(text);
            } catch (Exception ex) {
                ErrorUtils.ReportException(Context.DialogOwner, ex, "Failed to copy provider.");
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
                ErrorUtils.ReportDebugException(Context.DialogOwner, ex, "Failed to paste provider.");
                return Task.CompletedTask;
            }

            if (newProviders.Count != 0) {
                foreach (var newProvider in newProviders) {
                    newProvider.Name = MakeNumberedCopy(
                        string.IsNullOrEmpty(newProvider.Name) ? newProvider.Id.ToString() : newProvider.Name);
                    Providers.Add(newProvider);
                }

                SelectedProvider = newProviders[0];
            }

            return Task.CompletedTask;
        }

        private static string MakeNumberedCopy(string fullString)
        {
            var match = Regex.Match(fullString, @"\A(?<str>.*) \(Copy(?: (?<num>\d+))?\)\z");
            if (!match.Success)
                return fullString + " (Copy)";

            var str = match.Groups["str"].Value;
            int num = match.Groups["num"].Success ? int.Parse(match.Groups["num"].Value) : 1;
            return str + $" (Copy {num + 1})";
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
            string declarations = ImportProvidersDialog.Prompt(Context.DialogOwner);
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
            if (dialog.ShowDialog(Context.DialogOwner) != true)
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
            if (dialog.ShowDialog(Context.DialogOwner) != true)
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
            dialog.Show(Context.DialogOwner);
        }

        private enum MergeStrategy
        {
            Merge = TaskDialog.MinimumCustomButtonId,
            Keep = TaskDialog.MinimumCustomButtonId + 1,
            Overwrite = TaskDialog.MinimumCustomButtonId + 2
        }

        private TaskDialogResult PromptConflictResolution(
            EventProviderViewModel duplicateProvider)
        {
            var dialog = new TaskDialog();
            dialog.Caption = "Provider Conflict";
            dialog.Instruction = $"Provider {duplicateProvider.DisplayName} exists already";
            dialog.Controls.Add(new TaskDialogCommandLink((int)MergeStrategy.Merge, "Merge both"));
            dialog.Controls.Add(new TaskDialogCommandLink((int)MergeStrategy.Keep, "Keep existing provider"));
            dialog.Controls.Add(new TaskDialogCommandLink((int)MergeStrategy.Overwrite, "Overwrite existing provider"));
            dialog.VerificationText = "Do this for the next conflicts";
            return dialog.Show(Context.DialogOwner);
        }

        private void MergeProviders(
            EventProviderViewModel target,
            EventProviderViewModel source)
        {
            if (target.Name == null)
                target.Name = source.Name;
        }

        private void MergeEvent(
            EventViewModel target,
            EventViewModel source)
        {
            target.Symbol = target.Symbol ?? source.Symbol;
            target.Level = target.Level ?? source.Level;
            target.Channel = target.Channel ?? source.Channel;
            target.Task = target.Task ?? source.Task;
            target.Opcode = target.Opcode ?? source.Opcode;
            target.Keywords = target.Keywords ?? source.Keywords;
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
            //descriptor.CustomFlushPeriod = 100;
            return descriptor;
        }

        public async Task<Dictionary<EventKey, string>> GetEventSymbols()
        {
            var symbols = new Dictionary<EventKey, string>();
            foreach (var provider in Providers.Where(x => x.IsEnabled)) {
                var p = await provider.GetSchemaProviderAsync();
                foreach (var evt in p.Events) {
                    if (evt.Symbol != null)
                        symbols.Add(new EventKey(provider.Id, (ushort)evt.Value, evt.Version), evt.Symbol);
                }
            }

            return symbols;
        }
    }
}
