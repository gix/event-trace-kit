namespace EventTraceKit.VsExtension.Views
{
    using System.Collections.Generic;
    using System.Linq;

    public class StackTraceViewModel : ObservableModel
    {
        private readonly StackTraceInfo stackTrace;
        private readonly List<StackFrame> frames;

        public StackTraceViewModel(StackTraceInfo stackTrace)
        {
            this.stackTrace = stackTrace;
            frames = stackTrace.Addresses.Select(x => new StackFrame(x)).ToList();
        }

        public ulong MatchId => stackTrace.MatchId;
        public IReadOnlyList<StackFrame> Frames => frames;
    }

    public class StackFrame : ObservableModel
    {
        public StackFrame(ulong address)
        {
            Address = address;
        }

        public ulong Address { get; }
        public string ModuleName { get; }
    }
}
