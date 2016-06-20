#pragma once
#include "EtwMessageFormatter.h"

#include <msclr/marshal_cppstd.h>

namespace EventTraceKit
{

public ref class EtwMessageFormatter
{
public:
    System::String^ FormatEventMessage(
        void* record, void* info, size_t infoSize, size_t pointerSize)
    {
        size_t const bufferSize = 0x1000;
        wchar_t buffer[bufferSize];

        etk::EventInfo eventInfo((EVENT_RECORD*)record, (TRACE_EVENT_INFO*)info, infoSize);
        if (formatter->FormatEventMessage(eventInfo, pointerSize, buffer, bufferSize))
            return gcnew System::String(buffer);
        return nullptr;
    }

    EtwMessageFormatter() : formatter(new ::etk::EtwMessageFormatter()) {}
    ~EtwMessageFormatter() { this->!EtwMessageFormatter(); }
    !EtwMessageFormatter() { delete formatter; }
private:
    ::etk::EtwMessageFormatter* formatter;
};

} // namespace EventTraceKit
