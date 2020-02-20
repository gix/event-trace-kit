namespace EventTraceKit.EventTracing.Compilation.BinXml
{
    public enum BinXmlType : byte
    {
        Null = 0,
        String = 1,
        AnsiString = 2,
        Int8 = 3,
        UInt8 = 4,
        Int16 = 5,
        UInt16 = 6,
        Int32 = 7,
        UInt32 = 8,
        Int64 = 9,
        UInt64 = 10,
        Real32 = 11,
        Real64 = 12,
        Bool = 13,
        Binary = 14,
        Guid = 15,
        SizeT = 16,
        FileTime = 17,
        SysTime = 18,
        Sid = 19,
        HexInt32 = 20,
        HexInt64 = 21,
        ArrayFlag = 0x80,
    }
}
