namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Runtime.InteropServices;
    using EnvDTE;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.Win32;

    //[PackageRegistration(UseManagedResourcesOnly = true)]
    //[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    //[ProvideMenuResource("Menus.ctmenu", 1)]
    //[ProvideToolWindow(typeof(TraceLogWindow))]
    //[Guid(PackageGuidString)]
    //[SuppressMessage(
    //    "StyleCop.CSharp.DocumentationRules",
    //    "SA1650:ElementDocumentationMustBeSpelledCorrectly",
    //    Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    //public sealed class EventTraceKitPackage : Package
    //{
    //    /// <summary>
    //    /// ToolWindow1Package GUID string.
    //    /// </summary>
    //    public const string PackageGuidString = "4cb5ce07-d27f-4321-8705-dd4d1927d67e";

    //    /// <summary>
    //    ///   Initializes a new instance of the <see cref="TraceLogWindow"/> class.
    //    /// </summary>
    //    public EventTraceKitPackage()
    //    {
    //        // Inside this method you can place any initialization code that does not require
    //        // any Visual Studio service because at this point the package object is created but
    //        // not sited yet inside Visual Studio environment. The place to do all the other
    //        // initialization is the Initialize method.
    //    }

    //    /// <summary>
    //    /// Initialization of the package; this method is called right after the package is sited, so this is the place
    //    /// where you can put all the initialization code that rely on services provided by VisualStudio.
    //    /// </summary>
    //    protected override void Initialize()
    //    {
    //        TraceLogWindowCommand.Initialize(this);
    //        base.Initialize();
    //    }
    //}

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
    [ProvideToolWindow(
        typeof(PersistedWindowPane), Style = VsDockStyle.Tabbed,
        Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057")]
    [ProvideToolWindow(
        typeof(DynamicWindowPane), PositionX = 250, PositionY = 250,
        Width = 160, Height = 180, Transient = true)]
    [ProvideToolWindowVisibility(
        typeof(DynamicWindowPane), /*UICONTEXT_SolutionExists*/"f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [ProvideToolWindow(typeof(TraceLogWindow))]
    [ProvideMenuResource(1000, 1)]
    //[ProvideMenuResource("Menus.ctmenu", 1)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(PackageGuidString)]
    [ProvideProfile(typeof(PersistCurrentDesign), "MyDesigner", "CurrentDesign", 1004, 1005, false)]
    [SuppressMessage(
        "StyleCop.CSharp.DocumentationRules",
        "SA1650:ElementDocumentationMustBeSpelledCorrectly",
        Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public class EventTraceKitPackage : Package, IOperationalModeProvider
    {
        public const string PackageGuidString = "7867DA46-69A8-40D7-8B8F-92B0DE8084D8";

        // Cache the Menu Command Service since we will use it multiple times
        private OleMenuCommandService menuService;
        private TraceLogWindow traceLog;
        private DTE dte;
        private DebuggerEvents debuggerEvents;

        /// <summary>
        /// Initialization of the package; this is the place where you can put all the initialization
        /// code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            AddMenuCommandHandlers();

            dte = (DTE)GetGlobalService(typeof(SDTE));
            debuggerEvents = dte.Events.DebuggerEvents;
            debuggerEvents.OnEnterRunMode += DebuggerEventsOnEnterRunMode;
            debuggerEvents.OnEnterDesignMode += DebuggerEventsOnEnterDesignMode;
            debuggerEvents.OnEnterBreakMode += DebuggerEventsOnEnterBreakMode;

            switch (dte.Mode) {
                case vsIDEMode.vsIDEModeDesign:
                    currentOperationalMode = VsOperationalMode.Design;
                    break;
                case vsIDEMode.vsIDEModeDebug:
                    currentOperationalMode = VsOperationalMode.Debug;
                    break;
            }
        }

        private void AddMenuCommandHandlers()
        {
            var id = new CommandID(Guids.guidClientCmdSet, PkgCmdId.cmdidPersistedWindow);
            DefineCommandHandler(ShowPersistedWindow, id);

            id = new CommandID(Guids.guidClientCmdSet, PkgCmdId.cmdidUiEventsWindow);
            DefineCommandHandler(ShowDynamicWindow, id);

            id = new CommandID(Guids.TraceLogCmdSet, PkgCmdId.cmdidTraceLog);
            DefineCommandHandler(ShowTraceLogWindow, id);
        }

        private VsOperationalMode currentOperationalMode;
        private event EventHandler<VsOperationalMode> OperationalModeChanged;

        VsOperationalMode IOperationalModeProvider.CurrentMode => currentOperationalMode;

        event EventHandler<VsOperationalMode> IOperationalModeProvider.OperationalModeChanged
        {
            add { OperationalModeChanged += value; }
            remove { OperationalModeChanged -= value; }
        }

        private void DebuggerEventsOnEnterRunMode(dbgEventReason reason)
        {
            FireModeChanged(VsOperationalMode.Debug);
        }

        private void DebuggerEventsOnEnterDesignMode(dbgEventReason reason)
        {
            FireModeChanged(VsOperationalMode.Design);
        }

        private void DebuggerEventsOnEnterBreakMode(
            dbgEventReason reason, ref dbgExecutionAction executionAction)
        {
            FireModeChanged(VsOperationalMode.Debug);
        }

        private void FireModeChanged(VsOperationalMode newMode)
        {
            OutputString(
                VSConstants.OutputWindowPaneGuid.DebugPane_guid,
                $"{currentOperationalMode} -> {newMode}");

            if (currentOperationalMode == newMode)
                return;

            currentOperationalMode = newMode;
            OperationalModeChanged?.Invoke(this, newMode);
        }

        private void OutputString(Guid guidPane, string text)
        {
            const int DO_NOT_CLEAR_WITH_SOLUTION = 0;
            const int VISIBLE = 1;

            var outputWindow = GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow == null)
                return;

            int hr;

            // The General pane is not created by default. We must force its creation
            if (guidPane == VSConstants.OutputWindowPaneGuid.GeneralPane_guid) {
                hr = outputWindow.CreatePane(guidPane, "General", VISIBLE, DO_NOT_CLEAR_WITH_SOLUTION);
                ErrorHandler.ThrowOnFailure(hr);
            }

            IVsOutputWindowPane outputWindowPane;
            hr = outputWindow.GetPane(guidPane, out outputWindowPane);
            ErrorHandler.ThrowOnFailure(hr);

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
            if (toolWindowType == typeof(TraceLogWindow))
                return TraceLog;
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

        /// <summary>
        /// Event handler for our menu item.
        /// This results in the tool window being shown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        private void ShowPersistedWindow(object sender, EventArgs arguments)
        {
            // Get the 1 (index 0) and only instance of our tool window (if it does not already exist it will get created)
            ToolWindowPane pane = FindToolWindow(typeof(PersistedWindowPane), 0, true);
            if (pane == null)
                throw new COMException(GetResourceString("@101"));
            IVsWindowFrame frame = pane.Frame as IVsWindowFrame;
            if (frame == null)
                throw new COMException(GetResourceString("@102"));
            // Bring the tool window to the front and give it focus
            ErrorHandler.ThrowOnFailure(frame.Show());
        }

        /// <summary>
        /// Event handler for our menu item.
        /// This result in the tool window being shown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        private void ShowDynamicWindow(object sender, EventArgs arguments)
        {
            // Get the one (index 0) and only instance of our tool window (if it does not already exist it will get created)
            ToolWindowPane pane = FindToolWindow(typeof(DynamicWindowPane), 0, true);
            if (pane == null)
                throw new COMException(GetResourceString("@101"));
            IVsWindowFrame frame = pane.Frame as IVsWindowFrame;
            if (frame == null)
                throw new COMException(GetResourceString("@102"));
            // Bring the tool window to the front and give it focus
            ErrorHandler.ThrowOnFailure(frame.Show());
        }

        private void ShowTraceLogWindow(object sender, EventArgs e)
        {
            ShowToolWindow<TraceLogWindow>();
        }

        private void ShowToolWindow<T>() where T : ToolWindowPane
        {
            ToolWindowPane window = FindToolWindow(typeof(T), 0, true);
            if (window?.Frame == null)
                throw new NotSupportedException("Cannot create tool window");

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        internal TraceLogWindow TraceLog => traceLog ?? (traceLog = new TraceLogWindow(this));

        protected override void OnLoadOptions(string key, Stream stream)
        {
        }

        protected override void OnSaveOptions(string key, Stream stream)
        {
        }
    }

    [ComVisible(true)]
    [Guid("9619B7BF-69E2-4F5F-B95C-F2E6EDA02205")]
    public class UserOptions : Component, IProfileManager
    {
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

    public interface IOperationalModeProvider
    {
        VsOperationalMode CurrentMode { get; }
        event EventHandler<VsOperationalMode> OperationalModeChanged;
    }

    public enum VsOperationalMode
    {
        Design = 0,
        Debug = 1,
    }

    internal class VsOperationalModeChangedEventArgs : EventArgs
    {
        public VsOperationalModeChangedEventArgs(VsOperationalMode newMode)
        {
            NewMode = newMode;
        }

        public VsOperationalMode NewMode { get; }
    }

    [Guid("BF25126E-595C-42FC-BCF7-2DBE958E0C77")]
    internal class PersistCurrentDesign : Component, IProfileManager
    {
        public void SaveSettingsToXml(IVsSettingsWriter writer)
        {
        }

        public void LoadSettingsFromXml(IVsSettingsReader reader)
        {
        }

        public void SaveSettingsToStorage()
        {
        }

        public void LoadSettingsFromStorage()
        {
        }

        public void ResetSettings()
        {
        }
    }
}