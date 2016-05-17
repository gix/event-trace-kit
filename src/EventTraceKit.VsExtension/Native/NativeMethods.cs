namespace EventTraceKit.VsExtension.Native
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;

    internal static class NativeMethods
    {
        private const string User32 = "user32.dll";

        [DllImport(User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport(User32)]
        internal static extern uint GetSysColor(int nIndex);
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public RECT(Rect rect)
        {
            Left = (int)rect.Left;
            Top = (int)rect.Top;
            Right = (int)rect.Right;
            Bottom = (int)rect.Bottom;
        }

        public Point Position => new Point(Left, Top);

        public Size Size => new Size(Width, Height);

        public int Height
        {
            get { return Bottom - Top; }
            set { Bottom = Top + value; }
        }

        public int Width
        {
            get { return Right - Left; }
            set { Right = Left + value; }
        }

        public void Offset(int dx, int dy)
        {
            Left += dx;
            Right += dx;
            Top += dy;
            Bottom += dy;
        }

        public Int32Rect ToInt32Rect()
        {
            return new Int32Rect(Left, Top, Width, Height);
        }
    }
}
