﻿namespace EventTraceKit.VsExtension
{
    using System;
    using Serialization;

    [SerializedShape(typeof(Settings.TraceEvent))]
    public class TraceEventDescriptorViewModel : ViewModel
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
        public TraceEventDescriptorViewModel()
        {
        }

        public TraceEventDescriptorViewModel(ushort id)
        {
            Id = id;
        }

        public TraceEventDescriptorViewModel(ushort id, byte version, string symbol)
        {
            Id = id;
            Version = version;
            Symbol = symbol;
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set { SetProperty(ref isEnabled, value); }
        }

        public Tuple<ushort, byte> CreateKey() => Tuple.Create(Id, Version);

        public ushort Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }

        public byte Version
        {
            get { return version; }
            set { SetProperty(ref version, value); }
        }

        public string Symbol
        {
            get { return symbol; }
            set { SetProperty(ref symbol, value); }
        }

        public string Level
        {
            get { return level; }
            set { SetProperty(ref level, value); }
        }

        public string Channel
        {
            get { return channel; }
            set { SetProperty(ref channel, value); }
        }

        public string Task
        {
            get { return task; }
            set { SetProperty(ref task, value); }
        }

        public string Opcode
        {
            get { return opcode; }
            set { SetProperty(ref opcode, value); }
        }

        public string Keywords
        {
            get { return keywords; }
            set { SetProperty(ref keywords, value); }
        }

        public TraceEventDescriptorViewModel DeepClone()
        {
            var clone = new TraceEventDescriptorViewModel();
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