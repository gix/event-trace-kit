#pragma once
#include "ADT/VarStructPtr.h"
#include "EventInfo.h"

#include <unordered_map>

#include <boost/functional/hash/hash.hpp>
#include <windows.h>

#include <evntcons.h>
#include <tdh.h>

namespace etk
{

class EventKey
{
public:
    EventKey(GUID const& providerId, USHORT eventId, UCHAR version)
    {
        std::memcpy(data, &providerId, sizeof(providerId));
        std::memcpy(data + sizeof(providerId), &eventId, sizeof(eventId));
        std::memcpy(data + sizeof(providerId) + sizeof(eventId), &version,
                    sizeof(version));
    }

    static EventKey FromEvent(EVENT_RECORD const& record)
    {
        return FromEventHeader(record.EventHeader);
    }

    static EventKey FromEventHeader(EVENT_HEADER const& header)
    {
        bool const isClassic = (header.Flags & EVENT_HEADER_FLAG_CLASSIC_HEADER) != 0;
        return EventKey(header.ProviderId,
                        !isClassic ? header.EventDescriptor.Id
                                   : header.EventDescriptor.Opcode,
                        header.EventDescriptor.Version);
    }

    friend bool operator==(EventKey const& x, EventKey const& y)
    {
        return std::memcmp(&x, &y, sizeof(y)) == 0;
    }

    friend bool operator<(EventKey const& x, EventKey const& y)
    {
        return std::memcmp(&x, &y, sizeof(y)) < 0;
    }

    friend std::size_t hash_value(EventKey const& key)
    {
        std::size_t seed = 0;
        boost::hash_combine(seed, key.data);
        return seed;
    }

private:
    char data[sizeof(EVENT_HEADER::ProviderId) + sizeof(EVENT_DESCRIPTOR::Id) +
              sizeof(EVENT_DESCRIPTOR::Version)];
};

class EventInfoCache
{
public:
    EventInfoCache();
    EventInfo Get(EVENT_RECORD const& record);

    void Clear() { infos.clear(); }

    using TraceEventInfoPtr = std::tuple<vstruct_ptr<TRACE_EVENT_INFO>, size_t>;
    static TraceEventInfoPtr CreateEventInfo(EVENT_RECORD const& record);

private:
    std::unordered_map<EventKey, TraceEventInfoPtr, boost::hash<EventKey>> infos;
};

} // namespace etk
