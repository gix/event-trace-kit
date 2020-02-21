namespace EventTraceKit.VsExtension.Debugging
{
    using System.IO;
    using System.Reflection;
    using System.Text;

    public static class PortableExecutableUtils
    {
        private const int IMAGE_DOS_SIGNATURE = 0x5A4D;
        private const int IMAGE_NT_SIGNATURE = 0x00004550;
        private const int IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10B;
        private const int IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20B;
        private const int IMAGE_FILE_MACHINE_I386 = 0x014c;
        private const int IMAGE_FILE_MACHINE_ARM = 0x01c0;
        private const int IMAGE_FILE_MACHINE_IA64 = 0x0200;
        private const int IMAGE_FILE_MACHINE_AMD64 = 0x8664;

        private const int IMAGE_FILE_HEADER_SIZE =
            2 + // WORD Machine;
            2 + // WORD NumberOfSections;
            4 + // DWORD TimeDateStamp;
            4 + // DWORD PointerToSymbolTable;
            4 + // DWORD NumberOfSymbols;
            2 + // WORD SizeOfOptionalHeader;
            2 + // WORD Characteristics;
            0;

        public static ProcessorArchitecture GetImageArchitecture(
            string imagePath, out ImageSubsystem subsystem)
        {
            subsystem = ImageSubsystem.Unknown;

            using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream, Encoding.Default, true);
            if (!SkipToImageNtHeaders(reader))
                return ProcessorArchitecture.None;

            if (reader.ReadUInt32() != IMAGE_NT_SIGNATURE)
                return ProcessorArchitecture.None;

            var machine = MapMachine(reader.ReadUInt16());
            // Skip the rest of IMAGE_FILE_HEADER
            reader.BaseStream.Position += IMAGE_FILE_HEADER_SIZE - 2;

            if (machine == ProcessorArchitecture.X86) {
                if (SkipToSubsystem32(reader))
                    subsystem = (ImageSubsystem)reader.ReadUInt16();
            } else if (machine == ProcessorArchitecture.Amd64) {
                if (SkipToSubsystem64(reader))
                    subsystem = (ImageSubsystem)reader.ReadUInt16();
            }

            return machine;
        }

        private static ProcessorArchitecture MapMachine(ushort machine)
        {
            switch (machine) {
                case IMAGE_FILE_MACHINE_AMD64:
                    return ProcessorArchitecture.Amd64;
                case IMAGE_FILE_MACHINE_IA64:
                    return ProcessorArchitecture.IA64;
                case IMAGE_FILE_MACHINE_I386:
                    return ProcessorArchitecture.X86;
                case IMAGE_FILE_MACHINE_ARM:
                    return ProcessorArchitecture.Arm;
                default:
                    return ProcessorArchitecture.None;
            }
        }

        private static bool SkipToImageNtHeaders(BinaryReader r)
        {
            // WORD e_magic;
            var magic = r.ReadUInt16();
            if (magic != IMAGE_DOS_SIGNATURE)
                return false;

            int skip =
                2 + // WORD e_cblp;
                2 + // WORD e_cp;
                2 + // WORD e_crlc;
                2 + // WORD e_cparhdr;
                2 + // WORD e_minalloc;
                2 + // WORD e_maxalloc;
                2 + // WORD e_ss;
                2 + // WORD e_sp;
                2 + // WORD e_csum;
                2 + // WORD e_ip;
                2 + // WORD e_cs;
                2 + // WORD e_lfarlc;
                2 + // WORD e_ovno;
                2 * 4 + // WORD e_res[4];
                2 + // WORD e_oemid;
                2 + // WORD e_oeminfo;
                2 * 10 + // WORD e_res2[10];
                0;

            r.BaseStream.Position += skip;

            // LONG e_lfanew;
            var newExeHeaderAddress = r.ReadInt32();

            r.BaseStream.Seek(newExeHeaderAddress, SeekOrigin.Begin);
            return true;
        }

        private static bool SkipToSubsystem32(BinaryReader r)
        {
            // WORD Magic;
            var magic = r.ReadUInt16();
            if (magic != IMAGE_NT_OPTIONAL_HDR32_MAGIC)
                return false;

            int skip =
                1 + // BYTE MajorLinkerVersion;
                1 + // BYTE MinorLinkerVersion;
                4 + // DWORD SizeOfCode;
                4 + // DWORD SizeOfInitializedData;
                4 + // DWORD SizeOfUninitializedData;
                4 + // DWORD AddressOfEntryPoint;
                4 + // DWORD BaseOfCode;
                4 + // DWORD BaseOfData;
                4 + // DWORD ImageBase;
                4 + // DWORD SectionAlignment;
                4 + // DWORD FileAlignment;
                2 + // WORD MajorOperatingSystemVersion;
                2 + // WORD MinorOperatingSystemVersion;
                2 + // WORD MajorImageVersion;
                2 + // WORD MinorImageVersion;
                2 + // WORD MajorSubsystemVersion;
                2 + // WORD MinorSubsystemVersion;
                4 + // DWORD Win32VersionValue;
                4 + // DWORD SizeOfImage;
                4 + // DWORD SizeOfHeaders;
                4 + // DWORD CheckSum;
                0;

            r.BaseStream.Position += skip;
            return true;
        }

        private static bool SkipToSubsystem64(BinaryReader r)
        {
            // WORD Magic;
            var magic = r.ReadUInt16();
            if (magic != IMAGE_NT_OPTIONAL_HDR64_MAGIC)
                return false;

            int skip =
                1 + //BYTE MajorLinkerVersion;
                1 + //BYTE MinorLinkerVersion;
                4 + //DWORD SizeOfCode;
                4 + //DWORD SizeOfInitializedData;
                4 + //DWORD SizeOfUninitializedData;
                4 + //DWORD AddressOfEntryPoint;
                4 + //DWORD BaseOfCode;
                8 + //ULONGLONG ImageBase;
                4 + //DWORD SectionAlignment;
                4 + //DWORD FileAlignment;
                2 + //WORD MajorOperatingSystemVersion;
                2 + //WORD MinorOperatingSystemVersion;
                2 + //WORD MajorImageVersion;
                2 + //WORD MinorImageVersion;
                2 + //WORD MajorSubsystemVersion;
                2 + //WORD MinorSubsystemVersion;
                4 + //DWORD Win32VersionValue;
                4 + //DWORD SizeOfImage;
                4 + //DWORD SizeOfHeaders;
                4 + //DWORD CheckSum;
                0;

            r.BaseStream.Position += skip;
            return true;
        }
    }

    public enum ImageSubsystem
    {
        Unknown = 0,
        WindowsGui = 2,
        WindowsCui = 3,
    }
}
