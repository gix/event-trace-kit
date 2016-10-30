namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using System.Text;
    using Microsoft.VisualStudio.Shell.Interop;
    using Native;
    using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

    [Flags]
    public enum MAP_FLAGS
    {
        MANIFEST_BITMAP = 2,
        MANIFEST_PATTERNMAP = 4,
        MANIFEST_VALUEMAP = 1,
        WBEM_BITMAP = 0x10,
        WBEM_FLAG = 0x20,
        WBEM_NO_MAP = 0x40,
        WBEM_VALUEMAP = 8
    }

    public enum MAP_VALUETYPE
    {
        EVENTMAP_ENTRY_VALUETYPE_ULONG,
        EVENTMAP_ENTRY_VALUETYPE_STRING
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_MAP_ENTRY
    {
        public uint OutputOffset;
        public uint ValueAndInputOffsetUnion;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_MAP_INFO
    {
        public readonly uint NameOffset;
        public readonly MAP_FLAGS MapFlags;
        public readonly uint EntryCount;
        public readonly MAP_VALUETYPE MapValueTypeFormatStringUnion;
        private EVENT_MAP_ENTRY EventMapEntryArray;

        public static unsafe EVENT_MAP_ENTRY* GetEventMapInfoArray(EVENT_MAP_INFO* pEventMapInfo)
        {
            return &pEventMapInfo->EventMapEntryArray;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EventMapInfoCPtr
    {
        private readonly unsafe EVENT_MAP_INFO* pEventMapInfo;

        public unsafe EventMapInfoCPtr(EVENT_MAP_INFO* pEventMapInfo)
        {
            this.pEventMapInfo = pEventMapInfo;
        }

        public unsafe bool HasData =>
            pEventMapInfo != null;

        internal unsafe string ValueAtKey(void* pdata, TDH_IN_TYPE inputType)
        {
            if (pdata == null)
                throw new ArgumentNullException(nameof(pdata));

            var stringFromBitMap = string.Empty;
            if ((inputType == TDH_IN_TYPE.INT32) || (inputType == TDH_IN_TYPE.UINT32)) {
                if (pEventMapInfo[0].MapValueTypeFormatStringUnion !=
                    MAP_VALUETYPE.EVENTMAP_ENTRY_VALUETYPE_ULONG) {
                    throw new InternalErrorException(
                        "Map should be of a Property should be the same type as the Property");
                }
                var key = *(uint*)pdata;
                if (pEventMapInfo[0].MapFlags == MAP_FLAGS.MANIFEST_VALUEMAP) {
                    return GetStringFromValueMap(key);
                }
                if (pEventMapInfo[0].MapFlags == MAP_FLAGS.MANIFEST_BITMAP) {
                    stringFromBitMap = GetStringFromBitMap(key);
                }
            }
            return stringFromBitMap;
        }

        private unsafe string GetStringAtOffset(ulong offset)
        {
            if (offset > 0L) {
                var numPtr = (byte*)(pEventMapInfo + offset);
                return new UnmanagedString((char*)numPtr);
            }
            return string.Empty;
        }

        private unsafe string GetStringFromValueMap(uint key)
        {
            var eventMapInfoArray = EVENT_MAP_INFO.GetEventMapInfoArray(pEventMapInfo);
            uint num = 0;
            var entryCount = pEventMapInfo[0].EntryCount;
            while (num < entryCount) {
                var num3 = num + ((entryCount - num) >> 1);
                var valueAndInputOffsetUnion =
                    eventMapInfoArray[(int)(num3 * sizeof(EVENT_MAP_ENTRY))].ValueAndInputOffsetUnion;
                if (valueAndInputOffsetUnion < key) {
                    num = num3 + 1;
                } else {
                    if (valueAndInputOffsetUnion > key) {
                        entryCount = num3;
                        continue;
                    }
                    return
                        GetStringAtOffset(
                            eventMapInfoArray[(int)(num3 * sizeof(EVENT_MAP_ENTRY))].OutputOffset);
                }
            }
            return string.Empty;
        }

        private unsafe string GetStringFromBitMap(uint key)
        {
            var builder = new StringBuilder();
            var eventMapInfoArray = EVENT_MAP_INFO.GetEventMapInfoArray(pEventMapInfo);
            for (var i = 0; i < pEventMapInfo[0].EntryCount; i++) {
                if ((eventMapInfoArray[i].ValueAndInputOffsetUnion & key) != 0) {
                    builder.Append(GetStringAtOffset(eventMapInfoArray[i].OutputOffset));
                }
            }
            return builder.ToString();
        }
    }

    public static class TdhHelper
    {
        private const int MaxMessageSizeSupported = 0x1000;
        private static char[] messageBuffer;
        private const uint SizeofIPV6Address = 0x10;
        private const int sizeOfSOCKADDR_STORAGE = 0x80;
        private static readonly byte[] tempByteArrayForIpV6Parsing = new byte[SizeofIPV6Address];
        private static readonly char[] temporaryStringForSockAddressString = new char[0x100];
        private static readonly object tokenLock = new object();
        private static List<List<IntPtr>> tokens = new List<List<IntPtr>>();

        public static unsafe string GetMessageForEventRecord(
            EventRecordCPtr eventRecord,
            TraceEventInfoCPtr traceEventInfo,
            ParseTdhContext parseTdhContext,
            IFormatProvider formatProvider)
        {
            if (eventRecord.EventHeader.IsStringOnly)
                return CharCPtrUtils.GetString(
                    (char*)eventRecord.UserData, eventRecord.UserDataLength);

            var message = traceEventInfo.Message;
            if (message.IsEmpty)
                return string.Empty;

            var arguments = new string[traceEventInfo.TopLevelPropertyCount];
            var count = traceEventInfo.PropertyCount + 1;
            lock (tokenLock) {
                EnsureRoomForTokensAndClear(count);
                var build = TokenAccessContext.Build;
                TryGetTokens(
                    traceEventInfo, eventRecord, parseTdhContext, traceEventInfo.TopLevelPropertyCount, tokens,
                    build, count);
                build = TokenAccessContext.Consume;
                for (uint i = 0; i < traceEventInfo.TopLevelPropertyCount; i++) {
                    if (!tokens[(int)i].Any()) {
                        arguments[i] = "Unable to parse data";
                    } else {
                        var eventMapInfo = new EventMapInfoCPtr();
                        arguments[i] = GetStringForProperty(
                            traceEventInfo, eventRecord, i, parseTdhContext, tokens, build, count,
                            formatProvider, eventMapInfo);
                    }
                }

                if (messageBuffer == null)
                    messageBuffer = new char[MaxMessageSizeSupported];

                fixed (char* ptr = messageBuffer)
                {
                    var flags = FormatMessageFlags.FromString | FormatMessageFlags.ArgumentArray;
                    var numWritten = NativeMethods.FormatMessageW(
                        flags, message, 0, 0, new IntPtr(ptr),
                        (uint)messageBuffer.Length, arguments);
                    if (numWritten == 0)
                        return string.Empty;

                    if (messageBuffer[numWritten - 1] == ' ' &&
                        messageBuffer[numWritten] == '\0')
                        messageBuffer[numWritten - 1] = '\0';

                    return new UnmanagedString(ptr).ToString();
                }
            }
        }

        private static unsafe byte* AdjustPastAnsiString(byte* pData)
        {
            short num = 0;
            num = 0;
            while (num < 0x7fff) {
                var numPtr = pData + num;
                if (numPtr[0] == 0) {
                    break;
                }
                num = (short)(num + 1);
            }
            if (num == 0x7fff) {
                return null;
            }
            return pData + num + 1;
        }

        private static unsafe IntPtr AdjustPointerPastTDHData(
            TDH_IN_TYPE tdhInType,
            TDH_OUT_TYPE tdhOutType,
            IntPtr buffer,
            uint dataLength,
            bool hasExplicitLength,
            bool is32BitTrace) =>
                new IntPtr(
                    AdjustPointerPastTDHData(
                        tdhInType, tdhOutType, (byte*)buffer.ToPointer(), dataLength, hasExplicitLength,
                        is32BitTrace));

        private static unsafe byte* AdjustPointerPastTDHData(
            TDH_IN_TYPE tdhInType,
            TDH_OUT_TYPE tdhOutType,
            byte* pData,
            uint dataLength,
            bool hasExplicitLength,
            bool is32BitTrace)
        {
            switch (tdhInType) {
                case TDH_IN_TYPE.NULL:
                    return pData;

                case TDH_IN_TYPE.UNICODESTRING:
                    if ((dataLength == 0) && !hasExplicitLength) {
                        return AdjustPointerPastUnicodeString(pData);
                    }
                    return pData + dataLength * 2;

                case TDH_IN_TYPE.ANSISTRING:
                    if (dataLength == 0) {
                        return AdjustPastAnsiString(pData);
                    }
                    return pData + dataLength;

                case TDH_IN_TYPE.INT8:
                    return pData + 1;

                case TDH_IN_TYPE.UINT8:
                    return pData + 1;

                case TDH_IN_TYPE.INT16:
                    return pData + 2;

                case TDH_IN_TYPE.UINT16:
                    return pData + 2;

                case TDH_IN_TYPE.INT32:
                    return pData + 4;

                case TDH_IN_TYPE.UINT32:
                    return pData + 4;

                case TDH_IN_TYPE.INT64:
                    return pData + 8;

                case TDH_IN_TYPE.UINT64:
                    return pData + 8;

                case TDH_IN_TYPE.FLOAT:
                    return pData + 4;

                case TDH_IN_TYPE.DOUBLE:
                    return pData + 8;

                case TDH_IN_TYPE.BOOLEAN:
                    return pData + 4;

                case TDH_IN_TYPE.BINARY:
                    return pData + dataLength;

                case TDH_IN_TYPE.GUID:
                    return pData + sizeof(Guid);

                case TDH_IN_TYPE.POINTER:
                    return pData + (is32BitTrace ? 4 : 8);

                case TDH_IN_TYPE.FILETIME:
                    return pData + sizeof(FILETIME);

                case TDH_IN_TYPE.SYSTEMTIME:
                    return pData + sizeof(SYSTEMTIME);

                case TDH_IN_TYPE.SID:
                    if (dataLength == 0) {
                        dataLength = (uint)GetSIDLengthFromData(pData);
                    }
                    return pData + dataLength;

                case TDH_IN_TYPE.HEXINT32:
                    return pData + 4;

                case TDH_IN_TYPE.HEXINT64:
                    return pData + 8;

                case TDH_IN_TYPE.COUNTEDSTRING:
                case TDH_IN_TYPE.COUNTEDANSISTRING: {
                        var num = *(ushort*)pData;
                        return pData + 2 + num;
                    }
                case TDH_IN_TYPE.REVERSEDCOUNTEDSTRING:
                case TDH_IN_TYPE.REVERSEDCOUNTEDANSISTRING:
                case TDH_IN_TYPE.NONNULLTERMINATEDSTRING:
                case TDH_IN_TYPE.NONNULLTERMINATEDANSISTRING:
                    return null;

                case TDH_IN_TYPE.UNICODECHAR:
                    return null;

                case TDH_IN_TYPE.ANSICHAR:
                    return null;

                case TDH_IN_TYPE.SIZET:
                    return null;

                case TDH_IN_TYPE.HEXDUMP:
                    return null;

                case TDH_IN_TYPE.WBEMSID:
                    return null;
            }
            return null;
        }

        private static unsafe byte* AdjustPointerPastUnicodeString(byte* pData)
        {
            var wsz = (char*)pData;
            var length = CharCPtrUtils.GetLength(wsz, 0xffff);
            if (length == 0xffff) {
                return null;
            }
            var chPtr2 = wsz + length + 1;
            return (byte*)chPtr2;
        }

        private static void EnsureRoomForTokensAndClear(uint count)
        {
            tokens = new List<List<IntPtr>>();
            for (var i = 0; i < count; i++) {
                tokens.Add(new List<IntPtr>());
            }
        }

        private static unsafe string GetBinaryHextStringFromData(void* pData, uint dataLength)
        {
            var numPtr = (byte*)pData;
            var builder = new StringBuilder();
            builder.AppendFormat("{0:x8}", dataLength);
            for (uint i = 0; i < dataLength; i++) {
                builder.AppendFormat(" {0:x2}", numPtr[i]);
            }
            return builder.ToString();
        }

        private static bool GetDependentPropertyValue(
            TraceEventInfoCPtr traceEventInfo,
            uint propertyIndex,
            List<List<IntPtr>> tokenizedProperties,
            TokenAccessContext context,
            out uint result)
        {
            IntPtr ptr2;
            var infoForProperty = traceEventInfo.GetPropertyInfo(propertyIndex);
            if (
                infoForProperty.HasFlag(
                    PROPERTY_FLAGS.PropertyParamCount | PROPERTY_FLAGS.PropertyParamLength |
                    PROPERTY_FLAGS.PropertyStruct)) {
                result = 0;
                return false;
            }
            switch (context) {
                case TokenAccessContext.Build:
                    ptr2 = tokenizedProperties[(int)propertyIndex].Last();
                    break;

                case TokenAccessContext.Consume:
                    ptr2 = tokenizedProperties[(int)propertyIndex].First();
                    break;

                default:
                    throw new ArgumentException("Not a valid tokenAccessContext");
            }
            return GetULongForTDHData(infoForProperty.SimpleTdhInType, ptr2, out result);
        }

        private static string GetHexStringFor(IFormatProvider formatProvider, long data, int numDigits) =>
            GetHexStringFor(formatProvider, (ulong)data, numDigits);

        private static string GetHexStringFor(IFormatProvider formatProvider, ulong data, int numDigits)
        {
            string format;
            switch (numDigits) {
                case 2: format = "X2"; break;
                case 4: format = "X4"; break;
                case 8: format = "X8"; break;
                case 16: format = "X16"; break;
                default:
                    format = "X" + numDigits.ToString(CultureInfo.InvariantCulture);
                    break;
            }
            return "0x" + data.ToString(format, formatProvider);
        }

        private static bool GetPropertyParams(
            TraceEventInfoCPtr traceEventInfo,
            List<List<IntPtr>> tokenizedProperties,
            TokenAccessContext context,
            EventPropertyInfoCPtr eventPropertyInfo,
            bool is32Bit,
            out uint count,
            out bool isArray,
            out uint length,
            out bool hasExplicitLength)
        {
            count = 0;
            isArray = false;
            length = 0;
            hasExplicitLength = false;
            if (eventPropertyInfo.HasFlag(PROPERTY_FLAGS.PropertyParamCount)) {
                if (
                    !GetDependentPropertyValue(
                        traceEventInfo, eventPropertyInfo.CountPropertyIndex, tokenizedProperties, context,
                        out count)) {
                    return false;
                }
                isArray = true;
            } else {
                count = eventPropertyInfo.Count;
                isArray = count > 1;
            }
            if (!eventPropertyInfo.HasFlag(PROPERTY_FLAGS.PropertyStruct)) {
                if (eventPropertyInfo.HasFlag(PROPERTY_FLAGS.PropertyParamLength)) {
                    if (
                        !GetDependentPropertyValue(
                            traceEventInfo, eventPropertyInfo.LengthPropertyIndex, tokenizedProperties,
                            context, out length)) {
                        return false;
                    }
                    hasExplicitLength = true;
                } else {
                    length = eventPropertyInfo.Length;
                    hasExplicitLength = length != 0;
                    if (eventPropertyInfo.SimpleTdhInType == TDH_IN_TYPE.POINTER) {
                        length = is32Bit ? 4u : 8u;
                    }
                }
            }
            return true;
        }

        private static unsafe string GetSid(void* pData, uint dataLength)
        {
            var numPtr = (byte*)pData;
            var num = (int)dataLength;
            var binaryForm = new byte[num];
            for (var i = 0; i < num; i++) {
                binaryForm[i] = numPtr[i];
            }
            var identifier = new SecurityIdentifier(binaryForm, 0);
            return identifier.ToString();
        }

        private static unsafe int GetSIDLengthFromData(void* pData)
        {
            var numPtr = (byte*)pData;
            return 8 + numPtr[1] * 4;
        }

        private static string GetStringForProperty(
            TraceEventInfoCPtr traceEventInfo,
            EventRecordCPtr eventRecord,
            uint propertyIndex,
            ParseTdhContext parseTdhContext,
            List<List<IntPtr>> tokenizedProperties,
            TokenAccessContext context,
            uint countValidProperties,
            IFormatProvider formatProvider,
            EventMapInfoCPtr eventMapInfo = new EventMapInfoCPtr())
        {
            uint count;
            bool isArray;
            uint length;
            bool hasExplicitLength;
            var flag = eventRecord.Is32Bit(parseTdhContext.NativePointerSize);
            var infoForProperty = traceEventInfo.GetPropertyInfo(propertyIndex);
            if (
                !GetPropertyParams(
                    traceEventInfo, tokenizedProperties, context, infoForProperty, flag, out count,
                    out isArray,
                    out length, out hasExplicitLength))
                throw new InternalErrorException();

            if (length > eventRecord.UserDataLength) {
                return string.Format(
                    CultureInfo.CurrentUICulture,
                    "Unable to parse data (payload too short: expected {0} bytes, have {1})",
                    new object[] { length, eventRecord.UserDataLength });
            }

            if ((count == 1) && !isArray && !infoForProperty.HasFlag(PROPERTY_FLAGS.PropertyStruct)) {
                string str;
                var buffer = tokenizedProperties[(int)propertyIndex].FirstOrDefault();
                if (buffer == new IntPtr()) {
                    return "Unable to parse data";
                }
                if (
                    AdjustPointerPastTDHData(
                        infoForProperty.SimpleTdhInType, infoForProperty.SimpleTdhOutType, buffer, length, hasExplicitLength,
                        flag) == new IntPtr()) {
                    return "Unable to parse data";
                }
                if (TryGetStringForTDHData(
                    infoForProperty.SimpleTdhInType, infoForProperty.SimpleTdhOutType, buffer,
                    eventRecord.TimePoint, flag, length, parseTdhContext, formatProvider, out str, eventMapInfo)) {
                    return str;
                }
                return string.Empty;
            }

            var current = tokenizedProperties[(int)propertyIndex].FirstOrDefault();
            var result = new StringBuilder();
            GetStringForProperty(
                traceEventInfo, eventRecord, propertyIndex, parseTdhContext, tokenizedProperties, context,
                countValidProperties, result, formatProvider, ref current);

            return result.ToString();
        }

        private static void GetStringForProperty(
            TraceEventInfoCPtr traceEventInfo,
            EventRecordCPtr eventRecord,
            uint propertyIndex,
            ParseTdhContext parseTdhContext,
            List<List<IntPtr>> tokenizedProperties,
            TokenAccessContext context,
            uint countValidProperties,
            StringBuilder result,
            IFormatProvider formatProvider,
            ref IntPtr current)
        {
            uint count;
            bool isArray;
            uint length;
            bool hasExplicitLength;
            var flag = eventRecord.Is32Bit(parseTdhContext.NativePointerSize);
            var infoForProperty = traceEventInfo.GetPropertyInfo(propertyIndex);
            if (
                !GetPropertyParams(
                    traceEventInfo, tokenizedProperties, context, infoForProperty, flag, out count, out isArray,
                    out length, out hasExplicitLength)) {
                result.Append("Unable to parse data");
                return;
            }

            if (isArray)
                result.Append("[");

            for (uint i = 0; i < count; ++i) {
                if (i != 0)
                    result.Append(" : ");

                if (infoForProperty.HasFlag(PROPERTY_FLAGS.PropertyStruct)) {
                    result.Append("{");
                    uint structStartIndex = infoForProperty.StructStartIndex;
                    if (structStartIndex >= countValidProperties) {
                        result.Append("Unable to parse data");
                        return;
                    }
                    var num5 = structStartIndex + infoForProperty.NumOfStructMembers;
                    if (num5 >= countValidProperties) {
                        result.Append("Unable to parse data");
                        return;
                    }
                    for (var j = structStartIndex; j < num5; j++) {
                        if (j != structStartIndex) {
                            result.Append("; ");
                        }
                        GetStringForProperty(
                            traceEventInfo, eventRecord, j, parseTdhContext, tokenizedProperties, context,
                            countValidProperties, result, formatProvider, ref current);
                    }
                    for (var k = structStartIndex; k < num5; k++) {
                        tokens[(int)k].RemoveAt(0);
                    }
                    result.Append("}");
                } else {
                    string str;
                    var ptr3 = new IntPtr();
                    if (current == ptr3) {
                        result.Append("Unable to parse data");
                        return;
                    }
                    var ptr2 = AdjustPointerPastTDHData(
                        infoForProperty.SimpleTdhInType, infoForProperty.SimpleTdhOutType, current, length,
                        hasExplicitLength, flag);
                    var ptr4 = new IntPtr();
                    if (ptr2 == ptr4) {
                        result.Append("Unable to parse data");
                        return;
                    }
                    var eventMapInfo = new EventMapInfoCPtr();
                    if (TryGetStringForTDHData(
                        infoForProperty.SimpleTdhInType, infoForProperty.SimpleTdhOutType, current,
                        eventRecord.TimePoint, flag, length, parseTdhContext, formatProvider, out str,
                        eventMapInfo)) {
                        result.Append(str);
                    }
                    current = ptr2;
                }
            }
            if (isArray) {
                result.Append("]");
            }
        }

        public static string GetStringForPropertyAtIndex(
            EventRecordCPtr eventRecord,
            TimePoint eventTimeStamp,
            TraceEventInfoCPtr traceEventInfo,
            uint propertyIndex,
            ParseTdhContext parseTdhContext,
            IFormatProvider formatProvider,
            EventMapInfoCPtr eventMapInfo = new EventMapInfoCPtr())
        {
            if (propertyIndex >= traceEventInfo.TopLevelPropertyCount) {
                return "";
            }
            var count = traceEventInfo.PropertyCount + 1;
            lock (tokenLock) {
                EnsureRoomForTokensAndClear(count);
                var build = TokenAccessContext.Build;
                TryGetTokens(
                    traceEventInfo, eventRecord, parseTdhContext, traceEventInfo.TopLevelPropertyCount, tokens,
                    build, count);
                var num2 = (int)propertyIndex;
                if (!tokens[num2].Any())
                    return "Unable to parse data";

                build = TokenAccessContext.Consume;
                return GetStringForProperty(
                    traceEventInfo, eventRecord, propertyIndex, parseTdhContext, tokens, build, count,
                    formatProvider, eventMapInfo);
            }
        }

        private static unsafe string GetStringForTDHData(
            TDH_IN_TYPE tdhInType,
            TDH_OUT_TYPE tdhOutType,
            IntPtr buffer,
            TimePoint timestamp,
            bool is32BitEventRecord,
            uint dataLength,
            ParseTdhContext parseTdhContext,
            IFormatProvider formatProvider,
            EventMapInfoCPtr eventMapInfo = new EventMapInfoCPtr())
        {
            var pdata = buffer.ToPointer();
            if (pdata == null)
                return string.Empty;

            if (eventMapInfo.HasData) {
                var str = eventMapInfo.ValueAtKey(pdata, tdhInType);
                if (str.Length > 0)
                    return str;
            }

            switch (tdhInType) {
                case TDH_IN_TYPE.NULL:
                    return string.Empty;

                case TDH_IN_TYPE.UNICODESTRING:
                    var chPtr = (char*)pdata;
                    if (dataLength == 0)
                        return new string(chPtr);

                    int len;
                    for (len = 0; len < dataLength; ++len) {
                        if (chPtr[len] == '\0')
                            break;
                    }

                    return new string(chPtr, 0, len);

                case TDH_IN_TYPE.ANSISTRING:
                    if (dataLength == 0)
                        return Marshal.PtrToStringAnsi(new IntPtr(pdata));
                    return Marshal.PtrToStringAnsi(new IntPtr(pdata), (int)dataLength);

                case TDH_IN_TYPE.INT8:
                    return (*(sbyte*)pdata).ToString(formatProvider);

                case TDH_IN_TYPE.UINT8:
                    var byteVal = *(byte*)pdata;
                    switch (tdhOutType) {
                        case TDH_OUT_TYPE.NULL:
                        case TDH_OUT_TYPE.DATETIME:
                        case TDH_OUT_TYPE.BYTE:
                        case TDH_OUT_TYPE.UNSIGNEDBYTE:
                            return byteVal.ToString(formatProvider);
                        case TDH_OUT_TYPE.STRING:
                            return ((char)byteVal).ToString(formatProvider);
                        case TDH_OUT_TYPE.HEXINT8:
                            return GetHexStringFor(formatProvider, byteVal, 2);
                        default:
                            return byteVal.ToString(formatProvider);
                    }

                case TDH_IN_TYPE.INT16:
                    return (*(short*)pdata).ToString(formatProvider);

                case TDH_IN_TYPE.UINT16:
                    var ushortVal = *(ushort*)pdata;
                    switch (tdhOutType) {
                        case TDH_OUT_TYPE.NULL:
                        case TDH_OUT_TYPE.UNSIGNEDSHORT:
                            return ushortVal.ToString(formatProvider);
                        case TDH_OUT_TYPE.STRING:
                            return ((char)ushortVal).ToString(formatProvider);
                        case TDH_OUT_TYPE.HEXINT16:
                            return GetHexStringFor(formatProvider, ushortVal, 4);
                        default:
                            return ushortVal.ToString(formatProvider);
                    }

                case TDH_IN_TYPE.INT32:
                    var intVal = *(int*)pdata;
                    if (tdhOutType == TDH_OUT_TYPE.HRESULT)
                        return GetHexStringFor(formatProvider, intVal, 8);
                    return intVal.ToString(formatProvider);

                case TDH_IN_TYPE.UINT32:
                    var uintVal = *(uint*)pdata;
                    switch (tdhOutType) {
                        case TDH_OUT_TYPE.NULL:
                            return uintVal.ToString(formatProvider);
                        case TDH_OUT_TYPE.HEXINT32:
                        case TDH_OUT_TYPE.ETWTIME:
                        case TDH_OUT_TYPE.ERRORCODE:
                        case TDH_OUT_TYPE.WIN32ERROR:
                        case TDH_OUT_TYPE.NTSTATUS:
                            return GetHexStringFor(formatProvider, uintVal, 8);
                        case TDH_OUT_TYPE.PID: {
                            var pid = uintVal;
                            //ProcessDataCPtr processData = new ProcessDataCPtr(parseTdhContext.ProcessInfoSource.QueryProcess(ref timestamp, pid, Proximity.Exact));
                            //ProcessModernApplicationPair pair = new ProcessModernApplicationPair(processData, parseTdhContext.ModernApplicationProcessInfoSource);
                            //if (pair.HasValue) {
                            //    return pair.ToString();
                            //}
                            return pid.ToString(formatProvider);
                        }
                        case TDH_OUT_TYPE.PORT:
                            return uintVal.ToString(formatProvider);
                        case TDH_OUT_TYPE.IPV4:
                            return uintVal.ToString(formatProvider);
                        default:
                            return uintVal.ToString(formatProvider);
                    }

                case TDH_IN_TYPE.INT64:
                    return (*(long*)pdata).ToString(formatProvider);

                case TDH_IN_TYPE.UINT64:
                    if (tdhOutType == TDH_OUT_TYPE.HEXINT64)
                        return GetHexStringFor(formatProvider, *(ulong*)pdata, 16);
                    return (*(ulong*)pdata).ToString(formatProvider);

                case TDH_IN_TYPE.FLOAT:
                    return (*(float*)pdata).ToString(formatProvider);

                case TDH_IN_TYPE.DOUBLE:
                    return (*(double*)pdata).ToString(formatProvider);

                case TDH_IN_TYPE.BOOLEAN:
                    var boolVal = *(int*)pdata != 0;
                    return boolVal.ToString(formatProvider);

                case TDH_IN_TYPE.BINARY:
                    if (tdhOutType == TDH_OUT_TYPE.IPV6) {
                        if (dataLength == 0)
                            return string.Empty;
                        if (dataLength < 16)
                            return GetBinaryHextStringFromData(pdata, dataLength);

                        for (uint i = 0; i < dataLength; ++i)
                            tempByteArrayForIpV6Parsing[i] = *((byte*)pdata + i);
                        return new IPAddress(tempByteArrayForIpV6Parsing).ToString();
                    }

                    if (tdhOutType == TDH_OUT_TYPE.SOCKETADDRESS && dataLength <= 128) {
                        fixed (char* chRef = temporaryStringForSockAddressString) {
                            var length = temporaryStringForSockAddressString.Length;
                            if (NativeMethods.WSAAddressToString(
                                new IntPtr(pdata), dataLength, new IntPtr(), new IntPtr(chRef),
                                ref length) == 0)
                                return new UnmanagedString(chRef);
                        }
                    }

                    return GetBinaryHextStringFromData(pdata, dataLength);

                case TDH_IN_TYPE.GUID:
                    return (*(Guid*)pdata).ToString(null, formatProvider);

                case TDH_IN_TYPE.POINTER:
                    if (!is32BitEventRecord)
                        return GetHexStringFor(formatProvider, *(ulong*)pdata, 16);
                    return GetHexStringFor(formatProvider, *(uint*)pdata, 8);

                case TDH_IN_TYPE.FILETIME: {
                    var systemTime = new SYSTEMTIME();
                    var lpFileTime = *(FILETIME*)pdata;
                    NativeMethods.FileTimeToSystemTime(ref lpFileTime, out systemTime);
                    return string.Format(
                        CultureInfo.CurrentUICulture, "{0:00}/{1:00}/{2:0000} {3:00}:{4:00}:{5:00}.{6:000}",
                        systemTime.wMonth, systemTime.wDay, systemTime.wYear, systemTime.wHour,
                        systemTime.wMinute, systemTime.wSecond, systemTime.wMilliseconds);
                }

                case TDH_IN_TYPE.SYSTEMTIME: {
                    var systemtime = *(SYSTEMTIME*)pdata;
                    return string.Format(
                        CultureInfo.CurrentUICulture, "{0:00}/{1:00}/{2:0000} {3:00}:{4:00}:{5:00}.{6:000}",
                        systemtime.wMonth, systemtime.wDay, systemtime.wYear, systemtime.wHour,
                        systemtime.wMinute, systemtime.wSecond, systemtime.wMilliseconds);
                }

                case TDH_IN_TYPE.SID:
                    if (dataLength == 0)
                        dataLength = (uint)GetSIDLengthFromData(pdata);
                    return GetSid(pdata, dataLength);

                case TDH_IN_TYPE.HEXINT32:
                    return GetHexStringFor(formatProvider, *(int*)pdata, 8);
                case TDH_IN_TYPE.HEXINT64:
                    return GetHexStringFor(formatProvider, *(long*)pdata, 0x10);

                case TDH_IN_TYPE.COUNTEDSTRING:
                    return new string((char*)((byte*)pdata + 2), 0, *(ushort*)pdata / 2);

                case TDH_IN_TYPE.COUNTEDANSISTRING:
                    return Marshal.PtrToStringAnsi(
                        new IntPtr((byte*)pdata + 2), *(ushort*)pdata);

                case TDH_IN_TYPE.REVERSEDCOUNTEDSTRING:
                case TDH_IN_TYPE.REVERSEDCOUNTEDANSISTRING:
                case TDH_IN_TYPE.NONNULLTERMINATEDSTRING:
                case TDH_IN_TYPE.NONNULLTERMINATEDANSISTRING:
                    throw new NotImplementedException();

                case TDH_IN_TYPE.UNICODECHAR:
                    throw new NotImplementedException();

                case TDH_IN_TYPE.ANSICHAR:
                    throw new NotImplementedException();

                case TDH_IN_TYPE.SIZET:
                    throw new NotImplementedException();

                case TDH_IN_TYPE.HEXDUMP:
                    throw new NotImplementedException();

                case TDH_IN_TYPE.WBEMSID:
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException();
            }
        }

        private static unsafe bool GetULongForTDHData(
            TDH_IN_TYPE tdhInType, IntPtr buffer, out uint result)
        {
            var voidPtr = buffer.ToPointer();
            if (voidPtr == null) {
                result = 0;
                return false;
            }

            switch (tdhInType) {
                case TDH_IN_TYPE.UINT8:
                    result = *(uint*)voidPtr;
                    return true;

                case TDH_IN_TYPE.UINT16:
                    result = *(uint*)voidPtr;
                    return true;

                case TDH_IN_TYPE.UINT32:
                    result = *(uint*)voidPtr;
                    return true;

                default:
                    result = 0;
                    return false;
            }
        }

        private static bool TokenizeProperties(
            TraceEventInfoCPtr traceEventInfo,
            uint propertyIndex,
            List<List<IntPtr>> tokenizedProperties,
            TokenAccessContext context,
            uint countValidTokenizedProperties,
            bool is32Bit,
            ref IntPtr current)
        {
            uint count;
            bool isArray;
            uint length;
            bool hasExplicitLength;
            var infoForProperty = traceEventInfo.GetPropertyInfo(propertyIndex);
            if (
                !GetPropertyParams(
                    traceEventInfo, tokenizedProperties, context, infoForProperty,
                    is32Bit, out count, out isArray, out length, out hasExplicitLength)) {
                tokenizedProperties[(int)propertyIndex].Add(new IntPtr());
                return false;
            }

            tokenizedProperties[(int)propertyIndex].Add(current);
            for (uint i = 0; i < count; i++) {
                if (infoForProperty.HasFlag(PROPERTY_FLAGS.PropertyStruct)) {
                    uint structStartIndex = infoForProperty.StructStartIndex;
                    if (structStartIndex >= countValidTokenizedProperties) {
                        tokenizedProperties[(int)structStartIndex].Add(new IntPtr());
                        return false;
                    }
                    var num5 = structStartIndex + infoForProperty.NumOfStructMembers;
                    if (num5 >= countValidTokenizedProperties) {
                        tokenizedProperties[(int)structStartIndex].Add(new IntPtr());
                        return false;
                    }

                    for (uint j = structStartIndex; j < num5; ++j) {
                        if (
                            !TokenizeProperties(
                                traceEventInfo, j, tokenizedProperties, context, countValidTokenizedProperties,
                                is32Bit, ref current)) {
                            tokenizedProperties[(int)j].Add(new IntPtr());
                            return false;
                        }
                    }
                } else {
                    if (current == IntPtr.Zero)
                        return false;

                    current = AdjustPointerPastTDHData(
                        infoForProperty.SimpleTdhInType, infoForProperty.SimpleTdhOutType, current, length,
                        hasExplicitLength, is32Bit);

                    if (current == IntPtr.Zero)
                        return false;
                }
            }

            return true;
        }

        private static bool TryGetStringForTDHData(
            TDH_IN_TYPE tdhInType,
            TDH_OUT_TYPE tdhOutType,
            IntPtr buffer,
            TimePoint timestamp,
            bool is32BitEventRecord,
            uint dataLength,
            ParseTdhContext parseTdhContext,
            IFormatProvider formatProvider,
            out string result,
            EventMapInfoCPtr eventMapInfo = new EventMapInfoCPtr())
        {
            try {
                result = GetStringForTDHData(
                    tdhInType, tdhOutType, buffer, timestamp, is32BitEventRecord,
                    dataLength, parseTdhContext, formatProvider, eventMapInfo);
                return true;
            } catch {
                result = string.Empty;
                return false;
            }
        }

        private static bool TryGetTokens(
            TraceEventInfoCPtr traceEventInfo,
            EventRecordCPtr eventRecord,
            ParseTdhContext parseTdhContext,
            uint maxTopLevelProperty,
            List<List<IntPtr>> result,
            TokenAccessContext context,
            uint countValidProperties)
        {
            var flag = eventRecord.Is32Bit(parseTdhContext.NativePointerSize);
            var userData = eventRecord.UserData;
            var topLevelPropertyCount = traceEventInfo.TopLevelPropertyCount;
            for (uint i = 0; i < maxTopLevelProperty; ++i) {
                if (
                    !TokenizeProperties(
                        traceEventInfo, i, result, context, countValidProperties, flag, ref userData)) {
                    result[(int)i].Add(new IntPtr());
                    return false;
                }
            }

            return true;
        }

        private enum TokenAccessContext
        {
            Build,
            Consume
        }
    }

    public class ParseTdhContext
    {
        public int NativePointerSize { get; set; } = 8;
    }
}
