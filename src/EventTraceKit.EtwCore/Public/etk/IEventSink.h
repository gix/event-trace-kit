#pragma once
#include <evntcons.h>
#include <windows.h>

namespace etk
{

class IEventSink
{
public:
    virtual ~IEventSink() = default;
    virtual void ProcessEvent(EVENT_RECORD const& record) = 0;
};

} // namespace etk
