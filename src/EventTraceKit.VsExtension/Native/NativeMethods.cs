namespace EventTraceKit.VsExtension.Native
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.VisualStudio.Shell.Interop;
    using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

    internal static class NativeMethods
    {
        private const string User32 = "user32.dll";
        private const string Kernel32 = "kernel32.dll";

        public const int ERROR_NOT_SAME_DEVICE = 17;

        [DllImport(User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport(User32)]
        public static extern uint GetSysColor(int nIndex);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool MoveFileEx(
            string lpExistingFilename, string lpNewFileName, MOVEFILE_FLAGS flags);

        public static int HResultFromWin32(int errorCode)
        {
            Debug.Assert((0xFFFF0000 & errorCode) == 0, "This is an HRESULT, not an error code!");
            return unchecked((int)0x80070000 | errorCode);
        }

        public static string GetMessage(int errorCode)
        {
            var flags = FormatMessageFlags.IgnoreInserts |
                        FormatMessageFlags.FromSystem |
                        FormatMessageFlags.ArgumentArray;

            var buffer = new StringBuilder();
            uint result = FormatMessageW(
                flags, IntPtr.Zero, (uint)errorCode, 0, buffer,
                (uint)buffer.Capacity, IntPtr.Zero);

            if (result == 0)
                return $"Unknown error\"{errorCode}\".";

            return result.ToString();
        }

        [DllImport(Kernel32, SetLastError = true)]
        public static extern uint FormatMessageW(
            [MarshalAs(UnmanagedType.U4)] FormatMessageFlags dwFlags,
            UnmanagedString lpSource,
            uint dwMessageId,
            uint dwLanguageId,
            IntPtr lpBuffer,
            uint nSize,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)]
            string[] Arguments);

        [DllImport(Kernel32, SetLastError = true)]
        public static extern uint FormatMessageW(
            [MarshalAs(UnmanagedType.U4)] FormatMessageFlags dwFlags,
            IntPtr lpSource,
            uint dwMessageId,
            uint dwLanguageId,
            [Out] StringBuilder lpBuffer,
            uint nSize,
            IntPtr Arguments);

        [DllImport(Kernel32, SetLastError = true)]
        public static extern bool FileTimeToSystemTime(
            [In] ref FILETIME lpFileTime, out SYSTEMTIME lpSystemTime);

        [DllImport("WS2_32.dll", EntryPoint = "WSAAddressToStringW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int WSAAddressToString(
            IntPtr pSockAddressStruct, uint cbSockAddressStruct,
            IntPtr lpProtocolInfo, IntPtr resultString, ref int cbResultStringLength);

        [DllImport("msenv.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsSystemFontAvailable(
            [MarshalAs(UnmanagedType.BStr)] string bstrName);

        public const int ERROR_FILE_NOT_FOUND = 2;
        public const int ERROR_PATH_NOT_FOUND = 3;
        public const int ERROR_ACCESS_DENIED = 5;
        public const int ERROR_INVALID_DRIVE = 15;
        public const int ERROR_SHARING_VIOLATION = 32;
        public const int ERROR_FILE_EXISTS = 80;
        public const int ERROR_INVALID_PARAMETER = 87;
        public const int ERROR_ALREADY_EXISTS = 183;
        public const int ERROR_FILENAME_EXCED_RANGE = 206;
        public const int ERROR_OPERATION_ABORTED = 995;
        public const int ERROR_INVALID_NAME = 123;
        public const int ERROR_BAD_PATHNAME = 161;
    }
}
