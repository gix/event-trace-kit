namespace EventTraceKit.VsExtension.Native
{
    using System;
    using System.Runtime.InteropServices;

    public static class DebuggingNativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct DEBUG_EVENT
        {
            [MarshalAs(UnmanagedType.U4)]
            public DebugEventCode dwDebugEventCode;
            public uint dwProcessId;
            public uint dwThreadId;
            public DEBUG_EVENT_UNION u;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DEBUG_EVENT_UNION
        {
            [FieldOffset(0)]
            public EXCEPTION_DEBUG_INFO Exception;
            [FieldOffset(0)]
            public CREATE_THREAD_DEBUG_INFO CreateThread;
            [FieldOffset(0)]
            public CREATE_PROCESS_DEBUG_INFO CreateProcessInfo;
            [FieldOffset(0)]
            public EXIT_THREAD_DEBUG_INFO ExitThread;
            [FieldOffset(0)]
            public EXIT_PROCESS_DEBUG_INFO ExitProcess;
            [FieldOffset(0)]
            public LOAD_DLL_DEBUG_INFO LoadDll;
            [FieldOffset(0)]
            public UNLOAD_DLL_DEBUG_INFO UnloadDll;
            [FieldOffset(0)]
            public OUTPUT_DEBUG_STRING_INFO DebugString;
            [FieldOffset(0)]
            public RIP_INFO RipInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EXCEPTION_DEBUG_INFO
        {
            public EXCEPTION_RECORD ExceptionRecord;
            public uint dwFirstChance;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EXCEPTION_RECORD
        {
            public uint ExceptionCode;
            public uint ExceptionFlags;
            public IntPtr ExceptionRecord;
            public IntPtr ExceptionAddress;
            public uint NumberParameters;
            public UIntPtr ExceptionInformation00;
            public UIntPtr ExceptionInformation01;
            public UIntPtr ExceptionInformation02;
            public UIntPtr ExceptionInformation03;
            public UIntPtr ExceptionInformation04;
            public UIntPtr ExceptionInformation05;
            public UIntPtr ExceptionInformation06;
            public UIntPtr ExceptionInformation07;
            public UIntPtr ExceptionInformation08;
            public UIntPtr ExceptionInformation09;
            public UIntPtr ExceptionInformation10;
            public UIntPtr ExceptionInformation11;
            public UIntPtr ExceptionInformation12;
            public UIntPtr ExceptionInformation13;
            public UIntPtr ExceptionInformation14;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CREATE_THREAD_DEBUG_INFO
        {
            public IntPtr hThread;
            public IntPtr lpThreadLocalBase;
            public IntPtr lpStartAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CREATE_PROCESS_DEBUG_INFO
        {
            public IntPtr hFile;
            public IntPtr hProcess;
            public IntPtr hThread;
            public IntPtr lpBaseOfImage;
            public uint dwDebugInfoFileOffset;
            public uint nDebugInfoSize;
            public IntPtr lpThreadLocalBase;
            public IntPtr lpStartAddress;
            public IntPtr lpImageName;
            public ushort fUnicode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EXIT_THREAD_DEBUG_INFO
        {
            public uint dwExitCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EXIT_PROCESS_DEBUG_INFO
        {
            public uint dwExitCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LOAD_DLL_DEBUG_INFO
        {
            public IntPtr hFile;
            public IntPtr lpBaseOfDll;
            public uint dwDebugInfoFileOffset;
            public uint nDebugInfoSize;
            public IntPtr lpImageName;
            public ushort fUnicode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNLOAD_DLL_DEBUG_INFO
        {
            public IntPtr lpBaseOfDll;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OUTPUT_DEBUG_STRING_INFO
        {
            public IntPtr lpDebugStringData;
            public ushort fUnicode;
            public ushort nDebugStringLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RIP_INFO
        {
            public uint dwError;
            public uint dwType;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetProcessId([In] IntPtr Process);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DebugActiveProcess(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DebugActiveProcessStop(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WaitForDebugEvent(
            out DEBUG_EVENT lpDebugEvent, uint dwMilliseconds);

        public enum DebugEventCode : uint
        {
            EXCEPTION_DEBUG_EVENT = 1,
            CREATE_THREAD_DEBUG_EVENT = 2,
            CREATE_PROCESS_DEBUG_EVENT = 3,
            EXIT_THREAD_DEBUG_EVENT = 4,
            EXIT_PROCESS_DEBUG_EVENT = 5,
            LOAD_DLL_DEBUG_EVENT = 6,
            UNLOAD_DLL_DEBUG_EVENT = 7,
            OUTPUT_DEBUG_STRING_EVENT = 8,
            RIP_EVENT = 9,
        }
    }
}
