#define INITGUID
#include "EtwTraceProcessor.h"

#include "etk/IEventSink.h"
#include "etk/ITraceSession.h"
#include "etk/Support/ErrorHandling.h"
#include "etk/Support/SetThreadDescription.h"

#include <system_error>

#include <tdh.h>

namespace etk
{
namespace
{

using PFNGetSystemTimeAsFileTime = void(WINAPI*)(LPFILETIME);

PFNGetSystemTimeAsFileTime WINAPI InitializeGetSystemTimeAsFileTime()
{
    // GetSystemTimePreciseAsFileTime requires Windows 8 or later.
    auto const kernel32 = GetModuleHandleW(L"kernel32.dll");
    auto const func = reinterpret_cast<PFNGetSystemTimeAsFileTime>(
        GetProcAddress(kernel32, "GetSystemTimePreciseAsFileTime"));
    if (func)
        return func;

    return &::GetSystemTimeAsFileTime;
}

PFNGetSystemTimeAsFileTime const g_pGetSystemTimeAsFileTime =
    InitializeGetSystemTimeAsFileTime();

void GetSystemTimePrecise(LARGE_INTEGER* systemTime)
{
    FILETIME ft;
    g_pGetSystemTimeAsFileTime(&ft);
    systemTime->u.HighPart = static_cast<LONG>(ft.dwHighDateTime);
    systemTime->u.LowPart = ft.dwLowDateTime;
}

bool IsEventTraceHeader(EVENT_RECORD const& record)
{
    return record.EventHeader.ProviderId == EventTraceGuid &&
           record.EventHeader.EventDescriptor.Opcode == EVENT_TRACE_TYPE_INFO;
}

//! Wrap emplaces to back of container as output iterator
template<typename Container>
class string_back_insert_iterator
{
public:
    using iterator_category = std::output_iterator_tag;
    using value_type = void;
    using difference_type = void;
    using pointer = void;
    using reference = void;

    using container_type = Container;

    explicit string_back_insert_iterator(Container& container)
        : container(std::addressof(container))
    {
    }

    template<typename T>
    string_back_insert_iterator& operator=(T const& value)
    {
        container->push_back(static_cast<typename Container::value_type>(value));
        return *this;
    }

    string_back_insert_iterator& operator*() { return *this; }
    string_back_insert_iterator& operator++() { return *this; }
    string_back_insert_iterator operator++(int) { return *this; }

protected:
    Container* container;
};

template<typename Container>
string_back_insert_iterator<Container> string_back_inserter(Container& c)
{
    return string_back_insert_iterator<Container>(c);
}

} // namespace

EtwTraceProcessor::EtwTraceProcessor(cspan<std::wstring_view> loggerNames)
{
    std::copy(std::begin(loggerNames), std::end(loggerNames),
              string_back_inserter(this->loggerNames));
    traceHandles.reserve(this->loggerNames.size());

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

static void EnsureStartTime(EVENT_TRACE_LOGFILEW& traceLogFile,
                            LARGE_INTEGER& cachedQpcStartTime,
                            LARGE_INTEGER& cachedSystemStartTime)
{
    if (traceLogFile.LogfileHeader.StartTime.QuadPart != 0)
        return;

    if (traceLogFile.ProcessTraceMode & PROCESS_TRACE_MODE_RAW_TIMESTAMP) {
        // The log uses raw timestamps for which we always use QPC.
        if (cachedQpcStartTime.QuadPart == 0)
            QueryPerformanceCounter(&cachedQpcStartTime);

        traceLogFile.LogfileHeader.StartTime = cachedQpcStartTime;
    } else {
        // Timestamps have system time resolution.
        if (cachedSystemStartTime.QuadPart == 0)
            GetSystemTimePrecise(&traceLogFile.LogfileHeader.StartTime);

        traceLogFile.LogfileHeader.StartTime = cachedSystemStartTime;
    }
}

void EtwTraceProcessor::StartProcessing()
{
    if (!traceHandles.empty())
        return;

    LARGE_INTEGER cachedQpcStartTime = {};
    LARGE_INTEGER cachedSystemStartTime = {};
    for (auto& traceLogFile : traceLogFiles) {
        TraceHandle traceHandle{OpenTraceW(&traceLogFile)};
        if (!traceHandle) {
            DWORD const ec = GetLastError();
            throw std::system_error(ec, std::system_category());
        }

        traceHandles.push_back(std::move(traceHandle));

        // The start time seems to never be set automatically for real-time
        // sessions.
        EnsureStartTime(traceLogFile, cachedQpcStartTime, cachedSystemStartTime);

        processorThreads.emplace_back(&EtwTraceProcessor::ProcessTraceProc,
                                      traceHandles.back().Get());
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
}

void EtwTraceProcessor::ProcessTraceProc(TRACEHANDLE traceHandle)
{
    SetCurrentThreadDescription(L"ETW Trace Processor");

    try {
        HRESULT hr = HResultFromWin32(ProcessTrace(&traceHandle, 1, nullptr, nullptr));
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

    DWORD const st =
        WaitForMultipleObjects(threadHandles.size(), threadHandles.data(), TRUE, 0);
    return st == WAIT_OBJECT_0;
}

TRACE_LOGFILE_HEADER const* EtwTraceProcessor::GetLogFileHeader() const
{
    if (traceHandles.empty())
        return nullptr;
    return &traceLogFiles.front().LogfileHeader;
}

std::unique_ptr<ITraceProcessor>
CreateEtwTraceProcessor(cspan<std::wstring_view> loggerName)
{
    return std::make_unique<EtwTraceProcessor>(loggerName);
}

} // namespace etk
