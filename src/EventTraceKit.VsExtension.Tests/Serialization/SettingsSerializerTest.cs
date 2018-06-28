namespace EventTraceKit.VsExtension.Tests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Xml.Linq;
    using EventTraceKit.VsExtension.Controls;
    using EventTraceKit.VsExtension.Settings.Persistence;
    using EventTraceKit.VsExtension.Views;
    using VsExtension.Serialization;
    using Xunit;
    using Xunit.Abstractions;

    public class SettingsSerializerTest
    {
        private readonly ITestOutputHelper output;

        public SettingsSerializerTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void SerializeViewPreset()
        {
            var preset = new AsyncDataViewModelPreset();
            preset.Name = "Foo Preset";
            preset.ConfigurableColumns.Add(new ColumnViewModelPreset {
                Id = new Guid("C6B4A55A-72BB-4235-8F3C-5EDABC4DBA52"),
                Name = "Id",
                Width = 50,
                IsVisible = true,
                TextAlignment = TextAlignment.Right
            });
            preset.ConfigurableColumns.Add(new ColumnViewModelPreset {
                Id = new Guid("B6875CD8-AE93-4DC7-943D-8406FE406073"),
                Name = "Name",
                Width = 142,
                CellFormat = "F0",
                IsVisible = true,
            });

            var collection = new AdvmPresetCollection();
            collection.UserPresets.Add(preset);

            var stream = new MemoryStream();
            new SettingsSerializer().Save(collection, stream);

            var doc = XDocument.Parse(stream.ReadFullyAsString());
            output.WriteLine(doc.ToString());
        }

        [Fact]
        public void SerializeTraceProfile()
        {
            var provider = new EventProviderViewModel(
                new Guid("37D34B87-80A6-44CE-90BB-1C3D6EDB0784"), "MyProvider");

            var collector = new EventCollectorViewModel {
                Name = "MyCollector",
                BufferSize = 64,
                MinimumBuffers = 23,
                MaximumBuffers = 42,
                LogFileName = @"z:\path\to\logfile.etl",
                FlushPeriod = 123,
                Providers = { provider }
            };

            var profile = new TraceProfileViewModel(new Guid("5E25CAF0-1A7B-4ECA-9031-DAE0FCAEB3E1")) {
                Name = "MyProfile",
                Collectors = { collector }
            };

            var stream = new MemoryStream();
            new SettingsSerializer().Save(profile, stream);

            var doc = XDocument.Parse(stream.ReadFullyAsString());
            output.WriteLine(doc.ToString());

            stream.Position = 0;
            var actualProfile = new SettingsSerializer().Load<TraceProfileViewModel>(stream);

            Assert.NotNull(actualProfile);
            Assert.Equal(profile.Id, actualProfile.Id);
            Assert.Equal(profile.Name, actualProfile.Name);
            Assert.Equal(profile.Collectors.Count, actualProfile.Collectors.Count);

            var actualCollector = Assert.IsType<EventCollectorViewModel>(actualProfile.Collectors[0]);
            Assert.Equal(collector.Name, actualCollector.Name);
            Assert.Equal(collector.BufferSize, actualCollector.BufferSize);
            Assert.Equal(collector.MinimumBuffers, actualCollector.MinimumBuffers);
            Assert.Equal(collector.MaximumBuffers, actualCollector.MaximumBuffers);
            Assert.Equal(collector.LogFileName, actualCollector.LogFileName);
            Assert.Equal(collector.FlushPeriod, actualCollector.FlushPeriod);
            Assert.Equal(collector.Providers.Count, actualCollector.Providers.Count);

            var actualProvider = actualCollector.Providers[0];
            Assert.Equal(provider, actualProvider, new DelegateComparer<EventProviderViewModel>(Equals));
        }

        private static bool Equals(EventProviderViewModel x, EventProviderViewModel y)
        {
            return
                x.Id == y.Id &&
                x.Name == y.Name &&
                x.IsEnabled == y.IsEnabled &&
                x.Level == y.Level &&
                x.MatchAnyKeyword == y.MatchAnyKeyword &&
                x.MatchAllKeyword == y.MatchAllKeyword &&
                x.IncludeSecurityId == y.IncludeSecurityId &&
                x.IncludeTerminalSessionId == y.IncludeTerminalSessionId &&
                x.IncludeStackTrace == y.IncludeStackTrace &&
                x.Manifest == y.Manifest &&
                x.FilterProcessIds == y.FilterProcessIds &&
                x.ProcessIds == y.ProcessIds &&
                x.FilterEventIds == y.FilterEventIds &&
                x.EventIds == y.EventIds &&
                x.EventIdsFilterIn == y.EventIdsFilterIn &&
                x.StartupProject == y.StartupProject;
        }

        [Fact]
        public void SerializeEventCollector()
        {
            var provider = new EventProviderViewModel(
                new Guid("37D34B87-80A6-44CE-90BB-1C3D6EDB0784"), "Bar");

            var collector = new EventCollectorViewModel {
                Name = "Foo",
                BufferSize = 64,
                MinimumBuffers = 23,
                MaximumBuffers = 42,
                LogFileName = @"z:\path\to\logfile.etl",
                FlushPeriod = 1500,
                Providers = { provider }
            };

            var stream = new MemoryStream();
            new SettingsSerializer().Save(collector, stream);

            var doc = XDocument.Parse(stream.ReadFullyAsString());
            Assert.Equal("EventCollector", doc.Root.Name.LocalName);
            Assert.Equal(6, doc.Root.NonXmlnsAttributes().Count());
            Assert.Equal("Foo", doc.Root.Attribute("Name")?.Value);
            Assert.Equal("64", doc.Root.Attribute("BufferSize")?.Value);
            Assert.Equal("23", doc.Root.Attribute("MinimumBuffers")?.Value);
            Assert.Equal("42", doc.Root.Attribute("MaximumBuffers")?.Value);
            Assert.Equal("00:00:01.5000000", doc.Root.Attribute("FlushPeriod")?.Value);
            Assert.Equal(@"z:\path\to\logfile.etl", doc.Root.Attribute("LogFileName")?.Value);

            stream.Position = 0;
            var deserialized = new SettingsSerializer().Load<EventCollectorViewModel>(stream);

            Assert.Equal(collector.Name, deserialized.Name);
            Assert.Equal(collector.BufferSize, deserialized.BufferSize);
            Assert.Equal(collector.MinimumBuffers, deserialized.MinimumBuffers);
            Assert.Equal(collector.MaximumBuffers, deserialized.MaximumBuffers);
            Assert.Equal(collector.LogFileName, deserialized.LogFileName);
            Assert.Equal(collector.FlushPeriod, deserialized.FlushPeriod);
        }

        [Fact]
        public void SerializeEventProvider()
        {
            var source = new EventProviderViewModel {
                Id = new Guid("EC700760-3FAE-48AD-8110-E4AE77C69F85"),
                Name = "Foo",
                IsEnabled = true,

                Level = 0xFF,
                MatchAnyKeyword = 0xFEDC,
                MatchAllKeyword = 0xBA98,
                IncludeSecurityId = true,
                IncludeTerminalSessionId = true,
                IncludeStackTrace = true,

                FilterExecutableNames = true,
                ExecutableNames = "foo.exe",

                FilterProcessIds = true,
                ProcessIds = "23,42",

                FilterEventIds = true,
                EventIdsFilterIn = false,
                EventIds = "12,34",

                Manifest = @"z:\path\to\manifest.man",
                StartupProject = "z:\\foo.csproj"
            };

            var stream = new MemoryStream();
            new SettingsSerializer().Save(source, stream);

            var ns = XNamespace.Get("urn:schemas-eventtracekit:settings");
            var s = XNamespace.Get("clr-namespace:System;assembly=mscorlib");
            var xaml = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");

            var doc = XDocument.Parse(stream.ReadFullyAsString());
            output.WriteLine(doc.ToString());
            Assert.Equal(ns + "EventProvider", doc.Root.Name);
            Assert.Equal(14, doc.Root.NonXmlnsAttributes().Count());
            Assert.Equal("ec700760-3fae-48ad-8110-e4ae77c69f85", doc.Root.Attribute("Id")?.Value);
            Assert.Equal("Foo", doc.Root.Attribute("Name")?.Value);
            Assert.Equal(@"z:\path\to\manifest.man", doc.Root.Attribute("Manifest")?.Value);
            Assert.Equal("True", doc.Root.Attribute("IsEnabled")?.Value);
            Assert.Equal("255", doc.Root.Attribute("Level")?.Value);
            Assert.Equal("65244", doc.Root.Attribute("MatchAnyKeyword")?.Value);
            Assert.Equal("47768", doc.Root.Attribute("MatchAllKeyword")?.Value);
            Assert.Equal("True", doc.Root.Attribute("IncludeSecurityId")?.Value);
            Assert.Equal("True", doc.Root.Attribute("IncludeTerminalSessionId")?.Value);
            Assert.Equal("True", doc.Root.Attribute("IncludeStackTrace")?.Value);
            Assert.Equal("True", doc.Root.Attribute("FilterExecutableNames")?.Value);
            Assert.Equal("True", doc.Root.Attribute("FilterProcessIds")?.Value);
            Assert.Equal("True", doc.Root.Attribute("FilterEventIds")?.Value);
            Assert.Equal("False", doc.Root.Attribute("EventIdsFilterIn")?.Value);

            var eventIdsElem = doc.Root.Element(ns + "EventProvider.EventIds");
            Assert.NotNull(eventIdsElem);
            Assert.Equal(2, eventIdsElem.Elements().Count());
            Assert.Equal(new[] { "12", "34" }, eventIdsElem.Elements(s + "UInt16").Select(x => x.Value).ToList());

            var processIds = doc.Root.Element(ns + "EventProvider.ProcessIds");
            Assert.NotNull(processIds);
            Assert.Equal(2, processIds.Elements().Count());
            Assert.Equal(new[] { "23", "42" }, processIds.Elements(s + "UInt32").Select(x => x.Value).ToList());

            var exeNamesElem = doc.Root.Element(ns + "EventProvider.ExecutableNames");
            Assert.NotNull(exeNamesElem);
            Assert.Equal(new[] { "foo.exe" }, exeNamesElem.Elements(xaml + "String").Select(x => x.Value).ToList());

            var startupProjectsElem = doc.Root.Element(ns + "EventProvider.StartupProjects");
            Assert.NotNull(startupProjectsElem);
            Assert.Equal(new[] { "z:\\foo.csproj" }, startupProjectsElem.Elements(xaml + "String").Select(x => x.Value).ToList());

            stream.Position = 0;
            var deserialized = new SettingsSerializer().Load<EventProviderViewModel>(stream);

            Assert.Equal(source.Id, deserialized.Id);
            Assert.Equal(source.Name, deserialized.Name);
            Assert.Equal(source.IsEnabled, deserialized.IsEnabled);
            Assert.Equal(source.Level, deserialized.Level);
            Assert.Equal(source.MatchAnyKeyword, deserialized.MatchAnyKeyword);
            Assert.Equal(source.MatchAllKeyword, deserialized.MatchAllKeyword);

            Assert.Equal(source.IncludeSecurityId, deserialized.IncludeSecurityId);
            Assert.Equal(source.IncludeTerminalSessionId, deserialized.IncludeTerminalSessionId);
            Assert.Equal(source.IncludeStackTrace, deserialized.IncludeStackTrace);

            Assert.Equal(source.Manifest, deserialized.Manifest);

            Assert.Equal(source.FilterExecutableNames, deserialized.FilterExecutableNames);
            Assert.Equal(source.ExecutableNames, deserialized.ExecutableNames);
            Assert.Equal(source.FilterProcessIds, deserialized.FilterProcessIds);
            Assert.Equal(source.ProcessIds, deserialized.ProcessIds);
            Assert.Equal(source.FilterEventIds, deserialized.FilterEventIds);
            Assert.Equal(source.EventIds, deserialized.EventIds);
            Assert.Equal(source.EventIdsFilterIn, deserialized.EventIdsFilterIn);
        }

        [Fact]
        public void SerializeEventProviders()
        {
            var serializer = new SettingsSerializer();
            var provider1 = new EventProviderViewModel(new Guid("11111111-1111-1111-1111-111111111111"), "Provider1");
            var provider2 = new EventProviderViewModel(new Guid("22222222-2222-2222-2222-222222222222"), "Provider2");

            var deserialized = RoundtripMultiple(serializer, provider1, provider2);

            Assert.Equal(2, deserialized.Count);
            Assert.Equal(provider1.Name, deserialized[0].Name);
            Assert.Equal(provider2.Name, deserialized[1].Name);
        }

        [Fact]
        public void MapEventProviderViewModel()
        {
            var expected = new EventProviderViewModel {
                Id = new Guid("B3E63165-B450-4669-9EC6-D582C88AD37F"),
                Name = "MyProvider",
                IsEnabled = true,

                Level = 123,
                MatchAnyKeyword = 0xFFEEDDCCBBAA9988UL,
                MatchAllKeyword = 0x7766554433221100UL,

                IncludeSecurityId = true,
                IncludeTerminalSessionId = true,
                IncludeStackTrace = true,

                FilterExecutableNames = true,
                ExecutableNames = "project1.exe,project2.exe",

                FilterProcessIds = true,
                ProcessIds = "1, 2",

                FilterEventIds = true,
                EventIds = "10, 20",
                EventIdsFilterIn = true,

                Manifest = @"z:\etw.man",
                StartupProject = @"z:\project1.vcxproj",
            };

            var mapper = SettingsSerializer.Mapper;
            var actual = mapper.Map<EventProvider>(expected);

            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Level, actual.Level);
            Assert.Equal(expected.MatchAnyKeyword, actual.MatchAnyKeyword);
            Assert.Equal(expected.MatchAllKeyword, actual.MatchAllKeyword);

            Assert.Equal(expected.IncludeSecurityId, actual.IncludeSecurityId);
            Assert.Equal(expected.IncludeTerminalSessionId, actual.IncludeTerminalSessionId);
            Assert.Equal(expected.IncludeStackTrace, actual.IncludeStackTrace);

            Assert.Equal(new[] { "project1.exe", "project2.exe" }, actual.ExecutableNames);
            Assert.Equal(new uint[] { 1, 2 }, actual.ProcessIds);
            Assert.Equal(new ushort[] { 10, 20 }, actual.EventIds);
            Assert.Equal(expected.EventIdsFilterIn, actual.EventIdsFilterIn);

            //Assert.Equal(expected.FilterStackWalkEventIds, actual.FilterStackWalkEventIds);
            //Assert.Equal(expected.StackWalkEventIds, actual.StackWalkEventIds);
            //Assert.Equal(expected.StackWalkEventIdsFilterIn, actual.StackWalkEventIdsFilterIn);
            //Assert.Equal(expected.FilterStackWalkLevelKeyword, actual.FilterStackWalkLevelKeyword);
            //Assert.Equal(expected.StackWalkFilterIn, actual.StackWalkFilterIn);
            //Assert.Equal(expected.StackWalkLevel, actual.StackWalkLevel);
            //Assert.Equal(expected.StackWalkMatchAnyKeyword, actual.StackWalkMatchAnyKeyword);
            //Assert.Equal(expected.StackWalkMatchAllKeyword, actual.StackWalkMatchAllKeyword);

            Assert.Equal(expected.Manifest, actual.Manifest);
            Assert.Equal(new[] { expected.StartupProject }, actual.StartupProjects);
        }

        [Fact]
        public void MapCommaSeparatesValuesToCollection()
        {
            var mapper = SettingsSerializer.Mapper;
            Assert.Null(mapper.Map<Collection<ushort>>(null));
            Assert.Equal(new ushort[0], mapper.Map<Collection<ushort>>(""));
            Assert.Equal(new ushort[] { 10, 20 }, mapper.Map<Collection<ushort>>("10, 20"));
            Assert.Equal(new[] { 1, 2, -3 }, mapper.Map<Collection<int>>("1,2,-3"));
        }

        [Fact]
        public void MapCollectionToCommaSeparatesValues()
        {
            var mapper = SettingsSerializer.Mapper;
            Assert.Null(mapper.Map<string>(null));
            Assert.Null(mapper.Map<string>(new ushort[0]));
            Assert.Equal("10,20", mapper.Map<string>(new ushort[] { 10, 20 }));
            Assert.Equal("1,2,-3", mapper.Map<string>(new[] { 1, 2, -3 }));
        }

        [Fact]
        public void MapTraceProfileViewModel()
        {
            var provider = new EventProviderViewModel {
                Id = new Guid("B3E63165-B450-4669-9EC6-D582C88AD37F"),
                Name = "MyProvider",
                IsEnabled = true,

                Level = 123,
                MatchAnyKeyword = 0xFFEEDDCCBBAA9988UL,
                MatchAllKeyword = 0x7766554433221100UL,

                IncludeSecurityId = true,
                IncludeTerminalSessionId = true,
                IncludeStackTrace = true,

                FilterExecutableNames = true,
                ExecutableNames = "project1.exe,project2.exe",

                FilterProcessIds = true,
                ProcessIds = "1, 2",

                FilterEventIds = true,
                EventIds = "10, 20",
                EventIdsFilterIn = true,

                Manifest = @"z:\etw.man",
                StartupProject = @"z:\project1.vcxproj",
            };

            var collector = new EventCollectorViewModel {
                Name = "MyCollector",
                BufferSize = 64,
                MinimumBuffers = 23,
                MaximumBuffers = 42,
                LogFileName = @"z:\path\to\logfile.etl",
                FlushPeriod = 123,
                Providers = { provider }
            };

            var profile = new TraceProfileViewModel(new Guid("5E25CAF0-1A7B-4ECA-9031-DAE0FCAEB3E1")) {
                Name = "MyProfile",
                Collectors = { collector }
            };

            var mapper = SettingsSerializer.Mapper;
            var actualProfile = mapper.Map<TraceProfile>(profile);

            Assert.Equal(profile.Id, actualProfile.Id);
            Assert.Equal(profile.Name, actualProfile.Name);
            Assert.Equal(profile.Collectors.Count, actualProfile.Collectors.Count);

            var actualCollector = Assert.IsType<EventCollector>(actualProfile.Collectors[0]);
            Assert.Equal(collector.Name, actualCollector.Name);
            Assert.Equal(collector.BufferSize, actualCollector.BufferSize);
            Assert.Equal(collector.MinimumBuffers, actualCollector.MinimumBuffers);
            Assert.Equal(collector.MaximumBuffers, actualCollector.MaximumBuffers);
            Assert.Equal(collector.LogFileName, actualCollector.LogFileName);
            Assert.Equal(TimeSpan.FromMilliseconds(collector.FlushPeriod.Value), actualCollector.FlushPeriod);
            Assert.Equal(collector.Providers.Count, actualCollector.Providers.Count);

            var actualProvider = actualCollector.Providers[0];
            Assert.Equal(provider.Id, actualProvider.Id);
            Assert.Equal(provider.Level, actualProvider.Level);
            Assert.Equal(provider.MatchAnyKeyword, actualProvider.MatchAnyKeyword);
            Assert.Equal(provider.MatchAllKeyword, actualProvider.MatchAllKeyword);

            Assert.Equal(provider.IncludeSecurityId, actualProvider.IncludeSecurityId);
            Assert.Equal(provider.IncludeTerminalSessionId, actualProvider.IncludeTerminalSessionId);
            Assert.Equal(provider.IncludeStackTrace, actualProvider.IncludeStackTrace);

            Assert.Equal(new[] { "project1.exe", "project2.exe" }, actualProvider.ExecutableNames);
            Assert.Equal(new uint[] { 1, 2 }, actualProvider.ProcessIds);
            Assert.Equal(new ushort[] { 10, 20 }, actualProvider.EventIds);
            Assert.Equal(provider.EventIdsFilterIn, actualProvider.EventIdsFilterIn);

            //Assert.Equal(expected.FilterStackWalkEventIds, actual.FilterStackWalkEventIds);
            //Assert.Equal(expected.StackWalkEventIds, actual.StackWalkEventIds);
            //Assert.Equal(expected.StackWalkEventIdsFilterIn, actual.StackWalkEventIdsFilterIn);
            //Assert.Equal(expected.FilterStackWalkLevelKeyword, actual.FilterStackWalkLevelKeyword);
            //Assert.Equal(expected.StackWalkFilterIn, actual.StackWalkFilterIn);
            //Assert.Equal(expected.StackWalkLevel, actual.StackWalkLevel);
            //Assert.Equal(expected.StackWalkMatchAnyKeyword, actual.StackWalkMatchAnyKeyword);
            //Assert.Equal(expected.StackWalkMatchAllKeyword, actual.StackWalkMatchAllKeyword);

            Assert.Equal(provider.Manifest, actualProvider.Manifest);
            Assert.Equal(new[] { provider.StartupProject }, actualProvider.StartupProjects);
        }

        [Fact]
        public void MapViewPreset()
        {
            var preset = new AsyncDataViewModelPreset {
                Name = "Foo",
                ConfigurableColumns = {
                    new ColumnViewModelPreset {
                        Id = new Guid("C6B4A55A-72BB-4235-8F3C-5EDABC4DBA52"),
                        Name = "Col",
                        Width = 123,
                        CellFormat = "F0",
                        IsVisible = true,
                        TextAlignment = TextAlignment.Right
                    }
                }
            };

            var mapper = SettingsSerializer.Mapper;
            var serialized = mapper.Map<ViewPreset>(preset);
            var unserialized = mapper.Map<AsyncDataViewModelPreset>(serialized);

            Assert.Equal(preset.Name, serialized.Name);
            Assert.Equal(preset.LeftFrozenColumnCount, serialized.LeftFrozenColumnCount);
            Assert.Equal(preset.RightFrozenColumnCount, serialized.RightFrozenColumnCount);
            Assert.Equal(preset.ConfigurableColumns.Count, serialized.Columns.Count);

            Assert.Equal(preset.Name, unserialized.Name);
            Assert.Equal(preset.LeftFrozenColumnCount, unserialized.LeftFrozenColumnCount);
            Assert.Equal(preset.RightFrozenColumnCount, unserialized.RightFrozenColumnCount);
            Assert.Equal(preset.ConfigurableColumns.Count, unserialized.ConfigurableColumns.Count);
            Assert.Equal(preset.ConfigurableColumns, unserialized.ConfigurableColumns);
        }

        [Fact]
        public void MapViewPresetCollection()
        {
            var preset = new AsyncDataViewModelPreset {
                Name = "Foo",
                ConfigurableColumns = {
                    new ColumnViewModelPreset {
                        Id = new Guid("C6B4A55A-72BB-4235-8F3C-5EDABC4DBA52"),
                        Name = "Col",
                        Width = 123,
                        CellFormat = "F0",
                        IsVisible = true,
                        TextAlignment = TextAlignment.Right
                    }
                }
            };
            var presets = new AdvmPresetCollection();
            presets.PersistedPresets.Add(preset);

            var mapper = SettingsSerializer.Mapper;
            var serialized = mapper.Map<ViewPreset>(preset);
            var unserialized = mapper.Map<AsyncDataViewModelPreset>(serialized);

            Assert.Equal(preset.Name, serialized.Name);
            Assert.Equal(preset.LeftFrozenColumnCount, serialized.LeftFrozenColumnCount);
            Assert.Equal(preset.RightFrozenColumnCount, serialized.RightFrozenColumnCount);
            Assert.Equal(preset.ConfigurableColumns.Count, serialized.Columns.Count);

            Assert.Equal(preset.Name, unserialized.Name);
            Assert.Equal(preset.LeftFrozenColumnCount, unserialized.LeftFrozenColumnCount);
            Assert.Equal(preset.RightFrozenColumnCount, unserialized.RightFrozenColumnCount);
            Assert.Equal(preset.ConfigurableColumns.Count, unserialized.ConfigurableColumns.Count);
            Assert.Equal(preset.ConfigurableColumns, unserialized.ConfigurableColumns);
        }

        private static IReadOnlyList<T> RoundtripMultiple<T>(
            SettingsSerializer serializer, params T[] inputs)
        {
            var stream = new MemoryStream();
            serializer.Save(inputs, stream);

            stream.Position = 0;
            return serializer.LoadMultiple<T>(stream);
        }
    }
}
