namespace EventTraceKit.VsExtension
{
    public interface IEventInfoSource
    {
        EventSessionInfo GetInfo();
        EventInfo GetEvent(int index);
    }
}
