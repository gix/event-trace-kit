#pragma once
#include "ADT/StringView.h"
#include "ITraceSession.h"
#include "Support/Hashing.h"

#include <unordered_set>

#include <windows.h>
#include <evntrace.h>
#include <tdh.h>

namespace etk
{

class TdhManifestLoadCookie
{
public:
    TdhManifestLoadCookie()
        : path()
        , loaded(false) {}

    TdhManifestLoadCookie(std::wstring path)
        : path(std::move(path))
        , loaded(false) {}

    ~TdhManifestLoadCookie()
    {
        if (loaded)
            TdhUnloadManifest(const_cast<wchar_t*>(path.c_str()));
    }

    TdhManifestLoadCookie(TdhManifestLoadCookie&& source)
        : path(std::move(source.path))
        , loaded(std::exchange(source.loaded, false))
    {
    }

    TdhManifestLoadCookie& operator =(TdhManifestLoadCookie&& source)
    {
        using namespace std;
        path = std::move(source.path);
        loaded = std::exchange(source.loaded, false);
        return *this;
    }

    TdhManifestLoadCookie(TdhManifestLoadCookie const&) = delete;
    TdhManifestLoadCookie& operator =(TdhManifestLoadCookie const&) = delete;

private:
    std::wstring path;
    bool loaded;
};

class EventTraceProperties : public EVENT_TRACE_PROPERTIES
{
public:
    static std::unique_ptr<EventTraceProperties> Create(wstring_view name);
    void* operator new(size_t n, wstring_view name);
    void operator delete(void* mem);
private:
    EventTraceProperties() = default;
};

class EtwTraceSession : public ITraceSession
{
public:
    EtwTraceSession(wstring_view name, TraceProperties const& properties);
    virtual ~EtwTraceSession();

    virtual void Start() override;
    virtual void Stop() override;
    virtual void Flush() override;
    virtual void Query(TraceStatistics& stats) override;

    virtual bool AddProvider(TraceProviderDescriptor const& provider) override;
    virtual bool RemoveProvider(GUID const& providerId) override;
    virtual bool EnableProvider(GUID const& providerId) override;
    virtual bool DisableProvider(GUID const& providerId) override;
    virtual void EnableAllProviders() override;
    virtual void DisableAllProviders() override;

private:
    void SetProperties(TraceProperties const& properties);
    HRESULT EnableProviderTrace(TraceProviderDescriptor const& provider) const;
    HRESULT DisableProviderTrace(GUID const& providerId) const;

    std::wstring sessionName;
    std::unique_ptr<EventTraceProperties> traceProperties;
    TRACEHANDLE traceHandle;
    std::vector<TraceProviderDescriptor> providers;
    std::unordered_set<GUID> enabledProviders;
};

} // namespace etk
