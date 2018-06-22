namespace EventTraceKit.VsExtension.Native
{
    using Microsoft.Win32.SafeHandles;
    using System;
    using System.Diagnostics;
    using System.Management;
    using System.Runtime.InteropServices;
    using System.Text;
    using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

    public static class ProcessUtils
    {
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_VM_READ = 0x0010;

        public static string TryGetCommand(this Process process)
        {
            var buffer = new StringBuilder(260 + 1);
            int ec = GetModuleFileNameEx(process.SafeHandle, new HandleRef(), buffer, buffer.Capacity);
            if (ec == 0)
                return null;

            return buffer.ToString();
        }

        public static string TryGetCommandLine(this Process process)
        {
            var query = $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}";

            string commandLine = null;
            using (var searcher = new ManagementObjectSearcher(query)) {
                // By definition, the query returns at most 1 match, because the process
                // is looked up by ID (which is unique by definition).
                var enumerator = searcher.Get().GetEnumerator();
                if (enumerator.MoveNext())
                    commandLine = enumerator.Current["CommandLine"]?.ToString();
            }

            var fileName = process.MainModule.FileName;
            if (commandLine == null || !commandLine.StartsWith("\"" + fileName + "\" "))
                return null;

            return commandLine.Substring(fileName.Length + 3);
        }

        public static unsafe bool GetProcessCommandLineX(int processId, out string commandLine)
        {
            commandLine = null;

            int access = PROCESS_QUERY_INFORMATION | /* required for NtQueryInformationProcess */
                         PROCESS_VM_READ; /* required for ReadProcessMemory */
            var processHandle = OpenProcess(access, false, processId);
            if (processHandle.IsInvalid)
                return false;

            using (processHandle) {
                var pbi = new PROCESS_BASIC_INFORMATION();
                int st = NtQueryInformationProcess(
                    processHandle, PROCESSINFOCLASS.ProcessBasicInformation,
                    ref pbi, Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(),
                    out uint returnLength);
                if (st != 0)
                    return false;

                var peb = new PEB();
                if (!ReadProcessMemory<PEB>(processHandle, pbi.PebBaseAddress, &peb))
                    return false;

                var upp = new RTL_USER_PROCESS_PARAMETERS();
                if (!ReadProcessMemory<RTL_USER_PROCESS_PARAMETERS>(
                        processHandle, peb.ProcessParameters, &upp))
                    return false;

                UNICODE_STRING cmdLine;
                if (!ReadProcessMemory<UNICODE_STRING>(
                        processHandle, upp.CommandLine.Buffer, &cmdLine))
                    return false;

                var buffer = new StringBuilder(cmdLine.MaximumLength);
                if (!ReadProcessMemory(
                        processHandle, cmdLine.Buffer, buffer,
                        (UIntPtr)cmdLine.Length, out UIntPtr bytesRead))
                    return false;

                commandLine = buffer.ToString();
                return true;
            }
        }

        public static bool GetProcessTimes(
            this Process process, out TimeSpan kernelTime, out TimeSpan userTime)
        {
            if (!GetProcessTimes(process.SafeHandle, out _, out _, out var rawKernelTime, out var rawUserTime)) {
                kernelTime = TimeSpan.Zero;
                userTime = TimeSpan.Zero;
                return false;
            }


            kernelTime = ToTimeSpan(in rawKernelTime);
            userTime = ToTimeSpan(in rawUserTime);
            return true;
        }

        private static TimeSpan ToTimeSpan(in FILETIME time)
        {
            unchecked {
                ulong ticks = ((ulong)(uint)time.dwHighDateTime << 32) | (uint)time.dwLowDateTime;
                return new TimeSpan((long)ticks);
            }
        }

        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetProcessTimes(
            SafeProcessHandle hProcess,
            out FILETIME lpCreationTime,
            out FILETIME lpExitTime,
            out FILETIME lpKernelTime,
            out FILETIME lpUserTime);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeProcessHandle OpenProcess(
            int access, bool inherit, int processId);

        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern unsafe bool ReadProcessMemory(
            SafeProcessHandle hProcess, IntPtr lpBaseAddress,
            [Out] void* lpBuffer, UIntPtr dwSize, out UIntPtr nBytesRead);

        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadProcessMemory(
            SafeProcessHandle hProcess, IntPtr lpBaseAddress,
            [Out] StringBuilder lpBuffer, UIntPtr dwSize, out UIntPtr nBytesRead);

        [DllImport("ntdll", CharSet = CharSet.Auto)]
        private static extern int NtQueryInformationProcess(
            SafeProcessHandle ProcessHandle,
            [MarshalAs(UnmanagedType.U4)] PROCESSINFOCLASS ProcessInformationClass,
            ref PROCESS_BASIC_INFORMATION ProcessInformation,
            int ProcessInformationLength,
            out uint ReturnLength);

        private static unsafe bool ReadProcessMemory<T>(
            SafeProcessHandle hProcess, IntPtr lpBaseAddress, [Out] void* lpBuffer)
            where T : struct
        {
            return ReadProcessMemory(
                hProcess, lpBaseAddress, lpBuffer, (UIntPtr)Marshal.SizeOf<T>(),
                out var bytesRead);
        }

        private enum PROCESSINFOCLASS
        {
            ProcessBasicInformation = 0
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RTL_USER_PROCESS_PARAMETERS
        {
            public unsafe fixed byte Reserved1[16];
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr Reserved2_2;
            public IntPtr Reserved2_3;
            public IntPtr Reserved2_4;
            public IntPtr Reserved2_5;
            public IntPtr Reserved2_6;
            public IntPtr Reserved2_7;
            public IntPtr Reserved2_8;
            public IntPtr Reserved2_9;
            public UNICODE_STRING ImagePathName;
            public UNICODE_STRING CommandLine;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public UIntPtr UniqueProcessId;
            public IntPtr Reserved3;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PEB
        {
            public byte Reserved1_0;
            public byte Reserved1_1;
            public byte BeingDebugged;
            public byte Reserved2_0;
            public IntPtr Reserved3_0;
            public IntPtr Reserved3_1;
            public IntPtr Ldr;
            public IntPtr ProcessParameters;
            //byte Reserved4[104];
            //IntPtr Reserved5[52];
            //PPS_POST_PROCESS_INIT_ROUTINE PostProcessInitRoutine;
            //byte Reserved6[128];
            //IntPtr Reserved7[1];
            //uint SessionId;
        }

        [DllImport("psapi", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetModuleFileNameEx(
            SafeProcessHandle processHandle, HandleRef moduleHandle, StringBuilder baseName, int size);
    }
}
