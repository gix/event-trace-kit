#include "ITraceLog.h"

#include "EventInfoCache.h"
#include "Support/Allocator.h"

#include <atomic>
#include <deque>
#include <shared_mutex>
#include <string>
#include <vector>

namespace etk
{

namespace
{

class EtwTraceLog : public ITraceLog
{
public:
    explicit EtwTraceLog(TraceLogEventsChangedCallback* callback = nullptr);

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

    using EventRecordAllocator = BumpPtrAllocator<MallocAllocator>;
    EventRecordAllocator eventRecordAllocator;

    std::deque<EventInfo> events;
    std::atomic<size_t> eventCount;

    mutable std::shared_mutex mutex;
    TraceLogEventsChangedCallback* changedCallback;
};

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

} // namespace

static void NullCallback(size_t) {}

EtwTraceLog::EtwTraceLog(TraceLogEventsChangedCallback* callback /*= nullptr*/)
    : changedCallback(callback ? callback : &NullCallback)
{
}

void EtwTraceLog::ProcessEvent(EVENT_RECORD const& record)
{
    EVENT_RECORD* eventCopy = CopyEvent(eventRecordAllocator, &record);

    {
        std::unique_lock<std::shared_mutex> lock(mutex);
        events.push_back(eventInfoCache.Get(*eventCopy));
    }

    size_t newCount = ++eventCount;
    changedCallback(newCount);
}

void EtwTraceLog::Clear()
{
    eventCount = 0;
    {
        std::unique_lock<std::shared_mutex> lock(mutex);
        events.clear();
        eventInfoCache.Clear();
    }

    changedCallback(0);
}

EventInfo EtwTraceLog::GetEvent(size_t index) const
{
    if (index >= eventCount) return EventInfo();

    std::shared_lock<std::shared_mutex> lock(mutex);
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
    return std::make_unique<EtwTraceLog>(callback);
}

} // namespace etk
