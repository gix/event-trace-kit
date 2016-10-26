namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Windows.Input;
    using Collections;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.Win32;
    using Microsoft.Windows.TaskDialogs;
    using Microsoft.Windows.TaskDialogs.Controls;
    using Serialization;
    using Task = System.Threading.Tasks.Task;

    [SerializedShape(typeof(Settings.Persistence.TraceSession))]
    public class TraceSessionSettingsViewModel : ViewModel
    {
        private ICommand newProviderCommand;
        private ICommand removeProviderCommand;
        private ICommand addManifestCommand;
        private ICommand browseLogFileCommand;

        private Guid id = Guid.NewGuid();
        private string name;
        private string logFileName;
        private uint? bufferSize;
        private uint? minimumBuffers;
        private uint? maximumBuffers;
        private TraceProviderDescriptorViewModel selectedProvider;

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

        public ICommand RemoveProviderCommand =>
            removeProviderCommand ??
            (removeProviderCommand = new AsyncDelegateCommand<TraceProviderDescriptorViewModel>(RemoveProvider));

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
            get { return id; }
            set { SetProperty(ref id, value); }
        }

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public string LogFileName
        {
            get { return logFileName; }
            set { SetProperty(ref logFileName, value); }
        }

        public uint? BufferSize
        {
            get { return bufferSize; }
            set { SetProperty(ref bufferSize, value); }
        }

        public uint? MinimumBuffers
        {
            get { return minimumBuffers; }
            set { SetProperty(ref minimumBuffers, value); }
        }

        public uint? MaximumBuffers
        {
            get { return maximumBuffers; }
            set { SetProperty(ref maximumBuffers, value); }
        }

        public TraceProviderDescriptorViewModel SelectedProvider
        {
            get { return selectedProvider; }
            set { SetProperty(ref selectedProvider, value); }
        }

        private Task NewProvider()
        {
            var provider = new TraceProviderDescriptorViewModel(Guid.Empty, null);
            Providers.Add(provider);
            SelectedProvider = provider;
            return Task.CompletedTask;
        }

        private Task RemoveProvider(TraceProviderDescriptorViewModel provider)
        {
            Providers.Remove(provider);
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
            if (dialog.ShowDialog() != true)
                return Task.CompletedTask;

            LogFileName = dialog.FileName;
            return Task.CompletedTask;
        }

        private static string PromptManifest()
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Add Manifest";
            dialog.Filter = "Manifest Files (*.man)|*.man|" +
                            "Binary Providers (*.dll, *.exe)|*.dll, *.exe|" +
                            "All Files (*.*)|*.*";
            dialog.DefaultExt = ".man";
            if (dialog.ShowDialog() != true)
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
            dialog.Show();
        }

        private enum MergeStrategy
        {
            Merge = TaskDialog.MinimumCustomButtonId,
            Keep = TaskDialog.MinimumCustomButtonId + 1,
            Overwrite = TaskDialog.MinimumCustomButtonId + 2
        }

        private static TaskDialogResult PromptConflictResolution(
                TraceProviderDescriptorViewModel duplicateProvider)
        {
            var dialog = new TaskDialog();
            dialog.Caption = "Provider Conflict";
            dialog.Instruction = $"Provider {duplicateProvider.DisplayName} exists already";
            dialog.Controls.Add(new TaskDialogCommandLink((int)MergeStrategy.Merge, "Merge both"));
            dialog.Controls.Add(new TaskDialogCommandLink((int)MergeStrategy.Keep, "Keep existing provider"));
            dialog.Controls.Add(new TaskDialogCommandLink((int)MergeStrategy.Overwrite, "Overwrite existing provider"));
            dialog.VerificationCheckBoxText = "Do this for the next conflicts";
            return dialog.Show();
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

        public TraceSessionDescriptor GetDescriptor()
        {
            var descriptor = new TraceSessionDescriptor();
            descriptor.LogFileName = LogFileName;
            descriptor.BufferSize = BufferSize;
            descriptor.MinimumBuffers = MinimumBuffers;
            descriptor.MaximumBuffers = MaximumBuffers;
            descriptor.Providers.AddRange(
                from x in Providers where x.IsEnabled select x.ToModel());
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
}
