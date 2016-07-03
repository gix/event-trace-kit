#pragma once
#include "IEventSink.h"
#include "EventInfo.h"
#include <memory>

namespace etk
{

class ITraceLog : public IEventSink
{
public:
    virtual void Clear() = 0;
    virtual size_t GetEventCount() = 0;
    virtual EventInfo GetEvent(size_t index) const = 0;
    virtual void RegisterManifests() = 0;
    virtual void UnregisterManifests() = 0;
};

using TraceLogEventsChangedCallback = void(size_t);
std::unique_ptr<ITraceLog> CreateEtwTraceLog(TraceLogEventsChangedCallback* callback);

} // namespace etk
