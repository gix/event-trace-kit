namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Runtime.InteropServices;
    using EnvDTE;
    using Extensions;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Shell.Settings;
    using Serialization;
    using Settings;

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", productId: "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(TraceLogPane))]
    [ProvideProfile(typeof(EventTraceKitProfileManager), "EventTraceKit", "General", 1001, 1002, false, DescriptionResourceID = 1003)]
    [Guid(PackageGuidString)]
    public class EventTraceKitPackage : Package, IEventTraceKitSettingsService
    {
        public const string PackageGuidString = "7867DA46-69A8-40D7-8B8F-92B0DE8084D8";

        private IMenuCommandService menuService;

        private Lazy<TraceLogPane> traceLogPane = new Lazy<TraceLogPane>(() => null);
        private GlobalSettings globalSettings;
        private IVsUIShell vsUiShell;

        public EventTraceKitPackage()
        {
            AddOptionKey(EventTraceKitOptionKey);
        }

        protected override void Initialize()
        {
            base.Initialize();

            LoadSettings();

            var vsShell = this.GetService<SVsShell, IVsShell>();
            vsUiShell = this.GetService<SVsUIShell, IVsUIShell>();
            var dte = this.GetService<SDTE, DTE>();

            dte.Events.DTEEvents.OnBeginShutdown += OnShutdown;

            AddMenuCommandHandlers();

            var vpc = new ViewPresetsService(GetAppDataDirectory());
            var viewPresetsService = new PresetCollectionManagerView(vpc);
            viewPresetsService.ExceptionFilter += OnExceptionFilter;

            Func<IServiceProvider, TraceLogWindow> traceLogFactory = sp => {
                var operationalModeProvider = new DteOperationalModeProvider(dte, this);

                var traceLog = new TraceLogWindowViewModel(
                    this, operationalModeProvider, viewPresetsService.Presets, vsUiShell);

                var mcs = sp.GetService<IMenuCommandService>();
                if (mcs != null)
                    traceLog.Attach(mcs);

                return new TraceLogWindow { DataContext = traceLog };
            };

            Action onClose = () => {
                viewPresetsService.SaveRepo();
            };

            traceLogPane = new Lazy<TraceLogPane>(() => new TraceLogPane(traceLogFactory, onClose));
        }

        private string GetAppDataDirectory()
        {
            var shell = this.GetService<SVsShell, IVsShell>();
            object appDataDir;
            ErrorHandler.ThrowOnFailure(shell.GetProperty((int)__VSSPROPID.VSSPROPID_AppDataDir, out appDataDir));
            return Path.Combine((string)appDataDir, "EventTraceKit");
        }

        private void OnExceptionFilter(
            object sender, ExceptionFilterEventArgs args)
        {
            int result;
            vsUiShell?.ShowMessageBox(
                0, Guid.Empty, "Error", args.Message, null, 0, OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_CRITICAL, 0, out result);
        }

        private void OnShutdown()
        {
            SaveSettings();
        }

        private WritableSettingsStore CreateSettingsStore()
        {
            var mgr = new ShellSettingsManager(this);
            return mgr.GetWritableSettingsStore(SettingsScope.UserSettings);
        }

        private void LoadSettings()
        {
            var store = CreateSettingsStore();
            if (!store.PropertyExists("EventTraceKit", "GlobalSettings")) {
                GlobalSettings = new GlobalSettings();
                return;
            }

            var serializer = new SettingsSerializer();
            using (var stream = store.GetMemoryStream("EventTraceKit", "GlobalSettings"))
                GlobalSettings = serializer.Load<GlobalSettings>(stream);
        }

        private void SaveSettings()
        {
            if (GlobalSettings == null)
                return;

            var settingsStore = CreateSettingsStore();
            if (!settingsStore.CollectionExists("EventTraceKit"))
                settingsStore.CreateCollection("EventTraceKit");

            var serializer = new SettingsSerializer();
            using (var stream = new MemoryStream()) {
                serializer.Save(GlobalSettings, stream);
                settingsStore.SetMemoryStream("EventTraceKit", "GlobalSettings", stream);
            }
        }

        private void AddMenuCommandHandlers()
        {
            var id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidTraceLog);
            DefineCommandHandler(id, ShowTraceLogWindow);
        }

        internal void OutputString(Guid paneId, string text)
        {
            const int DO_NOT_CLEAR_WITH_SOLUTION = 0;
            const int VISIBLE = 1;

            var outputWindow = this.GetService<SVsOutputWindow, IVsOutputWindow>();
            if (outputWindow == null)
                return;

            // The General pane is not created by default. We must force its creation
            if (paneId == VSConstants.OutputWindowPaneGuid.GeneralPane_guid) {
                ErrorHandler.ThrowOnFailure(
                    outputWindow.CreatePane(paneId, "General", VISIBLE, DO_NOT_CLEAR_WITH_SOLUTION));
            }

            IVsOutputWindowPane outputWindowPane;
            ErrorHandler.ThrowOnFailure(
                outputWindow.GetPane(paneId, out outputWindowPane));

            outputWindowPane?.OutputString(text);
        }

        /// <summary>
        ///   Defines a command handler. When the user press the button corresponding
        ///   to the CommandID the EventHandler will be called.
        /// </summary>
        /// <param name="id">
        ///   The CommandID (Guid/ID pair) as defined in the. vsct file
        /// </param>
        /// <param name="handler">
        ///   Method that should be called to implement the command
        /// </param>
        /// <returns>
        ///   The menu command. This can be used to set parameter such as the
        ///   default visibility once the package is loaded
        /// </returns>
        private OleMenuCommand DefineCommandHandler(CommandID id, EventHandler handler)
        {
            if (Zombied)
                return null;

            if (menuService == null)
                menuService = this.GetService<IMenuCommandService>();

            if (menuService == null)
                return null;

            var command = new OleMenuCommand(handler, id);
            menuService.AddCommand(command);
            return command;
        }

        protected override WindowPane InstantiateToolWindow(Type toolWindowType)
        {
            if (toolWindowType == typeof(TraceLogPane))
                return traceLogPane.Value;
            return base.InstantiateToolWindow(toolWindowType);
        }

        /// <summary>
        /// This method loads a localized string based on the specified resource.
        /// </summary>
        /// <param name="resourceName">Resource to load</param>
        /// <returns>String loaded for the specified resource</returns>
        internal string GetResourceString(string resourceName)
        {
            string resourceValue;
            IVsResourceManager resourceManager = (IVsResourceManager)GetService(typeof(SVsResourceManager));
            if (resourceManager == null)
                throw new InvalidOperationException("Could not get SVsResourceManager service. Make sure the package is Sited before calling this method");
            Guid packageGuid = GetType().GUID;
            int hr = resourceManager.LoadResourceString(ref packageGuid, -1, resourceName, out resourceValue);
            ErrorHandler.ThrowOnFailure(hr);
            return resourceValue;
        }

        private void ShowTraceLogWindow(object sender, EventArgs e)
        {
            this.ShowToolWindow<TraceLogPane>();
        }

        protected override void OnLoadOptions(string key, Stream stream)
        {
            if (key == EventTraceKitOptionKey) {
                //var serializer = new TraceSessionSettingsSerializer();
                //using (var reader = XmlReader.Create(stream))
                //    Settings = serializer.Read(reader);
            }
        }

        protected override void OnSaveOptions(string key, Stream stream)
        {
            if (key == EventTraceKitOptionKey) {
                //var serializer = new TraceSessionSettingsSerializer();
                //using (var writer = XmlWriter.Create(stream))
                //    Settings = serializer.Write(writer);
            }
        }

        private const string EventTraceKitOptionKey = "ETK_D438FC6445E7BF9BDEA29EA3B07";

        public GlobalSettings GlobalSettings
        {
            get { return globalSettings ?? (globalSettings = new GlobalSettings()); }
            set { globalSettings = value; }
        }

        public SolutionSettings SolutionSettings { get; set; }
    }

    public interface IEtkGlobalSettings
    {
    }

    [Export(typeof(IEtkGlobalSettings))]
    internal sealed class EtkGlobalSettings : IEtkGlobalSettings
    {
        private const string CollectionPath = "EventTraceKit";
        private const string GlobalSettingsName = "GlobalSettings";
        private const string ErrorGetFormat = "Cannot get setting {0}";
        private const string ErrorSetFormat = "Cannot set setting {0}";

        private readonly WritableSettingsStore settingsStore;
        private GlobalSettings globalSettings;

        [ImportingConstructor]
        internal EtkGlobalSettings(
            SVsServiceProvider vsServiceProvider)
            : this(vsServiceProvider.GetWritableSettingsStore())
        {
        }

        internal EtkGlobalSettings(WritableSettingsStore settingsStore)
        {
            this.settingsStore = settingsStore;
        }

        private void Report(string format, Exception exception)
        {
            // FIXME
        }

        private void EnsureCollectionExists()
        {
            if (!settingsStore.CollectionExists(CollectionPath))
                settingsStore.CreateCollection(CollectionPath);
        }

        private string GetString(string propertyName, string defaultValue)
        {
            EnsureCollectionExists();
            try {
                if (!settingsStore.PropertyExists(CollectionPath, propertyName))
                    return defaultValue;
                return settingsStore.GetString(CollectionPath, propertyName);
            } catch (Exception ex) {
                Report(string.Format(ErrorGetFormat, propertyName), ex);
                return defaultValue;
            }
        }

        private void SetString(string propertyName, string value)
        {
            EnsureCollectionExists();
            try {
                settingsStore.SetString(CollectionPath, propertyName, value);
            } catch (Exception ex) {
                Report(string.Format(ErrorSetFormat, propertyName), ex);
            }
        }

        private T GetObject<T>(string propertyName, Func<T> defaultValue)
        {
            EnsureCollectionExists();
            try {
                if (!settingsStore.PropertyExists(CollectionPath, propertyName))
                    return defaultValue();
                var serializer = new SettingsSerializer();
                var stream = settingsStore.GetMemoryStream(CollectionPath, propertyName);
                using (stream)
                    return serializer.Load<T>(stream);
            } catch (Exception ex) {
                Report(string.Format(ErrorGetFormat, propertyName), ex);
                return defaultValue();
            }
        }

        private void SetObject<T>(string propertyName, T value)
        {
            EnsureCollectionExists();
            try {
                var serializer = new SettingsSerializer();
                var stream = serializer.SaveToStream(value);
                settingsStore.SetMemoryStream(CollectionPath, propertyName, stream);
            } catch (Exception ex) {
                Report(string.Format(ErrorSetFormat, propertyName), ex);
            }
        }

        public GlobalSettings GlobalSettings
        {
            get { return globalSettings ?? (GlobalSettings = GetObject(GlobalSettingsName, () => new GlobalSettings())); }
            set
            {
                globalSettings = value;
                SetObject(GlobalSettingsName, value);
            }
        }
    }

    [SerializedShape(typeof(Settings.GlobalSettings))]
    public class GlobalSettings
    {
        public Guid ActiveSession { get; set; }

        public ObservableCollection<TraceSessionSettingsViewModel> Sessions { get; } =
            new ObservableCollection<TraceSessionSettingsViewModel>();
    }

    public class SolutionSettings
    {
        public Guid ActiveSession { get; set; }

        public Collection<TraceSession> Sessions { get; set; }
    }

    public interface IEventTraceKitSettingsService
    {
        GlobalSettings GlobalSettings { get; }
        SolutionSettings SolutionSettings { get; }
    }
}
