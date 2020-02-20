namespace EventTraceKit.EventTracing.Schema
{
    using System.Xml.Linq;
    using EventTraceKit.EventTracing.Schema.Base;

    public static class EventManifestSchema
    {
        public static readonly XNamespace Namespace =
            "http://schemas.microsoft.com/win/2004/08/events";
    }

    public static class EventSchema
    {
        public static readonly XNamespace Namespace =
            "http://schemas.microsoft.com/win/2004/08/events/event";
    }

    public static class WinEventSchema
    {
        public static readonly XNamespace Namespace =
            "http://manifests.microsoft.com/win/2004/08/windows/events";

        public static readonly QName Int8 = new QName("Int8", "win", Namespace);
        public static readonly QName UInt8 = new QName("UInt8", "win", Namespace);
        public static readonly QName Int16 = new QName("Int16", "win", Namespace);
        public static readonly QName UInt16 = new QName("UInt16", "win", Namespace);
        public static readonly QName Int32 = new QName("Int32", "win", Namespace);
        public static readonly QName UInt32 = new QName("UInt32", "win", Namespace);
        public static readonly QName Int64 = new QName("Int64", "win", Namespace);
        public static readonly QName UInt64 = new QName("UInt64", "win", Namespace);
        public static readonly QName Float = new QName("Float", "win", Namespace);
        public static readonly QName Double = new QName("Double", "win", Namespace);
        public static readonly QName Boolean = new QName("Boolean", "win", Namespace);
        public static readonly QName UnicodeString = new QName("UnicodeString", "win", Namespace);
        public static readonly QName AnsiString = new QName("AnsiString", "win", Namespace);
        public static readonly QName Binary = new QName("Binary", "win", Namespace);
        public static readonly QName Guid = new QName("GUID", "win", Namespace);
        public static readonly QName Pointer = new QName("Pointer", "win", Namespace);
        public static readonly QName FileTime = new QName("FILETIME", "win", Namespace);
        public static readonly QName SystemTime = new QName("SYSTEMTIME", "win", Namespace);
        public static readonly QName SecurityId = new QName("SID", "win", Namespace);
        public static readonly QName HexInt32 = new QName("HexInt32", "win", Namespace);
        public static readonly QName HexInt64 = new QName("HexInt64", "win", Namespace);
    }
}
