namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Globalization;
    using System.Xml.Linq;
    using InstrManifestCompiler.EventManifestSchema.Base;
    using InstrManifestCompiler.Extensions;
    using InstrManifestCompiler.Support;

    internal static class XNodeExtensions
    {
        public static RefValue<string> GetString(this XElement elem, string name)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                throw CreateMissingAttributeException(elem, name);

            return Value.Create(attrib.Value, attrib.GetLocation());
        }

        public static RefValue<string> GetOptionalString(
            this XElement elem, string name, string defaultValue = null)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                return Value.Create(defaultValue);

            return Value.Create(attrib.Value, attrib.GetLocation());
        }

        public static RefValue<QName> GetQName(this XElement elem, string name)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                throw CreateMissingAttributeException(elem, name);

            return Value.Create(
                QName.Parse(attrib.Value, new XElementNamespaceResolver(elem)),
                attrib.GetLocation());
        }

        public static RefValue<QName> GetOptionalQName(
            this XElement elem, string name, QName defaultValue = null)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                return Value.Create(defaultValue);

            return Value.Create(
                QName.Parse(attrib.Value, new XElementNamespaceResolver(elem)),
                attrib.GetLocation());
        }

        public static RefValue<string> GetCSymbol(
            this XElement elem, string name, string defaultValue = null)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                return Value.Create(defaultValue);

            return Value.Create(attrib.Value, attrib.GetLocation());
        }

        public static StructValue<byte> GetUInt8(this XElement elem, string name)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                throw CreateMissingAttributeException(elem, name);

            if (!TryParse(attrib.Value, NumberFormat.HexDec, out byte value))
                throw CreateInvalidNumberValueException<byte>(attrib);

            return Value.Create(value, attrib.GetLocation());
        }

        public static StructValue<ushort> GetUInt16(this XElement elem, string name)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                throw CreateMissingAttributeException(elem, name);

            if (!TryParse(attrib.Value, NumberFormat.HexDec, out ushort value))
                throw CreateInvalidNumberValueException<ushort>(attrib);

            return Value.Create(value, attrib.GetLocation());
        }

        public static StructValue<int> GetInt32(this XElement elem, string name)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                throw CreateMissingAttributeException(elem, name);

            if (!TryParse(attrib.Value, NumberFormat.HexDec, out int value))
                throw CreateInvalidNumberValueException<int>(attrib);

            return Value.Create(value, attrib.GetLocation());
        }

        public static StructValue<uint> GetUInt32(this XElement elem, string name)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                throw CreateMissingAttributeException(elem, name);

            if (!TryParse(attrib.Value, NumberFormat.HexDec, out uint value))
                throw CreateInvalidNumberValueException<uint>(attrib);

            return Value.Create(value, attrib.GetLocation());
        }

        public static NullableValue<int> GetOptionalInt32(
            this XElement elem, string name, int? defaultValue = null)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                return Value.CreateOptional(defaultValue);

            if (!TryParse(attrib.Value, NumberFormat.HexDec, out int value))
                throw CreateInvalidNumberValueException<int>(attrib);

            return Value.Create((int?)value, attrib.GetLocation());
        }

        public static NullableValue<byte> GetOptionalUInt8(
            this XElement elem, string name, byte? defaultValue = null)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                return Value.CreateOptional(defaultValue);

            if (!TryParse(attrib.Value, NumberFormat.HexDec, out byte value))
                throw CreateInvalidNumberValueException<byte>(attrib);

            return Value.Create((byte?)value, attrib.GetLocation());
        }

        public static NullableValue<ushort> GetOptionalUInt16(
            this XElement elem, string name, ushort? defaultValue = null)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                return Value.CreateOptional(defaultValue);

            if (!TryParse(attrib.Value, NumberFormat.HexDec, out ushort value))
                throw CreateInvalidNumberValueException<ushort>(attrib);

            return Value.Create((ushort?)value, attrib.GetLocation());
        }

        public static StructValue<uint> GetHexInt32(this XElement elem, string name)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                throw CreateMissingAttributeException(elem, name);

            if (!TryParse(attrib.Value, NumberFormat.PrefixedHex, out uint value))
                throw CreateInvalidHexNumberValueException<uint>(attrib);

            return Value.Create(value, attrib.GetLocation());
        }

        public static StructValue<ulong> GetHexInt64(this XElement elem, string name)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                throw CreateMissingAttributeException(elem, name);

            if (!TryParse(attrib.Value, NumberFormat.PrefixedHex, out ulong value))
                throw CreateInvalidHexNumberValueException<ulong>(attrib);

            return Value.Create(value, attrib.GetLocation());
        }

        public static StructValue<bool> GetOptionalBool(
            this XElement elem, string name, bool defaultValue)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                return Value.Create(defaultValue);

            if (attrib.Value == "true" || attrib.Value == "1")
                return Value.Create(true, attrib.GetLocation());
            if (attrib.Value == "false" || attrib.Value == "0")
                return Value.Create(false, attrib.GetLocation());

            throw CreateInvalidBoolValueException(attrib);
        }

        public static NullableValue<bool> GetOptionalBool(
            this XElement elem, string name, bool? defaultValue = null)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                return Value.CreateOptional(defaultValue);

            if (attrib.Value == "true" || attrib.Value == "1")
                return Value.Create((bool?)true, attrib.GetLocation());
            if (attrib.Value == "false" || attrib.Value == "0")
                return Value.Create((bool?)false, attrib.GetLocation());

            throw CreateInvalidBoolValueException(attrib);
        }

        public static StructValue<Guid> GetGuid(this XElement elem, string name)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                throw CreateMissingAttributeException(elem, name);

            if (!Guid.TryParseExact(attrib.Value, "B", out var value))
                throw CreateInvalidNumberValueException<byte>(attrib);

            return Value.Create(value, attrib.GetLocation());
        }

        public static NullableValue<Guid> GetOptionalGuid(
            this XElement elem, string name, Guid? defaultValue = null)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            XAttribute attrib = elem.Attribute(name);
            if (attrib == null)
                return Value.CreateOptional(defaultValue);

            if (!Guid.TryParseExact(attrib.Value, "B", out var value))
                throw CreateInvalidNumberValueException<byte>(attrib);

            return Value.Create((Guid?)value, attrib.GetLocation());
        }

        [Flags]
        enum NumberFormat
        {
            None = 0,
            Decimal = 1,
            PrefixedHex = 2,
            HexDec = Decimal | PrefixedHex,
        }

        private static bool TryParse(string number, NumberFormat format, out byte value)
        {
            if (GetNumberStyle(format, ref number, out var style))
                return byte.TryParse(number, style, CultureInfo.InvariantCulture, out value);
            value = 0;
            return false;
        }

        private static bool TryParse(string number, NumberFormat format, out ushort value)
        {
            if (GetNumberStyle(format, ref number, out var style))
                return ushort.TryParse(number, style, CultureInfo.InvariantCulture, out value);
            value = 0;
            return false;
        }

        private static bool TryParse(string number, NumberFormat format, out uint value)
        {
            if (GetNumberStyle(format, ref number, out var style))
                return uint.TryParse(number, style, CultureInfo.InvariantCulture, out value);
            value = 0;
            return false;
        }

        private static bool TryParse(string number, NumberFormat format, out ulong value)
        {
            if (GetNumberStyle(format, ref number, out var style))
                return ulong.TryParse(number, style, CultureInfo.InvariantCulture, out value);
            value = 0;
            return false;
        }

        private static bool TryParse(string number, NumberFormat format, out int value)
        {
            if (GetNumberStyle(format, ref number, out var style))
                return int.TryParse(number, style, CultureInfo.InvariantCulture, out value);
            value = 0;
            return false;
        }

        private static bool GetNumberStyle(
            NumberFormat format, ref string number, out NumberStyles style)
        {
            style = NumberStyles.None;
            if (format == NumberFormat.None || format == NumberFormat.Decimal)
                return true;

            if ((format & NumberFormat.PrefixedHex) != 0) {
                bool isHex = HasHexPrefix(number);
                if (isHex) {
                    style = NumberStyles.AllowHexSpecifier;
                    number = number.Substring(2);
                }
                return format != NumberFormat.PrefixedHex || isHex;
            }

            throw new ArgumentOutOfRangeException(nameof(format));
        }

        private static bool HasHexPrefix(string number)
        {
            return
                number.Length > 2 &&
                number[0] == '0' &&
                (number[1] == 'x' || number[2] == 'X');
        }

        private static Exception CreateMissingAttributeException(XElement elem, string attrName)
        {
            string message = string.Format(
                CultureInfo.InvariantCulture,
                "Element '{0}' is missing attribute '{1}'",
                elem.Name,
                attrName);

            return CreateValidationException(message, elem);
        }

        private static Exception CreateInvalidNumberValueException<T>(XAttribute attrib)
        {
            string parentName = string.Empty;
            if (attrib.Parent != null)
                parentName = attrib.Parent.Name + "/";

            string message = string.Format(
                CultureInfo.InvariantCulture,
                "Attribute '{0}{1}' (which is '{2}') is not a valid {3} in decimal or hexadecimal",
                parentName,
                attrib.Name,
                attrib.Value,
                typeof(T).Name);

            return CreateValidationException(message, attrib);
        }

        private static Exception CreateValidationException(string message, XObject obj)
        {
            var location = obj.GetLocation();
            return new SchemaValidationException(
                message,
                location.FilePath,
                location.LineNumber,
                location.ColumnNumber);
        }

        private static Exception CreateInvalidHexNumberValueException<T>(XAttribute attrib)
        {
            string parentName = string.Empty;
            if (attrib.Parent != null)
                parentName = attrib.Parent.Name + "/";

            string message = string.Format(
                CultureInfo.InvariantCulture,
                "Attribute '{0}{1}' (which is '{2}') is not a valid {3} in hexadecimal",
                parentName,
                attrib.Name,
                attrib.Value,
                typeof(T).Name);

            return CreateValidationException(message, attrib);
        }

        private static Exception CreateInvalidBoolValueException(XAttribute attrib)
        {
            string parentName = string.Empty;
            if (attrib.Parent != null)
                parentName = attrib.Parent.Name + "/";

            string message = string.Format(
                CultureInfo.InvariantCulture,
                "Attribute '{0}{1}' (which is '{2}') is not a valid bool value",
                parentName,
                attrib.Name,
                attrib.Value);

            throw CreateValidationException(message, attrib);
        }
    }
}
