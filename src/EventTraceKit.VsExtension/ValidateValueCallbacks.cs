namespace EventTraceKit.VsExtension
{
    public static class ValidateValueCallbacks
    {
        public static bool IsFiniteDouble(object value)
        {
            return value is double && ((double)value).IsFinite();
        }
    }
}
