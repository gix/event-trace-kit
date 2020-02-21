namespace EventTraceKit.VsExtension.Windows
{
    using System.Windows;

    public static class Boxed
    {
        public static readonly object True = true;
        public static readonly object False = false;
        public static readonly object DoubleZero = 0.0;
        public static readonly object Int32Zero = 0;
        public static readonly object Int32Minus1 = -1;

        public static object Int32(int value)
        {
            return value switch
            {
                -1 => Int32Minus1,
                0 => Int32Zero,
                _ => value,
            };
        }

        public static object Bool(bool value)
        {
            return value ? True : False;
        }
    }

    public static class TextAlignmentBoxes
    {
        public static readonly object Left = TextAlignment.Left;
        public static readonly object Center = TextAlignment.Center;
        public static readonly object Right = TextAlignment.Right;
        public static readonly object Justify = TextAlignment.Justify;

        internal static object Box(TextAlignment value)
        {
            return value switch
            {
                TextAlignment.Left => Left,
                TextAlignment.Center => Center,
                TextAlignment.Right => Right,
                TextAlignment.Justify => Justify,
                _ => value,
            };
        }
    }
}
