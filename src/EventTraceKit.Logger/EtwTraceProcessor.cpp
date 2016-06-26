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

EtwTraceProcessor::EtwTraceProcessor(std::wstring loggerName,
                                     ArrayRef<TraceProviderSpec> providers)
    : loggerName(std::move(loggerName))
    , traceHandle()
    , traceLogFile()
{
    for (TraceProviderSpec const& provider : providers) {
        wstring_view manifest = provider.GetManifest();
        wstring_view providerBinary = provider.GetProviderBinary();
        if (!manifest.empty())
            manifests.push_back(manifest.to_string());
        if (!providerBinary.empty())
            providerBinaries.push_back(providerBinary.to_string());
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

EventInfo EtwTraceProcessor::GetEvent(size_t index) const
{
    if (index >= eventCount) return EventInfo();

    std::shared_lock<std::shared_mutex> lock(mutex);
    return events[index];
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

void EtwTraceProcessor::ClearEvents()
{
    eventCount = 0;
    std::unique_lock<std::shared_mutex> lock(mutex);
    events.clear();
    if (sink)
        sink->NotifyNewEvents(0);
}

TRACE_LOGFILE_HEADER const* EtwTraceProcessor::GetLogFileHeader() const
{
    if (!traceHandle)
        return nullptr;
    return &traceLogFile.LogfileHeader;
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

    EVENT_RECORD* eventCopy = CopyEvent(eventRecordAllocator, event);

    {
        std::unique_lock<std::shared_mutex> lock(mutex);
        events.push_back(eventInfoCache.Get(*eventCopy));
    }

    size_t newCount = ++eventCount;

    if (sink)
        sink->NotifyNewEvents(newCount);

#if 0
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
#endif
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
    //for (std::wstring& providerBinary : providerBinaries)
    //    ec = TdhUnloadManifest(&providerBinary[0]);
}

ETK_EXPORT std::unique_ptr<ITraceProcessor> CreateEtwTraceProcessor(
    std::wstring loggerName, ArrayRef<TraceProviderSpec> providers)
{
    return std::make_unique<EtwTraceProcessor>(std::move(loggerName), providers);
}

} // namespace etk
