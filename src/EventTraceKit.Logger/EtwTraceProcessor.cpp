#define INITGUID
#include "EtwTraceProcessor.h"

#include "ADT/Guid.h"
#include "ADT/ResPtr.h"
#include "IEventSink.h"
#include "ITraceSession.h"
#include "Support/ErrorHandling.h"
#include "Support/SetThreadName.h"

#include <vector>
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

} // namespace

EtwTraceProcessor::EtwTraceProcessor(std::wstring loggerName,
                                     ArrayRef<TraceProviderDescriptor> providers)
    : loggerName(std::move(loggerName))
    , traceHandle()
    , traceLogFile()
{
    for (TraceProviderDescriptor const& provider : providers) {
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
        static_cast<EtwTraceProcessor*>(EventRecord->UserContext)->OnEvent(*EventRecord);
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

TRACE_LOGFILE_HEADER const* EtwTraceProcessor::GetLogFileHeader() const
{
    if (!traceHandle)
        return nullptr;
    return &traceLogFile.LogfileHeader;
}

void EtwTraceProcessor::OnEvent(EVENT_RECORD const& record)
{
    // Skip the event if it is the event trace header. Log files contain this
    // event but real-time sessions do not. The event contains the same
    // information as the EVENT_TRACE_LOGFILE.LogfileHeader member.
    if (IsEventTraceHeader(record))
        return;

    sink->ProcessEvent(record);
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

std::unique_ptr<ITraceProcessor> CreateEtwTraceProcessor(
    std::wstring loggerName, ArrayRef<TraceProviderDescriptor> providers)
{
    return std::make_unique<EtwTraceProcessor>(std::move(loggerName), providers);
}

} // namespace etk
