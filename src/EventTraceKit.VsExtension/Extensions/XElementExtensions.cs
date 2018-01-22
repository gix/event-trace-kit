namespace EventTraceKit.VsExtension.Extensions
{
    using System;
    using System.Xml.Linq;

    public static class XElementExtensions
    {
        public static string AsString(this XAttribute attribute)
        {
            return attribute?.Value;
        }

        public static byte? AsByte(this XAttribute attribute)
        {
            if (attribute != null && byte.TryParse(attribute.Value, out byte value))
                return value;
            return null;
        }

        public static ushort? AsUShort(this XAttribute attribute)
        {
            if (attribute != null && ushort.TryParse(attribute.Value, out ushort value))
                return value;
            return null;
        }

        public static int? AsInt(this XAttribute attribute)
        {
            if (attribute != null && int.TryParse(attribute.Value, out int value))
                return value;
            return null;
        }

        public static Guid? AsGuid(this XAttribute attribute)
        {
            if (attribute != null && Guid.TryParse(attribute.Value, out Guid value))
                return value;
            return null;
        }
    }
}
