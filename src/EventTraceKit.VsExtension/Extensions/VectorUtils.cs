namespace EventTraceKit.VsExtension.Extensions
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Windows;

    public static class VectorUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Abs(this Vector vector)
        {
            return new Vector(Math.Abs(vector.X), Math.Abs(vector.Y));
        }
    }
}
