namespace PerfRunner
{
    using System;
    using System.Runtime.InteropServices;

    internal sealed class CoTaskMemHandle : SafeHandle
    {
        public CoTaskMemHandle(IntPtr handle)
            : base(handle, true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public static CoTaskMemHandle Allocate(int size)
        {
            var buffer = Marshal.AllocCoTaskMem(size);
            return new CoTaskMemHandle(buffer);
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeCoTaskMem(handle);
            return true;
        }
    }
}
