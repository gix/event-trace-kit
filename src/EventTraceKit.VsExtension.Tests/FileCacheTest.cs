namespace EventTraceKit.VsExtension.Tests
{
    using System;
    using System.IO;
    using System.Threading;
    using Xunit;

    public class FileCacheTest
    {
        private class Item
        {
            public Item(string path)
            {
                Path = path;
            }

            public string Path { get; }
        }

        private class DisposableItem : Item, IDisposable
        {
            public DisposableItem(string path, string value = null)
                : base(path)
            {
                Value = value;
            }

            public string Value { get; }
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        [Fact]
        public void GetUncachedItem()
        {
            string factoryArg = null;
            int factoryCalled = 0;
            var item = new Item("foo");
            Item Factory(string x)
            {
                factoryArg = x;
                ++factoryCalled;
                return item;
            }

            var cache = new FileCache<Item>(2, Factory);
            var arg = Path.GetTempFileName();
            var result = cache.Get(arg);

            Assert.Same(item, result);
            Assert.Equal(arg, factoryArg);
            Assert.Equal(1, factoryCalled);
        }

        [Fact]
        public void GetCachedItem()
        {
            var cache = new FileCache<Item>(2, x => new Item(x));
            var arg = Path.GetTempFileName();

            var result1 = cache.Get(arg);
            var result2 = cache.Get(arg);

            Assert.Same(result1, result2);
            Assert.Equal(arg, result1.Path);
            Assert.Equal(arg, result2.Path);
        }

        [Fact]
        public void RemoveUncachedItem()
        {
            var cache = new FileCache<Item>(2, x => new Item(x));
            var arg1 = Path.GetTempFileName();
            var arg2 = Path.GetTempFileName();

            var result1 = cache.Get(arg1);
            var removed = cache.Remove(arg2);
            var result2 = cache.Get(arg1);

            Assert.False(removed);
            Assert.Same(result1, result2);
            Assert.Equal(arg1, result1.Path);
            Assert.Equal(arg1, result2.Path);
        }

        [Fact]
        public void RemoveCachedItem()
        {
            var cache = new FileCache<Item>(2, x => new Item(x));
            var arg = Path.GetTempFileName();

            var result1 = cache.Get(arg);
            var removed = cache.Remove(arg);
            var result2 = cache.Get(arg);

            Assert.True(removed);
            Assert.NotSame(result1, result2);
            Assert.Equal(arg, result1.Path);
            Assert.Equal(arg, result2.Path);
        }

        [Fact]
        public void EvictsOldestItem()
        {
            var cache = new FileCache<DisposableItem>(2, x => new DisposableItem(x));
            var arg1 = Path.GetTempFileName();
            var arg2 = Path.GetTempFileName();
            var arg3 = Path.GetTempFileName();

            var result1 = cache.Get(arg1);
            var result2 = cache.Get(arg2);

            var result3 = cache.Get(arg3);

            Assert.True(result1.Disposed);
            Assert.False(result2.Disposed);
            Assert.False(result3.Disposed);
            Assert.NotSame(result1, result2);
            Assert.NotSame(result1, result3);
            Assert.NotSame(result2, result3);
            Assert.Equal(arg1, result1.Path);
            Assert.Equal(arg2, result2.Path);
            Assert.Equal(arg3, result3.Path);
        }

        [Fact]
        public void EvictsOldestItem2()
        {
            var cache = new FileCache<DisposableItem>(2, x => new DisposableItem(x));
            var arg1 = Path.GetTempFileName();
            var arg2 = Path.GetTempFileName();
            var arg3 = Path.GetTempFileName();

            var result1 = cache.Get(arg1);
            var result2 = cache.Get(arg2);
            Thread.Sleep(20); // Sleep a bit to account for timing resolution.
            var result12 = cache.Get(arg1); // Touch

            var result3 = cache.Get(arg3);

            Assert.False(result1.Disposed);
            Assert.True(result2.Disposed);
            Assert.False(result3.Disposed);
            Assert.Same(result1, result12);
            Assert.NotSame(result1, result2);
            Assert.NotSame(result1, result3);
            Assert.NotSame(result2, result3);
            Assert.Equal(arg1, result1.Path);
            Assert.Equal(arg2, result2.Path);
            Assert.Equal(arg3, result3.Path);
        }

        [Fact]
        public void EvictsChangedItem()
        {
            var cache = new FileCache<DisposableItem>(2, x => new DisposableItem(x, File.ReadAllText(x)));
            var arg1 = Path.GetTempFileName();
            File.WriteAllText(arg1, "foo");

            var result1 = cache.Get(arg1);
            File.WriteAllText(arg1, "bar"); // Change and evict
            Thread.Sleep(20); // Sleep a bit to account for timing resolution.

            var result2 = cache.Get(arg1); // Re-get

            Assert.True(result1.Disposed);
            Assert.Equal("foo", result1.Value);
            Assert.False(result2.Disposed);
            Assert.Equal("bar", result2.Value);
        }

        [Fact]
        public void Clear()
        {
            var cache = new FileCache<Item>(2, x => new Item(x));
            var arg1 = Path.GetTempFileName();
            var arg2 = Path.GetTempFileName();

            var result1 = cache.Get(arg1);
            var result2 = cache.Get(arg2);
            cache.Clear();
            var result12 = cache.Get(arg1);
            var result22 = cache.Get(arg2);

            Assert.NotSame(result1, result12);
            Assert.NotSame(result2, result22);
        }

        [Fact]
        public void ClearDisposable()
        {
            var cache = new FileCache<DisposableItem>(2, x => new DisposableItem(x));
            var arg1 = Path.GetTempFileName();
            var arg2 = Path.GetTempFileName();

            var result1 = cache.Get(arg1);
            var result2 = cache.Get(arg2);
            cache.Clear();
            var result12 = cache.Get(arg1);
            var result22 = cache.Get(arg2);

            Assert.True(result1.Disposed);
            Assert.True(result2.Disposed);
            Assert.NotSame(result1, result12);
            Assert.NotSame(result2, result22);
        }

        [Fact]
        public void Dispose()
        {
            var cache = new FileCache<DisposableItem>(2, x => new DisposableItem(x));
            var arg1 = Path.GetTempFileName();
            var arg2 = Path.GetTempFileName();

            var result1 = cache.Get(arg1);
            var result2 = cache.Get(arg2);
            cache.Dispose();

            Assert.True(result1.Disposed);
            Assert.True(result2.Disposed);
        }
    }
}
