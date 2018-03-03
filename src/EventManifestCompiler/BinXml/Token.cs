namespace EventManifestCompiler.BinXml
{
    internal static class Token
    {
        public const byte EndOfFragmentToken = 0x0;
        public const byte OpenStartElementToken = 0x1;
        public const byte CloseStartElementToken = 0x2;
        public const byte CloseEmptyElementToken = 0x3;
        public const byte EndElementToken = 0x4;
        public const byte ValueTextToken = 0x5;
        public const byte AttributeToken = 0x6;
        public const byte CDataSectionToken = 0x7;
        public const byte CharRefToken = 0x8;
        public const byte EntityRefToken = 0x8;
        public const byte PITargetToken = 0xA;
        public const byte PIDataToken = 0xB;
        public const byte TemplateInstanceToken = 0xC;
        public const byte NormalSubstitutionToken = 0xD;
        public const byte OptionalSubstitutionToken = 0xE;
        public const byte FragmentHeaderToken = 0xF;
    }
}
