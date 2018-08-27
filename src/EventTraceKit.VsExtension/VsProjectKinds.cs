namespace EventTraceKit.VsExtension
{
    using System;

    public static class VsProjectKinds
    {
        /// <summary>{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}</summary>
        public static readonly Guid CppProjectKindId =
            new Guid(0x8BC9CEB8, 0x8B4A, 0x11D0, 0x8D, 0x11, 0x00, 0xA0, 0xC9, 0x1B, 0xC9, 0x42);

        /// <summary>{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</summary>
        public static readonly Guid LegacyCSharpProjectKindId =
            new Guid(0xFAE04EC0, 0x301F, 0x11D3, 0xBF, 0x4B, 0x00, 0xC0, 0x4F, 0x79, 0xEF, 0xBC);

        /// <summary>{9A19103F-16F7-4668-BE54-9A1E7A4F7556}</summary>
        public static readonly Guid CSharpProjectKindId =
            new Guid(0x9A19103F, 0x16F7, 0x4668, 0xBE, 0x54, 0x9A, 0x1E, 0x7A, 0x4F, 0x75, 0x56);

        /// <summary>{F184B08F-C81C-45F6-A57F-5ABD9991F28F}</summary>
        public static readonly Guid VisualBasicNetProjectKindId =
            new Guid(0xF184B08F, 0xC81C, 0x45F6, 0xA5, 0x7F, 0x5A, 0xBD, 0x99, 0x91, 0xF2, 0x8F);
    }
}
