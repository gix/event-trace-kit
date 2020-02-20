namespace EventTraceKit.EventTracing.Tests
{
    using System;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Support;

    public class SpecificationTest
    {
        private readonly DiagnosticsCollector diags;
        private readonly DefaultEventManifestSpecification spec;
        private Provider dummyProvider;

        public SpecificationTest()
        {
            diags = new DiagnosticsCollector();
            spec = new DefaultEventManifestSpecification(diags);
        }

        private Provider DummyProvider
        {
            get
            {
                return dummyProvider ??
                    (dummyProvider = new Provider(
                        Located.Create("Dummy"), Located.Create(Guid.Empty), "Dummy"));
            }
        }

        //[Fact]
        //public void FactMethodName()
        //{
        //    var @event = new Event(0, 0);
        //    Assert.True(spec.IsSatisfiedBy(@event));
        //    Assert.Equal(0, diags.Diagnostics.Count);
        //}

        //[Fact]
        //public void Test2()
        //{
        //    var @event = new Event(uint.MaxValue, 0);
        //    Assert.False(spec.IsSatisfiedBy(@event));
        //    Assert.Equal(1, diags.Diagnostics.Count);
        //    Assert.Equal("Foo", diags.Diagnostics[0].FormattedMessage);
        //}
    }
}
