#pragma once
#include "ADT/ArrayRef.h"
#include "ITraceSession.h"

#include <memory>
#include <windows.h>
#include <evntrace.h>

namespace etk
{

class IEventSink;

class ITraceProcessor
{
public:
    virtual ~ITraceProcessor() {}
    virtual void SetEventSink(IEventSink* sink) = 0;
    virtual void StartProcessing() = 0;
    virtual void StopProcessing() = 0;
    virtual bool IsEndOfTracing() = 0;

    virtual TRACE_LOGFILE_HEADER const* GetLogFileHeader() const = 0;
};

std::unique_ptr<ITraceProcessor> CreateEtwTraceProcessor(
    std::wstring loggerName, ArrayRef<TraceProviderDescriptor> providers);

} // namespace etk
