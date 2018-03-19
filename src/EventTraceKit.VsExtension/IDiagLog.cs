namespace EventTraceKit.VsExtension
{
    public interface IDiagLog
    {
        void WriteLine(string format, params object[] args);
    }

    public class NullDiagLog : IDiagLog
    {
        public void WriteLine(string format, params object[] args)
        {
        }
    }
}
