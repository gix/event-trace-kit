#include "Descriptors.h"
#include "IMessageFormatter.h"

#include "TdhMessageFormatter.h"

namespace EventTraceKit
{

public ref class NativeTdhFormatter : public IMessageFormatter
{
public:
    virtual System::String^ GetMessageForEvent(
        EventInfo eventInfo,
        ParseTdhContext^ parseTdhContext,
        System::IFormatProvider^ /*formatProvider*/)
    {
        size_t const bufferSize = 0x1000;
        wchar_t buffer[bufferSize];

        etk::EventInfo nativeEventInfo(
            (EVENT_RECORD*)eventInfo.EventRecord.ToPointer(),
            (TRACE_EVENT_INFO*)eventInfo.TraceEventInfo.ToPointer(),
            (size_t)eventInfo.TraceEventInfoSize.ToPointer());
        size_t pointerSize = (size_t)parseTdhContext->NativePointerSize;

        if (formatter->FormatEventMessage(nativeEventInfo, pointerSize, buffer, bufferSize))
            return gcnew System::String(buffer);
        return nullptr;
    }

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

    NativeTdhFormatter() : formatter(new ::etk::TdhMessageFormatter()) {}
    ~NativeTdhFormatter() { this->!NativeTdhFormatter(); }
    !NativeTdhFormatter() { delete formatter; }

private:
    ::etk::TdhMessageFormatter* formatter;
};

} // namespace EventTraceKit
