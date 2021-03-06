namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel.Design;
    using System.Globalization;
    using System.Security.Permissions;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Views;
    using Microsoft.VisualStudio.Shell.Interop;

    internal sealed class ProjectTraceSettingsCommand : MenuCommand
    {
        private readonly Func<IVsMonitorSelection> vsMonitorSelectionFactory;
        private readonly Func<ProjectInfo, TraceSettingsViewModel> viewModelFactory;

        public ProjectTraceSettingsCommand(
            Func<IVsMonitorSelection> vsMonitorSelectionFactory,
            Func<ProjectInfo, TraceSettingsViewModel> viewModelFactory)
            : base(null, new CommandID(PkgCmdId.ProjectContextMenuCmdSet, PkgCmdId.cmdidTraceSettings))
        {
            this.vsMonitorSelectionFactory = vsMonitorSelectionFactory ?? throw new ArgumentNullException(nameof(vsMonitorSelectionFactory));
            this.viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        }

        public override int OleStatus
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                var project = vsMonitorSelectionFactory()?.GetSelectedProject();
                Enabled = project != null && project.IsSupported();
                return base.OleStatus;
            }
        }

        public override void Invoke()
        {
            var project = vsMonitorSelectionFactory()?.GetSelectedProject();
            if (project == null || !project.IsSupported()) {
                var projectName = project != null ? project.Name : string.Empty;
                var errorMessage = string.IsNullOrEmpty(projectName)
                    ? "No project is selected for this operation."
                    : string.Format(CultureInfo.CurrentCulture, "The project '{0}' is unsupported.", projectName);

                MessageHelper.ShowWarningMessage(errorMessage, "Operation failed");
                return;
            }

            var dialog = new TraceSettingsWindow {
                DataContext = viewModelFactory(project.GetProjectInfo())
            };
            dialog.ShowModal();
        }
    }
}
