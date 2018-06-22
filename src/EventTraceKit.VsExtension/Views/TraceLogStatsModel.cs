namespace EventTraceKit.VsExtension.Views
{
    public class TraceLogStatsModel : ObservableModel
    {
        private uint shownEvents;
        private uint totalEvents;
        private uint eventsLost;
        private uint numberOfBuffers;
        private uint freeBuffers;
        private uint buffersWritten;
        private uint logBuffersLost;
        private uint realTimeBuffersLost;

        public uint ShownEvents
        {
            get => shownEvents;
            set => SetProperty(ref shownEvents, value);
        }

        public uint TotalEvents
        {
            get => totalEvents;
            set => SetProperty(ref totalEvents, value);
        }

        public uint EventsLost
        {
            get => eventsLost;
            set => SetProperty(ref eventsLost, value);
        }

        public uint NumberOfBuffers
        {
            get => numberOfBuffers;
            set => SetProperty(ref numberOfBuffers, value);
        }

        public uint FreeBuffers
        {
            get => freeBuffers;
            set => SetProperty(ref freeBuffers, value);
        }

        public uint BuffersWritten
        {
            get => buffersWritten;
            set => SetProperty(ref buffersWritten, value);
        }

        public uint LogBuffersLost
        {
            get => logBuffersLost;
            set => SetProperty(ref logBuffersLost, value);
        }

        public uint RealTimeBuffersLost
        {
            get => realTimeBuffersLost;
            set => SetProperty(ref realTimeBuffersLost, value);
        }

        public void Reset()
        {
            totalEvents = 0;
            eventsLost = 0;
            numberOfBuffers = 0;
            freeBuffers = 0;
            buffersWritten = 0;
            logBuffersLost = 0;
            realTimeBuffersLost = 0;
        }
    }
}
