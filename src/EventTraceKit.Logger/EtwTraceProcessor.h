#pragma once
#include "ADT/SmallVector.h"
#include "ADT/ArrayRef.h"
#include "ADT/Handle.h"
#include "ITraceProcessor.h"

#include <atomic>
#include <thread>

#include <windows.h>
#include <evntcons.h>

namespace etk
{

struct TraceHandleTraits
{
    using HandleType = TRACEHANDLE;
    constexpr static HandleType InvalidHandle() noexcept { return INVALID_PROCESSTRACE_HANDLE; }
    constexpr static bool IsValid(HandleType h) noexcept { return h != InvalidHandle(); }
    static void Close(HandleType h) noexcept { ::CloseTrace(h); }
};
using TraceHandle = Handle<TraceHandleTraits>;

class IEventSink;

class EtwTraceProcessor : public ITraceProcessor
{
public:
    EtwTraceProcessor(ArrayRef<std::wstring_view> loggerNames,
                      ArrayRef<std::wstring_view> eventManifests,
                      ArrayRef<std::wstring_view> providerBinaries);
    virtual ~EtwTraceProcessor();

    virtual void SetEventSink(IEventSink* sink) override;
    virtual void StartProcessing() override;
    virtual void StopProcessing() override;
    virtual bool IsEndOfTracing() override;

    virtual TRACE_LOGFILE_HEADER const* GetLogFileHeader() const override;

private:
    static VOID WINAPI EventRecordCallback(_In_ PEVENT_RECORD EventRecord);

    static void ProcessTraceProc(TRACEHANDLE traceHandle);

    void RegisterManifests();
    void UnregisterManifests();

    struct X
    {
        std::wstring LoggerName;
        EVENT_TRACE_LOGFILEW TraceLogFile;
        TraceHandle Handle;
        std::thread ProcessorThread;
    };

    SmallVector<std::wstring, 2> loggerNames;
    SmallVector<EVENT_TRACE_LOGFILEW, 2> traceLogFiles;
    SmallVector<TraceHandle, 2> traceHandles;
    SmallVector<std::thread, 2> processorThreads;

    std::vector<std::wstring> manifests;
    std::vector<std::wstring> providerBinaries;

    IEventSink* sink = nullptr;
};

} // namespace etk
