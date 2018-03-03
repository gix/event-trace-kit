namespace EventManifestFramework.Internal.Native
{
    using System;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;

    public static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
            string StringSecurityDescriptor,
            uint StringSDRevision,
            out SafeLocalMemHandle pSecurityDescriptor,
            out uint SecurityDescriptorSize);

        public static bool IsValidSecurityDescriptorString(string descriptorString)
        {
            bool success = ConvertStringSecurityDescriptorToSecurityDescriptor(
                descriptorString, 1, out var descriptor, out var _);
            descriptor.Dispose();
            return success;
        }
    }
}
