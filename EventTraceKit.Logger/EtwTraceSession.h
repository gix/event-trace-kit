#pragma once
#include "ITraceSession.h"

#include "ADT/StringView.h"
#include <Windows.h>

#include <set>
#include <vector>
#include <evntrace.h>

namespace etk
{

class EtwTraceSession : public ITraceSession
{
public:
    EtwTraceSession(wstring_view name, TraceProperties const& properties);
    virtual ~EtwTraceSession();

    virtual void Start() override;
    virtual void Stop() override;
    virtual void Flush() override;
    virtual void Query() override;

    virtual bool AddProvider(ProviderState const& provider) override;
    virtual bool RemoveProvider(GUID const& providerId) override;
    virtual bool EnableProvider(GUID const& providerId) override;
    virtual bool DisableProvider(GUID const& providerId) override;
    virtual void EnableAllProviders() override;
    virtual void DisableAllProviders() override;

private:
    void SetProperties(TraceProperties const& properties);
    HRESULT EnableProviderTrace(ProviderState const& provider) const;
    HRESULT DisableProviderTrace(GUID const& providerId) const;

    std::wstring sessionName;
    std::vector<uint8_t> tracePropertiesBuffer;
    EVENT_TRACE_PROPERTIES* traceProperties;
    TRACEHANDLE traceHandle;
    std::vector<ProviderState> providers;
    std::set<GUID> enabledProviders;
};

} // namespace tracemon
