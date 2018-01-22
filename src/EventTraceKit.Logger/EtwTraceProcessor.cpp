#define INITGUID
#include "EtwTraceProcessor.h"

#include "ADT/Guid.h"
#include "ADT/ResPtr.h"
#include "IEventSink.h"
#include "ITraceSession.h"
#include "Support/ErrorHandling.h"
#include "Support/SetThreadName.h"

#include <system_error>
#include <vector>

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

} // namespace

EtwTraceProcessor::EtwTraceProcessor(std::wstring loggerName,
                                     ArrayRef<TraceProviderDescriptor> providers)
    : loggerName(std::move(loggerName))
    , traceHandle()
    , traceLogFile()
{
    for (TraceProviderDescriptor const& provider : providers) {
        std::wstring_view manifest = provider.GetManifest();
        std::wstring_view providerBinary = provider.GetProviderBinary();
        if (!manifest.empty())
            manifests.emplace_back(manifest);
        if (!providerBinary.empty())
            providerBinaries.emplace_back(providerBinary);
    }

    traceLogFile.LogFileName = nullptr;
    traceLogFile.LoggerName = const_cast<wchar_t*>(this->loggerName.c_str());
    traceLogFile.ProcessTraceMode =
        PROCESS_TRACE_MODE_EVENT_RECORD | PROCESS_TRACE_MODE_REAL_TIME;
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
        DWORD const ec = GetLastError();
        UnregisterManifests();
        throw std::system_error(ec, std::system_category());
    }

    if (traceLogFile.LogfileHeader.StartTime.QuadPart == 0)
        QueryPerformanceCounter(&traceLogFile.LogfileHeader.StartTime);

    processorThread = std::thread(&EtwTraceProcessor::ProcessTraceProc, this);
}

void EtwTraceProcessor::StopProcessing()
{
    if (!traceHandle)
        return;

    (void)CloseTrace(traceHandle);
    traceHandle = 0;

    if (processorThread.joinable())
        processorThread.join();
    UnregisterManifests();
}

void EtwTraceProcessor::ProcessTraceProc()
{
    SetCurrentThreadName("ETW Trace Processor");

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
    if (!processorThread.joinable())
        return true;

    DWORD const st = WaitForSingleObject(processorThread.native_handle(), 0);
    return st == WAIT_OBJECT_0;
}

TRACE_LOGFILE_HEADER const* EtwTraceProcessor::GetLogFileHeader() const
{
    if (!traceHandle)
        return nullptr;
    return &traceLogFile.LogfileHeader;
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
CreateEtwTraceProcessor(std::wstring loggerName,
                        ArrayRef<TraceProviderDescriptor> providers)
{
    return std::make_unique<EtwTraceProcessor>(std::move(loggerName), providers);
}

} // namespace etk
