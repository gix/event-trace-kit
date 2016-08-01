namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using EventTraceKit.VsExtension.Collections;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.Win32;
    using Task = System.Threading.Tasks.Task;

    public class TraceSessionSettingsViewModel : ViewModel
    {
        private readonly ISolutionFileGatherer gatherer;

        public TraceSessionSettingsViewModel(ISolutionFileGatherer gatherer)
        {
            this.gatherer = gatherer;

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
            var files = gatherer.GetFiles();
            var dlg = new Window();
            var listBox = new ListBox();
            listBox.ItemsSource = files;
            dlg.Content = listBox;
            dlg.Width = 500;
            dlg.Height = 500;
            dlg.ShowDialog();

            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true) {
                ParseManifest(dialog.FileName);
            }
        }

        private static readonly XNamespace EventManifestSchemaNamespace =
            "http://schemas.microsoft.com/win/2004/08/events";

        private static readonly XNamespace EventSchemaNamespace =
            "http://schemas.microsoft.com/win/2004/08/events/event";

        private static readonly XNamespace WinEventSchemaNamespace =
            "http://manifests.microsoft.com/win/2004/08/windows/events";

        private void ParseManifest(string fileName)
        {
            var reader = XmlReader.Create(fileName);
            var doc = XDocument.Load(reader);

            var xnsMgr = new XmlNamespaceManager(reader.NameTable ?? new NameTable());
            xnsMgr.AddNamespace("e", EventManifestSchemaNamespace.NamespaceName);
            xnsMgr.AddNamespace("w", WinEventSchemaNamespace.NamespaceName);

            const string providerXPath = "e:instrumentationManifest/e:instrumentation/e:events/e:provider";
            foreach (var providerElem in doc.XPathSelectElements(providerXPath, xnsMgr)) {
                var provider = ReadProvider(providerElem, xnsMgr);
                if (provider != null)
                    Providers.Add(provider);
            }
        }

        private static TraceProviderDescriptorViewModel ReadProvider(
            XElement providerElem, XmlNamespaceManager xnsMgr)
        {
            string name = providerElem.Attribute("name").AsString();
            string symbol = providerElem.Attribute("symbol").AsString();
            Guid? id = providerElem.Attribute("guid").AsGuid();
            if (id == null)
                return null;

            var provider = new TraceProviderDescriptorViewModel(
                id.Value, name ?? symbol ?? $"<id:{id.Value}>");

            provider.Events.AddRange(ReadEvents(providerElem, xnsMgr));

            return provider;
        }

        private static IEnumerable<TraceEventDescriptorViewModel> ReadEvents(
            XElement providerElem, XmlNamespaceManager xnsMgr)
        {
            foreach (var eventElem in providerElem.XPathSelectElements("e:events/e:event", xnsMgr)) {
                var eventInfo = ReadEvent(eventElem, xnsMgr);
                if (eventInfo != null)
                    yield return eventInfo;
            }
        }

        private static TraceEventDescriptorViewModel ReadEvent(
            XElement eventElem, XmlNamespaceManager xnsMgr)
        {
            string symbol = eventElem.Attribute("symbol").AsString();
            ushort? id = eventElem.Attribute("value").AsUShort();
            byte version = eventElem.Attribute("version").AsByte().GetValueOrDefault(0);
            if (id == null)
                return null;

            return new TraceEventDescriptorViewModel(id.Value, version, symbol ?? $"<id:{id.Value}>");
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

    public static class XElementExtensions
    {
        public static string AsString(this XAttribute attribute)
        {
            return attribute?.Value;
        }

        public static byte? AsByte(this XAttribute attribute)
        {
            byte value;
            if (attribute != null && byte.TryParse(attribute.Value, out value))
                return value;
            return null;
        }

        public static ushort? AsUShort(this XAttribute attribute)
        {
            ushort value;
            if (attribute != null && ushort.TryParse(attribute.Value, out value))
                return value;
            return null;
        }

        public static int? AsInt(this XAttribute attribute)
        {
            int value;
            if (attribute != null && int.TryParse(attribute.Value, out value))
                return value;
            return null;
        }

        public static Guid? AsGuid(this XAttribute attribute)
        {
            Guid value;
            if (attribute != null && Guid.TryParse(attribute.Value, out value))
                return value;
            return null;
        }
    }
}
