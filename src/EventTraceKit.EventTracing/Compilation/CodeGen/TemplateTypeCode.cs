namespace EventTraceKit.EventTracing.Compilation.CodeGen
{
    using System.Collections.Generic;
    using System.Globalization;
    using EventTraceKit.EventTracing.Internal.Extensions;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Schema.Base;
    using EventTraceKit.EventTracing.Support;

    public static class TemplateTypeCode
    {
        public static string MangleProperty(Property property)
        {
            if (property.Kind == PropertyKind.Data)
                return MangleProperty((DataProperty)property);
            string t = "n";
            if (property.Count.IsFixedMultiple) {
                t = t.ToUpperInvariant();
                t += property.Count.Value.Value;
            } else if (property.Count.DataPropertyRef != null) {
                t = t.ToUpperInvariant();
                t += "R" + property.Count.DataProperty.Index;
            }
            return t;
        }

        public static string MangleProperty(DataProperty data)
        {
            if (data.InType.Name.Namespace != WinEventSchema.Namespace)
                throw new InternalException("Cannot mangle type '{0}'", data.InType);

            string t = MangleType(data.InType);

            if (data.InType.Name != WinEventSchema.UnicodeString &&
                data.InType.Name != WinEventSchema.AnsiString &&
                data.InType.Name != WinEventSchema.Binary) {
                if (data.Count.IsFixedMultiple) {
                    t = t.ToUpperInvariant();
                    t += data.Count.Value.Value;
                } else if (data.Count.IsVariable) {
                    t = t.ToUpperInvariant();
                    t += "R" + data.Count.DataProperty.Index;
                }
                return t;
            }

            string len = string.Empty;

            bool hasFixedCount = data.Count.IsFixedMultiple;
            bool hasFixedLength = data.Length.IsFixed;
            bool hasVarCount = data.Count.IsVariable;
            bool hasVarLength = data.Length.IsVariable;

            if (hasFixedCount || hasVarCount)
                t = t.ToUpperInvariant();

            if (hasFixedCount && hasFixedLength) {
                len = (data.Count.Value.Value * data.Length.Value.Value).ToString(
                    CultureInfo.InvariantCulture);
            } else if (hasVarCount && hasVarLength) {
                len = "r" + data.Length.DataProperty.Index;
                len += "R" + data.Count.DataProperty.Index;
            } else if (hasFixedCount && hasVarLength) {
                len = data.Count.Value + "r" + data.Length.DataProperty.Index;
            } else if (hasFixedLength && hasVarCount) {
                len = data.Length.Value + "r" + data.Count.DataProperty.Index;
            } else if (hasFixedCount || hasVarCount) {
                len = "R";
            } else if (hasFixedLength) {
                len = data.Length.Value.ToString();
            } else if (hasVarLength) {
                len = "r" + data.Length.DataProperty.Index;
            } else if (hasFixedCount || hasVarCount || hasFixedLength || hasVarLength) {
                throw new InternalException();
            }

            return t + len;
        }

        public static string MangleType(InType type)
        {
            if (type.Name.Namespace != WinEventSchema.Namespace)
                throw new InternalException("Cannot mangle type '{0}'.", type);

            return type.Name.LocalName switch
            {
                "UnicodeString" => "z",
                "AnsiString" => "s",
                "Int8" => "c",
                "UInt8" => "u",
                "Int16" => "l",
                "UInt16" => "h",
                "Int32" => "d",
                "UInt32" => "q",
                "Int64" => "i",
                "UInt64" => "x",
                "Float" => "f",
                "Double" => "g",
                "Boolean" => "t",
                "Binary" => "b",
                "GUID" => "j",
                "Pointer" => "p",
                "FILETIME" => "m",
                "SYSTEMTIME" => "y",
                "SID" => "k",
                "HexInt32" => "d",
                "HexInt64" => "i",
                "CountedUnicodeString" => "w",
                "CountedAnsiString" => "a",
                "CountedBinary" => "e",
                _ => throw new InternalException("cannot mangle type '{0}'", type),
            };
        }
    }
}
