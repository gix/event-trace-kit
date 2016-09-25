namespace EventTraceKit.VsExtension
{
    using System;
    using EnvDTE;
    using Microsoft.VisualStudio;

    internal class DteOperationalModeProvider
        : IDisposable, IOperationalModeProvider
    {
        private readonly EventTraceKitPackage package;
        private DTE dte;
        private DebuggerEvents debuggerEvents;

        private event EventHandler<VsOperationalMode> OperationalModeChanged;

        public DteOperationalModeProvider(DTE dte, EventTraceKitPackage package)
        {
            this.dte = dte;
            this.package = package;

            debuggerEvents = dte.Events.DebuggerEvents;
            debuggerEvents.OnEnterRunMode += OnEnterRunMode;
            debuggerEvents.OnEnterDesignMode += OnEnterDesignMode;
            debuggerEvents.OnEnterBreakMode += OnEnterBreakMode;

            switch (dte.Mode) {
                case vsIDEMode.vsIDEModeDesign:
                    CurrentMode = VsOperationalMode.Design;
                    break;
                case vsIDEMode.vsIDEModeDebug:
                    CurrentMode = VsOperationalMode.Debug;
                    break;
            }
        }

        public void Dispose()
        {
            debuggerEvents.OnEnterRunMode -= OnEnterRunMode;
            debuggerEvents.OnEnterDesignMode -= OnEnterDesignMode;
            debuggerEvents.OnEnterBreakMode -= OnEnterBreakMode;
            debuggerEvents = null;
            dte = null;
        }

        public VsOperationalMode CurrentMode { get; private set; }

        event EventHandler<VsOperationalMode> IOperationalModeProvider.OperationalModeChanged
        {
            add { OperationalModeChanged += value; }
            remove { OperationalModeChanged -= value; }
        }

        private void OnEnterRunMode(dbgEventReason reason)
        {
            SwitchMode(VsOperationalMode.Debug);
        }

        private void OnEnterDesignMode(dbgEventReason reason)
        {
            SwitchMode(VsOperationalMode.Design);
        }

        private void OnEnterBreakMode(
            dbgEventReason reason, ref dbgExecutionAction executionAction)
        {
            SwitchMode(VsOperationalMode.Debug);
        }

        private void SwitchMode(VsOperationalMode newMode)
        {
            package.OutputString(
                VSConstants.OutputWindowPaneGuid.DebugPane_guid,
                $"DteOperationalModeProvider.SwitchMode: {CurrentMode} -> {newMode}");

            if (CurrentMode == newMode)
                return;

            CurrentMode = newMode;
            OperationalModeChanged?.Invoke(this, newMode);
        }
    }
}
