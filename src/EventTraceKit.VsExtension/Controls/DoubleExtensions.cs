namespace EventTraceKit.VsExtension.Controls
{
    public static class DoubleExtensions
    {
        public static bool IsFinite(this double value)
        {
            return value >= double.MinValue && value <= double.MaxValue;
        }
    }
}
