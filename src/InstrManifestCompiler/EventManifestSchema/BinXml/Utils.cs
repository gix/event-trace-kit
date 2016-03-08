namespace InstrManifestCompiler.EventManifestSchema.BinXml
{
    internal static class Utils
    {
        public static ushort HashString(string str)
        {
            int value = 0;
            for (int i = 0; i < str.Length; ++i)
                value = value * 65599 + str[i];
            return (ushort)(value & 0xFFFF);
        }
    }
}
