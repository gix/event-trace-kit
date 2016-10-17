namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Documents;
    using Controls;
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

        public EventTraceKitPackage()
        {
            AddOptionKey(EventTraceKitOptionKey);
        }

        protected override void Initialize()
        {
            base.Initialize();

            LoadSettings();

            AddMenuCommandHandlers();

            Func<IServiceProvider, TraceLogWindow> traceLogFactory = sp => {
                var dte = sp.GetService<SDTE, DTE>();
                var operationalModeProvider = new DteOperationalModeProvider(dte, this);

                var traceLog = new TraceLogWindowViewModel(this, operationalModeProvider);

                var mcs = sp.GetService<IMenuCommandService>();
                if (mcs != null)
                    traceLog.Attach(mcs);

                return new TraceLogWindow { DataContext = traceLog };
            };

            traceLogPane = new Lazy<TraceLogPane>(() => new TraceLogPane(traceLogFactory));
        }

        protected override void Dispose(bool disposing)
        {
            SaveSettings();
            base.Dispose(disposing);
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

            id = new CommandID(PkgCmdId.TraceLogPresetMenuCmdSet, PkgCmdId.cmdidPresetMenuDynamicStartCommand);
            var command = new DynamicItemMenuCommand(
                id,
                IsValidDynamicItem,
                OnInvokedDynamicItem,
                OnBeforeQueryStatusDynamicItem);
            menuService?.AddCommand(command);

            id = new CommandID(PkgCmdId.ComboBoxCmdSet, PkgCmdId.cmdidMyDropDownCombo);
            menuService?.AddCommand(new OleMenuCommand(OnMenuMyDropDownCombo, id) {
                ParametersDescription = "$"
            });

            id = new CommandID(PkgCmdId.ComboBoxCmdSet, PkgCmdId.cmdidMyDropDownComboGetList);
            menuService?.AddCommand(
                new OleMenuCommand(OnMenuMyDropDownComboGetList, id));

            id = new CommandID(PkgCmdId.TraceLogPresetMenuCmdSet, 0x0101);
            menuService?.AddCommand(
                new OleMenuCommand(null, null, (s, e) => {
                    var cmd = (OleMenuCommand)s;
                    cmd.Text = "foo*" + (c++);
                }, id));
        }

        private int c;

        private void OnMenuMyDropDownCombo(object sender, EventArgs args)
        {
            var cmdEventArgs = args as OleMenuCmdEventArgs;
            if (cmdEventArgs == null)
                throw new ArgumentException(nameof(args));

            string newChoice = cmdEventArgs.InValue as string;
            IntPtr outValue = cmdEventArgs.OutValue;
            if (newChoice != null && outValue != IntPtr.Zero)
                throw new ArgumentException("BothInOutParamsIllegal");

            if (outValue != IntPtr.Zero) {
                Marshal.GetNativeVariantForObject(currentDropDownComboChoice, outValue);
            } else if (newChoice != null) {
                bool validInput = false;
                int idx;
                for (idx = 0; idx < dropDownComboChoices.Length; ++idx) {
                    if (string.Compare(dropDownComboChoices[idx], newChoice,
                            StringComparison.CurrentCultureIgnoreCase) == 0) {
                        validInput = true;
                        break;
                    }
                }

                if (!validInput)
                    throw new ArgumentException("ParamNotValidStringInList");

                currentDropDownComboChoice = dropDownComboChoices[idx];
                MessageBox.Show(currentDropDownComboChoice);
            }
        }

        private string currentDropDownComboChoice;
        private List<string> dropDownComboChoicesList = new List<string> { "Foo", "Bar", "Baz" };
        private string[] dropDownComboChoices = new[] { "Foo", "Bar", "Baz" };

        private void OnMenuMyDropDownComboGetList(object sender, EventArgs args)
        {
            var cmdEventArgs = args as OleMenuCmdEventArgs;
            if (cmdEventArgs == null)
                return;

            object inValue = cmdEventArgs.InValue;
            IntPtr outValue = cmdEventArgs.OutValue;
            if (inValue != null)
                throw new ArgumentException("InParamIllegal");
            if (outValue == IntPtr.Zero)
                throw new ArgumentException("OutParamRequired");

            dropDownComboChoicesList.Add("X" + dropDownComboChoicesList.Count);
            dropDownComboChoices = dropDownComboChoicesList.ToArray();
            Marshal.GetNativeVariantForObject(dropDownComboChoices, outValue);
        }

        private void OnInvokedDynamicItem(object sender, EventArgs args)
        {
            var command = (DynamicItemMenuCommand)sender;
            if (command.Checked)
                return;

            bool isRootItem = command.MatchedCommandId == 0;
            int idx = command.MatchedCommandId - PkgCmdId.cmdidPresetMenuDynamicStartCommand;
            int indexForDisplay = isRootItem ? 0 : idx;
            var entry = indexForDisplay < presetNames.Count ? presetNames[indexForDisplay] : Tuple.Create("OutOfBounds", "OutOfBounds");
            MessageBox.Show(entry.Item2);

            UpdateCommand(PkgCmdId.TraceLogPresetMenuCmdSet, 0x0101);
        }

        private void UpdateCommand(Guid menuGroup, int cmdId)
        {
            var uiShell = this.GetService<SVsUIShell, IVsUIShell>();
            uiShell?.UpdateCommandUI(0);
        }

        private List<Tuple<string, string>> presetNames = new List<Tuple<string, string>> {
            Tuple.Create("Foo", "23"),
            Tuple.Create("Bar", "42"),
            Tuple.Create("Baz", "66"),
        };

        private bool IsValidDynamicItem(int commandId)
        {
            // The match is valid if the command ID is >= the id of our root dynamic start item 
            // and the command ID minus the ID of our root dynamic start item
            // is less than or equal to the number of projects in the solution.
            return
                commandId >= PkgCmdId.cmdidPresetMenuDynamicStartCommand &&
                (commandId - PkgCmdId.cmdidPresetMenuDynamicStartCommand < presetNames.Count);
        }

        private void OnBeforeQueryStatusDynamicItem(object sender, EventArgs args)
        {
            var command = (DynamicItemMenuCommand)sender;
            command.Enabled = true;
            command.Visible = true;

            // Find out whether the command ID is 0, which is the ID of the root item.
            // If it is the root item, it matches the constructed DynamicItemMenuCommand,
            // and IsValidDynamicItem won't be called.
            bool isRootItem = command.MatchedCommandId == 0;

            // The index is set to 1 rather than 0 because the Solution.Projects collection is 1-based.
            int idx = command.MatchedCommandId - PkgCmdId.cmdidPresetMenuDynamicStartCommand;
            int indexForDisplay = isRootItem ? 0 : idx;

            var entry = indexForDisplay < presetNames.Count ? presetNames[indexForDisplay] : Tuple.Create("OutOfBounds", "OutOfBounds");
            command.Text = entry.Item1;
            command.Checked = indexForDisplay == 1;
            //command.MatchedCommandId = 0;
        }

        private class DynamicItemMenuCommand : OleMenuCommand
        {
            private readonly Func<int, bool> matches;

            public DynamicItemMenuCommand(
                CommandID rootId, Func<int, bool> matches, EventHandler invokeHandler,
                EventHandler beforeQueryStatusHandler)
                : base(invokeHandler, null /*changeHandler*/, beforeQueryStatusHandler, rootId)
            {
                if (matches == null)
                    throw new ArgumentNullException(nameof(matches));

                this.matches = matches;
            }

            public override bool DynamicItemMatch(int cmdId)
            {
                // Call the supplied predicate to test whether the given cmdId is a match.
                // If it is, store the command id in MatchedCommandid 
                // for use by any BeforeQueryStatus handlers, and then return that it is a match.
                // Otherwise clear any previously stored matched cmdId and return that it is not a match.
                if (matches(cmdId)) {
                    MatchedCommandId = cmdId;
                    return true;
                }

                MatchedCommandId = 0;
                return false;
            }
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
        public ObservableCollection<AsyncDataViewModelPreset> ModifiedPresets { get; } =
            new ObservableCollection<AsyncDataViewModelPreset>();

        public ObservableCollection<AsyncDataViewModelPreset> PersistedPresets { get; } =
            new ObservableCollection<AsyncDataViewModelPreset>();

        public Guid ActiveSession { get; set; }

        public ObservableCollection<TraceSessionSettingsViewModel> Sessions { get; } =
            new ObservableCollection<TraceSessionSettingsViewModel>();
    }

    public class SolutionSettings
    {
        public Collection<ProfilePreset> ModifiedPresets { get; set; }

        public Collection<ProfilePreset> PersistedPresets { get; set; }

        public Guid ActiveSession { get; set; }

        public Collection<TraceSession> Sessions { get; set; }
    }

    public interface IEventTraceKitSettingsService
    {
        GlobalSettings GlobalSettings { get; }
        SolutionSettings SolutionSettings { get; }
    }
}
