namespace EventTraceKit.VsExtension.Tests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq.Expressions;
    using AutoMapper;
    using EventTraceKit.VsExtension.Serialization;
    using EventTraceKit.VsExtension.Views;
    using Xunit;
    using Xunit.Abstractions;

    public class SerializationMapperTest
    {
        private readonly ITestOutputHelper output;
        private readonly SerializationMapper<object> mapper;

        public SerializationMapperTest(ITestOutputHelper output)
        {
            this.output = output;
            mapper = new SerializationMapper<object>();
        }

        private class SSimple
        {
            public sbyte Int8Value { get; set; }
            public short Int16Value { get; set; }
            public int Int32Value { get; set; }
            public long Int64Value { get; set; }

            public byte UInt8Value { get; set; }
            public ushort UInt16Value { get; set; }
            public uint UInt32Value { get; set; }
            public ulong UInt64Value { get; set; }

            public float SingleValue { get; set; }
            public double DoubleValue { get; set; }

            public string StringValue { get; set; }

            public string RenamedValue2 { get; set; }
        }

        [SerializedShape(typeof(SSimple))]
        private class DSimple
        {
            public sbyte Int8Value { get; set; }
            public short Int16Value { get; set; }
            public int Int32Value { get; set; }
            public long Int64Value { get; set; }

            public byte UInt8Value { get; set; }
            public ushort UInt16Value { get; set; }
            public uint UInt32Value { get; set; }
            public ulong UInt64Value { get; set; }

            public float SingleValue { get; set; }
            public double DoubleValue { get; set; }

            public string StringValue { get; set; }

            [Serialize("RenamedValue2")]
            public string RenamedValue { get; set; }
        }

        [Fact]
        public void DeserializeSimpleTypes()
        {
            var serialized = new SSimple();
            serialized.Int8Value = sbyte.MaxValue;
            serialized.Int16Value = short.MaxValue;
            serialized.Int32Value = int.MaxValue;
            serialized.Int64Value = long.MaxValue;
            serialized.UInt8Value = byte.MaxValue;
            serialized.UInt16Value = ushort.MaxValue;
            serialized.UInt32Value = uint.MaxValue;
            serialized.UInt64Value = ulong.MaxValue;
            serialized.SingleValue = float.MaxValue;
            serialized.DoubleValue = double.MaxValue;
            serialized.StringValue = "foo";

            Assert.True(mapper.TryDeserialize(serialized, out DSimple deserialized));

            Assert.Equal(serialized.Int8Value, deserialized.Int8Value);
            Assert.Equal(serialized.Int16Value, deserialized.Int16Value);
            Assert.Equal(serialized.Int32Value, deserialized.Int32Value);
            Assert.Equal(serialized.Int64Value, deserialized.Int64Value);

            Assert.Equal(serialized.UInt8Value, deserialized.UInt8Value);
            Assert.Equal(serialized.UInt16Value, deserialized.UInt16Value);
            Assert.Equal(serialized.UInt32Value, deserialized.UInt32Value);
            Assert.Equal(serialized.UInt64Value, deserialized.UInt64Value);
            Assert.Equal(serialized.SingleValue, deserialized.SingleValue);

            Assert.Equal(serialized.DoubleValue, deserialized.DoubleValue);
            Assert.Equal(serialized.StringValue, deserialized.StringValue);

            Assert.Equal(serialized.RenamedValue2, deserialized.RenamedValue);
        }

        private class SList
        {
            public List<string> ReadOnly { get; } = new List<string>();
            public List<string> Writable { get; set; }
        }

        [SerializedShape(typeof(SList))]
        private class DList
        {
            public List<string> ReadOnly { get; } = new List<string>();
            public List<string> Writable { get; set; }
        }

        [Fact]
        public void DeserializeList_ReadOnly()
        {
            var serialized = new SList();
            serialized.ReadOnly.Add("foo");
            serialized.ReadOnly.Add("bar");

            Assert.True(mapper.TryDeserialize(serialized, out DList deserialized));

            Assert.Equal(serialized.ReadOnly, deserialized.ReadOnly);
            Assert.Equal(serialized.Writable, deserialized.Writable);
        }

        [Fact]
        public void DeserializeList_Writable()
        {
            var serialized = new SList();
            serialized.Writable = new List<string> { "foo", "bar" };

            Assert.True(mapper.TryDeserialize(serialized, out DList deserialized));

            Assert.Equal(serialized.ReadOnly, deserialized.ReadOnly);
            Assert.Equal(serialized.Writable, deserialized.Writable);
        }

        [Fact]
        public void DeserializeList_Null()
        {
            var serialized = new SList();
            serialized.Writable = null;

            Assert.True(mapper.TryDeserialize(serialized, out DList deserialized));

            Assert.Equal(serialized.ReadOnly, deserialized.ReadOnly);
            Assert.Equal(serialized.Writable, deserialized.Writable);
        }

        private abstract class SBase
        {
            public int Value { get; set; }
        }

        private class SDerived : SBase
        {
            public int DerivedValue { get; set; }
        }

        private abstract class DBase
        {
            public int Value { get; set; }
        }

        private class DDerived : DBase
        {
            public int DerivedValue { get; set; }
        }

        private class SHierarchy
        {
            //public Collection<SBase> Objects { get; } = new Collection<SBase>();
            public List<string> Strings { get; private set; } = new List<string>();
        }

        [SerializedShape(typeof(SList))]
        private class DHierarchy
        {
            //public Collection<DBase> Objects { get; } = new Collection<DBase>();
            public Collection<string> Strings { get; private set; } = new Collection<string>();
        }

        [Fact]
        public void Map()
        {
            var cfg = new MapperConfiguration(x => {
                x.CreateMap<SHierarchy, DHierarchy>()
                    .ReverseMap()
                    .ForAllMembers(y => y.UseDestinationValue());
                //x.CreateMap<SBase, DBase>().ReverseMap();
                //x.CreateMap<SDerived, DDerived>().IncludeBase<SBase, DBase>().ReverseMap();

                //x.CreateMap<DHierarchy, SHierarchy>();
                //x.CreateMap<DBase, SBase>();
                //x.CreateMap<DDerived, SDerived>().IncludeBase<DBase, SBase>();
            });
            var mapper = cfg.CreateMapper();

            var serialized = new SHierarchy();
            //serialized.Objects.Add(new SDerived { Value = 23, DerivedValue = 42 });
            serialized.Strings.Add("foo");

            var deserialized = mapper.Map<DHierarchy>(serialized);
            var serialized2 = mapper.Map<SHierarchy>(deserialized);

            Assert.Single(deserialized.Strings);
            Assert.Single(serialized2.Strings);

            //Assert.Single(deserialized.Objects);
            //Assert.Equal(serialized.Objects.Count, deserialized.Objects.Count);
            //Assert.Equal(serialized.Objects[0].Value, deserialized.Objects[0].Value);
            //Assert.Equal(((SDerived)serialized.Objects[0]).DerivedValue, ((DDerived)deserialized.Objects[0]).DerivedValue);

            //Assert.Single(serialized2.Objects);
            //Assert.Equal(serialized.Objects.Count, serialized2.Objects.Count);
            //Assert.Equal(serialized.Objects[0].Value, serialized2.Objects[0].Value);
            //Assert.Equal(((SDerived)serialized.Objects[0]).DerivedValue, ((SDerived)serialized2.Objects[0]).DerivedValue);
        }

        [Fact]
        public void Map2()
        {
            var cfg = new MapperConfiguration(x => {
                x.CreateMap<Settings.Persistence.TraceProfile, TraceProfileViewModel>().ReverseMap();
                x.CreateMap<Settings.Persistence.TraceSettings, TraceSettingsViewModel>().ReverseMap();
            });
            var mapper = cfg.CreateMapper();

            var a = new Settings.Persistence.TraceSettings();
            a.Profiles.Add(new Settings.Persistence.TraceProfile());

            var deserialized = mapper.Map<TraceSettingsViewModel>(a);
            var serialized = mapper.Map<Settings.Persistence.TraceSettings>(deserialized);

            var b = new TraceSettingsViewModel();
            b.Profiles.Add(new TraceProfileViewModel());

            var deserialized2 = mapper.Map<Settings.Persistence.TraceSettings>(b);
            var serialized2 = mapper.Map<TraceSettingsViewModel>(deserialized);

            output.WriteLine("{0}", a.Profiles.Count);
            output.WriteLine("{0}", deserialized.Profiles.Count);
            output.WriteLine("{0}", serialized.Profiles.Count);
            output.WriteLine("{0}", b.Profiles.Count);
            output.WriteLine("{0}", deserialized2.Profiles.Count);
            output.WriteLine("{0}", serialized2.Profiles.Count);
            Assert.Equal(a.Profiles.Count, deserialized.Profiles.Count);
            Assert.Equal(a.Profiles.Count, serialized.Profiles.Count);
        }

        private class SCsv
        {
            public Collection<ushort> Values { get; private set; } = new Collection<ushort>();
        }

        private class DCsv
        {
            public string Values { get; set; }
        }

        [Fact]
        public void Map3()
        {
            var cfg = new MapperConfiguration(x => {
                x.Mappers.Insert(0, new CommaSeparatedValuesToCollectionMapper());
                x.Mappers.Insert(0, new CollectionToCommaSeparatedValuesMapper());
                x.CreateMap<SCsv, DCsv>().ReverseMap();
            });
            var mapper = cfg.CreateMapper();

            var d = mapper.Map<DCsv>(new SCsv { Values = { 1, 2, 3 } });
            Assert.Equal("1,2,3", d.Values);

            var s = mapper.Map<SCsv>(d);

            Assert.Equal(new ushort[] { 1, 2, 3 }, s.Values);
        }

        [Fact]
        public void Map4()
        {
            var cfg = new MapperConfiguration(x => {
                x.CreateMap<SList, DList>().ReverseMap();
            });
            var mapper = cfg.CreateMapper();

            var d = mapper.Map<DList>(new SList { ReadOnly = { "1", "2", "3" } });
            Assert.Equal(new[] { "1", "2", "3" }, d.ReadOnly);

            var s = mapper.Map<SList>(d);
            Assert.Equal(new[] { "1", "2", "3" }, s.ReadOnly);
        }
    }
}
