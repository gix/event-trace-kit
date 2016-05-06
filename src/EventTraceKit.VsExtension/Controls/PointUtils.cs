namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Windows;

    public static class PointUtils
    {
        public static bool AreClose(Point point1, Point point2)
        {
            return
                DoubleUtils.AreClose(point1.X, point2.X) &&
                DoubleUtils.AreClose(point1.Y, point2.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point Round(this Point point)
        {
            return new Point(Math.Round(point.X), Math.Round(point.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point Round(this Point point, MidpointRounding mode)
        {
            return new Point(
                Math.Round(point.X, mode),
                Math.Round(point.Y, mode));
        }
    }
}
