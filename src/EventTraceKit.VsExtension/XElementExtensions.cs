namespace EventTraceKit.VsExtension
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
            byte value;
            if (attribute != null && byte.TryParse(attribute.Value, out value))
                return value;
            return null;
        }

        public static ushort? AsUShort(this XAttribute attribute)
        {
            ushort value;
            if (attribute != null && ushort.TryParse(attribute.Value, out value))
                return value;
            return null;
        }

        public static int? AsInt(this XAttribute attribute)
        {
            int value;
            if (attribute != null && int.TryParse(attribute.Value, out value))
                return value;
            return null;
        }

        public static Guid? AsGuid(this XAttribute attribute)
        {
            Guid value;
            if (attribute != null && Guid.TryParse(attribute.Value, out value))
                return value;
            return null;
        }
    }
}
