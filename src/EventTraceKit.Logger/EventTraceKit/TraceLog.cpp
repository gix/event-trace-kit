#include "TraceLog.h"

using namespace System;
using namespace System::Runtime::InteropServices;

namespace EventTraceKit
{

TraceLog::TraceLog()
{
    onEventsChangedCallback = gcnew EventsChangedDelegate(this, &TraceLog::OnEventsChanged);

    auto nativeCallback = static_cast<etk::TraceLogEventsChangedCallback*>(
        Marshal::GetFunctionPointerForDelegate(onEventsChangedCallback).ToPointer());

    std::unique_ptr<etk::ITraceLog> nativeLog;
    std::unique_ptr<etk::IFilteredTraceLog> filteredLog;

    std::tie(nativeLog, filteredLog) = etk::CreateFilteredTraceLog(nativeCallback, nullptr);
    if (!nativeLog)
        throw gcnew Exception("Failed to create native trave log.");

    this->nativeLog = nativeLog.release();
    this->filteredLog = filteredLog.release();
}

void TraceLog::OnEventsChanged(UIntPtr newCount)
{
    EventsChanged(newCount);
}

static etk::TraceLogFilterEvent* GetNativeFunctionPtr(TraceLogFilterPredicate^ filter)
{
    if (!filter) return nullptr;
    return static_cast<etk::TraceLogFilterEvent*>(
        Marshal::GetFunctionPointerForDelegate(filter).ToPointer());
}

void TraceLog::SetFilter(TraceLogFilterPredicate^ filter)
{
    this->filteredLog->SetFilter(GetNativeFunctionPtr(filter));
    this->filter = filter; // Keep the managed delegate alive.
}

} // namespace EventTraceKit
