namespace EventTraceKit.VsExtension
{
    using System.Windows;

    public static class ValidateValueCallbacks
    {
        private static bool IsFiniteDoubleImpl(object value)
        {
            return value is double && ((double)value).IsFinite();
        }

        public static ValidateValueCallback IsFiniteDouble { get; } = IsFiniteDoubleImpl;
    }
}
