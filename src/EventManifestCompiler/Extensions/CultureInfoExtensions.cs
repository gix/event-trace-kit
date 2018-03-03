namespace EventManifestCompiler.Extensions
{
    using System.Globalization;

    internal static class CultureInfoExtensions
    {
        public static int GetLangId(this CultureInfo culture)
        {
            return culture.LCID & 0xFFFF;
        }

        public static int GetPrimaryLangId(this CultureInfo culture)
        {
            return PRIMARYLANGID(culture.GetLangId());
        }

        public static int GetSubLangId(this CultureInfo culture)
        {
            return SUBLANGID(culture.GetLangId());
        }

        private static int PRIMARYLANGID(int langId)
        {
            return langId & 0x3FF;
        }

        private static int SUBLANGID(int langId)
        {
            return (langId & 0xFFFF) >> 10;
        }
    }
}
