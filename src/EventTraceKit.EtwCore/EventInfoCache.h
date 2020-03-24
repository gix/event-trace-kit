#pragma once
#include "EventInfo.h"

#include "ADT/VarStructPtr.h"
#include "Support/Allocator.h"
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

template<typename H>
H AbslHashValue(H state, GUID const& key)
{
    return H::combine_contiguous(std::move(state), reinterpret_cast<uint8_t const*>(&key),
                                 sizeof(key));
}

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

class TlogEventMetadataKey
{
public:
    explicit TlogEventMetadataKey(uint8_t const* metadata, uint16_t size)
        : metadata_(metadata)
        , size_(size)
    {
    }

    uint8_t const* data() const { return metadata_; }
    uint16_t size() const { return size_; }

    friend bool operator==(TlogEventMetadataKey const& lhs,
                           TlogEventMetadataKey const& rhs)
    {
        return std::equal(lhs.metadata_, lhs.metadata_ + lhs.size_, rhs.metadata_,
                          rhs.metadata_ + rhs.size_);
    }

    friend bool operator!=(TlogEventMetadataKey const& lhs,
                           TlogEventMetadataKey const& rhs)
    {
        return !operator==(lhs, rhs);
    }

    template<typename H>
    friend H AbslHashValue(H state, TlogEventMetadataKey const& key)
    {
        return H::combine_contiguous(std::move(state), key.metadata_, key.size_);
    }

private:
    uint8_t const* metadata_ = nullptr;
    uint16_t size_ = 0;
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
    using TlogEventMetadataKeyAllocator =
        BumpPtrAllocator<MallocAllocator, 1 * 1024 * 1024>;

    struct TlogProvider
    {
        using EventId = uint16_t;

        absl::flat_hash_map<TlogEventMetadataKey, EventId> eventIdMap;
        EventId uniqueEventId = 0;

        EventId GetOrCreateUniqueId(TlogEventMetadataKeyAllocator& allocator,
                                    EVENT_HEADER_EXTENDED_DATA_ITEM const& tlogExt)
        {
            auto key = TlogEventMetadataKey(
                reinterpret_cast<uint8_t const*>(tlogExt.DataPtr), tlogExt.DataSize);

            auto it = eventIdMap.find(key);
            if (it != eventIdMap.end())
                return it->second;

            auto eventId = uniqueEventId++;
            eventIdMap.insert(it, {AllocateKey(allocator, key), eventId});
            return eventId;
        }

        TlogEventMetadataKey AllocateKey(TlogEventMetadataKeyAllocator& allocator,
                                         TlogEventMetadataKey const& source)
        {
            auto const size = source.size();
            auto ptr = allocator.Allocate<uint8_t>(size);
            std::copy_n(source.data(), size, ptr);
            return TlogEventMetadataKey(ptr, size);
        }
    };

    EventKey CreateTlogEventKey(GUID const& providerId,
                                EVENT_HEADER_EXTENDED_DATA_ITEM const& tlogExt)
    {
        auto& provider = tlogProviders[providerId];
        auto eventId =
            provider.GetOrCreateUniqueId(tlogEventMetadataKeyAllocator, tlogExt);
        return EventKey(providerId, eventId, 0);
    }

    absl::flat_hash_map<EventKey, TraceEventInfoPtr> infos;
    absl::flat_hash_map<GUID, TlogProvider> tlogProviders;
    TlogEventMetadataKeyAllocator tlogEventMetadataKeyAllocator;
};

} // namespace etk
