#pragma once
#include <chrono>
#include <cstdint>
#include <memory>
#include <string>
#include <string_view>
#include <vector>

#include <guiddef.h>
#include <windows.h>

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
        , FlushPeriod(std::chrono::seconds(1))
        , ClockResolution(ClockResolutionType::QPC)
    {
    }

    GUID Id;
    unsigned BufferSize;
    unsigned MinimumBuffers;
    unsigned MaximumBuffers;
    std::chrono::duration<unsigned, std::milli> FlushPeriod;
    ClockResolutionType ClockResolution;
    std::wstring LogFileName;
};

struct TraceStatistics
{
    //! The number of buffers allocated for the event tracing session's buffer pool.
    unsigned NumberOfBuffers;

    //! The number of buffers that are allocated but unused in the event tracing session's
    //! buffer pool.
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

class TraceProviderDescriptor
{
public:
    TraceProviderDescriptor(GUID id, uint8_t level = 0xFF,
                            uint64_t anyKeywordMask = 0xFFFFFFFFFFFFFFFFULL,
                            uint64_t allKeywordMask = 0)
        : Id(id)
        , MatchAnyKeyword(anyKeywordMask)
        , MatchAllKeyword(allKeywordMask)
        , Level(level)
    {
    }

    GUID Id;
    uint64_t MatchAnyKeyword;
    uint64_t MatchAllKeyword;
    uint8_t Level;

    bool IncludeSecurityId = false;
    bool IncludeTerminalSessionId = false;
    bool IncludeStackTrace = false;

    std::wstring ExecutableName;
    std::vector<unsigned> ProcessIds;
    std::vector<uint16_t> EventIds;
    std::vector<uint16_t> StackWalkEventIds;
    bool EventIdsFilterIn = true;
    bool StackWalkEventIdsFilterIn = true;

    uint64_t StackWalkMatchAnyKeyword = 0;
    uint64_t StackWalkMatchAllKeyword = 0;
    uint8_t StackWalkLevel = 0;
    bool StackWalkFilterIn = true;
    bool FilterStackWalkLevelKeyword = false;

    std::wstring_view GetManifest() const
    {
        if (manifestOrProviderBinary.empty() || !isManifest)
            return std::wstring_view();
        return manifestOrProviderBinary;
    }

    void SetManifest(std::wstring path)
    {
        manifestOrProviderBinary = std::move(path);
        isManifest = true;
    }

    void SetManifest(std::wstring_view path)
    {
        manifestOrProviderBinary.assign(path.begin(), path.end());
        isManifest = true;
    }

    std::wstring_view GetProviderBinary() const
    {
        if (manifestOrProviderBinary.empty() || isManifest)
            return std::wstring_view();
        return manifestOrProviderBinary;
    }

    void SetProviderBinary(std::wstring path)
    {
        manifestOrProviderBinary = std::move(path);
        isManifest = false;
    }

    void SetProviderBinary(std::wstring_view path)
    {
        manifestOrProviderBinary.assign(path.begin(), path.end());
        isManifest = false;
    }

private:
    std::wstring manifestOrProviderBinary;
    bool isManifest = false;
};

class ITraceSession
{
public:
    virtual ~ITraceSession() = default;

    virtual HRESULT Start() = 0;
    virtual HRESULT Stop() = 0;
    virtual HRESULT Flush() = 0;
    virtual HRESULT Query(TraceStatistics& stats) = 0;

    virtual bool AddProvider(TraceProviderDescriptor const& provider) = 0;
    virtual bool RemoveProvider(GUID const& providerId) = 0;
    virtual bool EnableProvider(GUID const& providerId) = 0;
    virtual bool DisableProvider(GUID const& providerId) = 0;
    virtual void EnableAllProviders() = 0;
    virtual void DisableAllProviders() = 0;

    virtual HRESULT SetKernelProviders(unsigned flags, bool enable) = 0;
};

std::unique_ptr<ITraceSession> CreateEtwTraceSession(std::wstring_view name,
                                                     TraceProperties const& properties);

} // namespace etk
