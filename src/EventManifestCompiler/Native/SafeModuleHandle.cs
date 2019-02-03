namespace EventManifestCompiler.Native
{
    using System;
    using System.Security;

    /// <summary>Represents a wrapper class for a HMODULE handle.</summary>
    [SecurityCritical]
    internal sealed class SafeModuleHandle : SafeHandleZeroIsInvalid
    {
        public static SafeModuleHandle Load(string fileName, LoadLibraryExFlags flags = 0)
        {
            return UnsafeNativeMethods.LoadLibraryEx(fileName, IntPtr.Zero, flags);
        }

        public static SafeModuleHandle LoadImageResource(string fileName)
        {
            const LoadLibraryExFlags ImageResourceFlags =
                LoadLibraryExFlags.LOAD_LIBRARY_AS_IMAGE_RESOURCE |
                LoadLibraryExFlags.LOAD_LIBRARY_AS_DATAFILE;
            return Load(fileName, ImageResourceFlags);
        }

        private SafeModuleHandle()
            : base(true)
        {
        }

        /// <summary>
        ///   Frees the loaded module and, if necessary, decrements its reference
        ///   count. When the reference count reaches zero, the module is unloaded
        ///   from the address space of the calling process and the handle is no
        ///   longer valid.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if the handle is released successfully;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            return UnsafeNativeMethods.FreeLibrary(handle);
        }
    }
}
