namespace EventTraceKit.VsExtension.Tests.Serialization
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Xml.Linq;
    using Controls;
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
        public void SerializeTraceSettingsViewModel()
        {
            var settings = new TraceSettingsViewModel();

            var session = new TraceSessionSettingsViewModel();
            session.Id = new Guid("12133C31-FC77-4210-91A0-1EAFCE2B3537");
            session.Name = "Foo";
            session.BufferSize = 64;
            session.MinimumBuffers = 23;
            session.MaximumBuffers = 42;
            session.LogFileName = @"z:\path\to\logfile.etl";

            var provider = new TraceProviderDescriptorViewModel(
                new Guid("37D34B87-80A6-44CE-90BB-1C3D6EDB0784"), "Bar");
            session.Providers.Add(provider);

            settings.Sessions.Add(session);

            var stream = new MemoryStream();
            new SettingsSerializer().Save(settings, stream);

            var doc = XDocument.Parse(stream.ReadFullyAsString());
            output.WriteLine(doc.ToString());
        }

        [Fact]
        public void SerializeTraceSessionSettings()
        {
            var source = new TraceSessionSettingsViewModel();
            source.Id = new Guid("12133C31-FC77-4210-91A0-1EAFCE2B3537");
            source.Name = "Foo";
            source.BufferSize = 64;
            source.MinimumBuffers = 23;
            source.MaximumBuffers = 42;
            source.LogFileName= @"z:\path\to\logfile.etl";

            var provider = new TraceProviderDescriptorViewModel(
                new Guid("37D34B87-80A6-44CE-90BB-1C3D6EDB0784"), "Bar");
            source.Providers.Add(provider);

            var stream = new MemoryStream();
            new SettingsSerializer().Save(source, stream);

            var doc = XDocument.Parse(stream.ReadFullyAsString());
            output.WriteLine(doc.ToString());
            Assert.Equal("TraceSession", doc.Root.Name.LocalName);
            Assert.Equal(6, doc.Root.NonXmlnsAttributes().Count());
            Assert.Equal("Foo", doc.Root.Attribute("Name")?.Value);
            Assert.Equal("64", doc.Root.Attribute("BufferSize")?.Value);
            Assert.Equal("23", doc.Root.Attribute("MinimumBuffers")?.Value);
            Assert.Equal("42", doc.Root.Attribute("MaximumBuffers")?.Value);
            Assert.Equal(@"z:\path\to\logfile.etl", doc.Root.Attribute("LogFileName")?.Value);

            stream.Position = 0;
            var deserialized = new SettingsSerializer().Load<TraceSessionSettingsViewModel>(stream);

            Assert.Equal(source.Name, deserialized.Name);
            Assert.Equal(source.BufferSize, deserialized.BufferSize);
            Assert.Equal(source.MinimumBuffers, deserialized.MinimumBuffers);
            Assert.Equal(source.MaximumBuffers, deserialized.MaximumBuffers);
            Assert.Equal(source.LogFileName, deserialized.LogFileName);
            Assert.Equal(source.LogFileName, deserialized.LogFileName);
        }

        [Fact]
        public void SerializeTraceProviderDescriptor()
        {
            var source = new TraceProviderDescriptorViewModel(
                new Guid("EC700760-3FAE-48AD-8110-E4AE77C69F85"), "Foo");
            source.IsEnabled = true;
            source.Level = 0xFF;
            source.MatchAnyKeyword = 0xFEDC;
            source.MatchAllKeyword = 0xBA98;
            source.IncludeSecurityId = true;
            source.IncludeTerminalSessionId = true;
            source.IncludeStackTrace = true;
            source.ProcessIds.Add(23);
            source.ProcessIds.Add(42);
            source.Manifest = @"z:\path\to\manifest.man";
            source.FilterEvents = true;
            source.Events.Add(new TraceEventDescriptorViewModel { Id = 2 });

            var stream = new MemoryStream();
            new SettingsSerializer().Save(source, stream);

            var doc = XDocument.Parse(stream.ReadFullyAsString());
            Assert.Equal("TraceProvider", doc.Root.Name.LocalName);
            Assert.Equal(11, doc.Root.NonXmlnsAttributes().Count());
            Assert.Equal("ec700760-3fae-48ad-8110-e4ae77c69f85", doc.Root.Attribute("Id")?.Value);
            Assert.Equal("Foo", doc.Root.Attribute("Name")?.Value);
            Assert.Equal("True", doc.Root.Attribute("IsEnabled")?.Value);
            Assert.Equal("255", doc.Root.Attribute("Level")?.Value);
            Assert.Equal("65244", doc.Root.Attribute("MatchAnyKeyword")?.Value);
            Assert.Equal("47768", doc.Root.Attribute("MatchAllKeyword")?.Value);
            Assert.Equal("True", doc.Root.Attribute("IncludeSecurityId")?.Value);
            Assert.Equal("True", doc.Root.Attribute("IncludeTerminalSessionId")?.Value);
            Assert.Equal("True", doc.Root.Attribute("IncludeStackTrace")?.Value);
            Assert.Equal(@"z:\path\to\manifest.man", doc.Root.Attribute("Manifest")?.Value);
            Assert.Equal("True", doc.Root.Attribute("FilterEvents")?.Value);

            stream.Position = 0;
            var deserialized = new SettingsSerializer().Load<TraceProviderDescriptorViewModel>(stream);

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
            Assert.Equal(source.ProcessIds, deserialized.ProcessIds);

            Assert.Equal(source.FilterEvents, deserialized.FilterEvents);
            Assert.Equal(source.Events.Count, deserialized.Events.Count);
            Assert.Equal(source.Events[0].Id, deserialized.Events[0].Id);
            Assert.Equal(source.Events[0].Channel, deserialized.Events[0].Channel);
        }

        [Fact]
        public void SerializeTraceEventDescriptor()
        {
            var source = new TraceEventDescriptorViewModel {
                IsEnabled = true,
                Id = 23,
                Version = 42,
                Symbol = "MySymbol",
                Level = "MyLevel",
                Channel = "MyChannel",
                Task = "MyTask",
                Opcode = "MyOpcode",
                Keywords = "MyKeywords"
            };

            var stream = new MemoryStream();
            new SettingsSerializer().Save(source, stream);

            var doc = XDocument.Parse(stream.ReadFullyAsString());
            Assert.Equal("TraceEvent", doc.Root.Name.LocalName);
            Assert.Equal(9, doc.Root.NonXmlnsAttributes().Count());
            Assert.Equal("True", doc.Root.Attribute("IsEnabled")?.Value);
            Assert.Equal("23", doc.Root.Attribute("Id")?.Value);
            Assert.Equal("42", doc.Root.Attribute("Version")?.Value);
            Assert.Equal("MySymbol", doc.Root.Attribute("Symbol")?.Value);
            Assert.Equal("MyLevel", doc.Root.Attribute("Level")?.Value);
            Assert.Equal("MyChannel", doc.Root.Attribute("Channel")?.Value);
            Assert.Equal("MyTask", doc.Root.Attribute("Task")?.Value);
            Assert.Equal("MyOpcode", doc.Root.Attribute("Opcode")?.Value);
            Assert.Equal("MyKeywords", doc.Root.Attribute("Keywords")?.Value);

            stream.Position = 0;
            var deserialized = new SettingsSerializer().Load<TraceEventDescriptorViewModel>(stream);

            Assert.Equal(source.IsEnabled, deserialized.IsEnabled);
            Assert.Equal(source.Id, deserialized.Id);
            Assert.Equal(source.Version, deserialized.Version);
            Assert.Equal(source.Symbol, deserialized.Symbol);
            Assert.Equal(source.Level, deserialized.Level);
            Assert.Equal(source.Channel, deserialized.Channel);
            Assert.Equal(source.Task, deserialized.Task);
            Assert.Equal(source.Opcode, deserialized.Opcode);
            Assert.Equal(source.Keywords, deserialized.Keywords);
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
