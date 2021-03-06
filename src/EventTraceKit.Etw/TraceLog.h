#pragma once
#if __cplusplus_cli
#include "etk/ITraceLog.h"
#include "Descriptors.h"

namespace EventTraceKit::Tracing
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

    property unsigned TotalEventCount
    {
        unsigned get() { return nativeLog->GetEventCount(); }
    }

    void Clear() { nativeLog->Clear(); }

    EventInfo GetEvent(int index)
    {
        auto eventInfo = filteredLog->GetEvent(index);
        EventInfo info;
        info.EventRecord = System::IntPtr(const_cast<EVENT_RECORD*>(eventInfo.Record()));
        info.TraceEventInfo = System::IntPtr(const_cast<TRACE_EVENT_INFO*>(eventInfo.Info()));
        info.TraceEventInfoSize = System::UIntPtr((void*)eventInfo.InfoSize());
        return info;
    }

    EventSessionInfo GetInfo() { return sessionInfo; }

    void SetFilter(TraceLogFilterPredicate^ filter);

    void UpdateTraceData(TraceProfileDescriptor^ profile);

internal:
    etk::ITraceLog* Native() { return nativeLog; }

    void SetSessionInfo(EventSessionInfo sessionInfo)
    {
        this->sessionInfo = sessionInfo;
    }

private:
    void OnEventsChanged(System::UIntPtr newCount);

    EventSessionInfo sessionInfo;
    EventsChangedDelegate^ onEventsChangedCallback;
    etk::ITraceLog* nativeLog;
    etk::IFilteredTraceLog* filteredLog;
};

} // namespace EventTraceKit::Tracing

#endif // __cplusplus_cli
