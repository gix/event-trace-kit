#pragma once
#include "etk/EventInfo.h"
#include "etk/IEventSink.h"

#include <string>
#include <memory>
#include <tuple>

namespace etk
{

class ITraceLog : public IEventSink
{
public:
    virtual size_t GetEventCount() const = 0;
    virtual EventInfo GetEvent(size_t index) const = 0;
    virtual void Clear() = 0;
    virtual HRESULT UpdateTraceData(cspan<std::wstring> eventManifests) = 0;
};

using TraceLogFilterEvent = bool(void* record, void* info, size_t infoSize);

class TraceLogFilter
{
public:
    TraceLogFilter(TraceLogFilterEvent* filter) : Filter(filter) {}
    virtual ~TraceLogFilter() = default;
    TraceLogFilterEvent* Filter;
};

class IFilteredTraceLog
{
public:
    virtual ~IFilteredTraceLog() = default;
    virtual size_t GetEventCount() = 0;
    virtual EventInfo GetEvent(size_t index) const = 0;

    // Takes ownership of the passed in filter. Note: This cannot be a unique_ptr
    // directly due to a compiler bug:
    // https://developercommunity.visualstudio.com/content/problem/201217/ccli-stdmove-causes-stdunique-ptr-parameter-to-be.html
    virtual void SetFilter(TraceLogFilter* filter) = 0;
};

using TraceLogEventsChangedCallback = void(size_t, void*);
std::unique_ptr<ITraceLog> CreateEtwTraceLog(TraceLogEventsChangedCallback* callback);

std::tuple<std::unique_ptr<ITraceLog>, std::unique_ptr<IFilteredTraceLog>>
CreateFilteredTraceLog(TraceLogEventsChangedCallback* callback,
                       TraceLogFilter* filter);

} // namespace etk
