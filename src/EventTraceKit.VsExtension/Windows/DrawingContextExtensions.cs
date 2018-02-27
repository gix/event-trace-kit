namespace EventTraceKit.VsExtension.Windows
{
    using System;
    using System.Windows;
    using System.Windows.Media;

    public static class DrawingContextExtensions
    {
        public static void DrawLineSnapped(
            this DrawingContext dc, Pen pen, Point point0, Point point1)
        {
            dc.DrawLine(pen, SnapPoint(point0), SnapPoint(point1));
        }

        public static void DrawRectangleSnapped(
            this DrawingContext dc, Brush brush, Pen pen, Rect rectangle)
        {
            dc.DrawRectangle(brush, pen, SnapRect(rectangle));
        }

        private static Rect SnapRect(Rect rectangle)
        {
            return new Rect(
                SnapPoint(rectangle.TopLeft),
                SnapPoint(rectangle.BottomRight));
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
