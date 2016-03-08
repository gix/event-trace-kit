#pragma once
#include "ADT/LruCache.h"
#include "ADT/VarStructPtr.h"
#include "Support/CompilerSupport.h"

#include <cstdint>
#include <windows.h>
#include <evntcons.h>
#include <tdh.h>
#include <boost/functional/hash/hash.hpp>

namespace etk
{

class EventInfo
{
public:
    EventInfo() = default;

    EventInfo(EVENT_RECORD* record, TRACE_EVENT_INFO* info)
        : record(record), info(info)
    {
    }

    explicit operator bool() const { return info != nullptr; }
    TRACE_EVENT_INFO* operator ->() const { return info; }

    wchar_t const* ProviderName() const {
        return GetStringAt(info->ProviderNameOffset);
    }

    wchar_t const* LevelName() const {
        return GetStringAt(info->LevelNameOffset);
    }

    wchar_t const* ChannelName() const {
        return GetStringAt(info->ChannelNameOffset);
    }

    wchar_t const* TaskName() const {
        return GetStringAt(info->TaskNameOffset);
    }

    wchar_t const* OpcodeName() const {
        return GetStringAt(info->OpcodeNameOffset);
    }

    wchar_t const* KeywordsName() const {
        return GetStringAt(info->KeywordsNameOffset);
    }

    wchar_t const* EventMessage() const {
        return GetStringAt(info->EventMessageOffset);
    }

    wchar_t const* ProviderMessage() const {
        return GetStringAt(info->ProviderMessageOffset);
    }

    wchar_t const* ActivityIdName() const {
        return GetStringAt(info->ActivityIDNameOffset);
    }

    wchar_t const* RelatedActivityIdName() const {
        return GetStringAt(info->RelatedActivityIDNameOffset);
    }

    EVENT_RECORD* record = nullptr;
    TRACE_EVENT_INFO* info = nullptr;

    template<typename T>
    ETK_ALWAYS_INLINE T GetAt(size_t offset) const
    {
        return reinterpret_cast<T>(reinterpret_cast<uint8_t*>(info) + offset);
    }

private:
    ETK_ALWAYS_INLINE wchar_t const* GetStringAt(size_t offset) const
    {
        return offset ? GetAt<wchar_t const*>(offset) : nullptr;
    }
};

struct EventInfoKey
{
    EventInfoKey(GUID const& providerId, USHORT eventId)
    {
        std::memcpy(Buffer, &providerId, sizeof(providerId));
        std::memcpy(Buffer + sizeof(providerId), &eventId, sizeof(eventId));
    }

    static EventInfoKey FromEvent(EVENT_RECORD& record)
    {
        return EventInfoKey(record.EventHeader.ProviderId,
                            record.EventHeader.EventDescriptor.Id);
    }

    friend bool operator ==(EventInfoKey const& x, EventInfoKey const& y) {
        return std::memcmp(&x, &y, sizeof(y)) == 0;
    }

    friend bool operator <(EventInfoKey const& x, EventInfoKey const& y) {
        return std::memcmp(&x, &y, sizeof(y)) < 0;
    }

    friend std::size_t hash_value(EventInfoKey const& key)
    {
        std::size_t seed = 0;
        boost::hash_combine(seed, key.Buffer);
        return seed;
    }

private:
    char Buffer[sizeof(EVENT_HEADER::ProviderId) + sizeof(EVENT_DESCRIPTOR::Id)];
};

class EventInfoCache
{
public:
    EventInfoCache();
    EventInfo Get(EVENT_RECORD& event);

private:
    using EventInfoPtr = vstruct_ptr<TRACE_EVENT_INFO>;
    static EventInfoPtr CreateEventInfo(EVENT_RECORD& event);
    LruCache<EventInfoKey, EventInfoPtr, boost::hash<EventInfoKey>> infos;
};

} // namespace etk
