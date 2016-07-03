#pragma once
#include "ADT/ArrayRef.h"
#include "ITraceProcessor.h"

#include <atomic>
#include <thread>

#include <windows.h>
#include <evntcons.h>

namespace etk
{

class IEventSink;

class EtwTraceProcessor : public ITraceProcessor
{
public:
    EtwTraceProcessor(std::wstring loggerName, ArrayRef<TraceProviderDescriptor> providers);
    virtual ~EtwTraceProcessor();

    virtual void SetEventSink(IEventSink* sink) override;
    virtual void StartProcessing() override;
    virtual void StopProcessing() override;
    virtual bool IsEndOfTracing() override;

    virtual TRACE_LOGFILE_HEADER const* GetLogFileHeader() const override;

private:
    static DWORD WINAPI ProcessTraceProc(_In_ LPVOID lpParameter);
    static VOID WINAPI EventRecordCallback(_In_ PEVENT_RECORD EventRecord);

    void OnProcessTrace();
    void OnEvent(EVENT_RECORD const& record);

    void RegisterManifests();
    void UnregisterManifests();

    std::wstring loggerName;
    std::thread processorThread;
    TRACEHANDLE traceHandle;
    EVENT_TRACE_LOGFILEW traceLogFile;

    std::vector<std::wstring> manifests;
    std::vector<std::wstring> providerBinaries;

    IEventSink* sink = nullptr;
};

} // namespace etk
