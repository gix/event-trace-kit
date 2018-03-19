namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;

    public class VsSolutionManager
        : IVsSelectionEvents, IVsUpdateSolutionEvents, IVsSolutionManager, IDisposable
    {
        private readonly IDiagLog log;

        private readonly ISolutionBrowser solutionBrowser;
        private readonly IVsMonitorSelection monitorSelection;
        private readonly uint selectionEventsCookie;

        private readonly IVsSolutionBuildManager solutionBuildManager;
        private readonly uint updateSolutionEventsCookie;

        public VsSolutionManager(
            ISolutionBrowser solutionBrowser,
            IVsMonitorSelection monitorSelection,
            IVsSolutionBuildManager solutionBuildManager,
            IDiagLog log)
        {
            this.solutionBrowser = solutionBrowser;
            this.monitorSelection = monitorSelection ?? throw new ArgumentNullException(nameof(monitorSelection));
            this.solutionBuildManager = solutionBuildManager ?? throw new ArgumentNullException(nameof(solutionBuildManager));
            this.log = log ?? throw new ArgumentNullException(nameof(log));

            // Advise to selection events (e.g. startup project changed)
            monitorSelection.AdviseSelectionEvents(this, out selectionEventsCookie);

            // Advise to update solution events (e.g. switched debug/release configuration)
            solutionBuildManager.AdviseUpdateSolutionEvents(this, out updateSolutionEventsCookie);
        }

        public event EventHandler<StartupProjectChangedEventArgs> StartupProjectChanged;

        public IEnumerable<ProjectInfo> EnumerateStartupProjects()
        {
            return solutionBrowser.EnumerateStartupProjects();
        }

        public void Dispose()
        {
            monitorSelection.UnadviseSelectionEvents(selectionEventsCookie);
            solutionBuildManager.UnadviseUpdateSolutionEvents(updateSolutionEventsCookie);
        }

        int IVsSelectionEvents.OnSelectionChanged(
            IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld,
            IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }

        int IVsSelectionEvents.OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            if (elementid == (uint)VSConstants.VSSELELEMID.SEID_StartupProject) {
                StartupProjectChanged?.Invoke(this, new StartupProjectChangedEventArgs(
                    solutionBrowser.EnumerateStartupProjects().ToList()));
            }

            return VSConstants.S_OK;
        }

        int IVsSelectionEvents.OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }
    }
}
