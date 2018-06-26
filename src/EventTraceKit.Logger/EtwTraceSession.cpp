#include "ITraceSession.h"

#include "ADT/Handle.h"
#include "ADT/SmallVector.h"
#include "ADT/Span.h"
#include "Support/ByteCount.h"
#include "Support/ErrorHandling.h"
#include "Support/Hashing.h"
#include "Support/OSVersionInfo.h"
#include "Support/RangeAdaptors.h"
#include "Support/ThreadpoolTimer.h"

#include <algorithm>
#include <chrono>
#include <memory>
#include <string_view>
#include <unordered_set>

#include <evntcons.h>
#include <evntrace.h>
#include <windows.h>

using namespace std::chrono_literals;

namespace etk
{

namespace
{

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

using FilterPtr = std::unique_ptr<std::byte[]>;

void AddProcessIdFilter(SmallVectorBase<EVENT_FILTER_DESCRIPTOR>& filters,
                        cspan<unsigned> processIds)
{
    auto& descriptor = filters.emplace_back();
    descriptor.Ptr = reinterpret_cast<uintptr_t>(processIds.data());
    descriptor.Size = static_cast<ULONG>(ByteCount(processIds));
    descriptor.Type = EVENT_FILTER_TYPE_PID;
}

void AddExecutableNameFilter(SmallVectorBase<EVENT_FILTER_DESCRIPTOR>& filters,
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

void AddEventIdFilter(SmallVectorBase<EVENT_FILTER_DESCRIPTOR>& filters,
                      SmallVectorBase<FilterPtr>& buffers, cspan<uint16_t> eventIds,
                      bool filterIn, ULONG type)
{
    uint16_t const eventCount = static_cast<uint16_t>(eventIds.size());
    size_t const byteSize = ComputeEventIdFilterSize(eventCount);
    FilterPtr& buffer = buffers.emplace_back(std::make_unique<std::byte[]>(byteSize));

    auto const filter = new (buffer.get()) EVENT_FILTER_EVENT_ID();
    filter->FilterIn = filterIn ? TRUE : FALSE;
    filter->Count = eventCount;
    std::copy_n(eventIds.data(), eventCount, static_cast<uint16_t*>(filter->Events));

    auto& descriptor = filters.emplace_back();
    descriptor.Ptr = reinterpret_cast<uintptr_t>(buffer.get());
    descriptor.Size = static_cast<ULONG>(byteSize);
    descriptor.Type = type;
}

void AddEventFilter(SmallVectorBase<EVENT_FILTER_DESCRIPTOR>& filters,
                    SmallVectorBase<FilterPtr>& buffers, cspan<uint16_t> eventIds,
                    bool filterIn)
{
    AddEventIdFilter(filters, buffers, eventIds, filterIn, EVENT_FILTER_TYPE_EVENT_ID);
}

void AddStackWalkFilter(SmallVectorBase<EVENT_FILTER_DESCRIPTOR>& filters,
                        SmallVectorBase<FilterPtr>& buffers, cspan<uint16_t> eventIds,
                        bool filterIn)
{
    AddEventIdFilter(filters, buffers, eventIds, filterIn, EVENT_FILTER_TYPE_STACKWALK);
}

void AddStackWalkLevelKeywordFilter(SmallVectorBase<EVENT_FILTER_DESCRIPTOR>& filters,
                                    SmallVectorBase<FilterPtr>& buffers,
                                    ULONGLONG matchAnyKeyword, ULONGLONG matchAllKeyword,
                                    BYTE level, bool filterIn)
{
    size_t const byteSize = sizeof(EVENT_FILTER_LEVEL_KW);
    FilterPtr& buffer = buffers.emplace_back(std::make_unique<std::byte[]>(byteSize));

    auto const filter = new (buffer.get()) EVENT_FILTER_LEVEL_KW();
    filter->MatchAnyKeyword = matchAnyKeyword;
    filter->MatchAllKeyword = matchAllKeyword;
    filter->Level = level;
    filter->FilterIn = filterIn ? TRUE : FALSE;

    auto& descriptor = filters.emplace_back();
    descriptor.Ptr = reinterpret_cast<uintptr_t>(buffer.get());
    descriptor.Size = static_cast<ULONG>(byteSize);
    descriptor.Type = EVENT_FILTER_TYPE_STACKWALK_LEVEL_KW;
}

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

    virtual HRESULT SetKernelProviders(unsigned flags, bool enable) override;

private:
    void SetProperties(TraceProperties const& properties);
    HRESULT StartEventProvider(TraceProviderDescriptor const& provider) const;
    HRESULT DisableProviderTrace(GUID const& providerId) const;

    static void CALLBACK FlushTimerCallback(_Inout_ PTP_CALLBACK_INSTANCE /*Instance*/,
                                            _Inout_opt_ PVOID Context,
                                            _Inout_ PTP_TIMER /*Timer*/)
    {
        reinterpret_cast<EtwTraceSession*>(Context)->Flush();
    }

    std::wstring sessionName;
    std::unique_ptr<EventTraceProperties> traceProperties;
    TRACEHANDLE traceHandle;
    std::vector<TraceProviderDescriptor> providers;
    std::unordered_set<GUID> enabledProviders;
    ThreadpoolTimer customFlushTimer;
    std::chrono::duration<unsigned, std::milli> customFlushPeriod{};
};

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

void EventTraceProperties::operator delete(void* mem, size_t /*n*/)
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
    traceProperties->FlushTimer = 0;
    traceProperties->EnableFlags = 0;

    if (properties.FlushPeriod == 0ms) {
        // Default 1 second.
        traceProperties->FlushTimer = 1;
    } else if ((properties.FlushPeriod % 1000) == 0ms) {
        // Flush period with second resolution can use the buildt-in timer.
        traceProperties->FlushTimer = properties.FlushPeriod.count() / 1000ul;
    } else {
        // Custom timer for higher resolution periods.
        traceProperties->FlushTimer = 0;
        customFlushTimer = ThreadpoolTimer(FlushTimerCallback, this);
        customFlushPeriod = properties.FlushPeriod;
    }
}

HRESULT EtwTraceSession::Start()
{
    HR(HResultFromWin32(
        StartTraceW(&traceHandle, sessionName.c_str(), traceProperties.get())));

    for (auto& provider : providers) {
        if (contains(enabledProviders, provider.Id))
            (void)StartEventProvider(provider);
    }

    if (customFlushTimer)
        customFlushTimer.Start(customFlushPeriod);

    return S_OK;
}

HRESULT EtwTraceSession::Stop()
{
    if (customFlushTimer)
        customFlushTimer.Stop();

    if (traceHandle != 0) {
        HRESULT const hr = HResultFromWin32(ControlTraceW(
            traceHandle, nullptr, traceProperties.get(), EVENT_TRACE_CONTROL_STOP));
        if (FAILED(hr) && hr != HResultFromWin32(ERROR_MORE_DATA))
            (void)ETK_TRACE_HR(hr);
        traceHandle = 0;
    }
    return S_OK;
}

HRESULT EtwTraceSession::Flush()
{
    ControlTraceW(traceHandle, nullptr, traceProperties.get(), EVENT_TRACE_CONTROL_FLUSH);
    return S_OK;
}

HRESULT EtwTraceSession::Query(TraceStatistics& stats)
{
    HRESULT hr = HResultFromWin32(ControlTraceW(
        traceHandle, nullptr, traceProperties.get(), EVENT_TRACE_CONTROL_QUERY));
    if (FAILED(hr)) {
        stats = {};
        return hr;
    }

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
    auto const entry =
        find_if(providers, [&](auto const& p) { return p.Id == provider.Id; });
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
    auto const entry =
        find_if(providers, [&](auto const& p) { return p.Id == providerId; });
    if (entry == providers.end())
        return false;

    DisableProvider(providerId);
    providers.erase(entry);
    return true;
}

bool EtwTraceSession::EnableProvider(GUID const& providerId)
{
    auto const provider =
        find_if(providers, [&](auto const& p) { return p.Id == providerId; });
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
    SmallVector<EVENT_FILTER_DESCRIPTOR, 4> filters;
    SmallVector<std::unique_ptr<std::byte[]>, 3> buffers;

    if (OSVersion.IsWindows8Point1OrGreater() && !provider.ExecutableName.empty())
        AddExecutableNameFilter(filters, provider.ExecutableName);

    if (!provider.ProcessIds.empty())
        AddProcessIdFilter(filters, provider.ProcessIds);

    if (!provider.EventIds.empty())
        AddEventFilter(filters, buffers, provider.EventIds, provider.EventIdsFilterIn);

    if (OSVersion.IsWindows10Version1709OrGreater() &&
        !provider.StackWalkEventIds.empty())
        AddStackWalkFilter(filters, buffers, provider.StackWalkEventIds,
                           provider.StackWalkEventIdsFilterIn);

    if (OSVersion.IsWindows10Version1709OrGreater() &&
        provider.FilterStackWalkLevelKeyword) {
        AddStackWalkLevelKeywordFilter(
            filters, buffers, provider.StackWalkMatchAnyKeyword,
            provider.StackWalkMatchAllKeyword, provider.StackWalkLevel,
            provider.StackWalkFilterIn);
    }

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

HRESULT EtwTraceSession::SetKernelProviders(unsigned flags, bool enable)
{
    if (enable)
        traceProperties->EnableFlags |= flags;
    else
        traceProperties->EnableFlags &= ~flags;
    return S_OK;
}

std::unique_ptr<ITraceSession> CreateEtwTraceSession(std::wstring_view name,
                                                     TraceProperties const& properties)
{
    return std::make_unique<EtwTraceSession>(name, properties);
}

} // namespace etk
