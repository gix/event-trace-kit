namespace EventTraceKit.VsExtension.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public static class StringExtensions
    {
        public static string TrimToLength(
            this string s, int maxLength, TrimPosition position = TrimPosition.End)
        {
            if (s == null || s.Length <= maxLength)
                return s;
            if (maxLength <= 0)
                return string.Empty;

            var length = maxLength - 1;
            switch (position) {
                case TrimPosition.End:
                    return s.Substring(0, length) + "…";
                case TrimPosition.Start:
                    return "…" + s.Substring(s.Length - length, length);
                case TrimPosition.Middle:
                    int suffix = length / 2;
                    int prefix = length - suffix;
                    return s.Substring(0, prefix) + "…" + s.Substring(s.Length - suffix, suffix);
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }

        public static string MakeNumberedCopy(this string fullString, ISet<string> usedStrings = null)
        {
            if (usedStrings == null || !usedStrings.Contains(fullString))
                return fullString;

            var match = Regex.Match(fullString, @"\A(?<str>.*) \(Copy(?: (?<num>\d+))?\)\z");

            string str;
            int num;
            if (match.Success) {
                str = match.Groups["str"].Value;
                num = match.Groups["num"].Success ? int.Parse(match.Groups["num"].Value) : 1;
            } else {
                str = fullString;
                num = 0;
            }

            while (true) {
                ++num;
                string copiedStr = str + (num == 1 ? " (Copy)" : $" (Copy {num})");
                if (!usedStrings.Contains(copiedStr))
                    return copiedStr;
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
