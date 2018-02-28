namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows;
    using EventTraceKit.VsExtension.Views;
    using EventTraceKit.VsExtension.Views.PresetManager;
    using Extensions;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Shell.Settings;
    using Task = System.Threading.Tasks.Task;

    public interface IDiagLog
    {
        void WriteLine(string format, params object[] args);
    }

    public class NullDiagLog : IDiagLog
    {
        public void WriteLine(string format, params object[] args)
        {
        }
    }

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", productId: "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(TraceLogToolWindow),
        Style = VsDockStyle.Tabbed,
        Window = VsGuids.OutputWindowFrameString,
        Orientation = ToolWindowOrientation.Right)]
    [ProvideProfile(typeof(EventTraceKitProfileManager), "EventTraceKit", "General",
                    1001, 1002, false, DescriptionResourceID = 1003)]
    //[FontAndColorsRegistration(
    //    "Trace Log", FontAndColorDefaultsProvider.ServiceId,
    //    TraceLogFontAndColorDefaults.CategoryIdString)]
    [Guid(PackageGuidString)]
    public class EventTraceKitPackage : AsyncPackage, IDiagLog
    {
        public const string PackageGuidString = "7867DA46-69A8-40D7-8B8F-92B0DE8084D8";

        private IMenuCommandService menuService;

        private Lazy<TraceLogToolWindow> traceLogPane = new Lazy<TraceLogToolWindow>(() => null);
        private IGlobalSettings globalSettings;
        private IVsUIShell vsUiShell;
        private ViewPresetService viewPresetService;
        private TraceSettingsService traceSettingsService;

        private DefaultTraceSessionService sessionService;
        private SolutionMonitor solutionMonitor;
        private IVsOutputWindow outputWindow;

        //private ResourceSynchronizer resourceSynchronizer;

        public EventTraceKitPackage()
        {
            AddOptionKey(EventTraceKitOptionKey);
        }

        internal DefaultTraceSessionService SessionService => sessionService;

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            //var fncStorage = this.GetService<SVsFontAndColorStorage, IVsFontAndColorStorage>();
            //resourceSynchronizer = new ResourceSynchronizer(
            //    fncStorage, Application.Current.Resources.MergedDictionaries);
            //
            //IServiceContainer container = this;
            //container.AddService(
            //    typeof(SVsFontAndColorDefaultsProvider), CreateService, true);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Application.Current.Resources.MergedDictionaries.Add(
                new TraceLogResourceDictionary());

            vsUiShell = this.GetService<SVsUIShell, IVsUIShell>();
            menuService = this.GetService<IMenuCommandService>();
            var shell = this.GetService<SVsShell, IVsShell>();

            outputWindow = this.GetService<SVsOutputWindow, IVsOutputWindow>();

            AddMenuCommandHandlers();

            globalSettings = new SettingsStoreGlobalSettings(CreateSettingsStore());
            solutionMonitor = new SolutionMonitor(this);

            string appDataDirectory = GetAppDataDirectory(shell);
            var storage = new FileSettingsStorage(appDataDirectory);
            viewPresetService = new ViewPresetService(storage);

            viewPresetService.LoadFromStorage();
            viewPresetService.ExceptionFilter += OnExceptionFilter;

            traceSettingsService = new TraceSettingsService(appDataDirectory);
            traceSettingsService.Load();

            traceLogPane = new Lazy<TraceLogToolWindow>(
                () => new TraceLogToolWindow(TraceLogWindowFactory, TraceLogWindowClose));

            sessionService = new DefaultTraceSessionService();
        }

        //private object CreateService(IServiceContainer container, Type service)
        //{
        //    if (service.IsEquivalentTo(typeof(SVsFontAndColorDefaultsProvider)))
        //        return new SVsFontAndColorDefaultsProvider(resourceSynchronizer);
        //    return null;
        //}

        internal static string GetToolPath(string fileName)
        {
            var directory = Path.GetDirectoryName(typeof(EventTraceKitPackage).Assembly.Location);
            if (directory == null)
                return fileName;
            return Path.Combine(directory, fileName);
        }

        private TraceLogPaneContent TraceLogWindowFactory(IServiceProvider sp)
        {
            var traceLog = new TraceLogPaneViewModel(
                globalSettings,
                sessionService,
                viewPresetService,
                traceSettingsService,
                vsUiShell);

            var commandService = sp.GetService<IMenuCommandService>();
            if (commandService != null)
                traceLog.InitializeMenuCommands(commandService);

            return new TraceLogPaneContent { DataContext = traceLog };
        }

        private void TraceLogWindowClose()
        {
            viewPresetService.SaveToStorage();
        }

        private static string GetAppDataDirectory(IVsShell shell)
        {
            ErrorHandler.ThrowOnFailure(
                shell.GetProperty((int)__VSSPROPID.VSSPROPID_AppDataDir, out var appDataDir));
            return Path.Combine((string)appDataDir, "EventTraceKit");
        }

        private void OnExceptionFilter(
            object sender, ExceptionFilterEventArgs args)
        {
            vsUiShell?.ShowMessageBox(
                0, Guid.Empty, "Error", args.Message, null, 0, OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_CRITICAL, 0, out int _);
        }

        private WritableSettingsStore CreateSettingsStore()
        {
            var mgr = new ShellSettingsManager(this);
            return mgr.GetWritableSettingsStore(SettingsScope.UserSettings);
        }

        private void AddMenuCommandHandlers()
        {
            var id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidTraceLog);
            DefineCommandHandler(id, ShowTraceLogWindow);
        }

        void IDiagLog.WriteLine(string format, params object[] args)
        {
            var message = string.Format(format, args);
            if (outputWindow != null) {
                if (!string.IsNullOrEmpty(message) && message[message.Length - 1] != '\n')
                    message += '\n';

                var pane = GetOutputPane(in VSConstants.OutputWindowPaneGuid.GeneralPane_guid);
                pane.OutputStringThreadSafe(message);
            }
        }

        private IVsOutputWindowPane GetOutputPane(in Guid paneId)
        {
            if (ErrorHandler.Failed(outputWindow.GetPane(paneId, out var pane)) || pane == null)
                ErrorHandler.ThrowOnFailure(
                    outputWindow.CreatePane(paneId, "General", 1, 0));

            ErrorHandler.ThrowOnFailure(
                outputWindow.GetPane(paneId, out pane));

            return pane;
        }

        internal void OutputString(string format, params object[] args)
        {
            OutputString(VSConstants.OutputWindowPaneGuid.GeneralPane_guid, string.Format(format, args));
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

            ErrorHandler.ThrowOnFailure(
                outputWindow.GetPane(paneId, out var outputWindowPane));

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
                return null;

            var command = new OleMenuCommand(handler, id);
            menuService.AddCommand(command);
            return command;
        }

        protected override WindowPane InstantiateToolWindow(Type toolWindowType)
        {
            if (toolWindowType == typeof(TraceLogToolWindow))
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
            var resourceManager = this.GetService<SVsResourceManager, IVsResourceManager>();
            if (resourceManager == null)
                throw new InvalidOperationException(
                    "Could not get SVsResourceManager service. Make sure the package is Sited before calling this method");
            int hr = resourceManager.LoadResourceString(GetType().GUID, -1, resourceName, out var str);
            ErrorHandler.ThrowOnFailure(hr);
            return str;
        }

        private void ShowTraceLogWindow(object sender, EventArgs e)
        {
            this.ShowToolWindow<TraceLogToolWindow>();
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

        // {2C602C2D-4BA7-4C64-A5E1-DCE75CBBD530}
        // as Base64: LSxgLKdLZEyl4dznXLvVMA==
        private const string EventTraceKitOptionKey = "ETK_LSxgLKdLZEyl4dznXLvVMA==";

        internal static EventTraceKitPackage TryGetInstance()
        {
            var id = new Guid(PackageGuidString);
            if (GetGlobalService(typeof(SVsShell)) is IVsShell shell
                && shell.IsPackageLoaded(ref id, out var vsp) == 0
                && vsp is EventTraceKitPackage package) {
                return package;
            }

            return null;
        }
    }

    public static class DebuggingNativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct DEBUG_EVENT
        {
            [MarshalAs(UnmanagedType.U4)]
            public DebugEventCode dwDebugEventCode;
            public uint dwProcessId;
            public uint dwThreadId;
            public DEBUG_EVENT_UNION u;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DEBUG_EVENT_UNION
        {
            [FieldOffset(0)]
            public EXCEPTION_DEBUG_INFO Exception;
            [FieldOffset(0)]
            public CREATE_THREAD_DEBUG_INFO CreateThread;
            [FieldOffset(0)]
            public CREATE_PROCESS_DEBUG_INFO CreateProcessInfo;
            [FieldOffset(0)]
            public EXIT_THREAD_DEBUG_INFO ExitThread;
            [FieldOffset(0)]
            public EXIT_PROCESS_DEBUG_INFO ExitProcess;
            [FieldOffset(0)]
            public LOAD_DLL_DEBUG_INFO LoadDll;
            [FieldOffset(0)]
            public UNLOAD_DLL_DEBUG_INFO UnloadDll;
            [FieldOffset(0)]
            public OUTPUT_DEBUG_STRING_INFO DebugString;
            [FieldOffset(0)]
            public RIP_INFO RipInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EXCEPTION_DEBUG_INFO
        {
            public EXCEPTION_RECORD ExceptionRecord;
            public uint dwFirstChance;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EXCEPTION_RECORD
        {
            public uint ExceptionCode;
            public uint ExceptionFlags;
            public IntPtr ExceptionRecord;
            public IntPtr ExceptionAddress;
            public uint NumberParameters;
            public UIntPtr ExceptionInformation00;
            public UIntPtr ExceptionInformation01;
            public UIntPtr ExceptionInformation02;
            public UIntPtr ExceptionInformation03;
            public UIntPtr ExceptionInformation04;
            public UIntPtr ExceptionInformation05;
            public UIntPtr ExceptionInformation06;
            public UIntPtr ExceptionInformation07;
            public UIntPtr ExceptionInformation08;
            public UIntPtr ExceptionInformation09;
            public UIntPtr ExceptionInformation10;
            public UIntPtr ExceptionInformation11;
            public UIntPtr ExceptionInformation12;
            public UIntPtr ExceptionInformation13;
            public UIntPtr ExceptionInformation14;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CREATE_THREAD_DEBUG_INFO
        {
            public IntPtr hThread;
            public IntPtr lpThreadLocalBase;
            public IntPtr lpStartAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CREATE_PROCESS_DEBUG_INFO
        {
            public IntPtr hFile;
            public IntPtr hProcess;
            public IntPtr hThread;
            public IntPtr lpBaseOfImage;
            public uint dwDebugInfoFileOffset;
            public uint nDebugInfoSize;
            public IntPtr lpThreadLocalBase;
            public IntPtr lpStartAddress;
            public IntPtr lpImageName;
            public ushort fUnicode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EXIT_THREAD_DEBUG_INFO
        {
            public uint dwExitCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EXIT_PROCESS_DEBUG_INFO
        {
            public uint dwExitCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LOAD_DLL_DEBUG_INFO
        {
            public IntPtr hFile;
            public IntPtr lpBaseOfDll;
            public uint dwDebugInfoFileOffset;
            public uint nDebugInfoSize;
            public IntPtr lpImageName;
            public ushort fUnicode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNLOAD_DLL_DEBUG_INFO
        {
            public IntPtr lpBaseOfDll;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OUTPUT_DEBUG_STRING_INFO
        {
            public IntPtr lpDebugStringData;
            public ushort fUnicode;
            public ushort nDebugStringLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RIP_INFO
        {
            public uint dwError;
            public uint dwType;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetProcessId([In] IntPtr Process);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DebugActiveProcess(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DebugActiveProcessStop(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WaitForDebugEvent(
            out DEBUG_EVENT lpDebugEvent, uint dwMilliseconds);

        public enum DebugEventCode : uint
        {
            EXCEPTION_DEBUG_EVENT = 1,
            CREATE_THREAD_DEBUG_EVENT = 2,
            CREATE_PROCESS_DEBUG_EVENT = 3,
            EXIT_THREAD_DEBUG_EVENT = 4,
            EXIT_PROCESS_DEBUG_EVENT = 5,
            LOAD_DLL_DEBUG_EVENT = 6,
            UNLOAD_DLL_DEBUG_EVENT = 7,
            OUTPUT_DEBUG_STRING_EVENT = 8,
            RIP_EVENT = 9,
        }
    }

    public class SolutionMonitor : IVsSelectionEvents, IVsUpdateSolutionEvents
    {
        private readonly IDiagLog log;

        private IVsMonitorSelection monitorSelection;
        private uint selectionEventsCookie;
        private IVsSolutionBuildManager solutionBuildManager;
        private uint updateSolutionEventsCookie;

        public SolutionMonitor(IDiagLog log)
        {
            this.log = log;
            var provider = ServiceProvider.GlobalProvider;

            // Advise to selection events (e.g. startup project changed)

            monitorSelection = provider.GetService<SVsShellMonitorSelection, IVsMonitorSelection>();
            monitorSelection?.AdviseSelectionEvents(this, out selectionEventsCookie);

            // Advise to update solution events (e.g. switched debug/release configuration)
            solutionBuildManager = provider.GetService<SVsSolutionBuildManager, IVsSolutionBuildManager>();
            solutionBuildManager?.AdviseUpdateSolutionEvents(this, out updateSolutionEventsCookie);

            solutionBuildManager = provider.GetService<SVsSolutionBuildManager, IVsSolutionBuildManager>();
        }

        public int OnSelectionChanged(
            IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld,
            IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }

        public int OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            if (elementid == (uint)VSConstants.VSSELELEMID.SEID_StartupProject) {
                // When startup project is set in solution explorer a complete refresh is triggered
                var oldItem = (IVsHierarchy)varValueOld;
                var newItem = (IVsHierarchy)varValueNew;
                string oldName = null;
                string newName = null;
                oldItem?.GetCanonicalName((uint)VSConstants.VSITEMID.Root, out oldName);
                newItem?.GetCanonicalName((uint)VSConstants.VSITEMID.Root, out newName);
                log.WriteLine("Detected new StartupProject {0} -> {1}\n", oldName, newName);
                //OnRefreshAll();
            }

            return VSConstants.S_OK;
        }

        public int OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            if (pIVsHierarchy == null)
                log.WriteLine("OnActiveProjectCfgChange <null>\n");
            else {
                pIVsHierarchy.GetCanonicalName((uint)VSConstants.VSITEMID.Root, out var name);
                log.WriteLine("OnActiveProjectCfgChange {0}\n", name);
            }

            return VSConstants.S_OK;
        }
    }
}
