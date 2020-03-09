namespace EventTraceKit.EventTracing.Compilation.CodeGen
{
    using System.ComponentModel;

    [TypeConverter(typeof(CxxStandardConverter))]
    internal enum CxxLangStandard
    {
        Cxx11 = 11,
        Cxx14 = 14,
        Cxx17 = 17,
        Cxx20 = 20,
    }
}
