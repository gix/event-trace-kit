namespace EventTraceKit.VsExtension.Native
{
    using System;
    using System.Runtime.InteropServices;

    internal static class NativeMethods
    {
        private const string User32 = "user32.dll";

        [DllImport(User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport(User32)]
        internal static extern uint GetSysColor(int nIndex);
    }
}
