#define INITGUID
#include "EtwTraceProcessor.h"

#include "ADT/WaitEvent.h"
#include "IEventSink.h"
#include "ITraceSession.h"
#include "Support/ErrorHandling.h"
#include "Support/SetThreadName.h"

#include <system_error>

#include <tdh.h>

namespace etk
{
namespace
{

bool IsEventTraceHeader(EVENT_RECORD const& record)
{
    return record.EventHeader.ProviderId == EventTraceGuid &&
           record.EventHeader.EventDescriptor.Opcode == EVENT_TRACE_TYPE_INFO;
}

//! Wrap emplaces to back of container as output iterator
template<typename Container>
class string_back_inserter
{
public:
    using iterator_category = std::output_iterator_tag;
    using value_type = void;
    using difference_type = void;
    using pointer = void;
    using reference = void;

    using container_type = Container;

    explicit string_back_inserter(Container& container)
        : container(std::addressof(container))
    {
    }

    template<typename T>
    string_back_inserter& operator=(T const& value)
    {
        container->push_back(static_cast<typename Container::value_type>(value));
        return *this;
    }

    string_back_inserter& operator*() { return *this; }
    string_back_inserter& operator++() { return *this; }
    string_back_inserter operator++(int) { return *this; }

protected:
    Container* container;
};

template<typename Container>
string_back_inserter<Container> sting_back_inserter(Container& c)
{
    return string_back_inserter<Container>(c);
}

} // namespace

EtwTraceProcessor::EtwTraceProcessor(ArrayRef<std::wstring_view> loggerNames,
                                     ArrayRef<std::wstring_view> eventManifests,
                                     ArrayRef<std::wstring_view> providerBinaries)
{
    std::copy(std::begin(loggerNames), std::end(loggerNames),
              sting_back_inserter(this->loggerNames));
    traceHandles.reserve(this->loggerNames.size());

    std::copy(std::begin(eventManifests), std::end(eventManifests),
              sting_back_inserter(this->manifests));
    std::copy(std::begin(providerBinaries), std::end(providerBinaries),
              sting_back_inserter(this->providerBinaries));

    for (auto const& name : this->loggerNames) {
        auto& logFile = traceLogFiles.emplace_back();
        logFile.LogFileName = nullptr;
        logFile.LoggerName = const_cast<wchar_t*>(name.c_str());
        logFile.ProcessTraceMode =
            PROCESS_TRACE_MODE_EVENT_RECORD | PROCESS_TRACE_MODE_REAL_TIME;
        logFile.BufferCallback = nullptr;
        logFile.EventRecordCallback = EventRecordCallback;
        logFile.Context = this;
    }
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
    if (!traceHandles.empty())
        return;

    RegisterManifests();

    for (auto& traceLogFile : traceLogFiles) {
        TraceHandle traceHandle{OpenTraceW(&traceLogFile)};
        if (!traceHandle) {
            DWORD const ec = GetLastError();
            UnregisterManifests();
            throw std::system_error(ec, std::system_category());
        }

        traceHandles.push_back(std::move(traceHandle));
        if (traceLogFile.LogfileHeader.StartTime.QuadPart == 0)
            QueryPerformanceCounter(&traceLogFile.LogfileHeader.StartTime);

        processorThreads.emplace_back(
            &EtwTraceProcessor::ProcessTraceProc, traceHandles.back().Get());
    }
}

void EtwTraceProcessor::StopProcessing()
{
    if (traceHandles.empty())
        return;

    traceHandles.clear();

    for (auto& thread : processorThreads) {
        if (thread.joinable())
            thread.join();
    }

    UnregisterManifests();
}

void EtwTraceProcessor::ProcessTraceProc(TRACEHANDLE traceHandle)
{
    SetCurrentThreadName("ETW Trace Processor");

    try {
        HRESULT hr = HResultFromWin32(
            ProcessTrace(&traceHandle, 1, nullptr, nullptr));
        (void)ETK_TRACE_HR(hr);
    } catch (...) {
        fprintf(stderr, "Suppressing unhandled exception in ProcessTraceProc\n");
    }
}

VOID EtwTraceProcessor::EventRecordCallback(_In_ PEVENT_RECORD EventRecord)
{
    // Skip the event if it is the event trace header. Log files contain this
    // event but real-time sessions do not. The event contains the same
    // information as the EVENT_TRACE_LOGFILE.LogfileHeader member.
    if (IsEventTraceHeader(*EventRecord))
        return;

    try {
        auto sink = static_cast<EtwTraceProcessor*>(EventRecord->UserContext)->sink;
        sink->ProcessEvent(*EventRecord);
    } catch (std::exception const& ex) {
        fprintf(stderr, "Caught exception in EventRecordCallback: %s\n", ex.what());
    }
}

bool EtwTraceProcessor::IsEndOfTracing()
{
    if (processorThreads.empty())
        return true;

    SmallVector<HANDLE, 2> threadHandles;
    for (auto& thread : processorThreads)
        threadHandles.push_back(thread.native_handle());

    DWORD const st = WaitForMultipleObjects(threadHandles.size(), threadHandles.data(), TRUE, 0);
    return st == WAIT_OBJECT_0;
}

TRACE_LOGFILE_HEADER const* EtwTraceProcessor::GetLogFileHeader() const
{
    if (traceHandles.empty())
        return nullptr;
    return &traceLogFiles.front().LogfileHeader;
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
    // for (std::wstring& providerBinary : providerBinaries)
    //    ec = TdhUnloadManifest(&providerBinary[0]);
}

std::unique_ptr<ITraceProcessor>
CreateEtwTraceProcessor(ArrayRef<std::wstring_view> loggerName,
                        ArrayRef<std::wstring_view> eventManifests,
                        ArrayRef<std::wstring_view> providerBinaries)
{
    return std::make_unique<EtwTraceProcessor>(loggerName, eventManifests,
                                               providerBinaries);
}

} // namespace etk
