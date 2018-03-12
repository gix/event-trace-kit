namespace EventTraceKit.VsExtension.Extensions
{
    using System;

    public static class StringExtensions
    {
        public static string TrimToLength(
            this string s, int maxLength, TrimPosition position = TrimPosition.End)
        {
            if (s == null || s.Length <= maxLength)
                return s;

            var length = maxLength - 1;
            switch (position) {
                case TrimPosition.End:
                    return s.Substring(0, length) + "…";
                case TrimPosition.Start:
                    return "…" + s.Substring(s.Length - length, length);
                case TrimPosition.Middle:
                    int suffix = length - length / 2;
                    return s.Substring(0, length / 2) + "…" + s.Substring(s.Length - suffix, suffix);
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }
    }

    public enum TrimPosition
    {
        End,
        Start,
        Middle
    }
}
