namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Debugging;
    using EventTraceKit.VsExtension.Settings;
    using Microsoft.VisualStudio.Threading;
    using Process = System.Diagnostics.Process;

    public interface ITraceController
    {
        event Action<TraceLog> SessionStarting;
        event Action<EventSession> SessionStarted;
        event Action<EventSession> SessionStopped;

        Task<EventSession> StartSessionAsync(TraceProfileDescriptor descriptor);
        Task StopSessionAsync();

        bool IsAutoLogEnabled { get; }
        void EnableAutoLog(TraceProfileDescriptor profile);
        void DisableAutoLog();
    }

    internal interface ITraceControllerInternal : ITraceController
    {
        void LaunchTraceTargets(IReadOnlyList<TraceLaunchTarget> targets);
    }

    [Guid("F1D64E54-9508-47B3-90E1-30135AF5D992")]
    public sealed class STraceController
    {
    }

    public class DefaultTraceController : ITraceControllerInternal, IDisposable
    {
        private readonly object mutex = new object();

        private EventSession runningSession;
#pragma warning disable 649
        private bool asyncAutoLog;
#pragma warning restore 649
        private TraceProfileDescriptor autoLogProfile;
        private CancellationTokenSource autoLogExitCts;

        public event Action<TraceLog> SessionStarting;
        public event Action<EventSession> SessionStarted;
        public event Action<EventSession> SessionStopped;
        public bool IsAutoLogEnabled { get; private set; }

        public void EnableAutoLog(TraceProfileDescriptor profile)
        {
            lock (mutex) {
                autoLogProfile = profile;
                IsAutoLogEnabled = true;
            }
        }

        public void DisableAutoLog()
        {
            lock (mutex) {
                IsAutoLogEnabled = false;
                autoLogProfile = null;
            }
        }

        public EventSession StartSession(TraceProfileDescriptor descriptor)
        {
            if (runningSession != null)
                throw new InvalidOperationException("Session already in progress.");

            var traceLog = new TraceLog();
            var session = new EventSession(descriptor, traceLog);
            SessionStarting?.Invoke(traceLog);
            runningSession = session;

            session.Start();
            SessionStarted?.Invoke(runningSession);

            return session;
        }

        public async Task<EventSession> StartSessionAsync(TraceProfileDescriptor descriptor)
        {
            if (runningSession != null)
                throw new InvalidOperationException("Session already in progress.");

            var traceLog = new TraceLog();
            var session = new EventSession(descriptor, traceLog);
            SessionStarting?.Invoke(traceLog);

            await session.StartAsync();

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

        private TraceProfileDescriptor AugmentTraceProfile(
            TraceProfileDescriptor descriptor, IReadOnlyList<TraceLaunchTarget> targets)
        {
            foreach (var collector in descriptor.Collectors.OfType<EventCollectorDescriptor>()) {
                foreach (var provider in collector.Providers) {
                    if (provider.StartupProjects == null)
                        continue;

                    foreach (var project in provider.StartupProjects) {
                        var target = targets.FirstOrDefault(
                            x => string.Equals(x.ProjectPath, project, StringComparison.OrdinalIgnoreCase));

                        if (target != null) {
                            if (provider.ProcessIds == null)
                                provider.ProcessIds = new List<uint>();
                            provider.ProcessIds.Add(target.ProcessId);
                        }
                    }
                }
            }

            return descriptor;
        }

        public void LaunchTraceTargets(IReadOnlyList<TraceLaunchTarget> targets)
        {
            if (!IsAutoLogEnabled || runningSession != null || !autoLogProfile.IsUsable())
                return;

            autoLogExitCts?.Cancel();
            autoLogExitCts = new CancellationTokenSource();

            var profile = AugmentTraceProfile(autoLogProfile, targets);

            if (asyncAutoLog)
                StartSessionAsync(profile).Forget();
            else
                StartSession(profile);

            var processTasks = targets.Select(
                x => Process.GetProcessById((int)x.ProcessId).WaitForExitAsync());

            Task.WhenAll(processTasks).ContinueWith(t => {
                ExitTraceTargets(targets);
            }, autoLogExitCts.Token, TaskContinuationOptions.None, TaskScheduler.Default).Forget();
        }

        private void ExitTraceTargets(IReadOnlyList<TraceLaunchTarget> targets)
        {
            if (!IsAutoLogEnabled || runningSession == null)
                return;

            StopSession();
        }

        public void Dispose()
        {
            StopSession();
        }
    }
}
