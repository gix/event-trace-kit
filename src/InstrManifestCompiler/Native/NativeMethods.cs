namespace InstrManifestCompiler.Native
{
    using System;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;

    internal static class NativeMethods
    {
        public const int MESSAGE_RESOURCE_UNICODE = 1;

        [DllImport("kernel32.dll")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
            string StringSecurityDescriptor,
            uint StringSDRevision,
            out SafeLocalMemHandle pSecurityDescriptor,
            out uint SecurityDescriptorSize);

        public static bool IsValidSecurityDescriptorString(string descriptorString)
        {
            uint descriptorSize;
            bool success = ConvertStringSecurityDescriptorToSecurityDescriptor(
                descriptorString, 1, out var descriptor, out descriptorSize);
            descriptor.Dispose();
            return success;
        }
    }
}
