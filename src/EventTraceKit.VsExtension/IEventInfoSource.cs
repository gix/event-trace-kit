namespace EventTraceKit.VsExtension
{
    public interface IEventInfoSource
    {
        TraceSessionInfo GetInfo();
        EventInfo GetEvent(int index);
    }
}