#include "EtwTraceSession.h"

#include "ADT/ArrayRef.h"
#include "ADT/VarStructPtr.h"
#include "Support/ByteCount.h"
#include "Support/CountOf.h"
#include "Support/ErrorHandling.h"
#include "Support/RangeAdaptors.h"

#include <algorithm>
#include <iterator>
#include <utility>

#include <evntcons.h>

static bool operator <(GUID const& x, GUID const& y)
{
    return std::memcmp(&x, &y, sizeof(GUID)) < 0;
}

namespace etk
{

EtwTraceSession::EtwTraceSession(wstring_view name,
                                 TraceProperties const& properties)
    : sessionName(name)
    , traceHandle()
{
    SetProperties(properties);

    // Close an old session if needed.
    ULONG st = ControlTraceW(0, sessionName.c_str(), traceProperties,
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
    size_t const bufferSize = sizeof(EVENT_TRACE_PROPERTIES) + ByteCount(sessionName);
    tracePropertiesBuffer.resize(bufferSize);

    traceProperties = new(tracePropertiesBuffer.data()) EVENT_TRACE_PROPERTIES();
    traceProperties->Wnode.BufferSize = static_cast<ULONG>(bufferSize);
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
    traceProperties->LoggerNameOffset = sizeof(EVENT_TRACE_PROPERTIES);
}

void EtwTraceSession::Start()
{
    HRESULT hr = S_OK;

    hr = HResultFromWin32(StartTraceW(&traceHandle, sessionName.c_str(),
                                      traceProperties));
    THROW_HR(hr);

    for (auto& provider : providers) {
        if (contains(enabledProviders, provider.Id))
            EnableProviderTrace(provider);
    }
}

void EtwTraceSession::Stop()
{
    (void)ControlTraceW(traceHandle, nullptr, traceProperties,
                        EVENT_TRACE_CONTROL_STOP);
    traceHandle = 0;
}

void EtwTraceSession::Flush()
{
    (void)ControlTraceW(traceHandle, nullptr, traceProperties,
                        EVENT_TRACE_CONTROL_FLUSH);
}

void EtwTraceSession::Query()
{
    (void)ControlTraceW(traceHandle, nullptr, traceProperties,
                        EVENT_TRACE_CONTROL_QUERY);
}

namespace
{

struct EqualProviderId
{
    GUID const& id;
    EqualProviderId(GUID const& id) : id(id) {}
    EqualProviderId& operator =(EqualProviderId const&) = delete;

    bool operator ()(ProviderState const& provider) const
    {
        return provider.Id == id;
    }
};

} // namespace

bool EtwTraceSession::AddProvider(ProviderState const& provider)
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
        THROW_HR(EnableProviderTrace(*provider));

    enabledProviders.insert(provider->Id);
    return true;
}

bool EtwTraceSession::DisableProvider(GUID const& providerId)
{
    auto it = find(enabledProviders, providerId);
    if (it == enabledProviders.end())
        return false;

    THROW_HR(DisableProviderTrace(providerId));
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

static vstruct_ptr<EVENT_FILTER_EVENT_ID> MakeEventIdFilter(
    ArrayRef<uint16_t> eventIds)
{
    if (eventIds.size() > std::numeric_limits<uint16_t>::max())
        throw std::invalid_argument("Too many event ids.");

    size_t byteSize = sizeof(EVENT_FILTER_EVENT_ID) + (sizeof(USHORT) * (eventIds.size() - 1));
    auto filterDesc = make_vstruct<EVENT_FILTER_EVENT_ID>(byteSize);
    filterDesc->FilterIn = TRUE;
    filterDesc->Count = static_cast<uint16_t>(eventIds.size());
    std::copy(eventIds.begin(), eventIds.end(), static_cast<uint16_t*>(filterDesc->Events));
    return filterDesc;
}

HRESULT EtwTraceSession::EnableProviderTrace(ProviderState const& provider) const
{
    DWORD pids[] = { 45616 };

    uint16_t eventIds[] = {
        4003, // Video_Frame_Glitch
        4004, // Evr_Queue
        4005, // Evr_DWMDequeue
        4006, // Evr_Present
        //4007, // Evr_Monitor_Estimate
        4008, // Evr_Sleep
        4030, // Render_Sample_Render/Start
        4031, // Render_Sample_Render/Stop 
        4032, // Render_Sample_Start_Mix/Start
        4033, // Render_Sample_Mix/Stop 
        4034, // Render_Sample_Decode/Start
        4035, // Render_Sample_Decode/Stop 
        4036, // Render_Sample_Deinterlace/Start
        4037, // Render_Sample_Deinterlace/Stop 
        4038, // Render_Sample_Procamp/Start
        4039, // Render_Sample_Procamp/Stop 
        4040, // Render_Sample_FRC/Start
        4041, // Render_Sample_FRC/Stop 
        4042, // Render_Sample_MID_Render
        4043, // Render_Sample_MAP_Render
        4044, // Render_Sample_Actual_Render
    };

    EVENT_FILTER_DESCRIPTOR filters[2] = {};
    filters[0].Ptr = reinterpret_cast<uintptr_t>(pids);
    filters[0].Size = sizeof(pids);
    filters[0].Type = EVENT_FILTER_TYPE_PID;

    auto eventIdFilter = MakeEventIdFilter(eventIds);
    filters[1].Ptr = reinterpret_cast<uintptr_t>(eventIdFilter.get());
    filters[1].Size = sizeof(EVENT_FILTER_EVENT_ID) + (sizeof(USHORT) * (std::max(uint16_t(1), eventIdFilter->Count) - 1));
    filters[1].Type = EVENT_FILTER_TYPE_EVENT_ID;

    ENABLE_TRACE_PARAMETERS traceParams = {};
    traceParams.Version = ENABLE_TRACE_PARAMETERS_VERSION_2;
    //traceParams.EnableProperty = EVENT_ENABLE_PROPERTY_STACK_TRACE;
    traceParams.SourceId = traceProperties->Wnode.Guid;
    traceParams.FilterDescCount = countof(filters);
    traceParams.EnableFilterDesc = filters;

    return HResultFromWin32(EnableTraceEx2(
        traceHandle, &provider.Id, EVENT_CONTROL_CODE_ENABLE_PROVIDER,
        provider.Level, provider.MatchAnyKeyword, provider.MatchAllKeyword,
        0, &traceParams));
}

HRESULT EtwTraceSession::DisableProviderTrace(GUID const& providerId) const
{
    return HResultFromWin32(EnableTraceEx2(
        traceHandle, &providerId, EVENT_CONTROL_CODE_DISABLE_PROVIDER,
        0, 0, 0, 0, nullptr));
}

} // namespace etk
