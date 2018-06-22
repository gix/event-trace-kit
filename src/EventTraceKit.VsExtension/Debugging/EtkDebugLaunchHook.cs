namespace EventTraceKit.VsExtension.Debugging
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.Pipes;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using EventTraceKit.VsExtension.Extensions;
    using Microsoft;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Debugger.Internal;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;
    using Task = System.Threading.Tasks.Task;

    [Guid("4469EFAB-EEEB-48CD-A4F4-E85806D72925")]
    public sealed class EtkDebugLaunchHook : IVsDebugLaunchHook110
    {
        private IVsDebugLaunchHook110 nextHook;

        private EventTraceKitPackage package;
        private IDiagLog log = new NullDiagLog();
        private ISolutionBrowser solutionBrowser;
        private ITraceControllerInternal traceController;

        private bool EnsureInitialized()
        {
            if (package == null) {
                package = EventTraceKitPackage.TryGetInstance();
                if (package == null)
                    return false;

                Initialize();
            }

            return true;
        }

        private void Initialize()
        {
            log = package;
            traceController = package.GetService<STraceController, ITraceControllerInternal>();
            Assumes.Present(traceController);
            solutionBrowser = new DteSolutionBrowser(package.GetService<SDTE, EnvDTE.DTE>());
        }

        public int IsProcessRecycleRequired(VsDebugTargetProcessInfo[] pProcessInfo)
        {
            if (nextHook == null)
                return VSConstants.S_FALSE;

            return nextHook.IsProcessRecycleRequired(pProcessInfo);
        }

        public int OnLaunchDebugTargets(
            uint debugTargetCount, VsDebugTargetInfo4[] debugTargets,
            VsDebugTargetProcessInfo[] launchResults)
        {
            return ErrorHandler.CallWithCOMConvention(
                () => OnLaunchDebugTargetsCore(
                    debugTargetCount, debugTargets, launchResults), true);
        }

        public int SetNextHook(IVsDebugLaunchHook110 pNextHook)
        {
            nextHook = pNextHook;
            return VSConstants.S_OK;
        }

        private int OnLaunchDebugTargetsCore(
            uint debugTargetCount, VsDebugTargetInfo4[] debugTargets,
            VsDebugTargetProcessInfo[] launchResults)
        {
            if (nextHook == null)
                return VSConstants.S_FALSE;

            if (!EnsureInitialized() || !traceController.IsAutoLogEnabled)
                return nextHook.OnLaunchDebugTargets(debugTargetCount, debugTargets, launchResults);

            bool noDebug = debugTargets.Any(
                x => (x.LaunchFlags & (int)__VSDBGLAUNCHFLAGS.DBGLAUNCH_NoDebug) != 0);

            int hr;
            if (!noDebug) {
                hr = nextHook.OnLaunchDebugTargets(
                    debugTargetCount, debugTargets, launchResults);
                if (hr < 0)
                    return hr;

                var processIds = launchResults.Select(x => x.dwProcessId).ToList();
                LaunchTraceTargets(debugTargets, processIds);
                return hr;
            }

            using (var ctx = InterceptTargets(debugTargets)) {
                hr = nextHook.OnLaunchDebugTargets(
                    debugTargetCount, ctx.Targets, launchResults);
                if (hr < 0)
                    return hr;

                LaunchTraceTargets(debugTargets, launchResults, ctx);
            }

            return hr;
        }

        private static InterceptionContext InterceptTargets(VsDebugTargetInfo4[] debugTargets)
        {
            var interceptedTargets = (VsDebugTargetInfo4[])debugTargets.Clone();
            var pipes = new List<NamedPipeServerStream>(debugTargets.Length);

            for (int i = 0; i < interceptedTargets.Length; ++i) {
                ref var target = ref interceptedTargets[i];
                if ((target.LaunchFlags & (int)__VSDBGLAUNCHFLAGS.DBGLAUNCH_NoDebug) != 0) {
                    var traceLaunchExe = GetTraceLaunchPath(in target);

                    var pipeName = Guid.NewGuid().ToString("D");
                    target.bstrArg = $"{pipeName} \"{target.bstrExe}\" {target.bstrArg}";
                    target.bstrExe = traceLaunchExe;
                    pipes.Add(CreateLaunchPipe(pipeName));
                } else {
                    pipes.Add(null);
                }
            }

            return new InterceptionContext(interceptedTargets, pipes);
        }

        private sealed class InterceptionContext : IDisposable
        {
            private readonly IReadOnlyList<NamedPipeServerStream> pipes;

            public InterceptionContext(
                VsDebugTargetInfo4[] interceptedTargets,
                IReadOnlyList<NamedPipeServerStream> pipes)
            {
                if (interceptedTargets.Length != pipes.Count)
                    throw new InvalidOperationException();

                Targets = interceptedTargets;
                this.pipes = pipes;
            }

            public VsDebugTargetInfo4[] Targets { get; }

            public void Dispose()
            {
                foreach (var pipe in pipes)
                    pipe?.Dispose();
            }

            public async Task<IReadOnlyList<uint>> GatherRealProcessIdsAsync(
                IReadOnlyList<VsDebugTargetProcessInfo> launchResults,
                CancellationToken cancellationToken)
            {
                if (launchResults.Count != pipes.Count)
                    throw new InvalidOperationException();

                async Task<uint> ReadPipe(NamedPipeServerStream pipe, int index)
                {
                    if (pipe == null)
                        return launchResults[index].dwProcessId;
                    await pipe.WaitForConnectionAsync(cancellationToken);
                    return await pipe.ReadUInt32Async(cancellationToken);
                }

                return await Task.WhenAll(pipes.Select(ReadPipe));
            }

            public void ResumeTargets()
            {
                foreach (var pipe in pipes) {
                    if (pipe != null) {
                        pipe.WriteByte(1);
                        pipe.WaitForPipeDrain();
                    }
                }
            }
        }

        private void LaunchTraceTargets(
            VsDebugTargetInfo4[] debugTargets, VsDebugTargetProcessInfo[] launchResults,
            InterceptionContext ctx)
        {
            try {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                WhenAllProcessesExit(launchResults).ContinueWith(t => cts.Cancel(), cts.Token, 0, TaskScheduler.Default);

                var processIds = Task.Run(
                    async () => await ctx.GatherRealProcessIdsAsync(launchResults, cts.Token),
                    cts.Token).Result;

                LaunchTraceTargets(debugTargets, processIds);

                ctx.ResumeTargets();
            } catch (AggregateException ae) {
                ae.Handle(x => {
                    switch (x) {
                        case OperationCanceledException _:
                            log.WriteLine("EventTraceKit: Timeout during trace launch");
                            break;
                        case Exception ex:
                            log.WriteLine("EventTraceKit: Error in debug launch hook: {0}", ex.Message);
                            break;
                    }
                    return true;
                });
            } catch (OperationCanceledException) {
                log.WriteLine("EventTraceKit: Timeout during trace launch");
            } catch (Exception ex) {
                log.WriteLine("EventTraceKit: Error in debug launch hook: {0}", ex.Message);
            }
        }

        private void LaunchTraceTargets(VsDebugTargetInfo4[] debugTargets, IReadOnlyList<uint> processIds)
        {
            var targets = CreateTraceLaunchTargets(debugTargets, processIds);
            LogTraceTargets(targets);
            traceController.LaunchTraceTargets(targets);
        }

        private static bool IsCmdWrapper(in VsDebugTargetInfo4 target)
        {
            return
                target.bstrExe.EndsWith("\\cmd.exe", StringComparison.OrdinalIgnoreCase)
                && target.bstrArg.StartsWith("/c \"\"", StringComparison.OrdinalIgnoreCase)
                && target.bstrArg.EndsWith(" & pause\"", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetTraceLaunchPath(in VsDebugTargetInfo4 target)
        {
            string realExe = target.bstrExe;
            if (IsCmdWrapper(in target)) {
                var match = Regex.Match(target.bstrArg, @"\A/c """"(?<path>[^""]+?)"" ");
                if (match.Success)
                    realExe = match.Groups["path"].Value;
            }

            var arch = PortableExecutableUtils.GetImageArchitecture(realExe);
            switch (arch) {
                case ProcessorArchitecture.X86:
                    return EventTraceKitPackage.GetToolPath("TraceLaunch.x86.exe");
                case ProcessorArchitecture.Amd64:
                    return EventTraceKitPackage.GetToolPath("TraceLaunch.x64.exe");
                default:
                    throw new NotSupportedException($"Unsupported processor architecture {arch}");
            }
        }

        private static Process GetProcess(in VsDebugTargetProcessInfo process)
        {
            return Process.GetProcessById(unchecked((int)process.dwProcessId));
        }

        private static Task WhenAllProcessesExit(VsDebugTargetProcessInfo[] processes)
        {
            return Task.WhenAll(processes.Select(x => GetProcess(in x).WaitForExitAsync()));
        }

        private void LogTraceTargets(List<TraceLaunchTarget> targets)
        {
            for (int i = 0; i < targets.Count; ++i) {
                var target = targets[i];
                log.WriteLine("TraceLaunchTarget [{0}] {{", i);
                log.WriteLine("  Executable:  {0}", target.Executable);
                log.WriteLine("  Arguments:   {0}", target.Arguments);
                log.WriteLine("  ProcessId:   {0}", target.ProcessId);
                log.WriteLine("  ProjectPath: {0}", target.ProjectPath);
                log.WriteLine("}}");
            }
        }

        private static NamedPipeServerStream CreateLaunchPipe(string pipeName)
        {
            var pipe = new NamedPipeServerStream(
                pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message,
                PipeOptions.WriteThrough | PipeOptions.Asynchronous, 4, 4);
            pipe.ReadMode = PipeTransmissionMode.Message;
            return pipe;
        }

        private List<TraceLaunchTarget> CreateTraceLaunchTargets(
            VsDebugTargetInfo4[] debugTargets, IReadOnlyList<uint> processIds)
        {
            var startupProject = solutionBrowser.StartupProjectDTI().FirstOrDefault();

            var targets = new List<TraceLaunchTarget>(debugTargets.Length);
            for (int i = 0; i < debugTargets.Length; ++i) {
                var target = new TraceLaunchTarget(
                    debugTargets[i].bstrExe,
                    debugTargets[i].bstrArg,
                    processIds[i],
                    startupProject?.Project.FullName);
                targets.Add(target);
            }

            return targets;
        }

        private int OnLaunchDebugTargetsCoreDebug(
            uint debugTargetCount, VsDebugTargetInfo4[] pDebugTargets,
            VsDebugTargetProcessInfo[] pLaunchResults)
        {
            if (nextHook == null)
                return VSConstants.S_FALSE;

            if (!EnsureInitialized())
                return nextHook.OnLaunchDebugTargets(debugTargetCount, pDebugTargets, pLaunchResults);

            log.WriteLine("LaunchDebugTargets: ", debugTargetCount);
            for (int i = 0; i < pDebugTargets.Length; ++i) {
                log.WriteLine("  [{0}] dlo:                   {1}", i, pDebugTargets[i].dlo);
                log.WriteLine("   {0}  LaunchFlags:           {1}", " ", pDebugTargets[i].LaunchFlags);
                log.WriteLine("   {0}  bstrRemoteMachine:     {1}", " ", pDebugTargets[i].bstrRemoteMachine);
                log.WriteLine("   {0}  bstrExe:               {1}", " ", pDebugTargets[i].bstrExe);
                log.WriteLine("   {0}  bstrArg:               {1}", " ", pDebugTargets[i].bstrArg);
                log.WriteLine("   {0}  bstrCurDir:            {1}", " ", pDebugTargets[i].bstrCurDir);
                log.WriteLine("   {0}  bstrEnv:               {1}", " ", pDebugTargets[i].bstrEnv);
                log.WriteLine("   {0}  dwProcessId:           {1}", " ", pDebugTargets[i].dwProcessId);
                log.WriteLine("   {0}  pStartupInfo:          {1}", " ", pDebugTargets[i].pStartupInfo);
                log.WriteLine("   {0}  guidLaunchDebugEngine: {1}", " ", pDebugTargets[i].guidLaunchDebugEngine);
                log.WriteLine("   {0}  dwDebugEngineCount:    {1}", " ", pDebugTargets[i].dwDebugEngineCount);
                log.WriteLine("   {0}  pDebugEngines:         {1}", " ", pDebugTargets[i].pDebugEngines);
                for (int j = 0; j < pDebugTargets[0].dwDebugEngineCount; ++j)
                    log.WriteLine("   {0}    {1}: {2}", " ", j,
                        Marshal.PtrToStructure<Guid>(pDebugTargets[i].pDebugEngines + Marshal.SizeOf<Guid>() * j));
                log.WriteLine("   {0}  guidPortSupplier:      {1}", " ", pDebugTargets[i].guidPortSupplier);
                log.WriteLine("   {0}  bstrPortName:          {1}", " ", pDebugTargets[i].bstrPortName);
                log.WriteLine("   {0}  bstrOptions:           {1}", " ", pDebugTargets[i].bstrOptions);
                log.WriteLine("   {0}  fSendToOutputWindow:   {1}", " ", pDebugTargets[i].fSendToOutputWindow);
                log.WriteLine("   {0}  pUnknown:              {1}", " ", pDebugTargets[i].pUnknown);
                log.WriteLine("   {0}  guidProcessLanguage:   {1}", " ", pDebugTargets[i].guidProcessLanguage);
                log.WriteLine("   {0}  AppPackageLaunchInfo:  {1}", " ", pDebugTargets[i].AppPackageLaunchInfo);
                log.WriteLine("   {0}  project:               {1}", " ", pDebugTargets[i].project);
            }

            log.WriteLine("LaunchResults: ");
            for (int i = 0; i < pLaunchResults.Length; ++i) {
                log.WriteLine("  [{0}] Creat: {1}", i, pLaunchResults[0].creationTime.ToDateTime());
                log.WriteLine("   {0}  PID:   {1}", " ", pLaunchResults[0].dwProcessId);
            }

            log.WriteLine("BeforeLaunch");
            int hr = nextHook.OnLaunchDebugTargets(debugTargetCount, pDebugTargets, pLaunchResults);
            log.WriteLine("AfterLaunch: hr=0x{0:X8}", hr);

            log.WriteLine("LaunchDebugTargets: ", debugTargetCount);
            for (int i = 0; i < pDebugTargets.Length; ++i) {
                log.WriteLine("  [{0}] dlo:                   {1}", i, pDebugTargets[i].dlo);
                log.WriteLine("   {0}  LaunchFlags:           {1}", " ", pDebugTargets[i].LaunchFlags);
                log.WriteLine("   {0}  bstrRemoteMachine:     {1}", " ", pDebugTargets[i].bstrRemoteMachine);
                log.WriteLine("   {0}  bstrExe:               {1}", " ", pDebugTargets[i].bstrExe);
                log.WriteLine("   {0}  bstrArg:               {1}", " ", pDebugTargets[i].bstrArg);
                log.WriteLine("   {0}  bstrCurDir:            {1}", " ", pDebugTargets[i].bstrCurDir);
                log.WriteLine("   {0}  bstrEnv:               {1}", " ", pDebugTargets[i].bstrEnv);
                log.WriteLine("   {0}  dwProcessId:           {1}", " ", pDebugTargets[i].dwProcessId);
                log.WriteLine("   {0}  pStartupInfo:          {1}", " ", pDebugTargets[i].pStartupInfo);
                log.WriteLine("   {0}  guidLaunchDebugEngine: {1}", " ", pDebugTargets[i].guidLaunchDebugEngine);
                log.WriteLine("   {0}  dwDebugEngineCount:    {1}", " ", pDebugTargets[i].dwDebugEngineCount);
                log.WriteLine("   {0}  pDebugEngines:         {1}", " ", pDebugTargets[i].pDebugEngines);
                for (int j = 0; j < pDebugTargets[0].dwDebugEngineCount; ++j)
                    log.WriteLine("   {0}    {1}: {2}", " ", j,
                        Marshal.PtrToStructure<Guid>(pDebugTargets[i].pDebugEngines + Marshal.SizeOf<Guid>() * j));
                log.WriteLine("   {0}  guidPortSupplier:      {1}", " ", pDebugTargets[i].guidPortSupplier);
                log.WriteLine("   {0}  bstrPortName:          {1}", " ", pDebugTargets[i].bstrPortName);
                log.WriteLine("   {0}  bstrOptions:           {1}", " ", pDebugTargets[i].bstrOptions);
                log.WriteLine("   {0}  fSendToOutputWindow:   {1}", " ", pDebugTargets[i].fSendToOutputWindow);
                log.WriteLine("   {0}  pUnknown:              {1}", " ", pDebugTargets[i].pUnknown);
                log.WriteLine("   {0}  guidProcessLanguage:   {1}", " ", pDebugTargets[i].guidProcessLanguage);
                log.WriteLine("   {0}  AppPackageLaunchInfo:  {1}", " ", pDebugTargets[i].AppPackageLaunchInfo);
                log.WriteLine("   {0}  project:               {1}", " ", pDebugTargets[i].project);
            }

            log.WriteLine("LaunchResults: ");
            for (int i = 0; i < pLaunchResults.Length; ++i) {
                log.WriteLine("  [{0}] Creat: {1}", i, pLaunchResults[0].creationTime.ToDateTime());
                log.WriteLine("   {0}  PID:   {1}", " ", pLaunchResults[0].dwProcessId);
            }

            return hr;
        }
    }
}
