namespace EventTraceKit.EventTracing.Compilation.CodeGen
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    internal class CxxStandardConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string str) {
                return str switch
                {
                    "c++11" => CxxLangStandard.Cxx11,
                    "c++14" => CxxLangStandard.Cxx14,
                    "c++17" => CxxLangStandard.Cxx17,
                    "c++20" => CxxLangStandard.Cxx20,
                    _ => throw new NotSupportedException(),
                };
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(
            ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException(nameof(destinationType));

            if (value is CxxLangStandard standard) {
                return standard switch
                {
                    CxxLangStandard.Cxx11 => "c++11",
                    CxxLangStandard.Cxx14 => "c++14",
                    CxxLangStandard.Cxx17 => "c++17",
                    CxxLangStandard.Cxx20 => "c++20",
                    _ => throw new NotSupportedException(),
                };
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
