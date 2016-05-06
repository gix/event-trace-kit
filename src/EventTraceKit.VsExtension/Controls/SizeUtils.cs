namespace EventTraceKit.VsExtension.Controls
{
    using System.Windows;

    public static class SizeUtils
    {
        public static bool AreClose(Size size1, Size size2)
        {
            return
                DoubleUtils.AreClose(size1.Width, size2.Width) &&
                DoubleUtils.AreClose(size1.Height, size2.Height);
        }
    }
}
