namespace EventTraceKit.VsExtension
{
    using System.IO;
    using System.Reflection;
    using System.Text;

    public static class PortableExecutableUtils
    {
        private const int IMAGE_DOS_SIGNATURE = 0x5A4D;
        private const int IMAGE_NT_SIGNATURE = 0x00004550;
        private const int IMAGE_FILE_MACHINE_I386 = 0x014c;
        private const int IMAGE_FILE_MACHINE_ARM = 0x01c0;
        private const int IMAGE_FILE_MACHINE_IA64 = 0x0200;
        private const int IMAGE_FILE_MACHINE_AMD64 = 0x8664;

        public static ProcessorArchitecture GetImageArchitecture(string imagePath)
        {
            using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream, Encoding.Default, true)) {
                if (!SkipToImageNtHeaders(reader))
                    return ProcessorArchitecture.None;

                if (reader.ReadUInt32() != IMAGE_NT_SIGNATURE)
                    return ProcessorArchitecture.None;
                var machine = reader.ReadUInt16();

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
    }
}
