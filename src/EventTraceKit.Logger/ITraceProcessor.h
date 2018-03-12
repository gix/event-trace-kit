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
    virtual ~ITraceProcessor() = default;
    virtual void SetEventSink(IEventSink* sink) = 0;
    virtual void StartProcessing() = 0;
    virtual void StopProcessing() = 0;
    virtual bool IsEndOfTracing() = 0;

    virtual TRACE_LOGFILE_HEADER const* GetLogFileHeader() const = 0;
};

std::unique_ptr<ITraceProcessor> CreateEtwTraceProcessor(
    ArrayRef<std::wstring_view> loggerNames, ArrayRef<std::wstring_view> eventManifests,
    ArrayRef<std::wstring_view> providerBinaries);

inline std::unique_ptr<ITraceProcessor> CreateEtwTraceProcessor(
    ArrayRef<std::wstring_view> loggerNames, ArrayRef<TraceProviderDescriptor> providers)
{
    std::vector<std::wstring_view> manifests;
    std::vector<std::wstring_view> binaries;
    for (TraceProviderDescriptor const& provider : providers) {
        std::wstring_view manifest = provider.GetManifest();
        std::wstring_view providerBinary = provider.GetProviderBinary();
        if (!manifest.empty())
            manifests.emplace_back(manifest);
        if (!providerBinary.empty())
            binaries.emplace_back(providerBinary);
    }

    return CreateEtwTraceProcessor(loggerNames, manifests, binaries);
}

} // namespace etk
