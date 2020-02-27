namespace EventTraceKit.EventTracing.Internal.Native
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Security;

    [SecurityCritical]
    [SuppressUnmanagedCodeSecurity]
    internal static class UnsafeNativeMethods
    {
        public const short RT_RCDATA = 10;
        public const short RT_MESSAGETABLE = 11;
        public const short RT_HTML = 23;

        public static bool IS_INTRESOURCE(IntPtr ptr)
        {
            return (ptr.ToInt64() >> 16) == 0;
        }

        public static IntPtr MAKEINTRESOURCE(short i)
        {
            return new IntPtr(i);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeModuleHandle LoadLibraryEx(
            string lpFileName, IntPtr hFile, [MarshalAs(UnmanagedType.U4)] LoadLibraryExFlags dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool EnumResTypeProc(
            IntPtr hModule, IntPtr lpszType, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool EnumResNameProc(
            IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumResourceTypesEx(
            SafeModuleHandle hModule,
            EnumResTypeProc lpEnumFunc,
            IntPtr lParam,
            uint dwFlags,
            uint LangId);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumResourceNamesEx(
            SafeModuleHandle hModule,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ResourceNameMarshaler))]
                ResourceName lpszType,
            EnumResNameProc lpEnumFunc,
            IntPtr lParam,
            uint dwFlags,
            uint LangId);

        public static List<ResourceName> GetResourceTypesEx(
            SafeModuleHandle module)
        {
            var names = new List<ResourceName>();
            EnumResourceTypesEx(
                module,
                (m, type, p) => {
                    names.Add(ResourceName.FromPtr(type));
                    return true;
                },
                IntPtr.Zero,
                0,
                0);
            return names;
        }

        public static List<ResourceName> GetResourceNamesEx(
            SafeModuleHandle module, ResourceName type)
        {
            var names = new List<ResourceName>();
            EnumResourceNamesEx(
                module,
                type,
                (m, t, name, p) => {
                    names.Add(ResourceName.FromPtr(name));
                    return true;
                },
                IntPtr.Zero,
                0,
                0);
            return names;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindResourceEx(
            SafeModuleHandle hModule,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ResourceNameMarshaler))]
            ResourceName lpType,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ResourceNameMarshaler))]
            ResourceName lpName,
            short wLanguage);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindResourceEx(
            SafeModuleHandle hModule,
            string lpType,
            IntPtr lpName,
            short wLanguage);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindResourceEx(
            SafeModuleHandle hModule,
            IntPtr lpType,
            IntPtr lpName,
            short wLanguage);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadResource(
            SafeModuleHandle hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LockResource(IntPtr hResData);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SizeofResource(SafeModuleHandle hModule, IntPtr hResInfo);
    }
}
