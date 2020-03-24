#include "EventInfoCache.h"

namespace etk
{

static EVENT_HEADER_EXTENDED_DATA_ITEM const* GetTlogEventSchemaExtendedItem(
    EVENT_RECORD const& record)
{
    for (unsigned i = 0; i < record.ExtendedDataCount; ++i) {
        if (record.ExtendedData[i].ExtType == EVENT_HEADER_EXT_TYPE_EVENT_SCHEMA_TL)
            return &record.ExtendedData[i];
    }

    return nullptr;
}

EventInfoCache::EventInfoCache()
    : infos(50)
{}

EventInfo EventInfoCache::Get(EVENT_RECORD const& record)
{
    auto tlogExt = GetTlogEventSchemaExtendedItem(record);

    EventKey key = tlogExt ? CreateTlogEventKey(record.EventHeader.ProviderId, *tlogExt)
                           : EventKey::FromEvent(record);

    auto& entry = infos[key];
    if (!std::get<0>(entry))
        entry = CreateEventInfo(record);

    return EventInfo(&record, std::get<0>(entry).get(), std::get<1>(entry));
}

EventInfoCache::TraceEventInfoPtr EventInfoCache::CreateEventInfo(
    EVENT_RECORD const& record)
{
    TraceEventInfoPtr info;

    ULONG bufferSize = 0;
    TDHSTATUS ec = TdhGetEventInformation(const_cast<EVENT_RECORD*>(&record), 0, nullptr,
                                          nullptr, &bufferSize);
    if (ec == ERROR_INSUFFICIENT_BUFFER) {
        std::get<0>(info) = make_vstruct<TRACE_EVENT_INFO>(bufferSize);
        ec = TdhGetEventInformation(const_cast<EVENT_RECORD*>(&record), 0, nullptr,
                                    std::get<0>(info).get(), &bufferSize);
    }

    if (ec != ERROR_SUCCESS)
        return TraceEventInfoPtr(nullptr, 0);

    std::get<1>(info) = bufferSize;
    return info;
}

} // namespace etk
