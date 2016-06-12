#define INITGUID
#include "EtwTraceProcessor.h"

#include "ADT/Guid.h"
#include "ADT/ResPtr.h"
#include "ADT/VarStructPtr.h"
#include "ITraceSession.h"
#include "Support/DllExport.h"
#include "Support/ErrorHandling.h"
#include "Support/SetThreadName.h"

#include <cwchar>
#include <new>
#include <vector>
#include <system_error>

#include <Tdh.h>
#include <in6addr.h>

namespace etk
{
namespace
{

template<typename T, typename U>
ETK_ALWAYS_INLINE
T GetAt(U* ptr, size_t offset)
{
    return reinterpret_cast<T>(reinterpret_cast<uint8_t*>(ptr) + offset);
}

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
DWORD GetArraySize(EventInfo info, EVENT_PROPERTY_INFO const& propInfo,
                   USHORT* arraySize)
{
    if ((propInfo.Flags & PropertyParamCount) == 0) {
        *arraySize = propInfo.count;
        return ERROR_SUCCESS;
    }

    DWORD st = ERROR_SUCCESS;

    EVENT_PROPERTY_INFO const& paramInfo =
        info->EventPropertyInfoArray[propInfo.countPropertyIndex];

    PROPERTY_DATA_DESCRIPTOR pdd = {};
    pdd.PropertyName = info.GetAt<ULONGLONG>(paramInfo.NameOffset);
    pdd.ArrayIndex = ULONG_MAX;

    DWORD size = 0;
    st = TdhGetPropertySize(info.record, 0, nullptr, 1, &pdd, &size);

    DWORD count = 0; // Expects the count to be defined by a UINT16 or UINT32
    st = TdhGetProperty(info.record, 0, nullptr, 1, &pdd, size, reinterpret_cast<PBYTE>(&count));

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
DWORD GetPropertyLength(EventInfo info, EVENT_PROPERTY_INFO const& propInfo,
                        USHORT* propertyLength)
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
        DataDescriptor.PropertyName = (ULONGLONG)((PBYTE)(info.info) + info->EventPropertyInfoArray[j].NameOffset);
        DataDescriptor.ArrayIndex = ULONG_MAX;
        st = TdhGetPropertySize(info.record, 0, NULL, 1, &DataDescriptor, &PropertySize);
        st = TdhGetProperty(info.record, 0, NULL, 1, &DataDescriptor, PropertySize, (PBYTE)&Length);
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
    EventInfo info, EVENT_PROPERTY_INFO const& propInfo,
    size_t pointerSize, ArrayRef<uint8_t>& userData, std::wstring& sink,
    bool includeName = true)
{
    DWORD st = ERROR_SUCCESS;

    USHORT propertyLength = 0;
    st = GetPropertyLength(info, propInfo, &propertyLength);
    if (st != ERROR_SUCCESS) {
        wprintf(L"GetPropertyLength failed.\n");
        return st;
    }

    // Get the size of the array if the property is an array.
    USHORT arraySize = 0;
    st = GetArraySize(info, propInfo, &arraySize);
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
                st = FormatProperty(info, pi, pointerSize, userData, sink);
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
            st = GetMapInfo(info.record,
                            GetAt<PWCHAR>(info.info, propInfo.nonStructType.MapNameOffset),
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
            info.info,
            mapInfo.get(),
            static_cast<ULONG>(pointerSize),
            propInfo.nonStructType.InType,
            propInfo.nonStructType.OutType,
            propertyLength,
            static_cast<USHORT>(userData.size()),
            const_cast<BYTE*>(userData.data()),
            &formattedDataSize,
            formattedData.data(),
            &userDataConsumed);

        if (st == ERROR_INSUFFICIENT_BUFFER) {
            formattedData.resize(formattedDataSize);
            formattedDataSize = static_cast<DWORD>(formattedData.size());
            st = TdhFormatProperty(
                info.info,
                mapInfo.get(),
                static_cast<ULONG>(pointerSize),
                propInfo.nonStructType.InType,
                propInfo.nonStructType.OutType,
                propertyLength,
                static_cast<USHORT>(userData.size()),
                const_cast<BYTE*>(userData.data()),
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
            sink.append(GetAt<PWCHAR>(info.info, propInfo.NameOffset));
            sink.append(L"=");
        }

        sink.append(formattedData.data());
    }

    return st;
}

FormattedEvent CreateFormattedEvent(EVENT_RECORD* event, EventInfo eventInfo,
                                    wstring_view message)
{
    FormattedEvent formatted;
    formatted.ProviderId = event->EventHeader.ProviderId;
    formatted.ProcessId = static_cast<unsigned>(event->EventHeader.ProcessId);
    formatted.ThreadId = static_cast<unsigned>(event->EventHeader.ThreadId);
    formatted.ProcessorTime = event->EventHeader.ProcessorTime;
    formatted.Time.dwLowDateTime = event->EventHeader.TimeStamp.LowPart;
    formatted.Time.dwHighDateTime = event->EventHeader.TimeStamp.HighPart;
    formatted.Descriptor = event->EventHeader.EventDescriptor;
    formatted.Message = message;
    formatted.Info = eventInfo;
    return formatted;
}

FormattedEvent CreateUnformattedEvent(EVENT_RECORD* event)
{
    FormattedEvent formatted;
    formatted.ProviderId = event->EventHeader.ProviderId;
    formatted.ProcessId = static_cast<unsigned>(event->EventHeader.ProcessId);
    formatted.ThreadId = static_cast<unsigned>(event->EventHeader.ThreadId);
    formatted.ProcessorTime = event->EventHeader.ProcessorTime;
    formatted.Time.dwLowDateTime = event->EventHeader.TimeStamp.LowPart;
    formatted.Time.dwHighDateTime = event->EventHeader.TimeStamp.HighPart;
    formatted.Descriptor = event->EventHeader.EventDescriptor;
    return formatted;
}

bool IsEventTraceHeader(EVENT_RECORD* event)
{
    return IsEqualGUID(event->EventHeader.ProviderId, EventTraceGuid) &&
        event->EventHeader.EventDescriptor.Opcode == EVENT_TRACE_TYPE_INFO;
}

} // namespace


bool TraceFormatter::FormatEventMessage(
    EventInfo info, size_t pointerSize, std::wstring& sink)
{
    ArrayRef<uint8_t> userData(static_cast<uint8_t*>(info.record->UserData),
                               static_cast<size_t>(info.record->UserDataLength));

    DWORD ec;
    for (ULONG i = 0; i < info->TopLevelPropertyCount; ++i) {
        auto const& pi = info->EventPropertyInfoArray[i];
        unsigned begin = formattedProperties.size();
        ec = FormatProperty(info, pi, pointerSize, userData, formattedProperties, false);
        if (ec != ERROR_SUCCESS)
            return false;

        formattedPropertiesOffsets.push_back(begin);
    }

    formattedPropertiesOffsets.push_back(formattedProperties.size());

    wchar_t const* ptr = info.EventMessage();
    while (ptr) {
        auto begin = ptr;
        while (*ptr && *ptr != L'%')
            ++ptr;
        if (ptr != begin)
            sink.append(begin, ptr - begin);

        if (!*ptr)
            break;

        ++ptr; // Skip %
        if (*ptr == L'n') {
            ++ptr;
            sink += L'\n';
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
            sink += L'%';
            sink += *ptr;
            continue;
        }

        if (index < 1 || static_cast<unsigned>(index) > info->TopLevelPropertyCount) {
            sink.append(begin, ptr - begin);
            continue;
        }

        sink.append(formattedProperties,
                    formattedPropertiesOffsets[index - 1],
                    formattedPropertiesOffsets[index] - formattedPropertiesOffsets[index - 1]);
    }

    return true;
}


EtwTraceProcessor::EtwTraceProcessor(std::wstring loggerName,
                                     ArrayRef<TraceProviderSpec> providers)
    : loggerName(std::move(loggerName))
    , traceHandle()
    , traceLogFile()
{
    messageBuffer.reserve(1024);

    for (TraceProviderSpec const& provider : providers) {
        wstring_view manifest = provider.GetManifest();
        wstring_view providerBinary = provider.GetProviderBinary();
        if (!manifest.empty())
            manifests.push_back(manifest.to_string());
        if (!providerBinary.empty())
            manifests.push_back(providerBinary.to_string());
    }

    traceLogFile.LogFileName = nullptr;
    traceLogFile.LoggerName = const_cast<wchar_t*>(this->loggerName.c_str());
    traceLogFile.ProcessTraceMode = PROCESS_TRACE_MODE_EVENT_RECORD | PROCESS_TRACE_MODE_REAL_TIME;
    traceLogFile.BufferCallback = nullptr;
    traceLogFile.EventRecordCallback = EventRecordCallback;
    traceLogFile.Context = this;
}

EtwTraceProcessor::~EtwTraceProcessor()
{
    EtwTraceProcessor::StopProcessing();
}

void EtwTraceProcessor::SetEventSink(IEventSink* sink)
{
    this->sink = sink;
}

void EtwTraceProcessor::StartProcessing()
{
    if (traceHandle)
        return;

    RegisterManifests();

    traceHandle = OpenTraceW(&traceLogFile);
    if (traceHandle == INVALID_PROCESSTRACE_HANDLE) {
        DWORD ec = GetLastError();
        printf("OpenTrace failed with: %lu\n", ec);

        UnregisterManifests();
        throw std::system_error(ec, std::system_category());
    }

    processorThread = std::thread(ProcessTraceProc, this);
}

void EtwTraceProcessor::StopProcessing()
{
    if (!traceHandle)
        return;

    (void)CloseTrace(traceHandle);
    UnregisterManifests();

    if (processorThread.joinable())
        processorThread.join();
}

DWORD EtwTraceProcessor::ProcessTraceProc(_In_ LPVOID lpParameter)
{
    SetCurrentThreadName("ETW Trace Processor");

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

void EtwTraceProcessor::OnProcessTrace()
{
    ULONG ec = ProcessTrace(&traceHandle, 1, nullptr, nullptr);
    THROW_EC(ec);
}

bool EtwTraceProcessor::IsEndOfTracing()
{
    if (!processorThread.joinable())
        return true;

    DWORD st = WaitForSingleObject(processorThread.native_handle(), 0);
    return st == WAIT_OBJECT_0;
}

template<typename Allocator>
static EVENT_RECORD* CopyEvent(Allocator& alloc, EVENT_RECORD const* event)
{
    auto copy = alloc.Allocate<EVENT_RECORD>();
    *copy = *event;
    // Explicitly clear any supplied context as it may not be valid later on.
    copy->UserContext = nullptr;

    copy->UserData = alloc.Allocate(event->UserDataLength, Alignment(1));
    std::memcpy(copy->UserData, event->UserData, event->UserDataLength);

    copy->ExtendedData =
        alloc.Allocate<EVENT_HEADER_EXTENDED_DATA_ITEM>(event->ExtendedDataCount);
    std::copy_n(event->ExtendedData, event->ExtendedDataCount, copy->ExtendedData);

    for (unsigned i = 0; i < event->ExtendedDataCount; ++i) {
        auto const& src = event->ExtendedData[i];
        auto& dst = copy->ExtendedData[i];

        void* mem = alloc.Allocate(src.DataSize, Alignment(1));
        std::memcpy(mem, reinterpret_cast<void const*>(src.DataPtr),
                    src.DataSize);

        dst.DataSize = src.DataSize;
        dst.DataPtr = reinterpret_cast<uintptr_t>(mem);
    }

    return copy;
}

void EtwTraceProcessor::OnEvent(EVENT_RECORD* event)
{
    // Skip the event if it is the event trace header. Log files contain this
    // event but real-time sessions do not. The event contains the same
    // information as the EVENT_TRACE_LOGFILE.LogfileHeader member.
    if (IsEventTraceHeader(event))
        return;

    events.push_back(CopyEvent(eventRecordAllocator, event));
    size_t newCount = ++eventCount;

    if (sink)
        sink->NotifyNewEvents(newCount);
    return;

    EventInfo eventInfo = eventInfoCache.Get(*event);
    if (!eventInfo) {
        sink->ProcessEvent(CreateUnformattedEvent(event));
        return;
    }

    // Determine whether the event is defined by a MOF class, in an
    // instrumentation manifest, or a WPP template; to use TDH to decode
    // the event, it must be defined by one of these three sources.
    switch (eventInfo->DecodingSource) {
    default: break;

    case DecodingSourceXMLFile: // Instrumentation manifest
        break;
    case DecodingSourceWbem: // MOF class
        break;

    case DecodingSourceWPP: // Not handling the WPP case
        sink->ProcessEvent(CreateUnformattedEvent(event));
        return;
    }

    size_t pointerSize;
    if ((event->EventHeader.Flags & EVENT_HEADER_FLAG_32_BIT_HEADER) != 0)
        pointerSize = 4;
    else
        pointerSize = 8;

    if ((event->EventHeader.Flags & EVENT_HEADER_FLAG_STRING_ONLY) != 0) {
        wstring_view message = static_cast<wchar_t const*>(event->UserData);
        sink->ProcessEvent(CreateFormattedEvent(event, eventInfo, message));
    } else {
        formatter.FormatEventMessage(eventInfo, pointerSize, messageBuffer);
        sink->ProcessEvent(CreateFormattedEvent(event, eventInfo, messageBuffer));
        messageBuffer.clear();
    }
}

void EtwTraceProcessor::RegisterManifests()
{
    TDHSTATUS ec = 0;
    for (std::wstring& manifest : manifests)
        ec = TdhLoadManifest(&manifest[0]);
    for (std::wstring& providerBinary : providerBinaries)
        ec = TdhLoadManifestFromBinary(&providerBinary[0]);
}

void EtwTraceProcessor::UnregisterManifests()
{
    TDHSTATUS ec = 0;
    for (std::wstring& manifest : manifests)
        ec = TdhUnloadManifest(&manifest[0]);
    for (std::wstring& providerBinary : providerBinaries)
        ec = TdhUnloadManifest(&providerBinary[0]);
}

ETK_EXPORT std::unique_ptr<ITraceProcessor> CreateEtwTraceProcessor(
    std::wstring loggerName, ArrayRef<TraceProviderSpec> providers)
{
    return std::make_unique<EtwTraceProcessor>(std::move(loggerName), providers);
}

} // namespace etk
