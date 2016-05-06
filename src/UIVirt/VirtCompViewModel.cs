namespace UIVirt
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Threading;
    using EventTraceKit.VsExtension;

    public struct RawEvent
    {
        public int Id { get; set; }
    }

    public class Event
    {
        public int Id { get; set; }
        public string Message { get; set; }
    }

    public interface IItemProvider<T>
    {
        event Action<int> CountChanged;
        int Count { get; }
        T[] GetItems(int offset, int count);
    }

    public class VirtualList : IList<Event>, IList, INotifyPropertyChanged, INotifyCollectionChanged
    {
        private readonly IItemProvider<Event> provider;
        private int batchSize = 100;
        private readonly Dictionary<int, Event[]> loadedBatches =
            new Dictionary<int, Event[]>();

        public VirtualList(IItemProvider<Event> provider)
        {
            this.provider = provider;
            this.provider.CountChanged += OnCountChanged;
            Count = provider.Count;
        }

        private void OnCountChanged(int obj)
        {
            Count = provider.Count;
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, new List<Event>()));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsReadOnly => true;

        bool IList.IsFixedSize => false;

        void ICollection.CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public int Count { get; private set; }

        object ICollection.SyncRoot => this;

        bool ICollection.IsSynchronized => true;

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        object IList.this[int index]
        {
            get { return ((IList<Event>)this)[index]; }
            set { throw new NotSupportedException(); }
        }

        public Event this[int index]
        {
            get
            {
                int batchIndex = index / batchSize;
                int batchOffset = index % batchSize;
                Request(batchIndex);
                EvictOldPages();
                return loadedBatches[batchIndex][batchOffset];
            }
            set { throw new NotSupportedException(); }
        }

        private void Request(int batchIndex)
        {
            loadedBatches[batchIndex] = provider.GetItems(batchIndex, batchSize);
        }

        private void EvictOldPages()
        {
        }

        public int IndexOf(Event item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<Event> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(Event item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Event[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        int IList.Add(object value)
        {
            throw new NotImplementedException();
        }

        bool IList.Contains(object value)
        {
            throw new NotImplementedException();
        }

        void IList.Clear()
        {
            throw new NotSupportedException();
        }

        int IList.IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        void IList.Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        void IList.Remove(object value)
        {
            throw new NotImplementedException();
        }

        void ICollection<Event>.Clear()
        {
            throw new NotSupportedException();
        }

        void ICollection<Event>.Add(Event item)
        {
            throw new NotSupportedException();
        }

        bool ICollection<Event>.Remove(Event item)
        {
            throw new NotSupportedException();
        }

        void IList<Event>.Insert(int index, Event item)
        {
            throw new NotSupportedException();
        }

        void IList<Event>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }
    }

    public class DelegateCommand : ICommand
    {
        private readonly Action action;

        public DelegateCommand(Action action)
        {
            this.action = action;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            action();
        }
    }

    class EventProvider2 : IItemProvider<Event>
    {
        private readonly List<RawEvent> source;

        public EventProvider2(List<RawEvent> source)
        {
            this.source = source;
            Count = source.Count;
        }

        public event Action<int> CountChanged;

        public int Count { get; private set; }

        public Event[] GetItems(int offset, int count)
        {
            var array = new Event[count];
            for (int i = 0; i < count; ++i) {
                array[i] = Convert(source[offset + i]);
            }
            return array;
        }

        private Event Convert(RawEvent rawEvent)
        {
            return new Event {
                Id = rawEvent.Id,
                Message = Guid.NewGuid().ToString()
            };
        }

        public void Refresh(int added)
        {
            Count += added;
            CountChanged?.Invoke(added);
        }
    }

    public class VirtCompViewModel : ViewModel
    {
        private CancellationTokenSource cts;
        private List<RawEvent> eventSource = new List<RawEvent>();
        private EventProvider2 provider;
        private VirtualList list;

        public VirtCompViewModel()
        {
            ClearCommand = new DelegateCommand(Clear);
            StartCommand = new DelegateCommand(Start);
            StopCommand = new DelegateCommand(Stop);

            provider = new EventProvider2(eventSource);
            list = new VirtualList(provider);
            //Items = new MyCollectionView<Event>(list);
            Events = new ObservableCollection<VirtCompTraceEvent>();

            GenerateEvents(Events, 100000);
        }

        private void GenerateEvents(IList<VirtCompTraceEvent> events, int count)
        {
            var random = new Random();
            var providerId = new Guid("045791A4-4F03-4394-A37F-F83BD04251C7");
            var providerName = "FFMF-FFMF-Sculptor";
            var processId = Process.GetCurrentProcess().Id;
            var threadId = Thread.CurrentThread.ManagedThreadId;

            var names = new Dictionary<KeyValuePair<int, int>, string>();

            for (int i = 0; i < count; ++i) {
                var evt = new VirtCompTraceEvent {
                    ProviderId = providerId,
                    Id = (ushort)random.Next(1, 4000),
                    Version = 1,
                    ChannelId = (byte)random.Next(1, 4),
                    LevelId = (byte)random.Next(1, 4),
                    OpcodeId = (byte)random.Next(1, 4),
                    TaskId = (byte)random.Next(1, 4),
                    KeywordMask = 0
                };

                evt.Provider = providerName;
                evt.Channel = names.GetOrDefault(0, evt.ChannelId, () => "Channel" + evt.ChannelId);
                evt.Level = names.GetOrDefault(0, evt.ChannelId, () => "Level" + evt.LevelId);
                evt.Opcode = names.GetOrDefault(0, evt.ChannelId, () => "Opcode" + evt.OpcodeId);
                evt.Task = names.GetOrDefault(0, evt.ChannelId, () => "Task" + evt.TaskId);
                evt.Keywords = string.Empty;

                evt.Time = DateTime.Now;
                evt.ProcessId = (uint)processId;
                evt.ThreadId = (uint)threadId;
                evt.ProcessorTime = 0;
                evt.Message = $"EVR Queue Sample Tag={random.Next(1, 50000)} Object=%2 Sample=%3 Target QPC=%4 Submitted QPC=%5";
                evt.Formatted = true;

                events.Add(evt);
            }
        }

        public DelegateCommand ClearCommand { get; }
        public DelegateCommand StartCommand { get; }
        public DelegateCommand StopCommand { get; }
        public ObservableCollection<VirtCompTraceEvent> Events { get; }

        private void Clear()
        {
            lock (eventSource)
                eventSource.Clear();
        }

        private void Start()
        {
            cts = new CancellationTokenSource();
            Task.Run(() => Generate(cts.Token), cts.Token);
        }

        private void Stop()
        {
            cts.Cancel();
        }

        private async void Generate(CancellationToken cancellationToken)
        {
            try {
                int v = eventSource.Count;
                while (!cancellationToken.IsCancellationRequested) {
                    for (int i = 0; i < 1000; ++i)
                        eventSource.Add(new RawEvent { Id = v });

                    await Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal,
                        new Action(() => {
                            provider.Refresh(1000);
                        }));

                    await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                }
            } catch (TaskCanceledException) {
            }
        }
    }

    public static class Extensions
    {
        public static TValue GetOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dict, TKey key,
            Func<TValue> defaultFactory)
        {
            TValue value;
            if (!dict.TryGetValue(key, out value)) {
                value = defaultFactory();
                dict[key] = value;
            }

            return value;
        }

        public static TValue GetOrDefault<TValue>(
            this IDictionary<KeyValuePair<int, int>, TValue> dict, int key1, int key2,
            Func<TValue> defaultFactory)
        {
            var key = new KeyValuePair<int, int>(key1, key2);
            TValue value;
            if (!dict.TryGetValue(key, out value)) {
                value = defaultFactory();
                dict[key] = value;
            }

            return value;
        }
    }

    public class VirtCompTraceEvent
    {
        public Guid ProviderId { get; set; }
        public ushort Id { get; set; }
        public byte Version { get; set; }
        public byte ChannelId { get; set; }
        public byte LevelId { get; set; }
        public byte OpcodeId { get; set; }
        public ushort TaskId { get; set; }
        public ulong KeywordMask { get; set; }

        public string Provider { get; set; }
        public string Channel { get; set; }
        public string Level { get; set; }
        public string Opcode { get; set; }
        public string Task { get; set; }
        public string Keywords { get; set; }

        public DateTime Time { get; set; }
        public uint ProcessId { get; set; }
        public uint ThreadId { get; set; }
        public ulong ProcessorTime { get; set; }
        public string Message { get; set; }
        public bool Formatted { get; set; }
    }
}
