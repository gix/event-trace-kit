namespace EventTraceKit.VsExtension.Windows
{
    using System.Windows;
    using Extensions;

    public static class ValidateValueCallbacks
    {
        private static bool IsFiniteDoubleImpl(object value)
        {
            return value is double && ((double)value).IsFinite();
        }

        public static ValidateValueCallback IsFiniteDouble { get; } = IsFiniteDoubleImpl;
    }
}
