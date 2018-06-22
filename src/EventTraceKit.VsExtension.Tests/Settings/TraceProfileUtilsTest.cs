namespace EventTraceKit.VsExtension.Tests.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Settings;
    using EventTraceKit.VsExtension.Settings.Persistence;
    using Xunit;

    public class TraceProfileUtilsTest
    {
        public static IEnumerable<object[]> TraceProfileData
        {
            get
            {
                yield return new object[] {
                    new TraceProfile {
                        Id = new Guid("2A129FF0-3896-44D2-A387-5C8D4351789F"),
                        Collectors = { (EventCollector)EventCollectorData.First()[0] }
                    }
                };
            }
        }

        public static IEnumerable<object[]> EventCollectorData
        {
            get
            {
                yield return new object[] {
                    new EventCollector {
                        Name = "Default",
                        BufferSize = 123,
                        MinimumBuffers = 50,
                        MaximumBuffers = 100,
                        LogFileName = "events.etl",
                        FlushPeriod = TimeSpan.FromMilliseconds(250),
                        Providers = {
                            (EventProvider)EventProviderData.First()[0],
                            (EventProvider)EventProviderData.Skip(1).First()[0],
                        }
                    }
                };
            }
        }

        public static IEnumerable<object[]> EventProviderData
        {
            get
            {
                yield return new object[] {
                    new EventProvider {
                        Id = new Guid("B3E63165-B450-4669-9EC6-D582C88AD37F"),
                        IsEnabled = true,

                        Level = 123,
                        MatchAnyKeyword = 0xFFEEDDCCBBAA9988UL,
                        MatchAllKeyword = 0x7766554433221100UL,

                        IncludeSecurityId = true,
                        IncludeTerminalSessionId = true,
                        IncludeStackTrace = true,

                        FilterExecutableNames = true,
                        ExecutableNames = { "project1.exe", "project2.exe" },

                        FilterProcessIds = true,
                        ProcessIds = { 1, 2 },

                        FilterEventIds = true,
                        EventIdsFilterIn = true,
                        EventIds = { 10, 20 },

                        FilterStackWalkEventIds = true,
                        StackWalkEventIds = { 100, 200 },
                        StackWalkEventIdsFilterIn = true,
                        FilterStackWalkLevelKeyword = true,
                        StackWalkFilterIn = true,
                        StackWalkLevel = 123,
                        StackWalkMatchAnyKeyword = 0xFFEEDDCCBBAA9988UL,
                        StackWalkMatchAllKeyword = 0x7766554433221100UL,

                        Manifest = @"z:\etw.man",
                        StartupProjects = { @"z:\project1.vcxproj", @"z:\project2.vcxproj" },
                    }
                };
                yield return new object[] {
                    new EventProvider {
                        Id = new Guid("D62FA0B3-2DF5-48F7-B1C8-48084620DFA2"),
                        IsEnabled = true,

                        Level = 123,
                        MatchAnyKeyword = 0xFFEEDDCCBBAA9988UL,
                        MatchAllKeyword = 0x7766554433221100UL,

                        IncludeSecurityId = true,
                        IncludeTerminalSessionId = true,
                        IncludeStackTrace = true,

                        FilterExecutableNames = false,
                        ExecutableNames = { "project1.exe", "project2.exe" },

                        FilterProcessIds = false,
                        ProcessIds = { 1, 2 },

                        FilterEventIds = false,
                        EventIdsFilterIn = true,
                        EventIds = { 10, 20 },

                        FilterStackWalkEventIds = false,
                        StackWalkEventIds = { 100, 200 },
                        StackWalkEventIdsFilterIn = true,
                        FilterStackWalkLevelKeyword = false,
                        StackWalkFilterIn = true,
                        StackWalkLevel = 123,
                        StackWalkMatchAnyKeyword = 0xFFEEDDCCBBAA9988UL,
                        StackWalkMatchAllKeyword = 0x7766554433221100UL,

                        Manifest = @"z:\etw.man",
                        StartupProjects = { @"z:\project1.vcxproj", @"z:\project2.vcxproj" },
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(TraceProfileData))]
        public void GetDescriptor_TraceProfile(TraceProfile expected)
        {
            var actual = TraceProfileUtils.GetDescriptor(expected);
            VerifyEqual(expected, actual);
        }

        [Theory]
        [MemberData(nameof(EventCollectorData))]
        public void GetDescriptor_EventCollector(EventCollector expected)
        {
            var actual = TraceProfileUtils.GetDescriptor(expected);
            VerifyEqual(expected, actual);
        }

        [Theory]
        [MemberData(nameof(EventProviderData))]
        public void GetDescriptor_EventProvider(EventProvider expected)
        {
            var actual = TraceProfileUtils.GetDescriptor(expected);
            VerifyEqual(expected, actual);
        }

        [Fact]
        public void GetDescriptor_ThrowsForDisabledEventProvider()
        {
            var provider = new EventProvider {
                Id = new Guid("E436A7BC-76A4-4226-9BA1-A3EF2E977C55"),
                IsEnabled = false,
                Manifest = @"z:\etw.man",
            };
            var ex = Assert.Throws<InvalidOperationException>(() => TraceProfileUtils.GetDescriptor(provider));
            Assert.Contains("disabled", ex.Message);
        }

        private void VerifyEqual(TraceProfile expected, TraceProfileDescriptor actual)
        {
            Assert.Equal(expected.Collectors.Count, actual.Collectors.Count);
            for (int i = 0; i < expected.Collectors.Count; ++i) {
                VerifyEqual((dynamic)expected.Collectors[i], (dynamic)actual.Collectors[i]);
            }
        }

        private void VerifyEqual(EventCollector expected, EventCollectorDescriptor actual)
        {
            // "expected.Name" is not mapped
            Assert.Equal(expected.BufferSize, actual.BufferSize);
            Assert.Equal(expected.MinimumBuffers, actual.MinimumBuffers);
            Assert.Equal(expected.MaximumBuffers, actual.MaximumBuffers);
            Assert.Equal(expected.LogFileName, actual.LogFileName);
            Assert.Equal(expected.FlushPeriod, actual.FlushPeriod);

            Assert.Equal(expected.Providers.Count, actual.Providers.Count);
            for (int i = 0; i < expected.Providers.Count; ++i)
                VerifyEqual(expected.Providers[i], actual.Providers[i]);
        }

        private void VerifyEqual(EventProvider expected, EventProviderDescriptor actual)
        {
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Level, actual.Level);
            Assert.Equal(expected.MatchAnyKeyword, actual.MatchAnyKeyword);
            Assert.Equal(expected.MatchAllKeyword, actual.MatchAllKeyword);

            Assert.Equal(expected.IncludeSecurityId, actual.IncludeSecurityId);
            Assert.Equal(expected.IncludeTerminalSessionId, actual.IncludeTerminalSessionId);
            Assert.Equal(expected.IncludeStackTrace, actual.IncludeStackTrace);

            if (expected.FilterExecutableNames) {
                Assert.Equal(string.Join(";", expected.ExecutableNames), actual.ExecutableName);
            } else {
                Assert.Null(actual.ExecutableName);
            }

            if (expected.FilterProcessIds) {
                Assert.Equal(expected.ProcessIds, actual.ProcessIds);
            } else {
                Assert.Null(actual.ProcessIds);
            }

            if (expected.FilterEventIds) {
                Assert.Equal(expected.EventIds, actual.EventIds);
                Assert.Equal(expected.EventIdsFilterIn, actual.EventIdsFilterIn);
            } else {
                Assert.Null(actual.EventIds);
            }

            if (expected.FilterStackWalkEventIds) {
                Assert.Equal(expected.StackWalkEventIds, actual.StackWalkEventIds);
                Assert.Equal(expected.StackWalkEventIdsFilterIn, actual.StackWalkEventIdsFilterIn);
            } else {
                Assert.Null(actual.StackWalkEventIds);
            }

            Assert.Equal(expected.FilterStackWalkLevelKeyword, actual.FilterStackWalkLevelKeyword);
            if (expected.FilterStackWalkEventIds) {
                Assert.Equal(expected.StackWalkFilterIn, actual.StackWalkFilterIn);
                Assert.Equal(expected.StackWalkLevel, actual.StackWalkLevel);
                Assert.Equal(expected.StackWalkMatchAnyKeyword, actual.StackWalkMatchAnyKeyword);
                Assert.Equal(expected.StackWalkMatchAllKeyword, actual.StackWalkMatchAllKeyword);
            } else {
                Assert.Equal(expected.StackWalkFilterIn, actual.StackWalkFilterIn);
                Assert.Equal(0, actual.StackWalkLevel);
                Assert.Equal(0ul, actual.StackWalkMatchAnyKeyword);
                Assert.Equal(0ul, actual.StackWalkMatchAllKeyword);
            }

            Assert.Equal(expected.Manifest, actual.Manifest);
            Assert.Equal(expected.StartupProjects, actual.StartupProjects);
        }
    }
}
