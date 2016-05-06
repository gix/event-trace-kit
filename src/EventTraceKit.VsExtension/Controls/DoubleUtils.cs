namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Globalization;

    public static class DoubleUtils
    {
        // Smallest such that 1.0 + Epsilon != 1.0
        public const double Epsilon = 2.2204460492503131E-16;

        public static double Max(double value1, double value2, double value3)
        {
            return Math.Max(value1, Math.Max(value2, value3));
        }

        public static bool AreClose(double value1, double value2)
        {
            if (value1 == value2)
                return true;

            // Computes (|v1-v2| / (|v1| + |v2| + 10.0)) < Epsilon
            double tolerance = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * Epsilon;
            double diff = value1 - value2;
            return diff > -tolerance && diff < tolerance;
        }

        public static bool GreaterThan(this double value1, double value2)
        {
            return value1 > value2 && !AreClose(value1, value2);
        }

        public static bool GreaterThanOrClose(this double value1, double value2)
        {
            return value1 > value2 || AreClose(value1, value2);
        }

        public static bool LessThan(this double value1, double value2)
        {
            return value1 < value2 && !AreClose(value1, value2);
        }

        public static bool LessThanOrClose(this double value1, double value2)
        {
            return value1 < value2 || AreClose(value1, value2);
        }

        public static double Clamp(this double value, double min, double max)
        {
            if (max < min) {
                throw new ArgumentOutOfRangeException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "max ({0}) must be greater than or equal to min ({1})", min, max));
            }

            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        public static double SafeClamp(double value, double extreme1, double extreme2)
        {
            return Clamp(value, Math.Min(extreme1, extreme2), Math.Max(extreme1, extreme2));
        }
    }
}
