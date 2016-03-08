#include "EtwTraceSession.h"

#include "ADT/ArrayRef.h"
#include "Support/ByteCount.h"
#include "Support/ErrorHandling.h"
#include "Support/RangeAdaptors.h"

#include <algorithm>
#include <memory>

#include <evntcons.h>

static bool operator <(GUID const& x, GUID const& y)
{
    return std::memcmp(&x, &y, sizeof(GUID)) < 0;
}

using namespace etk;

namespace etk
{

namespace
{

void AddProcessIdFilter(
    ArrayRef<unsigned> processIds, std::vector<EVENT_FILTER_DESCRIPTOR>& filters)
{
    filters.emplace_back();
    auto& descriptor = filters.back();
    descriptor.Ptr = reinterpret_cast<uintptr_t>(processIds.data());
    descriptor.Size = static_cast<ULONG>(processIds.size() * sizeof(processIds[0]));
    descriptor.Type = EVENT_FILTER_TYPE_PID;
}

struct EventIdFilter : EVENT_FILTER_EVENT_ID
{
    size_t ByteSize() const
    {
        USHORT actualCount = std::max(USHORT(1), Count);
        return sizeof(EVENT_FILTER_EVENT_ID) + (sizeof(Events[0]) * actualCount);
    }

    void* operator new(size_t n, size_t eventIds)
    {
        size_t extraCount = std::max(size_t(1), eventIds) - 1;
        return ::operator new(n + extraCount * sizeof(USHORT));
    }

    void operator delete(void* mem)
    {
        ::operator delete(mem);
    }
};

void AddEventIdFilter(
    ArrayRef<uint16_t> eventIds, std::vector<EVENT_FILTER_DESCRIPTOR>& filters,
    std::unique_ptr<EventIdFilter>& filter)
{
    filter.reset(new(eventIds.size()) EventIdFilter());
    filter->FilterIn = TRUE;
    filter->Count = static_cast<uint16_t>(eventIds.size());
    std::copy(eventIds.begin(), eventIds.end(), static_cast<uint16_t*>(filter->Events));

    filters.emplace_back();
    auto& descriptor = filters.back();
    descriptor.Ptr = reinterpret_cast<uintptr_t>(filter.get());
    descriptor.Size = static_cast<ULONG>(filter->ByteSize());
    descriptor.Type = EVENT_FILTER_TYPE_EVENT_ID;
}

struct EqualProviderId
{
    GUID const& id;
    EqualProviderId(GUID const& id) : id(id) {}
    EqualProviderId& operator =(EqualProviderId const&) = delete;

    bool operator ()(TraceProviderSpec const& provider) const
    {
        return provider.Id == id;
    }
};

} // namespace

std::unique_ptr<EventTraceProperties> EventTraceProperties::Create(wstring_view name)
{
    size_t const bufferSize = sizeof(EVENT_TRACE_PROPERTIES) + ByteCount(name);

    std::unique_ptr<EventTraceProperties> p(new(name) EventTraceProperties());
    p->Wnode.BufferSize = static_cast<ULONG>(bufferSize);
    // NB: The actual name is copied late by StartTraceW().
    p->LoggerNameOffset = sizeof(EVENT_TRACE_PROPERTIES);
    return p;
}

void* EventTraceProperties::operator new(size_t n, wstring_view name)
{
    return ::operator new(n + ByteCount(name));
}

void EventTraceProperties::operator delete(void* mem)
{
    ::operator delete(mem);
}

EtwTraceSession::EtwTraceSession(wstring_view name,
    TraceProperties const& properties)
    : sessionName(name.to_string())
    , traceHandle()
{
    SetProperties(properties);

    // Close an old session if needed.
    ULONG st = ControlTraceW(0, sessionName.c_str(), traceProperties.get(),
                             EVENT_TRACE_CONTROL_STOP);
    if (st == ERROR_SUCCESS) {
        // Reinitialize.
        SetProperties(properties);
    }
}

EtwTraceSession::~EtwTraceSession()
{
    EtwTraceSession::Stop();
}

void EtwTraceSession::SetProperties(TraceProperties const& properties)
{
    traceProperties = EventTraceProperties::Create(sessionName);
    traceProperties->Wnode.Guid = properties.Id;
    traceProperties->Wnode.ClientContext = static_cast<ULONG>(properties.ClockResolution);
    traceProperties->Wnode.Flags = WNODE_FLAG_TRACED_GUID;
    traceProperties->BufferSize = static_cast<ULONG>(properties.BufferSize);
    traceProperties->MinimumBuffers = static_cast<ULONG>(properties.MinimumBuffers);
    traceProperties->MaximumBuffers = static_cast<ULONG>(properties.MaximumBuffers);
    traceProperties->MaximumFileSize = 0;
    traceProperties->LogFileMode = EVENT_TRACE_REAL_TIME_MODE | EVENT_TRACE_STOP_ON_HYBRID_SHUTDOWN;
    traceProperties->FlushTimer = static_cast<ULONG>(properties.FlushTimer);
    traceProperties->EnableFlags = 0;
    traceProperties->LogFileNameOffset = 0;
}

void EtwTraceSession::Start()
{
    HRESULT hr = S_OK;

    hr = HResultFromWin32(StartTraceW(&traceHandle, sessionName.c_str(),
                                      traceProperties.get()));
    ThrowOnFail(hr);

    for (auto& provider : providers) {
        if (contains(enabledProviders, provider.Id))
            EnableProviderTrace(provider);
    }
}

void EtwTraceSession::Stop()
{
    (void)ControlTraceW(traceHandle, nullptr, traceProperties.get(),
                        EVENT_TRACE_CONTROL_STOP);
    traceHandle = 0;
}

void EtwTraceSession::Flush()
{
    (void)ControlTraceW(traceHandle, nullptr, traceProperties.get(),
                        EVENT_TRACE_CONTROL_FLUSH);
}

void EtwTraceSession::Query(TraceStatistics& stats)
{
    (void)ControlTraceW(traceHandle, nullptr, traceProperties.get(),
                        EVENT_TRACE_CONTROL_QUERY);

    stats.NumberOfBuffers = static_cast<unsigned>(traceProperties->NumberOfBuffers);
    stats.FreeBuffers = static_cast<unsigned>(traceProperties->FreeBuffers);
    stats.EventsLost = static_cast<unsigned>(traceProperties->EventsLost);
    stats.BuffersWritten = static_cast<unsigned>(traceProperties->BuffersWritten);
    stats.LogBuffersLost = static_cast<unsigned>(traceProperties->LogBuffersLost);
    stats.RealTimeBuffersLost = static_cast<unsigned>(traceProperties->RealTimeBuffersLost);
    stats.LoggerThreadId = static_cast<unsigned>(GetThreadId(traceProperties->LoggerThreadId));
}

bool EtwTraceSession::AddProvider(TraceProviderSpec const& provider)
{
    auto entry = find_if(providers, EqualProviderId(provider.Id));
    if (entry == providers.end()) {
        providers.push_back(provider);
        return true;
    }

    bool enabled = contains(enabledProviders, provider.Id);

    // Update the provider.
    *entry = provider;
    if (enabled)
        EnableProvider(provider.Id);

    return false;
}

bool EtwTraceSession::RemoveProvider(GUID const& providerId)
{
    auto entry = find_if(providers, EqualProviderId(providerId));
    if (entry == providers.end())
        return false;

    DisableProvider(providerId);
    providers.erase(entry);
    return true;
}

bool EtwTraceSession::EnableProvider(GUID const& providerId)
{
    auto provider = find_if(providers, EqualProviderId(providerId));
    if (provider == providers.end())
        return false;

    if (traceHandle)
        ThrowOnFail(EnableProviderTrace(*provider));

    enabledProviders.insert(provider->Id);
    return true;
}

bool EtwTraceSession::DisableProvider(GUID const& providerId)
{
    auto it = find(enabledProviders, providerId);
    if (it == enabledProviders.end())
        return false;

    ThrowOnFail(DisableProviderTrace(providerId));
    enabledProviders.erase(it);
    return true;
}

void EtwTraceSession::EnableAllProviders()
{
    for (auto const& provider : providers)
        EnableProvider(provider.Id);
}

void EtwTraceSession::DisableAllProviders()
{
    for (auto const& providerId : enabledProviders)
        DisableProvider(providerId);
}

HRESULT EtwTraceSession::EnableProviderTrace(TraceProviderSpec const& provider) const
{
    std::vector<EVENT_FILTER_DESCRIPTOR> filters;
    std::unique_ptr<EventIdFilter> eventIdFilter;
    if (!provider.ProcessIds.empty())
        AddProcessIdFilter(provider.ProcessIds, filters);
    if (!provider.EventIds.empty())
        AddEventIdFilter(provider.EventIds, filters, eventIdFilter);

    ENABLE_TRACE_PARAMETERS parameters = {};
    parameters.Version = ENABLE_TRACE_PARAMETERS_VERSION_2;
    parameters.EnableProperty = 0;
    if (provider.IncludeSecurityId)
        parameters.EnableProperty |= EVENT_ENABLE_PROPERTY_SID;
    if (provider.IncludeTerminalSessionId)
        parameters.EnableProperty |= EVENT_ENABLE_PROPERTY_TS_ID;
    if (provider.IncludeStackTrace)
        parameters.EnableProperty |= EVENT_ENABLE_PROPERTY_STACK_TRACE;
    parameters.SourceId = traceProperties->Wnode.Guid;
    parameters.EnableFilterDesc = filters.data();
    parameters.FilterDescCount = static_cast<ULONG>(filters.size());

    return HResultFromWin32(EnableTraceEx2(
        traceHandle, &provider.Id, EVENT_CONTROL_CODE_ENABLE_PROVIDER,
        provider.Level, provider.MatchAnyKeyword, provider.MatchAllKeyword,
        0, &parameters));
}

HRESULT EtwTraceSession::DisableProviderTrace(GUID const& providerId) const
{
    return HResultFromWin32(EnableTraceEx2(
        traceHandle, &providerId, EVENT_CONTROL_CODE_DISABLE_PROVIDER,
        0, 0, 0, 0, nullptr));
}

ETK_EXPORT std::unique_ptr<ITraceSession> CreateEtwTraceSession(
    wstring_view name, TraceProperties const& properties)
{
    return std::make_unique<EtwTraceSession>(name, properties);
}

} // namespace etk
