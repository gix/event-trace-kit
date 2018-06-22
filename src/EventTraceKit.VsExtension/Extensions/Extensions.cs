namespace EventTraceKit.VsExtension.Extensions
{
    using System;

    public static class Extensions
    {
        public static DateTime ToDateTime(this Microsoft.VisualStudio.OLE.Interop.FILETIME time)
        {
            return new DateTime(((long)time.dwHighDateTime << 32) | time.dwLowDateTime);
        }

        public static bool IsArithmetic(this Type type)
        {
            if (type.IsEnum)
                return false;
            switch (Type.GetTypeCode(type)) {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsUnsignedInt(this Type type)
        {
            if (type.IsEnum)
                return false;
            switch (Type.GetTypeCode(type)) {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

    }
}
