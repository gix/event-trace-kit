#pragma once
#include "ADT/ArrayRef.h"
#include "ADT/LruCache.h"
#include "EventInfoCache.h"
#include "ITraceProcessor.h"
#include "ITraceSession.h"

#include <windows.h>
#include <thread>
#include <evntcons.h>

namespace etk
{

class TraceFormatter
{
public:
    bool FormatEventMessage(
        EventInfo info, size_t pointerSize, std::wstring& sink);

private:
    std::wstring formattedProperties;
    std::vector<size_t> formattedPropertiesOffsets;
};

class EtwTraceProcessor : public ITraceProcessor
{
public:
    EtwTraceProcessor(std::wstring loggerName, ArrayRef<TraceProviderSpec> providers);
    virtual ~EtwTraceProcessor();

    virtual void SetEventSink(IEventSink* sink) override;
    virtual void StartProcessing() override;
    virtual void StopProcessing() override;
    virtual bool IsEndOfTracing() override;

private:
    static DWORD WINAPI ProcessTraceProc(_In_ LPVOID lpParameter);
    static VOID WINAPI EventRecordCallback(_In_ PEVENT_RECORD EventRecord);

    void OnProcessTrace();
    void OnEvent(EVENT_RECORD* eventRecord);

    void RegisterManifests();
    void UnregisterManifests();

    std::wstring loggerName;
    std::thread processorThread;
    TRACEHANDLE traceHandle;
    EVENT_TRACE_LOGFILEW traceLogFile;
    EventInfoCache eventInfoCache;
    std::vector<std::wstring> manifests;
    std::vector<std::wstring> providerBinaries;

    IEventSink* sink = nullptr;
    TraceFormatter formatter;
    std::wstring messageBuffer;
};

} // namespace etk
