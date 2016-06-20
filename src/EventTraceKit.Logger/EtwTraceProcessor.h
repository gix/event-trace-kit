#pragma once
#include "ADT/ArrayRef.h"
#include "EventInfoCache.h"
#include "ITraceProcessor.h"
#include "ITraceSession.h"
#include "Support/Allocator.h"

#include <atomic>
#include <deque>
#include <shared_mutex>
#include <thread>

#include <windows.h>
#include <evntcons.h>

namespace etk
{

class EtwTraceProcessor : public ITraceProcessor
{
public:
    EtwTraceProcessor(std::wstring loggerName, ArrayRef<TraceProviderSpec> providers);
    virtual ~EtwTraceProcessor();

    virtual void SetEventSink(IEventSink* sink) override;
    virtual void StartProcessing() override;
    virtual void StopProcessing() override;
    virtual bool IsEndOfTracing() override;

    virtual void ClearEvents() override
    {
        eventCount = 0;
        events.clear();
        if (sink)
            sink->NotifyNewEvents(0);
    }

    virtual TRACE_LOGFILE_HEADER const* GetLogFileHeader() const override
    {
        if (!traceHandle)
            return nullptr;
        return &traceLogFile.LogfileHeader;
    }

    virtual size_t GetEventCount() override { return eventCount; }
    virtual EventInfo GetEvent(size_t index) const override
    {
        if (index >= eventCount) return EventInfo();

        std::shared_lock<std::shared_mutex> lock(mutex);
        return events[index];
    }

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

    using EventRecordAllocator = BumpPtrAllocator<MallocAllocator>;
    EventRecordAllocator eventRecordAllocator;

    std::deque<EventInfo> events;
    std::atomic<size_t> eventCount;

    IEventSink* sink = nullptr;

    mutable std::shared_mutex mutex;
};

} // namespace etk
