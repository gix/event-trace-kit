#include "TraceLog.h"
#include <msclr/marshal_cppstd.h>

using namespace System;
using namespace System::ComponentModel;
using namespace System::Runtime::InteropServices;
using msclr::interop::marshal_as;

namespace EventTraceKit::Tracing
{

namespace
{

etk::TraceLogFilterEvent* GetNativeFunctionPtr(TraceLogFilterPredicate^ filter)
{
    if (!filter) return nullptr;
    return static_cast<etk::TraceLogFilterEvent*>(
        Marshal::GetFunctionPointerForDelegate(filter).ToPointer());
}

class ManagedTraceLogFilter : public etk::TraceLogFilter
{
public:
    ManagedTraceLogFilter(TraceLogFilterPredicate^ filter)
        : etk::TraceLogFilter(GetNativeFunctionPtr(filter)), predicate(filter)
    {
    }

private:
    gcroot<TraceLogFilterPredicate^> predicate;
};

} // namespace

TraceLog::TraceLog()
{
    onEventsChangedCallback = gcnew EventsChangedDelegate(this, &TraceLog::OnEventsChanged);

    auto nativeCallback = static_cast<etk::TraceLogEventsChangedCallback*>(
        Marshal::GetFunctionPointerForDelegate(onEventsChangedCallback).ToPointer());

    auto [nativeLog, filteredLog] = etk::CreateFilteredTraceLog(nativeCallback, nullptr);
    if (!nativeLog)
        throw gcnew Exception("Failed to create native trace log.");

    this->nativeLog = nativeLog.release();
    this->filteredLog = filteredLog.release();
}

void TraceLog::OnEventsChanged(UIntPtr newCount)
{
    EventsChanged(newCount);
}

void TraceLog::SetFilter(TraceLogFilterPredicate^ filter)
{
    auto t = std::make_unique<ManagedTraceLogFilter>(filter);
    this->filteredLog->SetFilter(t.get());
    t.release();
}


void TraceLog::UpdateTraceData(TraceProfileDescriptor^ profile)
{
    std::vector<std::wstring> manifests;

    for each (auto collector in profile->Collectors) {
        EventCollectorDescriptor^ eventCollector = dynamic_cast<EventCollectorDescriptor^>(collector);
        if (!eventCollector)
            continue;

        for each (EventProviderDescriptor^ provider in eventCollector->Providers) {
            if (provider->Manifest)
                manifests.push_back(marshal_as<std::wstring>(provider->Manifest));
        }
    }

    if (!manifests.empty()) {
        HRESULT hr = nativeLog->UpdateTraceData(manifests);
        if (FAILED(hr))
            throw gcnew Win32Exception(hr);
    }
}

} // namespace EventTraceKit::Tracing
