#pragma once
#if __cplusplus_cli
#include "ITraceLog.h"
#include "Descriptors.h"

namespace EventTraceKit
{

[System::Runtime::InteropServices::UnmanagedFunctionPointer(
    System::Runtime::InteropServices::CallingConvention::Cdecl)]
public delegate bool TraceLogFilterPredicate(
    System::IntPtr eventRecord, System::IntPtr traceEventInfo,
    System::UIntPtr traceEventInfoSize);

public ref class TraceLog : public System::IDisposable
{
public:
    [System::Runtime::InteropServices::UnmanagedFunctionPointer(
        System::Runtime::InteropServices::CallingConvention::Cdecl)]
    delegate void EventsChangedDelegate(System::UIntPtr);

    TraceLog();

    ~TraceLog() { this->!TraceLog(); }
    !TraceLog()
    {
        delete nativeLog;
        delete filteredLog;
    }

    event System::Action<System::UIntPtr>^ EventsChanged;

    property unsigned EventCount
    {
        unsigned get() { return filteredLog->GetEventCount(); }
    }

    void Clear() { nativeLog->Clear(); }

    EventInfo GetEvent(int index)
    {
        auto eventInfo = filteredLog->GetEvent(index);
        EventInfo info;
        info.EventRecord = System::IntPtr(eventInfo.Record());
        info.TraceEventInfo = System::IntPtr(eventInfo.Info());
        info.TraceEventInfoSize = System::UIntPtr((void*)eventInfo.InfoSize());
        return info;
    }

    void SetFilter(TraceLogFilterPredicate^ filter);

internal:
    etk::ITraceLog* Native() { return nativeLog; }

private:
    void OnEventsChanged(System::UIntPtr newCount);

    EventsChangedDelegate^ onEventsChangedCallback;
    etk::ITraceLog* nativeLog;
    etk::IFilteredTraceLog* filteredLog;
    TraceLogFilterPredicate^ filter;
};

} // namespace EventTraceKit

#endif // __cplusplus_cli
