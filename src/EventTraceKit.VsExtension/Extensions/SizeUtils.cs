namespace EventTraceKit.VsExtension.Extensions
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Windows;

    public static class SizeUtils
    {
        public static bool AreClose(Size size1, Size size2)
        {
            return
                DoubleUtils.AreClose(size1.Width, size2.Width) &&
                DoubleUtils.AreClose(size1.Height, size2.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Size Max(Size size1, Size size2)
        {
            return new Size(
                Math.Max(size1.Width, size2.Width),
                Math.Max(size1.Height, size2.Height));
        }
    }
}
