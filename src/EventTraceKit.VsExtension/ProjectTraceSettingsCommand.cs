namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel.Design;
    using System.Globalization;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Views;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    internal sealed class ProjectTraceSettingsCommand : OleMenuCommand
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

        public override void Invoke()
        {
            var project = vsMonitorSelectionFactory()?.GetSelectedProject();
            if (project == null || !IsSupported(project)) {
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

        private static bool IsSupported(EnvDTE.Project project)
        {
            return project.Kind != null && IsSupportedProjectKind(new Guid(project.Kind));
        }

        private static bool IsSupportedProjectKind(Guid kind)
        {
            return
                kind == VsProjectKinds.CppProjectKindId ||
                kind == VsProjectKinds.LegacyCSharpProjectKindId ||
                kind == VsProjectKinds.CSharpProjectKindId;
        }
    }
}
