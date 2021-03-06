namespace EventTraceKit.EventTracing.Internal.Native
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;

    internal static class SafeModuleHandleExtensions
    {
        public static List<ResourceName> GetResourceTypes(this SafeModuleHandle module)
        {
            return UnsafeNativeMethods.GetResourceTypesEx(module);
        }

        public static List<ResourceName> GetResourceNames(this SafeModuleHandle module, ResourceName type)
        {
            return UnsafeNativeMethods.GetResourceNamesEx(module, type);
        }

        public static Stream OpenResource(this SafeModuleHandle module, ResourceName type, ResourceName name)
        {
            IntPtr resourceHandle = UnsafeNativeMethods.FindResourceEx(module, type, name, 0);
            if (resourceHandle == IntPtr.Zero)
                throw new Win32Exception();
            return module.OpenResource(resourceHandle);
        }

        private static Stream OpenResource(this SafeModuleHandle module, IntPtr resourceHandle)
        {
            IntPtr globalHandle = UnsafeNativeMethods.LoadResource(module, resourceHandle);
            if (globalHandle == IntPtr.Zero)
                throw new IOException("Failed to load resource.", new Win32Exception());

            long size = UnsafeNativeMethods.SizeofResource(module, resourceHandle);
            if (size == 0)
                throw new IOException("Unable to retrieve resource size.", new Win32Exception());

            IntPtr data = UnsafeNativeMethods.LockResource(globalHandle);
            if (data == IntPtr.Zero)
                throw new IOException("Failed to lock resource.", new Win32Exception());

            var resource = new SafeResourceBuffer(data, size);
            return new UnmanagedMemoryStream(resource, 0, size);
        }

        private sealed class SafeResourceBuffer : SafeBuffer
        {
            public SafeResourceBuffer(IntPtr handle, long size)
                : base(false)
            {
                SetHandle(handle);
                Initialize((ulong)size);
            }

            protected override bool ReleaseHandle()
            {
                return true;
            }
        }
    }
}
