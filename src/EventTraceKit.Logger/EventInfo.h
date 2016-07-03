#pragma once
#include "ADT/ArrayRef.h"
#include "Support/CompilerSupport.h"

#include <windows.h>
#include <evntcons.h>
#include <tdh.h>

namespace etk
{

class EventInfo
{
public:
    EventInfo() = default;

    EventInfo(EVENT_RECORD* record, TRACE_EVENT_INFO* info, size_t infoSize)
        : record(record), info(info), infoSize(infoSize)
    {
    }

    explicit operator bool() const { return info != nullptr; }
    TRACE_EVENT_INFO const* operator ->() const { return info; }

    EVENT_RECORD* Record() const { return record; }
    TRACE_EVENT_INFO* Info() const { return info; }
    size_t InfoSize() const { return infoSize; }

    ArrayRef<uint8_t> UserData() const
    {
        return{ static_cast<uint8_t*>(record->UserData),
            static_cast<size_t>(record->UserDataLength) };
    }

    wchar_t const* EventMessage() const
    {
        return GetStringAt(info->EventMessageOffset);
    }

    template<typename T>
    ETK_ALWAYS_INLINE T GetAt(size_t offset) const
    {
        return reinterpret_cast<T>(reinterpret_cast<uint8_t const*>(info) + offset);
    }

    template<typename T>
    ETK_ALWAYS_INLINE bool TryGetAt(size_t offset, T& value) const
    {
        if (offset >= infoSize) return false;
        value = reinterpret_cast<T>(reinterpret_cast<uint8_t const*>(info) + offset);
        return true;
    }

    ETK_ALWAYS_INLINE wchar_t const* GetStringAt(size_t offset) const
    {
        return offset >= 0 && offset < infoSize ? GetAt<wchar_t const*>(offset) : nullptr;
    }

private:
    EVENT_RECORD* record = nullptr;
    TRACE_EVENT_INFO* info = nullptr;
    size_t infoSize = 0;
};

} // namespace etk
