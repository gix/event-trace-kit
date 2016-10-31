#pragma once
#if __cplusplus_cli

namespace EventTraceKit
{

public ref class TraceProviderDescriptor
{
public:
    TraceProviderDescriptor(System::Guid id)
    {
        Id = id;
        Level = 0xFF;
        MatchAnyKeyword = 0xFFFFFFFFFFFFFFFFULL;
        MatchAllKeyword = 0;

        ProcessIds = gcnew System::Collections::Generic::List<unsigned>();
        EventIds = gcnew System::Collections::Generic::List<System::UInt16>();
    }

    property System::Guid Id;
    property System::Byte Level;
    property System::UInt64 MatchAnyKeyword;
    property System::UInt64 MatchAllKeyword;

    property bool IncludeSecurityId;
    property bool IncludeTerminalSessionId;
    property bool IncludeStackTrace;

    property System::String^ Manifest;
    property System::Collections::Generic::List<unsigned>^ ProcessIds;
    property System::Collections::Generic::List<System::UInt16>^ EventIds;
};

public ref class TraceSessionDescriptor
{
public:
    TraceSessionDescriptor()
    {
        Providers = gcnew System::Collections::Generic::List<TraceProviderDescriptor^>();
    }

    property System::Nullable<unsigned> BufferSize;
    property System::Nullable<unsigned> MinimumBuffers;
    property System::Nullable<unsigned> MaximumBuffers;
    property System::String^ LogFileName;
    property System::Collections::Generic::IList<TraceProviderDescriptor^>^ Providers;
};

public value struct TraceSessionInfo
{
    property long long StartTime;
    property long long PerfFreq;
    property unsigned PointerSize;
};

public value struct EventInfo
{
    property System::IntPtr EventRecord;
    property System::IntPtr TraceEventInfo;
    property System::UIntPtr TraceEventInfoSize;
};

} // namespace EventTraceKit

#endif // __cplusplus_cli
