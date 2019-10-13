#pragma once
#include "EventInfo.h"

#include "ADT/VarStructPtr.h"
#include "Support/CompilerSupport.h"

ETK_DIAGNOSTIC_PUSH()
ETK_DIAGNOSTIC_DISABLE_MSVC(4127)
ETK_DIAGNOSTIC_DISABLE_MSVC(4244)
ETK_DIAGNOSTIC_DISABLE_MSVC(4245)
ETK_DIAGNOSTIC_DISABLE_MSVC(4996)
#include <absl/container/flat_hash_map.h>
ETK_DIAGNOSTIC_POP()

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

    template<typename H>
    friend H AbslHashValue(H state, EventKey const& key)
    {
        return H::combine_contiguous(std::move(state), key.data, std::size(key.data));
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
    absl::flat_hash_map<EventKey, TraceEventInfoPtr> infos;
};

} // namespace etk
