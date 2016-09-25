namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows.Documents;
    using System.Xml;
    using EnvDTE;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// The Package class is responsible for the following:
    ///		- Attributes to enable registration of the components
    ///		- Enable the creation of our tool windows
    ///		- Respond to our commands
    ///
    /// The following attributes are covered in other samples:
    ///		PackageRegistration:   Reference.Package
    ///		ProvideMenuResource:   Reference.MenuAndCommands
    ///
    /// Our initialize method defines the command handlers for the commands that
    /// we provide under View|Other Windows to show our tool windows
    ///
    /// The first new attribute we are using is ProvideToolWindow. That attribute
    /// is used to advertise that our package provides a tool window. In addition
    /// it can specify optional parameters to describe the default start location
    /// of the tool window. For example, the PersistedWindowPane will start tabbed
    /// with Solution Explorer. The default position is only used the very first
    /// time a tool window with a specific Guid is shown for a user. After that,
    /// the position is persisted based on the last known position of the window.
    /// When trying different default start positions, you may find it useful to
    /// delete *.prf from:
    ///		"%USERPROFILE%\Application Data\Microsoft\VisualStudio\10.0Exp\"
    /// as this is where the positions of the tool windows are persisted.
    ///
    /// To get the Guid corresponding to the Solution Explorer window, we ran this
    /// sample, made sure the Solution Explorer was visible, selected it in the
    /// Persisted Tool Window and looked at the properties in the Properties
    /// window. You can do the same for any window.
    ///
    /// The DynamicWindowPane makes use of a different set of optional properties.
    /// First it specifies a default position and size (again note that this only
    /// affects the very first time the window is displayed). Then it specifies the
    /// Transient flag which means it will not be persisted when Visual Studio is
    /// closed and reopened.
    ///
    /// The second new attribute is ProvideToolWindowVisibility. This attribute
    /// is used to specify that a tool window visibility should be controled
    /// by a UI Context. For a list of predefined UI Context, look in vsshell.idl
    /// and search for "UICONTEXT_". Since we are using the UICONTEXT_SolutionExists,
    /// this means that it is possible to cause the window to be displayed simply by
    /// creating a solution/project.
    /// </summary>
    [ProvideToolWindow(typeof(TraceLogPane))]
    [ProvideMenuResource(1000, 1)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(PackageGuidString)]
    [ProvideProfile(typeof(EventTraceKitSettingsProfile), "EventTraceKit", "General", 1001, 1002, false)]
    [SuppressMessage(
        "StyleCop.CSharp.DocumentationRules",
        "SA1650:ElementDocumentationMustBeSpelledCorrectly",
        Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public class EventTraceKitPackage : Package, IEventTraceKitSettingsService
    {
        public const string PackageGuidString = "7867DA46-69A8-40D7-8B8F-92B0DE8084D8";

        private OleMenuCommandService menuService;
        private IOperationalModeProvider operationalModeProvider;

        private Lazy<TraceLogPane> traceLogPane = new Lazy<TraceLogPane>(() => null);

        public EventTraceKitPackage()
        {
            AddOptionKey(EventTraceKitOptionKey);
        }

        /// <summary>
        /// Initialization of the package; this is the place where you can put all the initialization
        /// code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            AddMenuCommandHandlers();

            var dte = (DTE)GetGlobalService(typeof(SDTE));
            operationalModeProvider = new DteOperationalModeProvider(dte, this);
            traceLogPane = new Lazy<TraceLogPane>(() => new TraceLogPane(operationalModeProvider));
        }

        private void AddMenuCommandHandlers()
        {
            var id = new CommandID(Guids.TraceLogCmdSet, PkgCmdId.cmdidTraceLog);
            DefineCommandHandler(ShowTraceLogWindow, id);
        }

        internal void OutputString(Guid paneId, string text)
        {
            const int DO_NOT_CLEAR_WITH_SOLUTION = 0;
            const int VISIBLE = 1;

            var outputWindow = GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
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
        /// Define a command handler.
        /// When the user press the button corresponding to the CommandID
        /// the EventHandler will be called.
        /// </summary>
        /// <param name="id">The CommandID (Guid/ID pair) as defined in the .vsct file</param>
        /// <param name="handler">Method that should be called to implement the command</param>
        /// <returns>The menu command. This can be used to set parameter such as the default visibility once the package is loaded</returns>
        internal OleMenuCommand DefineCommandHandler(EventHandler handler, CommandID id)
        {
            if (Zombied)
                return null;

            if (menuService == null) {
                // Get the OleCommandService object provided by the MPF; this object is the one
                // responsible for handling the collection of commands implemented by the package.
                menuService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            }

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

        public TraceSessionSettings Settings { get; set; }
    }

    public class TraceProviderSettings
    {
        public
            TraceProviderSettings(Guid id)
        {
            Id = id;
            Level = 0xFF;
            MatchAnyKeyword = 0xFFFFFFFFFFFFFFFFUL;
            MatchAllKeyword = 0;

            ProcessIds = new List<uint>();
            EventIds = new List<ushort>();
        }

        public Guid Id { get; set; }
        public byte Level { get; set; }
        public ulong MatchAnyKeyword { get; set; }
        public ulong MatchAllKeyword { get; set; }

        public bool IncludeSecurityId { get; set; }
        public bool IncludeTerminalSessionId { get; set; }
        public bool IncludeStackTrace { get; set; }

        public string Manifest { get; set; }
        public List<uint> ProcessIds { get; set; }
        public List<ushort> EventIds { get; set; }
    }

    public class TraceSessionSettings
    {
        public IList<TraceProviderSettings> Providers { get; } =
            new List<TraceProviderSettings>();
    }

    public interface IEventTraceKitSettingsService
    {
        TraceSessionSettings Settings { get; set; }
    }

    [ComVisible(true)]
    [Guid("9619B7BF-69E2-4F5F-B95C-F2E6EDA02205")]
    public sealed class EventTraceKitSettingsProfile : Component, IProfileManager
    {
        private EventTraceKitPackage package;

        public override ISite Site
        {
            get { return base.Site; }
            set
            {
                base.Site = value;
                package = Site?.GetService<EventTraceKitPackage>();
            }
        }

        public void LoadSettingsFromStorage()
        {
        }

        public void LoadSettingsFromXml(IVsSettingsReader reader)
        {
        }

        public void ResetSettings()
        {
        }

        public void SaveSettingsToStorage()
        {
        }

        public void SaveSettingsToXml(IVsSettingsWriter writer)
        {
        }
    }

    public static class ServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider provider)
            where T : class
        {
            return provider.GetService(typeof(T)) as T;
        }
    }
}
