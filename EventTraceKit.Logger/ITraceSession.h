#pragma once
#include <cstdint>
#include <guiddef.h>

namespace etk
{

enum class ClockResolutionType
{
    QPC = 1,
    SystemTime = 2,
    CpuCycles = 3,
};

struct TraceProperties
{
    TraceProperties(GUID const& id)
        : Id(id)
        , BufferSize(256)
        , MinimumBuffers(4)
        , MaximumBuffers(MinimumBuffers + 20)
        , FlushTimer(1)
        , ClockResolution(ClockResolutionType::QPC)
    {

    }

    GUID Id;
    unsigned BufferSize;
    unsigned MinimumBuffers;
    unsigned MaximumBuffers;
    unsigned FlushTimer;
    ClockResolutionType ClockResolution;
};

struct ProviderState
{
    ProviderState(GUID id, uint8_t level = 0xFF,
                  uint64_t anyKeywordMask = 0xFFFFFFFFFFFFFFFFULL,
                  uint64_t allKeywordMask = 0)
        : Id(id)
        , Level(level)
        , MatchAnyKeyword(anyKeywordMask)
        , MatchAllKeyword(allKeywordMask)
    {
    }

    GUID Id;
    uint8_t Level;
    uint64_t MatchAnyKeyword;
    uint64_t MatchAllKeyword;
};

class ITraceSession
{
public:
    virtual ~ITraceSession()
    {
    }

    virtual void Start() = 0;
    virtual void Stop() = 0;
    virtual void Flush() = 0;
    virtual void Query() = 0;

    virtual bool AddProvider(ProviderState const& provider) = 0;
    virtual bool RemoveProvider(GUID const& providerId) = 0;
    virtual bool EnableProvider(GUID const& providerId) = 0;
    virtual bool DisableProvider(GUID const& providerId) = 0;
    virtual void EnableAllProviders() = 0;
    virtual void DisableAllProviders() = 0;
};

} // namespace etk
