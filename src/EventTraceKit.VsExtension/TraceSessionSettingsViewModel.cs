namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Windows.Input;
    using EventTraceKit.VsExtension.Collections;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.Win32;
    using Task = System.Threading.Tasks.Task;

    public class TraceSessionSettingsViewModel : ViewModel
    {
        public TraceSessionSettingsViewModel()
        {
            AddProviderCommand = new AsyncDelegateCommand(AddProvider);
            AddManifestCommand = new DelegateCommand(AddManifest);
            AddBinaryCommand = new DelegateCommand(AddBinary);
            Providers = new ObservableCollection<TraceProviderDescriptorViewModel>();
        }

        public ICommand AddProviderCommand { get; }
        public ICommand AddManifestCommand { get; }
        public ICommand AddBinaryCommand { get; }
        public ObservableCollection<TraceProviderDescriptorViewModel> Providers { get; }

        private async Task AddProvider(object obj)
        {
            Providers.Clear();

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

            foreach (var providerInfo in manifestInfo.Providers ?? Enumerable.Empty<ProviderInfo>()) {
                var p = new TraceProviderDescriptorViewModel(providerInfo.Id, providerInfo.Name);
                p.IsMOF = providerInfo.IsMOF;
                foreach (var evtDesc in providerInfo.Events ?? Enumerable.Empty<ProviderEventInfo>())
                    p.Events.Add(new TraceEventDescriptorViewModel(evtDesc));

                Providers.Add(p);
            }
        }

        private void AddManifest(object obj)
        {
            var dialog = new OpenFileDialog();
            dialog.ShowDialog();
        }

        private void AddBinary(object obj)
        {
        }

        public TraceSessionDescriptor GetDescriptor()
        {
            var descriptor = new TraceSessionDescriptor();
            descriptor.Providers.AddRange(Providers.Select(x => x.ToModel()));
            return descriptor;
        }
    }
}
