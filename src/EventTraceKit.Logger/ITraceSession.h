#pragma once
#include "ADT/StringView.h"

#include <cstdint>
#include <memory>
#include <string>
#include <vector>
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

struct TraceStatistics
{
    //! The number of buffers allocated for the event tracing session's buffer pool.
    unsigned NumberOfBuffers;

    //! The number of buffers that are allocated but unused in the event tracing session's buffer pool.
    unsigned FreeBuffers;

    //! The number of events that were not recorded.
    unsigned EventsLost;

    //! The number of buffers written.
    unsigned BuffersWritten;

    //! The number of buffers that could not be written to the log file.
    unsigned LogBuffersLost;

    //! The number of buffers that could not be delivered in real-time to the consumer.
    unsigned RealTimeBuffersLost;

    //! The thread identifier for the event tracing session.
    unsigned LoggerThreadId;
};

struct TraceProviderDescriptor
{
    TraceProviderDescriptor(
        GUID id, uint8_t level = 0xFF,
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

    bool IncludeSecurityId = false;
    bool IncludeTerminalSessionId = false;
    bool IncludeStackTrace = false;

    wstring_view GetManifest() const
    {
        if (manifestOrProviderBinary.empty() || !isManifest)
            return wstring_view();
        return manifestOrProviderBinary;
    }

    void SetManifest(std::wstring&& path)
    {
        manifestOrProviderBinary = std::move(path);
        isManifest = true;
    }

    void SetManifest(wstring_view path)
    {
        manifestOrProviderBinary.assign(path.begin(), path.end());
        isManifest = true;
    }

    wstring_view GetProviderBinary() const
    {
        if (manifestOrProviderBinary.empty() || isManifest)
            return wstring_view();
        return manifestOrProviderBinary;
    }

    void SetProviderBinary(std::wstring&& path)
    {
        manifestOrProviderBinary = std::move(path);
        isManifest = false;
    }

    void SetProviderBinary(wstring_view path)
    {
        manifestOrProviderBinary.assign(path.begin(), path.end());
        isManifest = false;
    }

    std::vector<unsigned> ProcessIds;
    std::vector<uint16_t> EventIds;

private:
    std::wstring manifestOrProviderBinary;
    bool isManifest = false;
};

class ITraceSession
{
public:
    virtual ~ITraceSession() {}

    virtual void Start() = 0;
    virtual void Stop() = 0;
    virtual void Flush() = 0;
    virtual void Query(TraceStatistics& stats) = 0;

    virtual bool AddProvider(TraceProviderDescriptor const& provider) = 0;
    virtual bool RemoveProvider(GUID const& providerId) = 0;
    virtual bool EnableProvider(GUID const& providerId) = 0;
    virtual bool DisableProvider(GUID const& providerId) = 0;
    virtual void EnableAllProviders() = 0;
    virtual void DisableAllProviders() = 0;
};

std::unique_ptr<ITraceSession> CreateEtwTraceSession(
    wstring_view name, TraceProperties const& properties);

} // namespace etk
