#pragma once
#include "EventInfo.h"
#include "IEventSink.h"

#include <memory>
#include <tuple>

namespace etk
{

class ITraceLog : public IEventSink
{
public:
    virtual void Clear() = 0;
    virtual size_t GetEventCount() = 0;
    virtual EventInfo GetEvent(size_t index) const = 0;
    virtual HRESULT UpdateTraceData(ArrayRef<std::wstring> eventManifests) = 0;
};

using TraceLogFilterEvent = bool(void* record, void* info, size_t infoSize);

class IFilteredTraceLog
{
public:
    virtual ~IFilteredTraceLog() = default;
    virtual size_t GetEventCount() = 0;
    virtual EventInfo GetEvent(size_t index) const = 0;
    virtual void SetFilter(TraceLogFilterEvent* filter) = 0;
};

using TraceLogEventsChangedCallback = void(size_t, void*);
std::unique_ptr<ITraceLog> CreateEtwTraceLog(TraceLogEventsChangedCallback* callback);

std::tuple<std::unique_ptr<ITraceLog>, std::unique_ptr<IFilteredTraceLog>>
CreateFilteredTraceLog(TraceLogEventsChangedCallback* callback,
                       TraceLogFilterEvent* filter);

} // namespace etk
