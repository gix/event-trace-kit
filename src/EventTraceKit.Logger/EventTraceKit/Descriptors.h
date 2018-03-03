#pragma once
#if __cplusplus_cli

namespace EventTraceKit::Tracing
{

public ref class EventProviderDescriptor
{
public:
    EventProviderDescriptor(System::Guid id)
    {
        Id = id;
        Level = 0xFF;
        MatchAnyKeyword = 0xFFFFFFFFFFFFFFFFULL;
        MatchAllKeyword = 0;

        using namespace System::Collections::Generic;
        ProcessIds = gcnew List<unsigned>();
        EventIds = gcnew List<System::UInt16>();
        EnableEventIds = true;
        StackWalkEventIds = gcnew List<System::UInt16>();
        EnableStackWalkEventIds = true;
    }

    EventProviderDescriptor(EventProviderDescriptor^ source)
    {
        if (!source)
            throw gcnew System::ArgumentNullException("source");

        Id = source->Id;
        Level = source->Level;
        MatchAnyKeyword = source->MatchAnyKeyword;
        MatchAllKeyword = source->MatchAllKeyword;

        IncludeSecurityId = source->IncludeSecurityId;
        IncludeTerminalSessionId = source->IncludeTerminalSessionId;
        IncludeStackTrace = source->IncludeStackTrace;

        using namespace System::Collections::Generic;
        ExecutableName = source->ExecutableName;
        ProcessIds = gcnew List<unsigned>(source->ProcessIds);
        EventIds = gcnew List<System::UInt16>(source->EventIds);
        EnableEventIds = source->EnableEventIds;
        StackWalkEventIds = gcnew List<System::UInt16>(source->StackWalkEventIds);
        EnableStackWalkEventIds = source->EnableStackWalkEventIds;

        Manifest = source->Manifest;
        if (source->StartupProjects)
            StartupProjects = gcnew List<System::String^>(source->StartupProjects);
    }

    property System::Guid Id;
    property System::Byte Level;
    property System::UInt64 MatchAnyKeyword;
    property System::UInt64 MatchAllKeyword;

    property bool IncludeSecurityId;
    property bool IncludeTerminalSessionId;
    property bool IncludeStackTrace;

    property System::String^ ExecutableName;
    property System::Collections::Generic::List<unsigned>^ ProcessIds;
    property System::Collections::Generic::List<System::UInt16>^ EventIds;
    property bool EnableEventIds;
    property System::Collections::Generic::List<System::UInt16>^ StackWalkEventIds;
    property bool EnableStackWalkEventIds;

    property System::String^ Manifest;
    property System::Collections::Generic::List<System::String^>^ StartupProjects;
};

public ref class CollectorDescriptor abstract
{
};

public ref class EventCollectorDescriptor : public CollectorDescriptor
{
public:
    EventCollectorDescriptor()
    {
        providers = gcnew System::Collections::Generic::List<EventProviderDescriptor^>();
    }

    EventCollectorDescriptor(EventCollectorDescriptor^ source)
    {
        if (!source)
            throw gcnew System::ArgumentNullException("source");

        BufferSize = source->BufferSize;
        MinimumBuffers = source->MinimumBuffers;
        MaximumBuffers = source->MaximumBuffers;
        LogFileName = source->LogFileName;
        providers = gcnew System::Collections::Generic::List<EventProviderDescriptor^>(source->Providers->Count);
        for each (auto provider in source->Providers)
            providers->Add(gcnew EventProviderDescriptor(provider));
    }

    property System::Nullable<unsigned> BufferSize;
    property System::Nullable<unsigned> MinimumBuffers;
    property System::Nullable<unsigned> MaximumBuffers;
    property System::String^ LogFileName;
    property System::Collections::Generic::IList<EventProviderDescriptor^>^ Providers {
        System::Collections::Generic::IList<EventProviderDescriptor^>^ get() { return providers; }
    }

private:
    System::Collections::Generic::IList<EventProviderDescriptor^>^ providers;
};

public ref class TraceProfileDescriptor
{
public:
    TraceProfileDescriptor()
    {
        collectors = gcnew System::Collections::Generic::List<CollectorDescriptor^>();
    }

    TraceProfileDescriptor(TraceProfileDescriptor^ source)
    {
        if (!source)
            throw gcnew System::ArgumentNullException("source");

        collectors = gcnew System::Collections::Generic::List<CollectorDescriptor^>(source->Collectors->Count);
        for each (auto collector in source->Collectors) {
            if (auto eventCollector = dynamic_cast<EventCollectorDescriptor^>(collector))
                collectors->Add(gcnew EventCollectorDescriptor(eventCollector));
        }
    }

    property System::Collections::Generic::IList<CollectorDescriptor^>^ Collectors {
        System::Collections::Generic::IList<CollectorDescriptor^>^ get() { return collectors; }
    }

private:
    System::Collections::Generic::IList<CollectorDescriptor^>^ collectors;
};

} // namespace EventTraceKit::Tracing

namespace EventTraceKit
{

public value struct EventSessionInfo
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

} // namespace EventTraceKit::Tracing

#endif // __cplusplus_cli
