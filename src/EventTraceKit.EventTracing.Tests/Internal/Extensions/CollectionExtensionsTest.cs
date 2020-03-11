namespace EventTraceKit.EventTracing.Tests.Internal.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using EventTraceKit.EventTracing.Internal.Extensions;
    using Xunit;

    public class CollectionExtensionsTest
    {
        [Fact]
        public void IsEmpty()
        {
            Assert.True(CollectionExtensions.IsEmpty((IReadOnlyCollection<int>)null));
            Assert.True(CollectionExtensions.IsEmpty((IReadOnlyCollection<int>)new List<int>()));
            Assert.False(CollectionExtensions.IsEmpty((IReadOnlyCollection<int>)new List<int> { 1 }));

            Assert.True(CollectionExtensions.IsEmpty((ICollection<int>)null));
            Assert.True(CollectionExtensions.IsEmpty((ICollection<int>)new List<int>()));
            Assert.False(CollectionExtensions.IsEmpty((ICollection<int>)new List<int> { 1 }));

            Assert.True(CollectionExtensions.IsEmpty((string)null));
            Assert.True(CollectionExtensions.IsEmpty(""));
            Assert.False(CollectionExtensions.IsEmpty("1"));

            Assert.True(CollectionExtensions.IsEmpty((IEnumerable<int>)null));
            Assert.True(CollectionExtensions.IsEmpty(Enumerable.Empty<int>()));
            Assert.False(CollectionExtensions.IsEmpty(Enumerable.Repeat(1, 1)));
        }
    }
}
