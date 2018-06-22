namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using EventTraceKit.VsExtension.Resources;
    using EventTraceKit.VsExtension.Settings;
    using EventTraceKit.VsExtension.Views;
    using Extensions;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Task = System.Threading.Tasks.Task;

    [Guid(PackageGuidString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "0.1.1", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(TraceLogToolWindow),
        Style = VsDockStyle.Tabbed,
        Window = VsGuids.OutputWindowFrameString,
        Orientation = ToolWindowOrientation.Right)]
    [ProvideService(typeof(STraceController), IsAsyncQueryable = true)]
    public class EventTraceKitPackage : AsyncPackage, IDiagLog
    {
        public const string PackageGuidString = "7867DA46-69A8-40D7-8B8F-92B0DE8084D8";

        private IVsUIShell vsUiShell;

        private DefaultTraceController traceController;
        private DteSolutionBrowser solutionBrowser;
        private VsSolutionManager vsSolutionManager;
        private IVsOutputWindow outputWindow;
        private ISettingsService settings;

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            await AddMenuCommandHandlers();

            traceController = new DefaultTraceController();

            AddService(typeof(STraceController), CreateServiceAsync);

            // -----------------------------------------------------------------
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            Application.Current.Resources.MergedDictionaries.Add(
                new TraceLogResourceDictionary());

            outputWindow = this.GetService<SVsOutputWindow, IVsOutputWindow>();
            vsUiShell = this.GetService<SVsUIShell, IVsUIShell>();
            var shell = this.GetService<SVsShell, IVsShell>();

            var monitorSelection = this.GetService<SVsShellMonitorSelection, IVsMonitorSelection>();
            var solutionBuildManager = this.GetService<SVsSolutionBuildManager, IVsSolutionBuildManager>();
            var dte = this.GetService<SDTE, EnvDTE.DTE>();

            solutionBrowser = new DteSolutionBrowser(dte);
            vsSolutionManager = new VsSolutionManager(
                solutionBrowser, monitorSelection, solutionBuildManager, this);

            string appDataDirectory = GetAppDataDirectory(shell);
            settings = new SettingsServiceImpl(vsSolutionManager, appDataDirectory);
        }

        protected override void Dispose(bool disposing)
        {
            traceController?.Dispose();
            vsSolutionManager?.Dispose();

            base.Dispose(disposing);
        }

        private Task<object> CreateServiceAsync(
            IAsyncServiceContainer asyncServiceContainer,
            CancellationToken cancellationToken, Type serviceType)
        {
            if (serviceType.IsEquivalentTo(typeof(STraceController)))
                return Task.FromResult<object>(traceController);

            return null;
        }

        internal static string GetToolPath(string fileName)
        {
            var directory = Path.GetDirectoryName(typeof(EventTraceKitPackage).Assembly.Location);
            if (directory == null)
                return fileName;
            return Path.Combine(directory, fileName);
        }

        private TraceLogToolWindow CreateTraceLogToolWindow()
        {
            TraceLogToolContent ContentFactory(IServiceProvider sp)
            {
                var traceLog = new TraceLogToolViewModel(
                    settings,
                    traceController,
                    store => new TraceSettingsViewModel(
                        new SolutionTraceSettingsContext(solutionBrowser), store),
                    vsUiShell);

                var commandService = sp.GetService<IMenuCommandService>();
                if (commandService != null)
                    traceLog.InitializeMenuCommands(commandService);

                return new TraceLogToolContent { DataContext = traceLog };
            }

            void OnClose(object context)
            {
                if (context is TraceLogToolViewModel viewModel)
                    viewModel.OnClose();
            }

            return new TraceLogToolWindow(ContentFactory, OnClose);
        }

        private static string GetAppDataDirectory(IVsShell shell)
        {
            ErrorHandler.ThrowOnFailure(
                shell.GetProperty((int)__VSSPROPID.VSSPROPID_AppDataDir, out var appDataDir));
            return Path.Combine((string)appDataDir, "EventTraceKit");
        }

        private async Task AddMenuCommandHandlers()
        {
            var menuCommandService = await this.GetServiceAsync<IMenuCommandService>();
            if (menuCommandService == null)
                return;

            menuCommandService.AddCommand(
                new MenuCommand(
                    (s, e) => this.ShowToolWindow<TraceLogToolWindow>(),
                    new CommandID(PkgCmdId.ViewCmdSet, PkgCmdId.cmdidTraceLog)));

            menuCommandService.AddCommand(
                new ProjectTraceSettingsCommand(
                    this.GetService<IVsMonitorSelection>,
                    x => new TraceSettingsViewModel(
                        new SolutionTraceSettingsContext(solutionBrowser),
                        settings.GetProjectStore(x))));
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

        protected override WindowPane InstantiateToolWindow(Type toolWindowType)
        {
            if (toolWindowType == typeof(TraceLogToolWindow))
                return CreateTraceLogToolWindow();
            return base.InstantiateToolWindow(toolWindowType);
        }

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
}
