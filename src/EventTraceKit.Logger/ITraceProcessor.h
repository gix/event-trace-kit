#pragma once
#include "ADT/StringView.h"
#include "EventInfoCache.h"
#include <windows.h>
#include <evntcons.h>

namespace etk
{

struct FormattedEvent
{
    GUID ProviderId;
    unsigned ProcessId;
    unsigned ThreadId;
    uint64_t ProcessorTime;
    EVENT_DESCRIPTOR Descriptor;
    FILETIME Time;
    wstring_view Message;
    EventInfo Info;
};

class IEventSink
{
public:
    virtual ~IEventSink() {};
    virtual void ProcessEvent(FormattedEvent const& event) = 0;
    virtual void NotifyNewEvents(size_t newEventCount) = 0;
};

class ITraceProcessor
{
public:
    virtual ~ITraceProcessor() {};
    virtual void SetEventSink(IEventSink* sink) = 0;
    virtual void StartProcessing() = 0;
    virtual void StopProcessing() = 0;
    virtual bool IsEndOfTracing() = 0;

    virtual void ClearEvents() = 0;
    virtual size_t GetEventCount() = 0;
    virtual EventInfo GetEvent(size_t index) const = 0;
};

} // namespace etk
