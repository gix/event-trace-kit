#include "EventInfoCache.h"

namespace etk
{

EventInfoCache::EventInfoCache()
    : infos(50)
{
}

EventInfo EventInfoCache::Get(EVENT_RECORD& record)
{
    auto key = EventKey::FromEvent(record);

    auto& entry = infos[key];
    if (!std::get<0>(entry))
        entry = CreateEventInfo(record);

    return EventInfo(&record, std::get<0>(entry).get(), std::get<1>(entry));
}

EventInfoCache::TraceEventInfoPtr
EventInfoCache::CreateEventInfo(EVENT_RECORD& record)
{
    TraceEventInfoPtr info;

    ULONG bufferSize = 0;
    DWORD ec = TdhGetEventInformation(&record, 0, nullptr, nullptr, &bufferSize);
    if (ec == ERROR_INSUFFICIENT_BUFFER) {
        std::get<0>(info) = make_vstruct<TRACE_EVENT_INFO>(bufferSize);
        ec = TdhGetEventInformation(&record, 0, nullptr, std::get<0>(info).get(), &bufferSize);
    }

    if (ec != ERROR_SUCCESS)
        return TraceEventInfoPtr(nullptr, 0);

    std::get<1>(info) = bufferSize;
    return info;
}

} // namespace etk
