namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Media;

    public static class DrawingContextExtensions
    {
        public static void DrawLineSnapped(this DrawingContext dc, Pen pen, Point point0, Point point1)
        {
            dc.DrawLine(pen, SnapPoint(point0), SnapPoint(point1));
        }

        private static Point SnapPoint(Point point)
        {
            return new Point(SnapValue(point.X), SnapValue(point.Y));
        }

        private static double SnapValue(double value)
        {
            return 0.5 + Math.Round(value - 0.499999, MidpointRounding.AwayFromZero);
        }
    }
}
