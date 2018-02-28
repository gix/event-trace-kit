namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using EnvDTE;
    using EventTraceKit.Tracing;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.VCProjectEngine;
    using Native;
    using VSLangProj;
    using Process = System.Diagnostics.Process;

    public interface ITraceSessionService
    {
        event Action<TraceLog> SessionStarting;
        event Action<TraceSession> SessionStarted;
        event Action<TraceSession> SessionStopped;
        Task<TraceSession> StartSessionAsync(EventSessionDescriptor descriptor);
        Task StopSessionAsync();
        void EnableAutoLog(EventSessionDescriptor descriptor);
        void DisableAutoLog();
    }

    public class DefaultTraceSessionService : ITraceSessionService
    {
        private TraceSession runningSession;

        private bool autoLogEnabled;
        private bool asyncAutoLog;
        private EventSessionDescriptor autoLogDescriptor;
        private CancellationTokenSource autoLogExitCts;

        public DefaultTraceSessionService()
        {
        }

        public event Action<TraceLog> SessionStarting;
        public event Action<TraceSession> SessionStarted;
        public event Action<TraceSession> SessionStopped;
        public bool IsAutoLogEnabled => autoLogEnabled;

        public void EnableAutoLog(EventSessionDescriptor descriptor)
        {
            autoLogDescriptor = descriptor;
            autoLogEnabled = true;
        }

        public void DisableAutoLog()
        {
            autoLogEnabled = false;
            autoLogDescriptor = null;
        }

        public TraceSession StartSession(EventSessionDescriptor descriptor)
        {
            if (runningSession != null)
                throw new InvalidOperationException("Session already in progress.");

            var traceLog = new TraceLog();
            SessionStarting?.Invoke(traceLog);
            var session = new TraceSession(descriptor);
            session.Start(traceLog);

            runningSession = session;
            SessionStarted?.Invoke(runningSession);
            return session;
        }

        public async Task<TraceSession> StartSessionAsync(EventSessionDescriptor descriptor)
        {
            if (runningSession != null)
                throw new InvalidOperationException("Session already in progress.");

            var traceLog = new TraceLog();
            SessionStarting?.Invoke(traceLog);
            var session = new TraceSession(descriptor);
            await session.StartAsync(traceLog);

            runningSession = session;
            SessionStarted?.Invoke(runningSession);
            return session;
        }

        public void StopSession()
        {
            if (runningSession == null)
                return;

            autoLogExitCts?.Cancel();
            using (runningSession) {
                runningSession.Stop();
                SessionStopped?.Invoke(runningSession);
                runningSession = null;
            }
        }

        public async Task StopSessionAsync()
        {
            if (runningSession == null)
                return;

            autoLogExitCts?.Cancel();
            using (runningSession) {
                await Task.Run(() => runningSession.Stop());
                SessionStopped?.Invoke(runningSession);
                runningSession = null;
            }
        }

        private static bool IsUsableDescriptor(EventSessionDescriptor descriptor)
        {
            return descriptor != null && descriptor.Providers.Count > 0;
        }

        private EventSessionDescriptor AugmentDescriptor(
            EventSessionDescriptor descriptor, List<TraceLaunchTarget> targets)
        {
            foreach (var provider in descriptor.Providers) {
                if (provider.StartupProjects == null)
                    continue;

                foreach (var project in provider.StartupProjects) {
                    var target = targets.FirstOrDefault(
                        x => string.Equals(x.ProjectPath, project, StringComparison.OrdinalIgnoreCase));

                    if (target != null)
                        provider.ProcessIds.Add(target.ProcessId);
                }
            }

            return descriptor;
        }

        public void LaunchTraceTargets(List<TraceLaunchTarget> targets)
        {
            if (!autoLogEnabled || runningSession != null || !IsUsableDescriptor(autoLogDescriptor))
                return;

            autoLogExitCts?.Cancel();
            autoLogExitCts = new CancellationTokenSource();

            var descriptor = AugmentDescriptor(autoLogDescriptor, targets);

            if (asyncAutoLog)
                StartSessionAsync(descriptor).Forget();
            else
                StartSession(descriptor);

            var processTasks = targets.Select(
                x => Process.GetProcessById((int)x.ProcessId).WaitForExitAsync());

            Task.WhenAll(processTasks).ContinueWith(t => {
                ExitTraceTargets(targets);
            }, autoLogExitCts.Token).Forget();
        }

        private void ExitTraceTargets(List<TraceLaunchTarget> targets)
        {
            if (!autoLogEnabled || runningSession == null)
                return;

            StopSession();
        }
    }

    public class DebugTargetInfo
    {
        public DebugTargetInfo(string projectPath, string command, string commandArguments)
        {
            ProjectPath = projectPath;
            Command = command;
            CommandArguments = commandArguments;
        }

        public string ProjectPath { get; }
        public string Command { get; }
        public string CommandArguments { get; }

        public bool MatchesProcess(Process process)
        {
            return
                string.Equals(Command, process.TryGetCommand(), StringComparison.OrdinalIgnoreCase) &&
                CommandArguments == process.TryGetCommandLine();
        }
    }

    public class DebuggedProjectInfo
    {
        public DebuggedProjectInfo(string projectPath, int processId)
        {
            ProjectPath = projectPath;
            ProcessId = processId;
        }

        public string ProjectPath { get; }
        public int ProcessId { get; }
    }

#if false
    public class DebuggerListener : IDebugEventCallback2, IOperationalModeProvider, IDisposable
    {
        private readonly IVsDebugger debugger;
        private readonly IProjectProvider projectProvider;

        private readonly List<DebuggedProjectInfo> debuggedProjects =
            new List<DebuggedProjectInfo>();

        private Action<VsOperationalMode, IReadOnlyList<DebuggedProjectInfo>> operationalModeChanged;

        private bool isReceivingEvents;
        private int listeners;

        public DebuggerListener(EventTraceKitPackage package)
        {
            debugger = package.GetService<SVsShellDebugger, IVsDebugger>();
            projectProvider = new DefaultProjectProvider(package.GetService<SDTE, DTE>());
            AdviseEvents();

            CurrentMode = VsOperationalMode.Design;
        }

        public VsOperationalMode CurrentMode { get; }

        public event Action<VsOperationalMode, IReadOnlyList<DebuggedProjectInfo>> OperationalModeChanged
        {
            add
            {
                operationalModeChanged += value;
                if (Interlocked.Increment(ref listeners) == 1)
                    AdviseEvents();
            }
            remove
            {
                if (Interlocked.Decrement(ref listeners) == 0)
                    UnadviseEvents();
                operationalModeChanged -= value;
            }
        }

        private void AdviseEvents()
        {
            if (!isReceivingEvents) {
                ErrorHandler.ThrowOnFailure(debugger.AdviseDebugEventCallback(this));
                isReceivingEvents = true;
            }
        }

        private void UnadviseEvents()
        {
            if (isReceivingEvents) {
                ErrorHandler.ThrowOnFailure(debugger.AdviseDebugEventCallback(this));
                isReceivingEvents = false;
            }
        }

        public void Dispose()
        {
            UnadviseEvents();
        }

        int IDebugEventCallback2.Event(
            IDebugEngine2 engine, IDebugProcess2 process, IDebugProgram2 program,
            IDebugThread2 thread, IDebugEvent2 debugEvent, ref Guid riidEvent, uint attributes)
        {
            //var eventType = GetEventType(riidEvent);
            //Debug.WriteLine($"Event: {eventType}");

            switch (debugEvent) {
                case IDebugSessionCreateEvent2 _:
                    NewSession();
                    break;

                case IDebugSessionDestroyEvent2 _:
                    EndSession();
                    break;

                case IDebugProcessCreateEvent2 _:
                    int pid = process.GetProcessId();
                    var proc = Process.GetProcessById(pid);

                    if (IsNewProcess(proc)) {
                        foreach (var dti in projectProvider.StartupProjectDTI()) {
                            if (dti.MatchesProcess(proc))
                                debuggedProjects.Add(new DebuggedProjectInfo(dti.ProjectPath, pid));
                        }
                    }

                    break;

                case IDebugLoadCompleteEvent2 _:
                    LoadCompleted();
                    break;
            }

            // Documentation advises strongly to explicitly release these.
            Marshal.ReleaseComObject(debugEvent);
            if (thread != null)
                Marshal.ReleaseComObject(thread);
            if (program != null)
                Marshal.ReleaseComObject(program);
            if (process != null)
                Marshal.ReleaseComObject(process);
            if (engine != null)
                Marshal.ReleaseComObject(engine);

            return 0;
        }

        private static bool IsNewProcess(Process process)
        {
            return
                process.GetProcessTimes(out var kernelTime, out var userTime) &&
                kernelTime + userTime < TimeSpan.FromSeconds(1);
        }

        private void NewSession()
        {
            debuggedProjects.Clear();
        }

        private void LoadCompleted()
        {
            operationalModeChanged?.Invoke(VsOperationalMode.Debug, debuggedProjects);
        }

        private void EndSession()
        {
            operationalModeChanged?.Invoke(VsOperationalMode.Design, null);
            debuggedProjects.Clear();
        }

        private string GetEventType(Guid eventId)
        {
            var eventTypes = new Type[] {
                //typeof(IDebugActivateDocumentEvent2),
                //typeof(IDebugBeforeSymbolSearchEvent2),
                //typeof(IDebugBreakEvent2),
                //typeof(IDebugBreakpointBoundEvent2),
                //typeof(IDebugBreakpointErrorEvent2),
                //typeof(IDebugBreakpointEvent2),
                //typeof(IDebugBreakpointUnboundEvent2),
                //typeof(IDebugCanStopEvent2),
                //typeof(IDebugDocumentTextEvents2),
                //typeof(IDebugEngineCreateEvent2),
                //typeof(IDebugEntryPointEvent2),
                //typeof(IDebugErrorEvent2),
                ////typeof(IDebugExitB‌​reakStateEvent),
                //typeof(IDebugEventCallback2),
                //typeof(IDebugExceptionEvent2),
                //typeof(IDebugExpressionEvaluationCompleteEvent2),
                //typeof(IDebugInterceptExceptionCompleteEvent2),
                //typeof(IDebugLoadCompleteEvent2),
                //typeof(IDebugMessageEvent2),
                //typeof(IDebugModuleLoadEvent2),
                //typeof(IDebugNoSymbolsEvent2),
                //typeof(IDebugOutputStringEvent2),
                //typeof(IDebugPortEvents2),
                //typeof(IDebugProcessCreateEvent2),
                //typeof(IDebugProcessDestroyEvent2),
                //typeof(IDebugProgramCreateEvent2),
                //typeof(IDebugProgramDestroyEvent2),
                //typeof(IDebugProgramDestroyEventFlags2),
                //typeof(IDebugProgramNameChangedEvent2),
                //typeof(IDebugPropertyCreateEvent2),
                //typeof(IDebugPropertyDestroyEvent2),
                //typeof(IDebugReturnValueEvent2),
                //typeof(IDebugSessionCreateEvent2),
                //typeof(IDebugSettingsCallback2),
                //typeof(IDebugStepCompleteEvent2),
                //typeof(IDebugSymbolSearchEvent2),
                //typeof(IDebugThreadCreateEvent2),
                //typeof(IDebugThreadDestroyEvent2),
                //typeof(IDebugThreadNameChangedEvent2),
                //
                //typeof(IDebugProcessContinueEvent100),
                //typeof(IDebugCustomEvent110),
                //typeof(IDebugSessionDestroyEvent2),
            };

            foreach (var eventType in eventTypes) {
                if (eventId == eventType.GUID)
                    return eventType.Name;
            }

            return $"<{eventId}>";
        }
    }

    public static class DebuggerExtensions
    {
        public static int GetProcessId(this IDebugProcess2 process)
        {
            if (process == null)
                return 0;

            var id = new AD_PROCESS_ID[1];
            if (process.GetPhysicalProcessId(id) != VSConstants.S_OK)
                return 0;

            return (int)id[0].dwProcessId;
        }
    }
#endif

    public class ProjectInfo
    {
        public ProjectInfo(Guid kind, string fullName, string name)
        {
            Kind = kind;
            FullName = fullName;
            Name = name;
        }

        public Guid Kind { get; }
        public string FullName { get; }
        public string Name { get; }
    }

    public interface IProjectProvider
    {
        IEnumerable<ProjectInfo> EnumerateProjects();
        IEnumerable<DebugTargetInfo> StartupProjectDTI();
    }

    internal static class PropertiesExtensions
    {
        public static T GetValue<T>(this Properties properties, string name)
        {
            try {
                var property = properties.Item(name);
                if (property?.Value is T val) {
                    return val;
                }

                return default;
            } catch (Exception) {
                return default;
            }
        }

        public static bool TryGetProperty<T>(this Properties properties, string name, out T value)
        {
            try {
                var property = properties.Item(name);
                if (property?.Value is T val) {
                    value = val;
                    return true;
                }

                value = default;
                return false;
            } catch (Exception) {
                value = default;
                return false;
            }
        }
    }

    public class DefaultProjectProvider : IProjectProvider
    {
        private readonly DTE dte;

        public DefaultProjectProvider(DTE dte)
        {
            this.dte = dte;
        }

        public IEnumerable<Project> EnumerateStartupProjects()
        {
            foreach (string name in (Array)dte.Solution.SolutionBuild.StartupProjects) {
                Project project = dte.Solution.Item(name);
                yield return project;
            }
        }

        /// <summary>{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}</summary>
        private static readonly Guid CppProjectKindId =
            new Guid(0x8BC9CEB8, 0x8B4A, 0x11D0, 0x8D, 0x11, 0x00, 0xA0, 0xC9, 0x1B, 0xC9, 0x42);

        /// <summary>{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</summary>
        private static readonly Guid LegacyCSharpProjectKindId =
            new Guid(0xFAE04EC0, 0x301F, 0x11D3, 0xBF, 0x4B, 0x00, 0xC0, 0x4F, 0x79, 0xEF, 0xBC);

        /// <summary>{9A19103F-16F7-4668-BE54-9A1E7A4F7556}</summary>
        private static readonly Guid CSharpProjectKindId =
            new Guid(0x9A19103F, 0x16F7, 0x4668, 0xBE, 0x54, 0x9A, 0x1E, 0x7A, 0x4F, 0x75, 0x56);

        public IEnumerable<DebugTargetInfo> EnumerateDebugInfos()
        {
            foreach (Project project in dte.Solution.Projects) {
                var dti = GetDTI(project);
                if (dti != null)
                    yield return dti;
            }
        }

        private static DebugTargetInfo GetDTI(Project project)
        {
            var kind = new Guid(project.Kind);

            if (kind == CppProjectKindId)
                return GetNativeDTI(project);
            if (kind == LegacyCSharpProjectKindId)
                return GetManagedDTI(project);
            if (kind == CSharpProjectKindId)
                return GetManagedDTI(project);

            return null;
        }

        private static DebugTargetInfo GetNativeDTI(Project project)
        {
            var vcp = (VCProject)project.Object;
            var cfg = vcp.ActiveConfiguration;
            var debugSettings = (VCDebugSettings)cfg.DebugSettings;

            var debuggerFlavor = debugSettings.DebuggerFlavor;
            if (debuggerFlavor != eDebuggerTypes.eLocalDebugger)
                return null;

            string command = cfg.Evaluate(debugSettings.Command);
            string args = cfg.Evaluate(debugSettings.CommandArguments);
            return new DebugTargetInfo(project.FullName, command, args);
        }

        private static DebugTargetInfo GetManagedDTI(Project project)
        {
            try {
                var config = project.ConfigurationManager.ActiveConfiguration;
                var cfgProperties = config.Properties;

                var startAction = cfgProperties.GetValue<prjStartAction>(
                    nameof(ProjectConfigurationProperties.StartAction));

                string command;
                switch (startAction) {
                    case prjStartAction.prjStartActionProject:
                        var rootPath = Path.GetDirectoryName(project.FullName);
                        var outputPath = cfgProperties.GetValue<string>(
                            nameof(ProjectConfigurationProperties.OutputPath));
                        command = Path.Combine(rootPath, outputPath);
                        break;
                    case prjStartAction.prjStartActionProgram:
                        command = cfgProperties.GetValue<string>(
                            nameof(ProjectConfigurationProperties.StartProgram));
                        break;
                    case prjStartAction.prjStartActionURL:
                    case prjStartAction.prjStartActionNone:
                        return null;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var commandArguments = cfgProperties.GetValue<string>(
                    nameof(ProjectConfigurationProperties.StartArguments));

                return new DebugTargetInfo(project.FullName, command, commandArguments);
            } catch (Exception) {
                return null;
            }
        }

        public IEnumerable<ProjectInfo> EnumerateProjects()
        {
            var result = new List<ProjectInfo>();

            var solution = dte.Solution;
            var projects = solution.Projects;
            foreach (Project project in projects)
                result.Add(GetDebugInfo(project));

            return result;
        }

        private static ProjectInfo GetDebugInfo(Project project)
        {
            var name = project.Name;
            var kind = new Guid(project.Kind);
            var fullName = project.FullName;
            var info = new ProjectInfo(kind, fullName, name);
            return info;
        }

        public IEnumerable<DebugTargetInfo> StartupProjectDTI()
        {
            foreach (var project in EnumerateStartupProjects())
                yield return GetDTI(project);
        }
    }
}
