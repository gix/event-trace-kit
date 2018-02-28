namespace EventTraceKit.VsExtension.Native
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;

    internal sealed class SafeBstrHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeBstrHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        public static SafeBstrHandle Create(string str)
        {
            return new SafeBstrHandle(Marshal.StringToBSTR(str));
        }

        public unsafe UnmanagedString Get()
        {
            return new UnmanagedString((char*)handle);
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeBSTR(handle);
            return true;
        }
    }
}
