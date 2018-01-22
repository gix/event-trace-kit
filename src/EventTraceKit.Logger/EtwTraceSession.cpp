#include "ITraceSession.h"

#include "ADT/ArrayRef.h"
#include "ADT/SmallVector.h"
#include "Support/ByteCount.h"
#include "Support/ErrorHandling.h"
#include "Support/Hashing.h"
#include "Support/RangeAdaptors.h"

#include <algorithm>
#include <memory>
#include <string_view>
#include <unordered_set>

#include <evntcons.h>
#include <evntrace.h>
#include <windows.h>

static bool operator<(GUID const& x, GUID const& y)
{
    return std::memcmp(&x, &y, sizeof(GUID)) < 0;
}

namespace etk
{

namespace
{

using FilterPtr = std::unique_ptr<std::byte[]>;

void AddProcessIdFilter(std::vector<EVENT_FILTER_DESCRIPTOR>& filters,
                        ArrayRef<unsigned> processIds)
{
    auto& descriptor = filters.emplace_back();
    descriptor.Ptr = reinterpret_cast<uintptr_t>(processIds.data());
    descriptor.Size = static_cast<ULONG>(ByteCount(processIds));
    descriptor.Type = EVENT_FILTER_TYPE_PID;
}

void AddExecutableNameFilter(std::vector<EVENT_FILTER_DESCRIPTOR>& filters,
                             std::wstring const& executableName)
{
    auto& descriptor = filters.emplace_back();
    descriptor.Ptr = reinterpret_cast<uintptr_t>(executableName.data());
    descriptor.Size = static_cast<ULONG>(ZStringByteCount(executableName));
    descriptor.Type = EVENT_FILTER_TYPE_EXECUTABLE_NAME;
}

size_t ComputeEventIdFilterSize(uint16_t eventCount)
{
    uint16_t const extraCount = std::max(uint16_t(1), eventCount) - 1;
    return sizeof(EVENT_FILTER_EVENT_ID) +
           (sizeof(EVENT_FILTER_EVENT_ID::Events[0]) * extraCount);
}

void AddEventIdFilter(std::vector<EVENT_FILTER_DESCRIPTOR>& filters,
                      SmallVectorBase<FilterPtr>& buffers, ArrayRef<uint16_t> eventIds,
                      bool enable, ULONG type)
{
    uint16_t const eventCount = static_cast<uint16_t>(eventIds.size());
    size_t const byteSize = ComputeEventIdFilterSize(eventCount);
    FilterPtr& buffer = buffers.emplace_back(std::make_unique<std::byte[]>(byteSize));

    auto const filter = new (buffer.get()) EVENT_FILTER_EVENT_ID();
    filter->FilterIn = enable ? TRUE : FALSE;
    filter->Count = eventCount;
    std::copy_n(eventIds.data(), eventCount, static_cast<uint16_t*>(filter->Events));

    auto& descriptor = filters.emplace_back();
    descriptor.Ptr = reinterpret_cast<uintptr_t>(buffer.get());
    descriptor.Size = static_cast<ULONG>(byteSize);
    descriptor.Type = type;
}

void AddEventFilter(std::vector<EVENT_FILTER_DESCRIPTOR>& filters,
                    SmallVectorBase<FilterPtr>& buffers, ArrayRef<uint16_t> eventIds,
                    bool enable)
{
    AddEventIdFilter(filters, buffers, eventIds, enable, EVENT_FILTER_TYPE_EVENT_ID);
}

void AddStackWalkFilter(std::vector<EVENT_FILTER_DESCRIPTOR>& filters,
                        SmallVectorBase<FilterPtr>& buffers, ArrayRef<uint16_t> eventIds,
                        bool enable)
{
    AddEventIdFilter(filters, buffers, eventIds, enable, EVENT_FILTER_TYPE_STACKWALK);
}

struct EqualProviderId
{
    GUID const& id;

    EqualProviderId(GUID const& id)
        : id(id)
    {
    }

    EqualProviderId& operator=(EqualProviderId const&) = delete;

    bool operator()(TraceProviderDescriptor const& provider) const
    {
        return provider.Id == id;
    }
};

class EventTraceProperties : public EVENT_TRACE_PROPERTIES
{
public:
    static std::unique_ptr<EventTraceProperties> Create(std::wstring const& loggerName,
                                                        std::wstring const& logFileName);

    void* operator new(size_t n, size_t loggerNameBytes, size_t logFileNameBytes);
    void operator delete(void* mem, size_t n);

private:
    EventTraceProperties() = default;
};

class EtwTraceSession : public ITraceSession
{
public:
    EtwTraceSession(std::wstring_view name, TraceProperties const& properties);
    virtual ~EtwTraceSession();

    virtual HRESULT Start() override;
    virtual HRESULT Stop() override;
    virtual HRESULT Flush() override;
    virtual HRESULT Query(TraceStatistics& stats) override;

    virtual bool AddProvider(TraceProviderDescriptor const& provider) override;
    virtual bool RemoveProvider(GUID const& providerId) override;
    virtual bool EnableProvider(GUID const& providerId) override;
    virtual bool DisableProvider(GUID const& providerId) override;
    virtual void EnableAllProviders() override;
    virtual void DisableAllProviders() override;

private:
    void SetProperties(TraceProperties const& properties);
    HRESULT StartEventProvider(TraceProviderDescriptor const& provider) const;
    HRESULT DisableProviderTrace(GUID const& providerId) const;

    std::wstring sessionName;
    std::unique_ptr<EventTraceProperties> traceProperties;
    TRACEHANDLE traceHandle;
    std::vector<TraceProviderDescriptor> providers;
    std::unordered_set<GUID> enabledProviders;
};

template<typename T, typename U>
ETK_ALWAYS_INLINE T OffsetPtr(U* ptr, size_t offset)
{
    return reinterpret_cast<T>(reinterpret_cast<uint8_t*>(ptr) + offset);
}

void ZStringCopy(std::wstring_view str, wchar_t* dst)
{
    size_t const numChars = str.copy(dst, str.length());
    dst[numChars] = 0;
}

} // namespace

std::unique_ptr<EventTraceProperties>
EventTraceProperties::Create(std::wstring const& loggerName,
                             std::wstring const& logFileName)
{
    size_t const loggerNameBytes = ZStringByteCount(loggerName);
    size_t const logFileNameBytes =
        !loggerName.empty() ? ZStringByteCount(logFileName) : 0;
    size_t const bufferSize =
        sizeof(EVENT_TRACE_PROPERTIES) + loggerNameBytes + logFileNameBytes;

    std::unique_ptr<EventTraceProperties> p(new (loggerNameBytes, logFileNameBytes)
                                                EventTraceProperties());
    p->Wnode.BufferSize = static_cast<ULONG>(bufferSize);
    // NB: The actual name is copied later by StartTraceW().
    p->LoggerNameOffset = static_cast<ULONG>(sizeof(EVENT_TRACE_PROPERTIES));
    if (logFileNameBytes) {
        p->LogFileNameOffset = static_cast<ULONG>(p->LoggerNameOffset + loggerNameBytes);
        ZStringCopy(logFileName, OffsetPtr<wchar_t*>(p.get(), p->LogFileNameOffset));
    }
    return p;
}

void* EventTraceProperties::operator new(size_t n, size_t loggerNameBytes,
                                         size_t logFileNameBytes)
{
    return ::operator new(n + loggerNameBytes + logFileNameBytes);
}

void EventTraceProperties::operator delete(void* mem, size_t n)
{
    ::operator delete(mem);
}

EtwTraceSession::EtwTraceSession(std::wstring_view name,
                                 TraceProperties const& properties)
    : sessionName(name)
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
    traceProperties = EventTraceProperties::Create(sessionName, properties.LogFileName);
    traceProperties->Wnode.Guid = properties.Id;
    traceProperties->Wnode.ClientContext = static_cast<ULONG>(properties.ClockResolution);
    traceProperties->Wnode.Flags = WNODE_FLAG_TRACED_GUID;
    traceProperties->BufferSize = static_cast<ULONG>(properties.BufferSize);
    traceProperties->MinimumBuffers = static_cast<ULONG>(properties.MinimumBuffers);
    traceProperties->MaximumBuffers = static_cast<ULONG>(properties.MaximumBuffers);
    traceProperties->MaximumFileSize = 0;
    traceProperties->LogFileMode =
        EVENT_TRACE_REAL_TIME_MODE | EVENT_TRACE_STOP_ON_HYBRID_SHUTDOWN;
    traceProperties->FlushTimer = static_cast<ULONG>(properties.FlushTimer);
    traceProperties->EnableFlags = 0;
}

HRESULT EtwTraceSession::Start()
{
    HR(HResultFromWin32(
        StartTraceW(&traceHandle, sessionName.c_str(), traceProperties.get())));

    for (auto& provider : providers) {
        if (contains(enabledProviders, provider.Id))
            (void)StartEventProvider(provider);
    }

    return S_OK;
}

HRESULT EtwTraceSession::Stop()
{
    HR(HResultFromWin32(ControlTraceW(traceHandle, nullptr, traceProperties.get(),
                                      EVENT_TRACE_CONTROL_STOP)));
    traceHandle = 0;
    return S_OK;
}

HRESULT EtwTraceSession::Flush()
{
    HR(HResultFromWin32(ControlTraceW(traceHandle, nullptr, traceProperties.get(),
                                      EVENT_TRACE_CONTROL_FLUSH)));
    return S_OK;
}

HRESULT EtwTraceSession::Query(TraceStatistics& stats)
{
    HR(HResultFromWin32(HResultFromWin32(ControlTraceW(
        traceHandle, nullptr, traceProperties.get(), EVENT_TRACE_CONTROL_QUERY))));

    stats.NumberOfBuffers = static_cast<unsigned>(traceProperties->NumberOfBuffers);
    stats.FreeBuffers = static_cast<unsigned>(traceProperties->FreeBuffers);
    stats.EventsLost = static_cast<unsigned>(traceProperties->EventsLost);
    stats.BuffersWritten = static_cast<unsigned>(traceProperties->BuffersWritten);
    stats.LogBuffersLost = static_cast<unsigned>(traceProperties->LogBuffersLost);
    stats.RealTimeBuffersLost =
        static_cast<unsigned>(traceProperties->RealTimeBuffersLost);
    stats.LoggerThreadId =
        static_cast<unsigned>(GetThreadId(traceProperties->LoggerThreadId));
    return S_OK;
}

bool EtwTraceSession::AddProvider(TraceProviderDescriptor const& provider)
{
    auto entry = find_if(providers, [&](auto const& p) { return p.Id == provider.Id; });
    if (entry == providers.end()) {
        providers.push_back(provider);
        return true;
    }

    bool const enabled = contains(enabledProviders, provider.Id);

    // Update the provider.
    *entry = provider;
    if (enabled)
        EnableProvider(provider.Id);

    return false;
}

bool EtwTraceSession::RemoveProvider(GUID const& providerId)
{
    auto const entry = find_if(providers, EqualProviderId(providerId));
    if (entry == providers.end())
        return false;

    DisableProvider(providerId);
    providers.erase(entry);
    return true;
}

bool EtwTraceSession::EnableProvider(GUID const& providerId)
{
    auto const provider = find_if(providers, EqualProviderId(providerId));
    if (provider == providers.end())
        return false;

    if (traceHandle)
        ThrowOnFail(StartEventProvider(*provider));

    enabledProviders.insert(provider->Id);
    return true;
}

bool EtwTraceSession::DisableProvider(GUID const& providerId)
{
    auto const provider = find(enabledProviders, providerId);
    if (provider == enabledProviders.end())
        return false;

    ThrowOnFail(DisableProviderTrace(providerId));
    enabledProviders.erase(provider);
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

HRESULT
EtwTraceSession::StartEventProvider(TraceProviderDescriptor const& provider) const
{
    std::vector<EVENT_FILTER_DESCRIPTOR> filters;
    SmallVector<std::unique_ptr<std::byte[]>, 3> buffers;

    if (!provider.ExecutableName.empty())
        AddExecutableNameFilter(filters, provider.ExecutableName);
    if (!provider.ProcessIds.empty())
        AddProcessIdFilter(filters, provider.ProcessIds);
    if (!provider.EventIds.empty())
        AddEventFilter(filters, buffers, provider.EventIds, provider.EnableEventIds);
    if (!provider.StackWalkEventIds.empty())
        AddStackWalkFilter(filters, buffers, provider.StackWalkEventIds,
                           provider.EnableStackWalkEventIds);

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
        traceHandle, &provider.Id, EVENT_CONTROL_CODE_ENABLE_PROVIDER, provider.Level,
        provider.MatchAnyKeyword, provider.MatchAllKeyword, 0, &parameters));
}

HRESULT EtwTraceSession::DisableProviderTrace(GUID const& providerId) const
{
    return HResultFromWin32(EnableTraceEx2(traceHandle, &providerId,
                                           EVENT_CONTROL_CODE_DISABLE_PROVIDER, 0, 0, 0,
                                           0, nullptr));
}

std::unique_ptr<ITraceSession> CreateEtwTraceSession(std::wstring_view name,
                                                     TraceProperties const& properties)
{
    return std::make_unique<EtwTraceSession>(name, properties);
}

} // namespace etk
