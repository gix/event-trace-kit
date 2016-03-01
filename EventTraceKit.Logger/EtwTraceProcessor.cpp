#define INITGUID
#include "EtwTraceProcessor.h"

#include "ADT/Guid.h"
#include "ADT/ResPtr.h"
#include "ADT/VarStructPtr.h"
#include "Support/ErrorHandling.h"

#include <cstring>
#include <cwchar>
#include <tuple>
#include <vector>
#include <system_error>
#include <DbgHelp.h>
#include <Tdh.h>
#include <Winsock2.h>
#include <ws2tcpip.h>
#include <mstcpip.h>
#include <in6addr.h>

namespace etk
{

size_t const MAX_NAME = 256;

EtwTraceProcessor::EtwTraceProcessor(std::wstring loggerName)
    : loggerName(loggerName)
    , traceHandle()
    , logFile()
    , symProcess()
{
}

EtwTraceProcessor::~EtwTraceProcessor()
{
    CloseSym();

    if (traceHandle != 0)
        (void)CloseTrace(traceHandle);

    if (procThread.joinable())
        procThread.join();
}

void EtwTraceProcessor::StartProcessing()
{
    logFile.LogFileName = nullptr;
    logFile.LoggerName = const_cast<wchar_t*>(loggerName.c_str());
    logFile.ProcessTraceMode = PROCESS_TRACE_MODE_EVENT_RECORD | PROCESS_TRACE_MODE_REAL_TIME;
    logFile.BufferCallback = nullptr;
    logFile.EventRecordCallback = EventRecordCallback;
    logFile.Context = this;

    traceHandle = OpenTraceW(&logFile);
    if (traceHandle == INVALID_PROCESSTRACE_HANDLE) {
        DWORD ec = GetLastError();
        printf("OpenTrace failed with: %lu\n", ec);
        throw std::system_error(ec, std::system_category());
    }

    procThread = std::thread(ProcessTraceProc, this);
}

DWORD EtwTraceProcessor::ProcessTraceProc(_In_ LPVOID lpParameter)
{
    try {
        static_cast<EtwTraceProcessor*>(lpParameter)->OnProcessTrace();
    } catch (std::exception const& ex) {
        fprintf(stderr, "Caught exception in ProcessTraceProc: %s\n", ex.what());
        return 1;
    }

    return 0;
}

VOID EtwTraceProcessor::EventRecordCallback(_In_ PEVENT_RECORD EventRecord)
{
    try {
        static_cast<EtwTraceProcessor*>(EventRecord->UserContext)->OnEvent(EventRecord);
    } catch (std::exception const& ex) {
        fprintf(stderr, "Caught exception in EventRecordCallback: %s\n", ex.what());
    }
}

void EtwTraceProcessor::InitSym(DWORD processId)
{
    if (symProcess)
        return;

    DWORD opt = SymGetOptions();
    opt |= SYMOPT_DEBUG;
    SymSetOptions(opt);

    symProcess = OpenProcess(MAXIMUM_ALLOWED, FALSE, processId);
    DWORD ec = GetLastError();
    BOOL ret = SymInitializeW(symProcess, L"C:\\Windows\\symbols\\dll;D:\\var\\vc\\debug", TRUE);
    if (!ret) {
        fprintf(stderr, "SymInitializeW failed: ec=%lu\n", GetLastError());
    }
}

void EtwTraceProcessor::CloseSym()
{
    if (!symProcess)
        return;

    (void)SymCleanup(symProcess);
    CloseHandle(symProcess);
    symProcess = nullptr;
}

void EtwTraceProcessor::OnProcessTrace()
{
    ULONG ec = ProcessTrace(&traceHandle, 1, 0, 0);
    THROW_EC(ec);
}

bool EtwTraceProcessor::IsEndOfTracing()
{
    if (!procThread.joinable())
        return true;

    DWORD st = WaitForSingleObject(procThread.native_handle(), 0);
    return st == WAIT_OBJECT_0;
}

template<typename T, typename U>
ETK_ALWAYS_INLINE
T GetAt(U* ptr, size_t offset)
{
    return reinterpret_cast<T>(reinterpret_cast<uint8_t*>(ptr) + offset);
}

// Get the metadata for the event.
DWORD GetEventInformation(EVENT_RECORD* event, vstruct_ptr<TRACE_EVENT_INFO>& info)
{
    ULONG bufferSize = 0;
    DWORD st = TdhGetEventInformation(event, 0, nullptr, info.get(), &bufferSize);
    if (st == ERROR_INSUFFICIENT_BUFFER) {
        info = make_vstruct<TRACE_EVENT_INFO>(bufferSize);
        st = TdhGetEventInformation(event, 0, nullptr, info.get(), &bufferSize);
    }

    if (st == ERROR_NOT_FOUND)
        return st;
    if (st != ERROR_SUCCESS)
        wprintf(L"TdhGetEventInformation failed with 0x%x.\n", st);

    return st;
}

DWORD PrintProperties(
    EVENT_RECORD* event, TRACE_EVENT_INFO* info, USHORT i,
    LPWSTR structureName, USHORT structIndex, size_t pointerSize);

DWORD FormatProperty(
    EVENT_RECORD* event, TRACE_EVENT_INFO* info, EVENT_PROPERTY_INFO const& propInfo,
    size_t pointerSize, ArrayRef<uint8_t>& userData, std::wstring& sink,
    bool includeName = true);

bool vsprintf(std::wstring& sink, size_t expectedLength, wchar_t const* format, va_list args)
{
    size_t currSize = sink.size();

    bool measured = false;
retry:
    if (expectedLength == 0) {
        va_list a;
        va_copy(a, args);
        int ret = _scwprintf(format, a);
        va_end(a);
        if (ret < 0)
            return false;
        if (ret == 0)
            return true;
        measured = true;
        expectedLength = static_cast<size_t>(ret);
    }

    if (expectedLength > 0)
        sink.resize(currSize + expectedLength);

    int ret = vswprintf(&sink[currSize], expectedLength + 1, format, args);
    if (ret < 0) {
        if (measured)
            return false;
        expectedLength = 0;
        goto retry;
    }

    size_t written = static_cast<unsigned>(ret);
    sink.resize(currSize + written);

    return written <= expectedLength;
}

bool vsprintf(std::wstring& sink, wchar_t const* format, va_list args)
{
    return vsprintf(sink, 0, format, args);
}

bool sprintf(std::wstring& sink, wchar_t const* format, ...)
{
    va_list args;
    va_start(args, format);
    bool ret = vsprintf(sink, 0, format, args);
    va_end(args);
    return ret;
}

bool sprintf(std::wstring& sink, size_t expectedLength, wchar_t const* format, ...)
{
    va_list args;
    va_start(args, format);
    bool ret = vsprintf(sink, expectedLength, format, args);
    va_end(args);
    return ret;
}

void EtwTraceProcessor::OnEvent(EVENT_RECORD* event)
{
    EVENT_HEADER const& header = event->EventHeader;

    size_t size = sizeof(*event);
    size += event->ExtendedDataCount * sizeof(EVENT_HEADER_EXTENDED_DATA_ITEM);
    size += event->UserDataLength;
    for (unsigned i = 0; i < event->ExtendedDataCount; ++i) {
        size += event->ExtendedData[i].DataSize;
    }
    //wprintf(L"record size: %llu (%llu, userdata=%u)\n", static_cast<uint64_t>(size), static_cast<uint64_t>(sizeof(*event)), event->UserDataLength);

    // Skips the event if it is the event trace header. Log files contain this event
    // but real-time sessions do not. The event contains the same information as
    // the EVENT_TRACE_LOGFILE.LogfileHeader member that you can access when you open
    // the trace.
    if (IsEqualGUID(event->EventHeader.ProviderId, EventTraceGuid) &&
        event->EventHeader.EventDescriptor.Opcode == EVENT_TRACE_TYPE_INFO) {
        return; // Skip this event.
    }

    int64_t ns = event->EventHeader.TimeStamp.QuadPart % (1000 * 1000 * 10);

    if (event->ExtendedDataCount > 0) {
        auto const& ext = event->ExtendedData[0];
        if (ext.ExtType == EVENT_HEADER_EXT_TYPE_RELATED_ACTIVITYID) {
            wprintf(L"  EVENT_HEADER_EXT_TYPE_RELATED_ACTIVITYID\n");
        } else if (ext.ExtType == EVENT_HEADER_EXT_TYPE_SID) {
            wprintf(L"  EVENT_HEADER_EXT_TYPE_SID\n");
        } else if (ext.ExtType == EVENT_HEADER_EXT_TYPE_TS_ID) {
            wprintf(L"  EVENT_HEADER_EXT_TYPE_TS_ID\n");
        } else if (ext.ExtType == EVENT_HEADER_EXT_TYPE_INSTANCE_INFO) {
            wprintf(L"  EVENT_HEADER_EXT_TYPE_INSTANCE_INFO\n");
        } else if (ext.ExtType == EVENT_HEADER_EXT_TYPE_STACK_TRACE32) {
            wprintf(L"  EVENT_HEADER_EXT_TYPE_STACK_TRACE32\n");
        } else if (ext.ExtType == EVENT_HEADER_EXT_TYPE_STACK_TRACE64) {
            wprintf(L"  EVENT_HEADER_EXT_TYPE_STACK_TRACE64\n");
            auto st64 = reinterpret_cast<EVENT_EXTENDED_ITEM_STACK_TRACE64 const*>(ext.DataPtr);
            unsigned count = (ext.DataSize - sizeof(ULONG64)) / sizeof(ULONG64);

            for (unsigned i = 0; i < count; ++i) {
                uint64_t addr = st64->Address[i];
                uint64_t disp = 0;
                SYMBOL_INFO_PACKAGEW sip;
                sip.si.SizeOfStruct = sizeof(SYMBOL_INFOW);
                sip.si.MaxNameLen = sizeof(sip.name);

                BOOL ret = SymFromAddrW(symProcess, addr, &disp, &sip.si);
                if (!ret)
                    fwprintf(stderr, L"  [%08llX] %ls\n", addr, L"<unknown>");
                else if (disp == 0)
                    fwprintf(stderr, L"  [%08llX] %ls\n", addr, sip.si.Name);
                else
                    fwprintf(stderr, L"  [%08llX] (%ls+%llu)\n", addr, sip.si.Name, disp);
            }
        }
    }

    DWORD st = ERROR_SUCCESS;

    vstruct_ptr<TRACE_EVENT_INFO> info;
    st = GetEventInformation(event, info);
    if (st == ERROR_NOT_FOUND) {
        fprintf(stderr,
                "[%lld] Unknown event: flags=%d, prop=%d, src=0x%08X(%X), "
                "evt=%d:l%d:t%d:o%d, exlen=%d, userlen=%d\n",
                header.TimeStamp.QuadPart,
                header.Flags,
                header.EventProperty,
                header.ProcessId,
                header.ThreadId,
                header.EventDescriptor.Id,
                header.EventDescriptor.Level,
                header.EventDescriptor.Task,
                header.EventDescriptor.Opcode,
                event->ExtendedDataCount,
                event->UserDataLength);
        return;
    } else if (st != ERROR_SUCCESS) {
        wprintf(L"GetEventInformation failed with %lu\n", st);
        return;
    }

    std::wstring message;

    // Print the time stamp for when the event occurred.
    FILETIME ft = {};
    ft.dwHighDateTime = event->EventHeader.TimeStamp.HighPart;
    ft.dwLowDateTime = event->EventHeader.TimeStamp.LowPart;

    SYSTEMTIME time;
    SYSTEMTIME stLocal;
    FileTimeToSystemTime(&ft, &time);
    SystemTimeToTzSpecificLocalTime(nullptr, &time, &stLocal);

    ULONGLONG timeStamp = event->EventHeader.TimeStamp.QuadPart;
    ULONGLONG nanos = (timeStamp % 10000000) * 100;

    sprintf(message, wcslen(L"YYYY-MM-DD HH:mm:ss.nnnnnnnnn: "),
            L"%04d-%02d-%02d %02d:%02d:%02d.%09llu: ",
            stLocal.wYear, stLocal.wMonth, stLocal.wDay, stLocal.wHour,
            stLocal.wMinute, stLocal.wSecond, nanos);

    // Determine whether the event is defined by a MOF class, in an
    // instrumentation manifest, or a WPP template; to use TDH to decode
    // the event, it must be defined by one of these three sources.
    switch (info->DecodingSource) {
    default: break;

        // Instrumentation manifest
    case DecodingSourceXMLFile: {
        sprintf(message, L"[%u:%u:%u:%u:0x%llX]: ", info->EventDescriptor.Id,
                info->EventDescriptor.Level, info->EventDescriptor.Task,
                info->EventDescriptor.Opcode, info->EventDescriptor.Keyword);
        break;
    }

                                // MOF class
    case DecodingSourceWbem: {
        GuidToString(info->EventGuid, message);
        sprintf(message, 9, L" V:%d T:%d", event->EventHeader.EventDescriptor.Version,
                event->EventHeader.EventDescriptor.Opcode);
        break;
    }

                             // Not handling the WPP case
    case DecodingSourceWPP:
        return;
    }

    // If the event contains event-specific data use TDH to extract
    // the event data. For this example, to extract the data, the event
    // must be defined by a MOF class or an instrumentation manifest.
    size_t pointerSize;
    if ((event->EventHeader.Flags & EVENT_HEADER_FLAG_32_BIT_HEADER) != 0)
        pointerSize = 4;
    else
        pointerSize = 8;

    // Print the event data for all the top-level properties. Metadata for all the
    // top-level properties come before structure member properties in the
    // property information array. If the EVENT_HEADER_FLAG_STRING_ONLY flag is set,
    // the event data is a null-terminated string, so just print it.
    if ((event->EventHeader.Flags & EVENT_HEADER_FLAG_STRING_ONLY) != 0) {
        message.append(static_cast<wchar_t const*>(event->UserData));

        wprintf(L"%ls: %ls\n", message.c_str(),
                GetAt<wchar_t const*>(info.get(), info->EventMessageOffset));
    } else if (false) {
        ArrayRef<uint8_t> userData(static_cast<uint8_t*>(event->UserData),
                                   static_cast<size_t>(event->UserDataLength));

        for (ULONG i = 0; i < info->TopLevelPropertyCount; ++i) {
            auto const& pi = info->EventPropertyInfoArray[i];
            st = FormatProperty(event, info.get(), pi, pointerSize, userData, message);
            if (st != ERROR_SUCCESS) {
                wprintf(L"Printing top level properties failed.\n");
                return;
            }
        }

        wprintf(L"%ls: %ls\n", message.c_str(),
                GetAt<wchar_t const*>(info.get(), info->EventMessageOffset));
    } else {
        ArrayRef<uint8_t> userData(static_cast<uint8_t*>(event->UserData),
                                   static_cast<size_t>(event->UserDataLength));

        std::wstring formattedProperties;
        std::vector<size_t> formattedPropertiesOffsets;

        for (ULONG i = 0; i < info->TopLevelPropertyCount; ++i) {
            auto const& pi = info->EventPropertyInfoArray[i];
            unsigned begin = formattedProperties.size();
            st = FormatProperty(event, info.get(), pi, pointerSize, userData, formattedProperties, false);
            if (st != ERROR_SUCCESS) {
                wprintf(L"Printing top level properties failed.\n");
                return;
            }
            formattedPropertiesOffsets.push_back(begin);
        }

        formattedPropertiesOffsets.push_back(formattedProperties.size());

        wchar_t const* ptr = GetAt<wchar_t const*>(info.get(), info->EventMessageOffset);
        while (ptr) {
            auto begin = ptr;
            while (*ptr && *ptr != L'%')
                ++ptr;
            if (ptr != begin)
                message.append(begin, ptr - begin);

            if (!*ptr)
                break;

            ++ptr; // Skip %
            if (*ptr == L'n') {
                ++ptr;
                message += L'\n';
                continue;
            }

            begin = ptr;
            int index = 0;
            while (*ptr && *ptr >= L'0' && *ptr <= L'9') {
                if (index >= 255)
                    break; // FIXME
                index = (index * 10) + (*ptr - L'0');
                ++ptr;
            }

            if (ptr == begin) {
                // Invalid char after %, ignore.
                ++ptr;
                message += L'%';
                message += *ptr;
                continue;
            }

            if (index < 1 || index > info->TopLevelPropertyCount) {
                message.append(begin, ptr - begin);
                continue;
            }

            message.append(formattedProperties,
                           formattedPropertiesOffsets[index - 1],
                           formattedPropertiesOffsets[index] - formattedPropertiesOffsets[index - 1]);
        }

        wprintf(L"%ls\n", message.c_str());
    }
}

DWORD FormatAndPrintData(
    EVENT_RECORD* event, USHORT inType, USHORT outType, PBYTE data, DWORD dataSize,
    EVENT_MAP_INFO* mapInfo, size_t pointerSize);

// The mapped string values defined in a manifest will contain a trailing space
// in the EVENT_MAP_ENTRY structure. Replace the trailing space with a null-
// terminating character, so that the bit mapped strings are correctly formatted.
void RemoveTrailingSpace(EVENT_MAP_INFO* mapInfo)
{
    for (ULONG i = 0; i < mapInfo->EntryCount; ++i) {
        EVENT_MAP_ENTRY const& entry = mapInfo->MapEntryArray[i];
        wchar_t* str = GetAt<wchar_t*>(mapInfo, entry.OutputOffset);
        str[wcslen(str) - 1] = L'\0';
    }
}

// Get the size of the array. For MOF-based events, the size is specified in the declaration or using
// the MAX qualifier. For manifest-based events, the property can specify the size of the array
// using the count attribute. The count attribute can specify the size directly or specify the name
// of another property in the event data that contains the size.
DWORD GetArraySize(EVENT_RECORD* event, TRACE_EVENT_INFO* info,
                   EVENT_PROPERTY_INFO const& propInfo, USHORT* arraySize)
{
    if ((propInfo.Flags & PropertyParamCount) == 0) {
        *arraySize = propInfo.count;
        return ERROR_SUCCESS;
    }

    DWORD st = ERROR_SUCCESS;

    EVENT_PROPERTY_INFO const& paramInfo =
        info->EventPropertyInfoArray[propInfo.countPropertyIndex];

    PROPERTY_DATA_DESCRIPTOR pdd = {};
    pdd.PropertyName = GetAt<ULONGLONG>(info, paramInfo.NameOffset);
    pdd.ArrayIndex = ULONG_MAX;

    DWORD size = 0;
    st = TdhGetPropertySize(event, 0, nullptr, 1, &pdd, &size);

    DWORD count = 0; // Expects the count to be defined by a UINT16 or UINT32
    st = TdhGetProperty(event, 0, nullptr, 1, &pdd, size, reinterpret_cast<PBYTE>(&count));

    *arraySize = static_cast<USHORT>(count);

    return st;
}


// Both MOF-based events and manifest-based events can specify name/value maps. The
// map values can be integer values or bit values. If the property specifies a value
// map, get the map.
DWORD GetMapInfo(EVENT_RECORD* event, LPWSTR mapName, DWORD decodingSource,
                 vstruct_ptr<EVENT_MAP_INFO>& mapInfo)
{
    DWORD st = ERROR_SUCCESS;

    // Retrieve the required buffer size for the map info.
    DWORD bufferSize = 0;
    st = TdhGetEventMapInformation(event, mapName, mapInfo.get(), &bufferSize);

    if (st == ERROR_INSUFFICIENT_BUFFER) {
        mapInfo = make_vstruct<EVENT_MAP_INFO>(bufferSize);
        st = TdhGetEventMapInformation(event, mapName, mapInfo.get(), &bufferSize);
    }

    if (st == ERROR_SUCCESS) {
        if (decodingSource == DecodingSourceXMLFile)
            RemoveTrailingSpace(mapInfo.get());
    } else if (st == ERROR_NOT_FOUND) {
        st = ERROR_SUCCESS; // This case is okay.
    } else {
        wprintf(L"TdhGetEventMapInformation failed with 0x%x.\n", st);
    }

    return st;
}

// Get the length of the property data. For MOF-based events, the size is inferred from the data type
// of the property. For manifest-based events, the property can specify the size of the property value
// using the length attribute. The length attribute can specify the size directly or specify the name
// of another property in the event data that contains the size. If the property does not include the
// length attribute, the size is inferred from the data type. The length will be zero for variable
// length, null-terminated strings and structures.
DWORD GetPropertyLength(EVENT_RECORD* event, TRACE_EVENT_INFO* info,
                        EVENT_PROPERTY_INFO const& propInfo, USHORT* propertyLength)
{
    DWORD st = ERROR_SUCCESS;
    PROPERTY_DATA_DESCRIPTOR DataDescriptor;
    DWORD PropertySize = 0;

    // If the property is a binary blob and is defined in a manifest, the property can 
    // specify the blob's size or it can point to another property that defines the 
    // blob's size. The PropertyParamLength flag tells you where the blob's size is defined.

    if ((propInfo.Flags & PropertyParamLength) == PropertyParamLength) {
        DWORD Length = 0;  // Expects the length to be defined by a UINT16 or UINT32
        DWORD j = propInfo.lengthPropertyIndex;
        ZeroMemory(&DataDescriptor, sizeof(PROPERTY_DATA_DESCRIPTOR));
        DataDescriptor.PropertyName = (ULONGLONG)((PBYTE)(info)+info->EventPropertyInfoArray[j].NameOffset);
        DataDescriptor.ArrayIndex = ULONG_MAX;
        st = TdhGetPropertySize(event, 0, NULL, 1, &DataDescriptor, &PropertySize);
        st = TdhGetProperty(event, 0, NULL, 1, &DataDescriptor, PropertySize, (PBYTE)&Length);
        *propertyLength = (USHORT)Length;
    } else {
        if (propInfo.length > 0) {
            *propertyLength = propInfo.length;
        } else {
            // If the property is a binary blob and is defined in a MOF class, the extension
            // qualifier is used to determine the size of the blob. However, if the extension 
            // is IPAddrV6, you must set the PropertyLength variable yourself because the 
            // EVENT_PROPERTY_INFO.length field will be zero.

            if (TDH_INTYPE_BINARY == propInfo.nonStructType.InType &&
                TDH_OUTTYPE_IPV6 == propInfo.nonStructType.OutType)
            {
                *propertyLength = (USHORT)sizeof(IN6_ADDR);
            } else if (TDH_INTYPE_UNICODESTRING == propInfo.nonStructType.InType ||
                       TDH_INTYPE_ANSISTRING == propInfo.nonStructType.InType ||
                       (propInfo.Flags & PropertyStruct) == PropertyStruct)
            {
                *propertyLength = propInfo.length;
            } else
            {
                wprintf(L"Unexpected length of 0 for intype %d and outtype %d\n",
                        propInfo.nonStructType.InType,
                        propInfo.nonStructType.OutType);

                st = ERROR_EVT_INVALID_EVENT_DATA;
                goto cleanup;
            }
        }
    }

cleanup:

    return st;
}

DWORD FormatProperty(
    EVENT_RECORD* event, TRACE_EVENT_INFO* info, EVENT_PROPERTY_INFO const& propInfo,
    size_t pointerSize, ArrayRef<uint8_t>& userData, std::wstring& sink,
    bool includeName)
{
    DWORD st = ERROR_SUCCESS;

    USHORT propertyLength = 0;
    st = GetPropertyLength(event, info, propInfo, &propertyLength);
    if (st != ERROR_SUCCESS) {
        wprintf(L"GetPropertyLength failed.\n");
        return st;
    }

    // Get the size of the array if the property is an array.
    USHORT arraySize = 0;
    st = GetArraySize(event, info, propInfo, &arraySize);
    if (st != ERROR_SUCCESS) {
        wprintf(L"GetArraySize failed.\n");
        return st;
    }

    for (USHORT k = 0; k < arraySize; ++k) {
        // If the property is a structure, print the members of the structure.
        if ((propInfo.Flags & PropertyStruct) == PropertyStruct) {
            DWORD lastMember = propInfo.structType.StructStartIndex +
                propInfo.structType.NumOfStructMembers;

            for (USHORT j = propInfo.structType.StructStartIndex; j < lastMember; ++j) {
                EVENT_PROPERTY_INFO const& pi = info->EventPropertyInfoArray[j];
                st = FormatProperty(event, info, pi, pointerSize, userData, sink);
                if (st != ERROR_SUCCESS) {
                    wprintf(L"Printing the members of the structure failed.\n");
                    return st;
                }
            }
            continue;
        }

        // Get the name/value mapping if the property specifies a value map.
        vstruct_ptr<EVENT_MAP_INFO> mapInfo;
        if (propInfo.nonStructType.MapNameOffset != 0) {
            st = GetMapInfo(event,
                            GetAt<PWCHAR>(info, propInfo.nonStructType.MapNameOffset),
                            info->DecodingSource,
                            mapInfo);

            if (st != ERROR_SUCCESS) {
                wprintf(L"GetMapInfo failed\n");
                return st;
            }
        }

        DWORD formattedDataSize = 0;
        USHORT userDataConsumed = 0;

        std::vector<wchar_t> formattedData;
        formattedDataSize = static_cast<DWORD>(formattedData.size());
        st = TdhFormatProperty(
            info,
            mapInfo.get(),
            static_cast<ULONG>(pointerSize),
            propInfo.nonStructType.InType,
            propInfo.nonStructType.OutType,
            propertyLength,
            static_cast<USHORT>(userData.size()),
            const_cast<uint8_t*>(userData.data()),
            &formattedDataSize,
            formattedData.data(),
            &userDataConsumed);

        if (st == ERROR_INSUFFICIENT_BUFFER) {
            formattedData.resize(formattedDataSize);
            formattedDataSize = static_cast<DWORD>(formattedData.size());
            st = TdhFormatProperty(
                info,
                mapInfo.get(),
                static_cast<ULONG>(pointerSize),
                propInfo.nonStructType.InType,
                propInfo.nonStructType.OutType,
                propertyLength,
                static_cast<USHORT>(userData.size()),
                const_cast<uint8_t*>(userData.data()),
                &formattedDataSize,
                formattedData.data(),
                &userDataConsumed);
        }

        if (st != ERROR_SUCCESS) {
            wprintf(L"TdhFormatProperty failed with %lu.\n", st);
            return st;
        }

        userData.remove_prefix(userDataConsumed);
        if (includeName) {
            sink.append(L"  ");
            sink.append(GetAt<PWCHAR>(info, propInfo.NameOffset));
            sink.append(L"=");
            sink.append(formattedData.data());
        } else {
            sink.append(formattedData.data());
        }
    }

    return st;
}

DWORD PrintProperties(
    EVENT_RECORD* event, TRACE_EVENT_INFO* info, USHORT i,
    LPWSTR structureName, USHORT structIndex, size_t pointerSize)
{
    DWORD st = ERROR_SUCCESS;
    EVENT_PROPERTY_INFO const& propInfo = info->EventPropertyInfoArray[i];

    // Get the size of the array if the property is an array.
    USHORT arraySize = 0;
    st = GetArraySize(event, info, propInfo, &arraySize);

    for (USHORT k = 0; k < arraySize; ++k) {
        wprintf(L"%*ls%ls: ", structureName ? 4 : 0, L"", GetAt<LPWSTR>(info, propInfo.NameOffset));

        // If the property is a structure, print the members of the structure.

        if ((propInfo.Flags & PropertyStruct) == PropertyStruct) {
            wprintf(L"\n");

            DWORD lastMember = propInfo.structType.StructStartIndex +
                propInfo.structType.NumOfStructMembers;

            for (USHORT j = propInfo.structType.StructStartIndex; j < lastMember; ++j) {
                st = PrintProperties(event, info, j, GetAt<LPWSTR>(info, propInfo.NameOffset), k, pointerSize);
                if (st != ERROR_SUCCESS) {
                    wprintf(L"Printing the members of the structure failed.\n");
                    return st;
                }
            }
            continue;
        }

        PROPERTY_DATA_DESCRIPTOR dataDescriptors[2] = {};

        // To retrieve a member of a structure, you need to specify an array of descriptors.
        // The first descriptor in the array identifies the name of the structure and the second
        // descriptor defines the member of the structure whose data you want to retrieve.
        ULONG descriptorCount = 0;
        if (structureName) {
            dataDescriptors[0].PropertyName = (ULONGLONG)structureName;
            dataDescriptors[0].ArrayIndex = structIndex;
            dataDescriptors[1].PropertyName = GetAt<ULONGLONG>(info, propInfo.NameOffset);
            dataDescriptors[1].ArrayIndex = k;
            descriptorCount = 2;
        } else {
            dataDescriptors[0].PropertyName = GetAt<ULONGLONG>(info, propInfo.NameOffset);
            dataDescriptors[0].ArrayIndex = k;
            descriptorCount = 1;
        }

        // The TDH API does not support IPv6 addresses. If the output type is TDH_OUTTYPE_IPV6,
        // you will not be able to consume the rest of the event. If you try to consume the
        // remainder of the event, you will get ERROR_EVT_INVALID_EVENT_DATA.

        if (TDH_INTYPE_BINARY == propInfo.nonStructType.InType &&
            TDH_OUTTYPE_IPV6 == propInfo.nonStructType.OutType) {
            wprintf(L"The event contains an IPv6 address. Skipping event.\n");
            st = ERROR_EVT_INVALID_EVENT_DATA;
            break;
        } else {
            // Get the name/value mapping if the property specifies a value map.
            vstruct_ptr<EVENT_MAP_INFO> mapInfo;
            st = GetMapInfo(event,
                            GetAt<PWCHAR>(info, propInfo.nonStructType.MapNameOffset),
                            info->DecodingSource,
                            mapInfo);

            DWORD propertySize = 0;
            st = TdhGetPropertySize(event, 0, nullptr, descriptorCount, dataDescriptors, &propertySize);

            if (st != ERROR_SUCCESS) {
                wprintf(L"TdhGetPropertySize failed with %lu\n", st);
                return st;
            }

            std::vector<uint8_t> pData;
            pData.resize(propertySize);

            st = TdhGetProperty(event, 0, nullptr, descriptorCount,
                                dataDescriptors, propertySize,
                                pData.data());

            if (st != ERROR_SUCCESS) {
                wprintf(L"GetMapInfo failed\n");
                return st;
            }

            st = FormatAndPrintData(
                event,
                propInfo.nonStructType.InType,
                propInfo.nonStructType.OutType,
                pData.data(),
                propertySize,
                mapInfo.get(),
                pointerSize);

            if (st != ERROR_SUCCESS) {
                wprintf(L"GetMapInfo failed\n");
                return st;
            }
        }
    }

    return st;
}

void PrintMapString(PEVENT_MAP_INFO pMapInfo, PBYTE pData);

DWORD FormatAndPrintData(
    EVENT_RECORD* event, USHORT inType, USHORT outType, PBYTE data,
    DWORD dataSize, EVENT_MAP_INFO* mapInfo, size_t pointerSize)
{
    UNREFERENCED_PARAMETER(event);

    DWORD status = ERROR_SUCCESS;

    switch (inType) {
    case TDH_INTYPE_UNICODESTRING:
    case TDH_INTYPE_COUNTEDSTRING:
    case TDH_INTYPE_REVERSEDCOUNTEDSTRING:
    case TDH_INTYPE_NONNULLTERMINATEDSTRING: {
        size_t length = 0;
        switch (inType) {
        case TDH_INTYPE_COUNTEDSTRING:
            length = *(PUSHORT)data;
            break;
        case TDH_INTYPE_REVERSEDCOUNTEDSTRING:
            length = MAKEWORD(HIBYTE((PUSHORT)data), LOBYTE((PUSHORT)data));
            break;
        case TDH_INTYPE_NONNULLTERMINATEDSTRING:
            length = dataSize;
            break;
        default:
            length = wcslen((LPWSTR)data);
            break;
        }

        wprintf(L"%.*ls\n", (unsigned)length, (LPWSTR)data);
        break;
    }

    case TDH_INTYPE_ANSISTRING:
    case TDH_INTYPE_COUNTEDANSISTRING:
    case TDH_INTYPE_REVERSEDCOUNTEDANSISTRING:
    case TDH_INTYPE_NONNULLTERMINATEDANSISTRING: {
        size_t length = 0;
        switch (inType) {
        case TDH_INTYPE_COUNTEDANSISTRING:
            length = *(PUSHORT)data;
            break;
        case TDH_INTYPE_REVERSEDCOUNTEDANSISTRING:
            length = MAKEWORD(HIBYTE((PUSHORT)data), LOBYTE((PUSHORT)data));
            break;
        case TDH_INTYPE_NONNULLTERMINATEDANSISTRING:
            length = dataSize;
            break;
        default:
            length = strlen((LPSTR)data);
            break;
        }

        wprintf(L"%.*s\n", (unsigned)length, (LPSTR)data);
        break;
    }

    case TDH_INTYPE_INT8:
        wprintf(L"%hd\n", *(PCHAR)data);
        break;

    case TDH_INTYPE_UINT8:
        if (outType == TDH_OUTTYPE_HEXINT8)
            wprintf(L"0x%x\n", *(PBYTE)data);
        else
            wprintf(L"%hu\n", *(PBYTE)data);
        break;

    case TDH_INTYPE_INT16:
        wprintf(L"%hd\n", *(PSHORT)data);
        break;

    case TDH_INTYPE_UINT16:
        if (outType == TDH_OUTTYPE_HEXINT16)
            wprintf(L"0x%x\n", *(PUSHORT)data);
        else if (outType == TDH_OUTTYPE_PORT)
            wprintf(L"%hu\n", ntohs(*(PUSHORT)data));
        else
            wprintf(L"%hu\n", *(PUSHORT)data);

        break;

    case TDH_INTYPE_INT32:
        if (outType == TDH_OUTTYPE_HRESULT)
            wprintf(L"0x%x\n", *(PLONG)data);
        else
            wprintf(L"%d\n", *(PLONG)data);
        break;

    case TDH_INTYPE_UINT32:
    {
        if (outType == TDH_OUTTYPE_HRESULT ||
            outType == TDH_OUTTYPE_WIN32ERROR ||
            outType == TDH_OUTTYPE_NTSTATUS ||
            outType == TDH_OUTTYPE_HEXINT32) {
            wprintf(L"0x%x\n", *(PULONG)data);
        } else if (outType == TDH_OUTTYPE_IPV4) {
            wprintf(L"%d.%d.%d.%d\n", (*(PLONG)data >> 0) & 0xff,
                    (*(PLONG)data >> 8) & 0xff,
                    (*(PLONG)data >> 16) & 0xff,
                    (*(PLONG)data >> 24) & 0xff);
        } else if (mapInfo)
            PrintMapString(mapInfo, data);
        else
            wprintf(L"%lu\n", *(PULONG)data);

        break;
    }

    case TDH_INTYPE_INT64:
        wprintf(L"%I64d\n", *(PLONGLONG)data);
        break;

    case TDH_INTYPE_UINT64:
        if (TDH_OUTTYPE_HEXINT64 == outType)
            wprintf(L"0x%llx\n", *(PULONGLONG)data);
        else
            wprintf(L"%I64u\n", *(PULONGLONG)data);
        break;

    case TDH_INTYPE_FLOAT:
        wprintf(L"%f\n", *(PFLOAT)data);
        break;

    case TDH_INTYPE_DOUBLE:
        wprintf(L"%f\n", *(DOUBLE*)data);
        break;

    case TDH_INTYPE_BOOLEAN:
        wprintf(L"%ls\n", (0 == (PBOOL)data) ? L"false" : L"true");
        break;

    case TDH_INTYPE_BINARY: {
        if (TDH_OUTTYPE_IPV6 == outType) {
            WCHAR IPv6AddressAsString[46];
            RtlIpv6AddressToStringW((IN6_ADDR*)data, IPv6AddressAsString);

            wprintf(L"%ls\n", IPv6AddressAsString);
        } else {
            for (DWORD i = 0; i < dataSize; ++i)
                wprintf(L"%.2x", data[i]);

            wprintf(L"\n");
        }

        break;
    }

    case TDH_INTYPE_GUID: {
        wchar_t guidStr[Guid::StringBufSize];
        GuidToString(*reinterpret_cast<GUID*>(data), guidStr);
        wprintf(L"%ls\n", guidStr);
        break;
    }

    case TDH_INTYPE_POINTER:
    case TDH_INTYPE_SIZET:
        if (pointerSize == 4)
            wprintf(L"0x%x\n", *(PULONG)data);
        else
            wprintf(L"0x%llx\n", *(PULONGLONG)data);
        break;

    case TDH_INTYPE_FILETIME:
        break;

    case TDH_INTYPE_SYSTEMTIME:
        break;

    case TDH_INTYPE_HEXINT32:
        wprintf(L"0x%lx\n", *(PULONG)data);
        break;

    case TDH_INTYPE_HEXINT64:
        wprintf(L"0x%llx\n", *(PULONGLONG)data);
        break;

    case TDH_INTYPE_UNICODECHAR:
        wprintf(L"%c\n", *(PWCHAR)data);
        break;

    case TDH_INTYPE_ANSICHAR:
        wprintf(L"%C\n", *(PCHAR)data);
        break;

    case TDH_INTYPE_SID:
    {
        wchar_t userName[MAX_NAME];
        wchar_t domainName[MAX_NAME];
        DWORD cchUserSize = MAX_NAME;
        DWORD cchDomainSize = MAX_NAME;
        SID_NAME_USE eNameUse;

        if (!LookupAccountSidW(NULL, (PSID)data, userName, &cchUserSize, domainName, &cchDomainSize, &eNameUse))
        {
            if (status == ERROR_NONE_MAPPED)
            {
                wprintf(L"Unable to locate account for the specified SID\n");
                status = ERROR_SUCCESS;
            } else {
                wprintf(L"LookupAccountSid failed with %lu\n", status = GetLastError());
            }

            return status;
        } else {
            wprintf(L"%ls\\%ls\n", domainName, userName);
        }

        break;
    }

    case TDH_INTYPE_WBEMSID: {
        wchar_t userName[MAX_NAME];
        wchar_t domainName[MAX_NAME];
        DWORD cchUserSize = MAX_NAME;
        DWORD cchDomainSize = MAX_NAME;
        SID_NAME_USE eNameUse;

        if ((PULONG)data > 0) {
            // A WBEM SID is actually a TOKEN_USER structure followed
            // by the SID. The size of the TOKEN_USER structure differs
            // depending on whether the events were generated on a 32-bit
            // or 64-bit architecture. Also the structure is aligned
            // on an 8-byte boundary, so its size is 8 bytes on a
            // 32-bit computer and 16 bytes on a 64-bit computer.
            // Doubling the pointer size handles both cases.

            data += pointerSize * 2;

            if (!LookupAccountSidW(NULL, (PSID)data, userName, &cchUserSize, domainName, &cchDomainSize, &eNameUse)) {
                if (ERROR_NONE_MAPPED == status)
                {
                    wprintf(L"Unable to locate account for the specified SID\n");
                    status = ERROR_SUCCESS;
                } else {
                    wprintf(L"LookupAccountSid failed with %lu\n", status = GetLastError());
                }

                return status;
            } else {
                wprintf(L"%ls\\%ls\n", domainName, userName);
            }
        }

        break;
    }

    default:
        status = ERROR_NOT_FOUND;
    }

    return status;
}


void PrintMapString(PEVENT_MAP_INFO pMapInfo, PBYTE pData)
{
    BOOL MatchFound = FALSE;

    if ((pMapInfo->Flag & EVENTMAP_INFO_FLAG_MANIFEST_VALUEMAP) == EVENTMAP_INFO_FLAG_MANIFEST_VALUEMAP ||
        ((pMapInfo->Flag & EVENTMAP_INFO_FLAG_WBEM_VALUEMAP) == EVENTMAP_INFO_FLAG_WBEM_VALUEMAP &&
         (pMapInfo->Flag & (~EVENTMAP_INFO_FLAG_WBEM_VALUEMAP)) != EVENTMAP_INFO_FLAG_WBEM_FLAG))
    {
        if ((pMapInfo->Flag & EVENTMAP_INFO_FLAG_WBEM_NO_MAP) == EVENTMAP_INFO_FLAG_WBEM_NO_MAP) {
            wprintf(L"%ls\n", (LPWSTR)((PBYTE)pMapInfo + pMapInfo->MapEntryArray[*(PULONG)pData].OutputOffset));
        } else {
            for (DWORD i = 0; i < pMapInfo->EntryCount; i++)
            {
                if (pMapInfo->MapEntryArray[i].Value == *(PULONG)pData)
                {
                    wprintf(L"%ls\n", (LPWSTR)((PBYTE)pMapInfo + pMapInfo->MapEntryArray[i].OutputOffset));
                    MatchFound = TRUE;
                    break;
                }
            }

            if (FALSE == MatchFound)
            {
                wprintf(L"%lu\n", *(PULONG)pData);
            }
        }
    } else if ((pMapInfo->Flag & EVENTMAP_INFO_FLAG_MANIFEST_BITMAP) == EVENTMAP_INFO_FLAG_MANIFEST_BITMAP ||
               (pMapInfo->Flag & EVENTMAP_INFO_FLAG_WBEM_BITMAP) == EVENTMAP_INFO_FLAG_WBEM_BITMAP ||
               ((pMapInfo->Flag & EVENTMAP_INFO_FLAG_WBEM_VALUEMAP) == EVENTMAP_INFO_FLAG_WBEM_VALUEMAP &&
                (pMapInfo->Flag & (~EVENTMAP_INFO_FLAG_WBEM_VALUEMAP)) == EVENTMAP_INFO_FLAG_WBEM_FLAG))
    {
        if ((pMapInfo->Flag & EVENTMAP_INFO_FLAG_WBEM_NO_MAP) == EVENTMAP_INFO_FLAG_WBEM_NO_MAP)
        {
            DWORD BitPosition = 0;

            for (DWORD i = 0; i < pMapInfo->EntryCount; i++)
            {
                if ((*(PULONG)pData & (BitPosition = (1 << i))) == BitPosition)
                {
                    wprintf(L"%ls%ls",
                            (MatchFound) ? L" | " : L"",
                            (LPWSTR)((PBYTE)pMapInfo + pMapInfo->MapEntryArray[i].OutputOffset));

                    MatchFound = TRUE;
                }
            }

        } else
        {
            for (DWORD i = 0; i < pMapInfo->EntryCount; i++)
            {
                if ((pMapInfo->MapEntryArray[i].Value & *(PULONG)pData) == pMapInfo->MapEntryArray[i].Value)
                {
                    wprintf(L"%ls%ls",
                            (MatchFound) ? L" | " : L"",
                            (LPWSTR)((PBYTE)pMapInfo + pMapInfo->MapEntryArray[i].OutputOffset));

                    MatchFound = TRUE;
                }
            }
        }

        if (MatchFound) {
            wprintf(L"\n");
        } else {
            wprintf(L"%lu\n", *(PULONG)pData);
        }
    }
}

} // namespace etk
