namespace EventTraceKit.VsExtension
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    internal class FileCache<T> : IDisposable
        where T : class
    {
        private readonly object mutex = new object();
        private readonly Entry[] entries;
        private readonly Func<string, T> factory;

        public FileCache(int cacheSlots, Func<string, T> factory)
        {
            if (cacheSlots < 1)
                throw new ArgumentOutOfRangeException(nameof(cacheSlots));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            this.factory = factory;
            entries = new Entry[cacheSlots];
        }

        public T Get(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            filePath = Path.GetFullPath(filePath);

            Entry entry;
            lock (mutex)
                entry = GetEntryLocked(filePath);

            return entry.GetValue();
        }

        public bool Remove(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            filePath = Path.GetFullPath(filePath);

            lock (mutex) {
                for (int i = 0; i < entries.Length; ++i) {
                    ref var entry = ref entries[i];
                    if (entry != null && entry.Matches(filePath)) {
                        EvictEntryLocked(entry);
                        return true;
                    }
                }
            }

            return false;
        }

        public void Clear()
        {
            lock (mutex) {
                foreach (var entry in entries) {
                    if (entry != null)
                        EvictEntryLocked(entry);
                }
            }
        }

        public void Dispose()
        {
            Clear();
        }

        private Entry GetEntryLocked(string filePath)
        {
            Entry oldest = null;
            int freeSlot = -1;

            for (int i = 0; i < entries.Length; ++i) {
                var entry = entries[i];
                if (entry != null && entry.Matches(filePath))
                    return entry;

                if (entry == null) {
                    if (freeSlot == -1)
                        freeSlot = i;
                } else if (Entry.Compare(entry, oldest) < 0) {
                    oldest = entry;
                }
            }

            int slot;
            if (freeSlot == -1) {
                slot = oldest.Slot;
                EvictEntryLocked(oldest);
            } else {
                slot = freeSlot;
            }

            Debug.Assert(entries[slot] == null);
            return entries[slot] = new Entry(filePath, this, slot, factory);
        }

        private void EvictEntryLocked(Entry entry)
        {
            Debug.Assert(entries[entry.Slot] == entry);
            entries[entry.Slot] = null;
            entry.Dispose();
        }

        private sealed class Entry : IDisposable
        {
            private readonly FileCache<T> cache;
            private readonly Func<string, T> factory;
            private readonly FileSystemWatcher watcher;
            private readonly Lazy<T> lazyValue;
            private readonly string filePath;
            private T value;
            private DateTime lastAccessTime;

            public Entry(string filePath, FileCache<T> cache, int slot, Func<string, T> factory)
            {
                this.cache = cache;
                this.factory = factory;
                this.filePath = filePath;
                Slot = slot;
                lazyValue = new Lazy<T>(() => factory(filePath), LazyThreadSafetyMode.ExecutionAndPublication);

                watcher = new FileSystemWatcher(
                    Path.GetDirectoryName(this.filePath),
                    Path.GetFileName(this.filePath));
                watcher.Changed += OnChanged;
                watcher.EnableRaisingEvents = true;
            }

            public int Slot { get; }

            public T GetValue()
            {
                lock (this) {
                    if (value == null)
                        value = factory(filePath);
                }

                lastAccessTime = DateTime.UtcNow;
                return value;
            }

            public void Dispose()
            {
                if (lazyValue.IsValueCreated)
                    (lazyValue.Value as IDisposable)?.Dispose();
                (value as IDisposable)?.Dispose();
                watcher.Dispose();
            }

            public bool Matches(string filePath)
            {
                return string.Equals(this.filePath, filePath, StringComparison.OrdinalIgnoreCase);
            }

            public static int Compare(Entry lhs, Entry rhs)
            {
                if (ReferenceEquals(null, lhs)) return 1;
                if (ReferenceEquals(null, rhs)) return -1;
                if (ReferenceEquals(lhs, rhs)) return 0;
                return lhs.lastAccessTime.CompareTo(rhs.lastAccessTime);
            }

            private void OnChanged(object sender, FileSystemEventArgs args)
            {
                lock (cache.mutex)
                    cache.EvictEntryLocked(this);
            }
        }
    }
}
