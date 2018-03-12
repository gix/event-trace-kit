namespace EventTraceKit.VsExtension.Views
{
    using System;

    public class EventViewModel : ObservableModel
    {
        private bool isEnabled;
        private ushort id;
        private byte version;
        private string symbol;
        private string level;
        private string channel;
        private string task;
        private string opcode;
        private string keywords;

        [UsedImplicitly("Adding new items to DataGrid")]
        public EventViewModel()
        {
        }

        public EventViewModel(ushort id)
        {
            Id = id;
        }

        public EventViewModel(ushort id, byte version, string symbol)
        {
            Id = id;
            Version = version;
            Symbol = symbol;
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => SetProperty(ref isEnabled, value);
        }

        public Tuple<ushort, byte> CreateKey() => Tuple.Create(Id, Version);

        public ushort Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        public byte Version
        {
            get => version;
            set => SetProperty(ref version, value);
        }

        public string Symbol
        {
            get => symbol;
            set => SetProperty(ref symbol, value);
        }

        public string Level
        {
            get => level;
            set => SetProperty(ref level, value);
        }

        public string Channel
        {
            get => channel;
            set => SetProperty(ref channel, value);
        }

        public string Task
        {
            get => task;
            set => SetProperty(ref task, value);
        }

        public string Opcode
        {
            get => opcode;
            set => SetProperty(ref opcode, value);
        }

        public string Keywords
        {
            get => keywords;
            set => SetProperty(ref keywords, value);
        }

        public EventViewModel DeepClone()
        {
            var clone = new EventViewModel();
            clone.IsEnabled = IsEnabled;
            clone.Id = Id;
            clone.Version = Version;
            clone.Symbol = Symbol;
            clone.Level = Level;
            clone.Channel = Channel;
            clone.Task = Task;
            clone.Opcode = Opcode;
            clone.Keywords = Keywords;
            return clone;
        }
    }
}
