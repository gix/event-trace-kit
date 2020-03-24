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

        EventIdsFilterIn = true;
        StackWalkEventIdsFilterIn = true;

        FilterStackWalkLevelKeyword = false;
        StackWalkFilterIn = true;
        StackWalkLevel = 0;
        StackWalkMatchAnyKeyword = 0;
        StackWalkMatchAllKeyword = 0;
    }

    EventProviderDescriptor(EventProviderDescriptor^ source)
    {
        using namespace System;
        using namespace System::Collections::Generic;

        if (!source)
            throw gcnew System::ArgumentNullException("source");

        Id = source->Id;
        Level = source->Level;
        MatchAnyKeyword = source->MatchAnyKeyword;
        MatchAllKeyword = source->MatchAllKeyword;

        IncludeSecurityId = source->IncludeSecurityId;
        IncludeTerminalSessionId = source->IncludeTerminalSessionId;
        IncludeStackTrace = source->IncludeStackTrace;

        ExecutableName = source->ExecutableName;
        if (source->ProcessIds)
            ProcessIds = gcnew List<unsigned>(source->ProcessIds);

        EventIdsFilterIn = source->EventIdsFilterIn;
        if (source->EventIds)
            EventIds = gcnew List<UInt16>(source->EventIds);

        StackWalkEventIdsFilterIn = source->StackWalkEventIdsFilterIn;
        if (source->StackWalkEventIds)
            StackWalkEventIds = gcnew List<UInt16>(source->StackWalkEventIds);

        FilterStackWalkLevelKeyword = source->FilterStackWalkLevelKeyword;
        StackWalkFilterIn = source->StackWalkFilterIn;
        StackWalkLevel = source->StackWalkLevel;
        StackWalkMatchAnyKeyword = source->StackWalkMatchAnyKeyword;
        StackWalkMatchAllKeyword = source->StackWalkMatchAllKeyword;

        Manifest = source->Manifest;
        if (source->StartupProjects)
            StartupProjects = gcnew List<String^>(source->StartupProjects);
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

    property bool EventIdsFilterIn;
    property System::Collections::Generic::List<System::UInt16>^ EventIds;

    property bool StackWalkEventIdsFilterIn;
    property System::Collections::Generic::List<System::UInt16>^ StackWalkEventIds;

    property bool FilterStackWalkLevelKeyword;
    property bool StackWalkFilterIn;
    property System::Byte StackWalkLevel;
    property System::UInt64 StackWalkMatchAnyKeyword;
    property System::UInt64 StackWalkMatchAllKeyword;

    property System::String^ Manifest;
    property System::Collections::Generic::List<System::String^>^ StartupProjects;
};

public ref class CollectorDescriptor abstract
{
public:
    property System::Nullable<unsigned> BufferSize;
    property System::Nullable<unsigned> MinimumBuffers;
    property System::Nullable<unsigned> MaximumBuffers;
    property System::String^ LogFileName;
    property System::Nullable<System::TimeSpan> FlushPeriod;

protected:
    CollectorDescriptor()
    {
    }

    CollectorDescriptor(CollectorDescriptor^ source)
    {
        if (!source)
            throw gcnew System::ArgumentNullException("source");

        BufferSize = source->BufferSize;
        MinimumBuffers = source->MinimumBuffers;
        MaximumBuffers = source->MaximumBuffers;
        LogFileName = source->LogFileName;
        FlushPeriod = source->FlushPeriod;
    }
};

public ref class EventCollectorDescriptor : public CollectorDescriptor
{
public:
    EventCollectorDescriptor()
    {
        providers = gcnew System::Collections::Generic::List<EventProviderDescriptor^>();
    }

    EventCollectorDescriptor(EventCollectorDescriptor^ source)
        : CollectorDescriptor(source)
    {
        providers = gcnew System::Collections::Generic::List<EventProviderDescriptor^>(source->Providers->Count);
        for each (auto provider in source->Providers)
            providers->Add(gcnew EventProviderDescriptor(provider));
    }

    property System::Collections::Generic::IList<EventProviderDescriptor^>^ Providers {
        System::Collections::Generic::IList<EventProviderDescriptor^>^ get() { return providers; }
    }

private:
    System::Collections::Generic::IList<EventProviderDescriptor^>^ providers;
};

public ref class SystemCollectorDescriptor : public CollectorDescriptor
{
public:
    SystemCollectorDescriptor()
    {
        KernelFlags = 0;
    }

    SystemCollectorDescriptor(SystemCollectorDescriptor^ source)
    {
        if (!source)
            throw gcnew System::ArgumentNullException("source");

        KernelFlags = source->KernelFlags;
    }

    property System::UInt32 KernelFlags;
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
            if (auto systemCollector = dynamic_cast<SystemCollectorDescriptor^>(collector))
                collectors->Add(gcnew SystemCollectorDescriptor(systemCollector));
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
