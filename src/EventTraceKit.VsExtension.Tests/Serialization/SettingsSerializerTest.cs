namespace EventTraceKit.VsExtension.Tests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Xml.Linq;
    using Controls;
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
        public void SerializeTraceSettings()
        {
            var provider = new EventProviderViewModel(
                new Guid("37D34B87-80A6-44CE-90BB-1C3D6EDB0784"), "MyProvider");

            var collector = new EventCollectorViewModel {
                Id = new Guid("12133C31-FC77-4210-91A0-1EAFCE2B3537"),
                Name = "MyCollector",
                BufferSize = 64,
                MinimumBuffers = 23,
                MaximumBuffers = 42,
                LogFileName = @"z:\path\to\logfile.etl",
                Providers = { provider }
            };

            var profile = new TraceProfileViewModel {
                Id = new Guid("5E25CAF0-1A7B-4ECA-9031-DAE0FCAEB3E1"),
                Name = "MyProfile",
                Collectors = { collector }
            };

            var settings = new TraceSettingsViewModel {
                Profiles = { profile }
            };

            var stream = new MemoryStream();
            new SettingsSerializer().Save(settings, stream);

            var doc = XDocument.Parse(stream.ReadFullyAsString());
            output.WriteLine(doc.ToString());

            stream.Position = 0;
            var actualSettings = new SettingsSerializer().Load<TraceSettingsViewModel>(stream);

            Assert.NotNull(actualSettings);
            Assert.Equal(settings.Profiles.Count, actualSettings.Profiles.Count);

            var actualProfile = actualSettings.Profiles[0];
            Assert.NotNull(actualSettings);
            Assert.Equal(profile.Id, actualProfile.Id);
            Assert.Equal(profile.Name, actualProfile.Name);
            Assert.Equal(profile.Collectors.Count, actualProfile.Collectors.Count);

            var actualCollector = Assert.IsType<EventCollectorViewModel>(actualProfile.Collectors[0]);
            Assert.Equal(collector.Name, actualCollector.Name);
            Assert.Equal(collector.BufferSize, actualCollector.BufferSize);
            Assert.Equal(collector.MinimumBuffers, actualCollector.MinimumBuffers);
            Assert.Equal(collector.MaximumBuffers, actualCollector.MaximumBuffers);
            Assert.Equal(collector.LogFileName, actualCollector.LogFileName);
            Assert.Equal(collector.LogFileName, actualCollector.LogFileName);
            Assert.Equal(collector.Providers.Count, actualCollector.Providers.Count);

            var actualProvider = actualCollector.Providers[0];
            Assert.Equal(provider, actualProvider, new DelegateComparer<EventProviderViewModel>(Equals));
        }

        [Fact]
        public void SerializeTraceSettings2()
        {
            var profile = new TraceProfileViewModel {
                Name = "MyProfile"
            };

            var settings = new TraceSettingsViewModel {
                Profiles = { profile }
            };

            var stream = new MemoryStream();
            new SettingsSerializer().Save(settings, stream);

            var doc = XDocument.Parse(stream.ReadFullyAsString());
            output.WriteLine(doc.ToString());

            stream.Position = 0;
            var actualSettings = new SettingsSerializer().Load<TraceSettingsViewModel>(stream);

            Assert.NotNull(actualSettings);
            Assert.Equal(settings.Profiles.Count, actualSettings.Profiles.Count);

            var actualProfile = actualSettings.Profiles[0];
            Assert.NotNull(actualSettings);
            Assert.Equal(profile.Id, actualProfile.Id);
            Assert.Equal(profile.Name, actualProfile.Name);
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
                x.EnableEvents == y.EnableEvents &&
                x.StartupProject == y.StartupProject;
        }

        private class DelegateComparer<T> : IEqualityComparer<T>
        {
            private readonly Func<T, T, bool> comparer;
            private readonly Func<T, int> hasher;

            public DelegateComparer(Func<T, T, bool> comparer, Func<T, int> hasher = null)
            {
                this.comparer = comparer;
                this.hasher = hasher;
            }

            public bool Equals(T x, T y)
            {
                return comparer(x, y);
            }

            public int GetHashCode(T obj)
            {
                return hasher != null ? hasher(obj) : 0;
            }
        }

        [Fact]
        public void SerializeEventCollector()
        {
            var collector = new EventCollectorViewModel();
            collector.Id = new Guid("12133C31-FC77-4210-91A0-1EAFCE2B3537");
            collector.Name = "Foo";
            collector.BufferSize = 64;
            collector.MinimumBuffers = 23;
            collector.MaximumBuffers = 42;
            collector.LogFileName = @"z:\path\to\logfile.etl";

            var provider = new EventProviderViewModel(
                new Guid("37D34B87-80A6-44CE-90BB-1C3D6EDB0784"), "Bar");
            collector.Providers.Add(provider);

            var stream = new MemoryStream();
            new SettingsSerializer().Save(collector, stream);

            var doc = XDocument.Parse(stream.ReadFullyAsString());
            Assert.Equal("EventCollector", doc.Root.Name.LocalName);
            Assert.Equal(6, doc.Root.NonXmlnsAttributes().Count());
            Assert.Equal("Foo", doc.Root.Attribute("Name")?.Value);
            Assert.Equal("64", doc.Root.Attribute("BufferSize")?.Value);
            Assert.Equal("23", doc.Root.Attribute("MinimumBuffers")?.Value);
            Assert.Equal("42", doc.Root.Attribute("MaximumBuffers")?.Value);
            Assert.Equal(@"z:\path\to\logfile.etl", doc.Root.Attribute("LogFileName")?.Value);

            stream.Position = 0;
            var deserialized = new SettingsSerializer().Load<EventCollectorViewModel>(stream);

            Assert.Equal(collector.Name, deserialized.Name);
            Assert.Equal(collector.BufferSize, deserialized.BufferSize);
            Assert.Equal(collector.MinimumBuffers, deserialized.MinimumBuffers);
            Assert.Equal(collector.MaximumBuffers, deserialized.MaximumBuffers);
            Assert.Equal(collector.LogFileName, deserialized.LogFileName);
            Assert.Equal(collector.LogFileName, deserialized.LogFileName);
        }

        [Fact]
        public void SerializeEventProvider()
        {
            var source = new EventProviderViewModel(
                new Guid("EC700760-3FAE-48AD-8110-E4AE77C69F85"), "Foo");
            source.Manifest = @"z:\path\to\manifest.man";
            source.IsEnabled = true;
            source.Level = 0xFF;
            source.MatchAnyKeyword = 0xFEDC;
            source.MatchAllKeyword = 0xBA98;
            source.IncludeSecurityId = true;
            source.IncludeTerminalSessionId = true;
            source.IncludeStackTrace = true;
            source.FilterExecutableNames = true;
            source.ExecutableNames = "foo.exe";
            source.FilterProcessIds = true;
            source.ProcessIds = "23,42";
            source.FilterEventIds = true;
            source.EventIds = "12,34";
            source.EnableEvents = false;
            source.StartupProject = "z:\\foo.csproj";

            var stream = new MemoryStream();
            new SettingsSerializer().Save(source, stream);

            var doc = XDocument.Parse(stream.ReadFullyAsString());
            output.WriteLine(doc.ToString());
            Assert.Equal("EventProvider", doc.Root.Name.LocalName);
            Assert.Equal(15, doc.Root.NonXmlnsAttributes().Count());
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
            Assert.Equal("False", doc.Root.Attribute("EnableEvents")?.Value);
            Assert.Equal("z:\\foo.csproj", doc.Root.Attribute("StartupProject")?.Value);

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
            //Assert.Equal(source.ExecutableNames, deserialized.ExecutableNames);
            Assert.Equal(source.FilterProcessIds, deserialized.FilterProcessIds);
            Assert.Equal(source.ProcessIds, deserialized.ProcessIds);
            Assert.Equal(source.FilterEventIds, deserialized.FilterEventIds);
            Assert.Equal(source.EventIds, deserialized.EventIds);
            Assert.Equal(source.EnableEvents, deserialized.EnableEvents);
        }

        [Fact]
        public void SerializeEventProviders()
        {
            var serializer = new SettingsSerializer();
            var stream = new MemoryStream();

            var provider1 = new EventProviderViewModel(new Guid("11111111-1111-1111-1111-111111111111"), "Provider1");
            var provider2 = new EventProviderViewModel(new Guid("22222222-2222-2222-2222-222222222222"), "Provider2");
            serializer.Save(new[] { provider1, provider2 }, stream);

            stream.Position = 0;
            var deserialized = serializer.LoadMultiple<EventProviderViewModel>(stream);
            Assert.Equal(2, deserialized.Count);
            Assert.Equal(provider1.Name, deserialized[0].Name);
            Assert.Equal(provider2.Name, deserialized[1].Name);
        }

        [Fact]
        public void Foo()
        {
            var serializer = new SettingsSerializer();

            var preset = new AsyncDataViewModelPreset();
            preset.Name = "Foo";
            preset.ConfigurableColumns.Add(new ColumnViewModelPreset {
                Id = new Guid("C6B4A55A-72BB-4235-8F3C-5EDABC4DBA52"),
                Name = "Col",
                Width = 123,
                CellFormat = "F0",
                IsVisible = true,
                TextAlignment = TextAlignment.Right
            });

            var stream = new MemoryStream();
            serializer.Save(preset, stream);

            output.WriteLine(stream.ReadFullyAsString());
        }
    }
}
