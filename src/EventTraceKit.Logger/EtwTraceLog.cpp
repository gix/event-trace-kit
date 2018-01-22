#include "ITraceLog.h"

#include "EventInfoCache.h"
#include "Support/Allocator.h"

#include <atomic>
#include <condition_variable>
#include <deque>
#include <shared_mutex>
#include <string>
#include <vector>
#include "Support/SetThreadName.h"

namespace etk
{

namespace
{

class ManualResetEventSlim
{
public:
    ManualResetEventSlim(bool signaled = false)
        : signaled(signaled)
    {
    }

    void Set()
    {
        {
            std::unique_lock<std::mutex> lock(mutex);
            signaled = true;
        }

        cv.notify_all();
    }

    void Reset()
    {
        std::unique_lock<std::mutex> lock(mutex);
        signaled = false;
    }


    void Wait()
    {
        std::unique_lock<std::mutex> lock(mutex);
        while (!signaled)
            cv.wait(lock);
    }

private:
    std::mutex mutex;
    std::condition_variable cv;
    bool signaled;
};

class EtwTraceLog : public ITraceLog
{
public:
    explicit EtwTraceLog();

    void SetCallback(TraceLogEventsChangedCallback* callback, void* state)
    {
        this->changedCallback = callback;
        this->changedCallbackState = state;
    }

    virtual void ProcessEvent(EVENT_RECORD const& record) override;

    virtual void Clear() override;

    virtual size_t GetEventCount() override { return eventCount; }

    virtual EventInfo GetEvent(size_t index) const override;

    virtual void RegisterManifests() override;
    virtual void UnregisterManifests() override;

private:
    EventInfoCache eventInfoCache;
    std::vector<std::wstring> manifests;
    std::vector<std::wstring> providerBinaries;

    using EventRecordAllocator = BumpPtrAllocator<MallocAllocator, 10 * 1024 * 1024>;
    EventRecordAllocator eventRecordAllocator;

    std::deque<EventInfo> events;
    std::atomic<size_t> eventCount{};

    mutable std::shared_mutex mutex;
    TraceLogEventsChangedCallback* changedCallback;
    void* changedCallbackState;
};

template<typename Allocator>
EVENT_RECORD* CopyEvent(Allocator& alloc, EVENT_RECORD const* record)
{
    auto copy = alloc.Allocate<EVENT_RECORD>();
    *copy = *record;
    // Explicitly clear any supplied context as it may not be valid later on.
    copy->UserContext = nullptr;

    copy->UserData = alloc.Allocate(record->UserDataLength, Alignment(1));
    std::memcpy(copy->UserData, record->UserData, record->UserDataLength);

    copy->ExtendedData =
        alloc.Allocate<EVENT_HEADER_EXTENDED_DATA_ITEM>(record->ExtendedDataCount);
    std::copy_n(record->ExtendedData, record->ExtendedDataCount, copy->ExtendedData);

    for (unsigned i = 0; i < record->ExtendedDataCount; ++i) {
        auto const& src = record->ExtendedData[i];
        auto& dst = copy->ExtendedData[i];

        void* mem = alloc.Allocate(src.DataSize, Alignment(1));
        std::memcpy(mem, reinterpret_cast<void const*>(src.DataPtr),
                    src.DataSize);

        dst.DataSize = src.DataSize;
        dst.DataPtr = reinterpret_cast<uintptr_t>(mem);
    }

    return copy;
}

void NullCallback(size_t, void*) {}
bool AlwaysFilter(void*, void*, size_t) { return true; }

class FilteredTraceLog : public IFilteredTraceLog
{
public:
    explicit FilteredTraceLog(TraceLogEventsChangedCallback* callback = nullptr,
                              TraceLogFilterEvent* filter = nullptr)
        : filter(filter ? filter : &AlwaysFilter)
        , changedCallback(callback ? callback : &NullCallback)
        , changedCallbackState(nullptr)
    {
        running = true;
        filterThread = std::thread(std::bind(&FilteredTraceLog::ThreadProc, this));
    }

    ~FilteredTraceLog()
    {
        running = false;
        changedEvent.Set();
        if (filterThread.joinable())
            filterThread.join();
    }

    virtual size_t GetEventCount() override { return eventCount; }

    virtual EventInfo GetEvent(size_t index) const override
    {
        if (index >= eventCount) return EventInfo();

        std::shared_lock<decltype(mutex)> lock(mutex);
        if (index < events.size())
            return events[index];
        return EventInfo();
    }

    virtual void SetFilter(TraceLogFilterEvent* filter) override
    {
        pendingFilter = filter != nullptr ? filter : &AlwaysFilter;
        changedEvent.Set();
    }

    void SetLog(ITraceLog* traceLog)
    {
        this->traceLog = traceLog;
    }

    static void Callback(size_t /*newCount*/, void* state)
    {
        static_cast<FilteredTraceLog*>(state)->changedEvent.Set();
    }

    ManualResetEventSlim changedEvent;

private:
    void ThreadProc()
    {
        SetCurrentThreadName("ETW Filter Thread");

        for (;;) {
            changedEvent.Wait();
            if (!running)
                break;

            changedEvent.Reset();
            auto const newFilter = pendingFilter.exchange(nullptr);
            if (newFilter) {
                filter = newFilter;
                Rebuild();
                continue;
            }

            ProcessEvents();
        }
    }

    void ProcessEvents()
    {
        size_t const newTotal = traceLog->GetEventCount();

        if (newTotal > prevTotal)
            ProcessLog(prevTotal, newTotal);
        else if (newTotal == 0)
            Clear();
        else if (newTotal < prevTotal)
            Rebuild();

        prevTotal = newTotal;
    }

    void Rebuild()
    {
        Clear();
        ProcessLog(0, traceLog->GetEventCount());
    }

    void ProcessLog(size_t begin, size_t end)
    {
        size_t count = 0;
        for (size_t i = begin; i < end; ++i) {
            EventInfo const evt = traceLog->GetEvent(i);
            if (!evt.Record())
                break;

            if (MatchesFilter(evt)) {
                std::unique_lock<decltype(mutex)> lock(mutex);
                events.push_back(evt);
                ++count;
            }

            if (count > RebuildBatchSize) {
                AddCount(count);
                count = 0;
            }
        }

        if (count > 0)
            AddCount(count);
    }

    bool MatchesFilter(EventInfo const& evt) const
    {
        return filter(evt.Record(), evt.Info(), evt.InfoSize());
    }

    void Clear()
    {
        SetCount(0);
        std::unique_lock<decltype(mutex)> lock(mutex);
        events.clear();
        events.shrink_to_fit();
    }

    void AddCount(size_t additionalCount)
    {
        size_t const total = eventCount += additionalCount;
        changedCallback(total, changedCallbackState);
    }

    void SetCount(size_t newCount)
    {
        eventCount = newCount;
        changedCallback(newCount, changedCallbackState);
    }

    static size_t const RebuildBatchSize = 50;

    // Owned by ThreadProc
    size_t prevTotal = 0;
    TraceLogFilterEvent* filter;

    // Shared
    mutable std::shared_mutex mutex;
    std::deque<EventInfo> events;
    std::atomic<size_t> eventCount{};
    std::atomic<TraceLogFilterEvent*> pendingFilter{};

    std::atomic<bool> running{};
    std::thread filterThread;

    // Immutable
    ITraceLog* traceLog{};
    TraceLogEventsChangedCallback* changedCallback;
    void* changedCallbackState;
};

} // namespace

EtwTraceLog::EtwTraceLog()
    : changedCallback(&NullCallback)
    , changedCallbackState()
{
}

void EtwTraceLog::ProcessEvent(EVENT_RECORD const& record)
{
    EVENT_RECORD* eventCopy = CopyEvent(eventRecordAllocator, &record);

    {
        std::unique_lock<std::shared_mutex> lock(mutex);
        events.push_back(eventInfoCache.Get(*eventCopy));
    }

    size_t const newCount = ++eventCount;
    changedCallback(newCount, changedCallbackState);
}

void EtwTraceLog::Clear()
{
    eventCount = 0;
    {
        std::unique_lock<decltype(mutex)> lock(mutex);
        events.clear();
        events.shrink_to_fit();
        eventInfoCache.Clear();
        eventRecordAllocator.Reset();
    }

    changedCallback(0, changedCallbackState);
}

EventInfo EtwTraceLog::GetEvent(size_t index) const
{
    if (index >= eventCount) return EventInfo();

    std::shared_lock<decltype(mutex)> lock(mutex);
    if (index >= events.size()) return EventInfo();
    return events[index];
}

void EtwTraceLog::RegisterManifests()
{
    TDHSTATUS ec = 0;
    for (std::wstring& manifest : manifests)
        ec = TdhLoadManifest(&manifest[0]);
    for (std::wstring& providerBinary : providerBinaries)
        ec = TdhLoadManifestFromBinary(&providerBinary[0]);
}

void EtwTraceLog::UnregisterManifests()
{
    TDHSTATUS ec = 0;
    for (std::wstring& manifest : manifests)
        ec = TdhUnloadManifest(&manifest[0]);
    //for (std::wstring& providerBinary : providerBinaries)
    //    ec = TdhUnloadManifest(&providerBinary[0]);
}

std::unique_ptr<ITraceLog> CreateEtwTraceLog(TraceLogEventsChangedCallback* callback)
{
    auto log = std::make_unique<EtwTraceLog>();
    log->SetCallback(callback, nullptr);
    return std::move(log);
}

std::tuple<std::unique_ptr<ITraceLog>, std::unique_ptr<IFilteredTraceLog>>
CreateFilteredTraceLog(TraceLogEventsChangedCallback* callback, TraceLogFilterEvent* filter)
{
    auto filteredLog = std::make_unique<FilteredTraceLog>(callback, filter);
    auto traceLog = std::make_unique<EtwTraceLog>();
    filteredLog->SetLog(traceLog.get());
    traceLog->SetCallback(&FilteredTraceLog::Callback, filteredLog.get());

    return {std::move(traceLog), std::move(filteredLog)};
}

} // namespace etk
