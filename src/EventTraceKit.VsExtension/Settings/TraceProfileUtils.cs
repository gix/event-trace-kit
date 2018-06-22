namespace EventTraceKit.VsExtension.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Settings.Persistence;

    public static class TraceProfileUtils
    {
        public static TraceProfileDescriptor GetDescriptor(this TraceProfile activeProfile)
        {
            var d = new TraceProfileDescriptor();
            foreach (var collector in activeProfile.Collectors) {
                if (collector is EventCollector ec)
                    d.Collectors.Add(GetDescriptor(ec));
            }

            return d;
        }

        public static EventCollectorDescriptor GetDescriptor(this EventCollector collector)
        {
            var d = new EventCollectorDescriptor();
            d.BufferSize = collector.BufferSize;
            d.MinimumBuffers = collector.MinimumBuffers;
            d.MaximumBuffers = collector.MaximumBuffers;
            d.LogFileName = collector.LogFileName;
            d.FlushPeriod = collector.FlushPeriod;
            d.Providers.AddRange(collector.Providers.Where(x => x.IsEnabled).Select(GetDescriptor));
            return d;
        }

        public static EventProviderDescriptor GetDescriptor(this EventProvider provider)
        {
            if (!provider.IsEnabled)
                throw new InvalidOperationException("Provider is disabled");

            var d = new EventProviderDescriptor(provider.Id);

            d.Id = provider.Id;
            d.Level = provider.Level;
            d.MatchAnyKeyword = provider.MatchAnyKeyword;
            d.MatchAllKeyword = provider.MatchAllKeyword;

            d.IncludeSecurityId = provider.IncludeSecurityId;
            d.IncludeTerminalSessionId = provider.IncludeTerminalSessionId;
            d.IncludeStackTrace = provider.IncludeStackTrace;

            if (provider.FilterExecutableNames)
                d.ExecutableName = string.Join(";", provider.ExecutableNames);

            if (provider.FilterProcessIds)
                d.ProcessIds = new List<uint>(provider.ProcessIds);

            if (provider.FilterEventIds) {
                d.EventIds = new List<ushort>(provider.EventIds);
                d.EventIdsFilterIn = provider.EventIdsFilterIn;
            }

            if (provider.FilterStackWalkEventIds) {
                d.StackWalkEventIds = new List<ushort>(provider.StackWalkEventIds);
                d.StackWalkEventIdsFilterIn = provider.StackWalkEventIdsFilterIn;
            }

            if (provider.FilterStackWalkLevelKeyword) {
                d.FilterStackWalkLevelKeyword = provider.FilterStackWalkLevelKeyword;
                d.StackWalkFilterIn = provider.StackWalkFilterIn;
                d.StackWalkLevel = provider.StackWalkLevel;
                d.StackWalkMatchAnyKeyword = provider.StackWalkMatchAnyKeyword;
                d.StackWalkMatchAllKeyword = provider.StackWalkMatchAllKeyword;
            }

            d.Manifest = provider.Manifest;
            d.StartupProjects = new List<string>(provider.StartupProjects);

            return d;
        }

        public static bool IsUsable(this TraceProfileDescriptor profile)
        {
            return
                profile != null &&
                profile.Collectors.Count > 0 &&
                profile.Collectors.OfType<EventCollectorDescriptor>().All(x => x.Providers.Count > 0);
        }
    }
}
