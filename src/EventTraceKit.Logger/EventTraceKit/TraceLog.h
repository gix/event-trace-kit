#pragma once
#include "ITraceLog.h"

#if __cplusplus_cli

namespace EventTraceKit
{

public ref class TraceLog : public System::IDisposable
{
public:
    [System::Runtime::InteropServices::UnmanagedFunctionPointer(
        System::Runtime::InteropServices::CallingConvention::Cdecl)]
    delegate void EventsChangedDelegate(System::UIntPtr);

    TraceLog()
    {
        using namespace System;
        using namespace System::Runtime::InteropServices;

        onEventsChangedCallback = gcnew EventsChangedDelegate(this, &TraceLog::OnEventsChanged);

        auto nativeCallback = static_cast<etk::TraceLogEventsChangedCallback*>(
            Marshal::GetFunctionPointerForDelegate(onEventsChangedCallback).ToPointer());
        auto nativeLog = etk::CreateEtwTraceLog(nativeCallback);
        if (!nativeLog)
            throw gcnew Exception("Failed to create native trave log.");

        this->nativeLog = nativeLog.release();
    }

    ~TraceLog() { this->!TraceLog(); }
    !TraceLog() { delete nativeLog; }

    event System::Action<System::UIntPtr>^ EventsChanged;

    property unsigned EventCount
    {
        unsigned get() { return nativeLog->GetEventCount(); }
    }

    void Clear() { nativeLog->Clear(); }

    EventInfo GetEvent(int index)
    {
        auto eventInfo = nativeLog->GetEvent(index);
        EventInfo info;
        info.EventRecord = System::IntPtr(eventInfo.Record());
        info.TraceEventInfo = System::IntPtr(eventInfo.Info());
        info.TraceEventInfoSize = System::UIntPtr((void*)eventInfo.InfoSize());
        return info;
    }

internal:
    etk::ITraceLog* Native() { return nativeLog; }

private:
    void OnEventsChanged(System::UIntPtr newCount)
    {
        EventsChanged(newCount);
    }

    EventsChangedDelegate^ onEventsChangedCallback;
    etk::ITraceLog* nativeLog;
};

} // namespace EventTraceKit

#endif // __cplusplus_cli
