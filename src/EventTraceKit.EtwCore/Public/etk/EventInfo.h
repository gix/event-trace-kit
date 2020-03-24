#pragma once
#include "etk/ADT/Span.h"
#include "etk/Support/CompilerSupport.h"

#include <windows.h>

#include <evntcons.h>
#include <tdh.h>

namespace etk
{

class EventInfo
{
public:
    EventInfo() = default;

    EventInfo(EVENT_RECORD const* record, TRACE_EVENT_INFO const* info, size_t infoSize)
        : record(record)
        , info(info)
        , infoSize(infoSize)
    {
    }

    explicit operator bool() const { return info != nullptr; }
    TRACE_EVENT_INFO const* operator->() const { return info; }

    EVENT_RECORD const* Record() const { return record; }
    TRACE_EVENT_INFO const* Info() const { return info; }
    size_t InfoSize() const { return infoSize; }

    cspan<std::byte> UserData() const
    {
        return {static_cast<std::byte*>(record->UserData),
                static_cast<size_t>(record->UserDataLength)};
    }

    bool IsStringOnly() const
    {
        return (record->EventHeader.Flags & EVENT_HEADER_FLAG_STRING_ONLY) != 0;
    }

    wchar_t const* EventMessage() const { return GetStringAt(info->EventMessageOffset); }

    ETK_ALWAYS_INLINE bool IsValidOffset(size_t offset) const
    {
        return offset >= sizeof(*info) && offset < infoSize;
    }

    template<typename T>
    ETK_ALWAYS_INLINE T GetAt(size_t offset) const
    {
        return reinterpret_cast<T>(reinterpret_cast<std::byte const*>(info) + offset);
    }

    template<typename T>
    ETK_ALWAYS_INLINE bool TryGetAt(size_t offset, T& value) const
    {
        if (!IsValidOffset(offset))
            return false;
        value = reinterpret_cast<T>(reinterpret_cast<std::byte const*>(info) + offset);
        return true;
    }

    ETK_ALWAYS_INLINE wchar_t const* GetStringAt(size_t offset) const
    {
        return IsValidOffset(offset) ? GetAt<wchar_t const*>(offset) : nullptr;
    }

private:
    EVENT_RECORD const* record = nullptr;
    TRACE_EVENT_INFO const* info = nullptr;
    size_t infoSize = 0;
};

} // namespace etk
