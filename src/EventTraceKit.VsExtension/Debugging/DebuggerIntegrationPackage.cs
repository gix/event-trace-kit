namespace EventTraceKit.VsExtension.Debugging
{
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;

    [Guid(PackageGuidString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.Debugging_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideDebuggerLaunchHook(typeof(EtkDebugLaunchHook))]
    [ProvideBindingPath]
    public sealed class DebuggerIntegrationPackage : AsyncPackage
    {
        public const string PackageGuidString = "6BC1D938-E03E-4BF9-9C2E-92E08360680E";
    }
}
