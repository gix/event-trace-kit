namespace EventTraceKit.VsExtension.Extensions
{
    public static class NumberUtils
    {
        public static int Clamp(this int value, int min, int max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;
            return value;
        }
    }
}
