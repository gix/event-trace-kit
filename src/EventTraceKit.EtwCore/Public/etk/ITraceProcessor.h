#pragma once
#include "ITraceSession.h"
#include "etk/ADT/Span.h"

#include <evntrace.h>
#include <memory>
#include <windows.h>

namespace etk
{

class IEventSink;

class ITraceProcessor
{
public:
    virtual ~ITraceProcessor() = default;
    virtual void SetEventSink(IEventSink* sink) = 0;
    virtual void StartProcessing() = 0;
    virtual void StopProcessing() = 0;
    virtual bool IsEndOfTracing() = 0;

    virtual TRACE_LOGFILE_HEADER const* GetLogFileHeader() const = 0;
};

std::unique_ptr<ITraceProcessor> CreateEtwTraceProcessor(
    cspan<std::wstring_view> loggerNames);

} // namespace etk
