#include "EventInfoCache.h"

namespace etk
{

EventInfoCache::EventInfoCache()
    : infos(50)
{
}

EventInfo EventInfoCache::Get(EVENT_RECORD& event)
{
    auto key = EventInfoKey::FromEvent(event);

    EventInfoPtr& info = infos.GetOrCreate(key, [&](EventInfoKey const&) {
        return CreateEventInfo(event);
    });

    return EventInfo(&event, info.get());
}

EventInfoCache::EventInfoPtr
EventInfoCache::CreateEventInfo(EVENT_RECORD& event)
{
    EventInfoPtr info;

    ULONG bufferSize = 0;
    DWORD ec = TdhGetEventInformation(&event, 0, nullptr, info.get(), &bufferSize);
    if (ec == ERROR_INSUFFICIENT_BUFFER) {
        info = make_vstruct<TRACE_EVENT_INFO>(bufferSize);
        ec = TdhGetEventInformation(&event, 0, nullptr, info.get(), &bufferSize);
    }

    if (ec != ERROR_SUCCESS)
        return EventInfoPtr();

    return info;
}

} // namespace etk
