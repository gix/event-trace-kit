#include "ADT/Guid.h"
#include "ADT/LruCache.h"
#include "EtwTraceSession.h"
#include "EtwTraceProcessorCreate.h"

#include <msclr/lock.h>
#include <msclr/marshal.h>
#include <msclr/marshal_cppstd.h>
#include <vcclr.h>

#include <memory>
#include <string>

using namespace System;
using namespace System::Linq;
using namespace System::Collections::Generic;
using msclr::interop::marshal_as;


namespace EventTraceKit
{

public ref struct TraceEvent
{
    property System::Guid ProviderId;
    property uint16_t Id;
    property uint8_t Version;
    property uint8_t ChannelId;
    property uint8_t LevelId;
    property uint8_t OpcodeId;
    property uint16_t TaskId;
    property uint64_t KeywordMask;

    property String^ Provider;
    property String^ Channel;
    property String^ Level;
    property String^ Opcode;
    property String^ Task;
    property String^ Keywords;

    property DateTime Time;
    property unsigned ProcessId;
    property unsigned ThreadId;
    property uint64_t ProcessorTime;
    property String^ Message;
    property bool Formatted;
};

public ref struct TraceStatistics
{
    property unsigned NumberOfBuffers;
    property unsigned FreeBuffers;
    property unsigned EventsLost;
    property unsigned BuffersWritten;
    property unsigned LogBuffersLost;
    property unsigned RealTimeBuffersLost;
    property unsigned LoggerThreadId;
};

public ref class TraceProviderSpec
{
public:
    TraceProviderSpec(Guid id)
    {
        Id = id;
        Level = 0xFF;
        MatchAnyKeyword = 0xFFFFFFFFFFFFFFFFULL;
        MatchAllKeyword = 0;
    }

    property Guid Id;
    property uint8_t Level;
    property uint64_t MatchAnyKeyword;
    property uint64_t MatchAllKeyword;

    property bool IncludeSecurityId;
    property bool IncludeTerminalSessionId;
    property bool IncludeStackTrace;

    property String^ Manifest
    {
        String^ get()
        {
            if (manifestOrProviderBinary->Length == 0 || !isManifest)
                return nullptr;
            return manifestOrProviderBinary;
        }
    }

    property String^ ProviderBinary
    {
        String^ get()
        {
            if (manifestOrProviderBinary->Length == 0 || isManifest)
                return nullptr;
            return manifestOrProviderBinary;
        }
    }

    void SetManifest(String^ path)
    {
        manifestOrProviderBinary = path;
        isManifest = true;
    }

    void SetProviderBinary(String^ path)
    {
        manifestOrProviderBinary = path;
        isManifest = false;
    }

    List<unsigned>^ ProcessIds;
    List<uint16_t>^ EventIds;

private:
    String^ manifestOrProviderBinary;
    bool isManifest = false;
};

} // namespace EventTraceKit

namespace msclr
{
namespace interop
{

template<>
inline System::DateTime marshal_as<System::DateTime, FILETIME>(FILETIME const& time)
{
    uint64_t ticks = (uint64_t(time.dwHighDateTime) << 32) | time.dwLowDateTime;
    return System::DateTime::FromFileTime(ticks);
}

template<>
inline String^ marshal_as(etk::wstring_view const& str)
{
    auto size = static_cast<int>(
        std::min(static_cast<size_t>(System::Int32::MaxValue), str.size()));
    return gcnew System::String(str.data(), 0, size);
}

template<>
inline System::Guid marshal_as(GUID const& guid)
{
    return System::Guid(
        guid.Data1, guid.Data2, guid.Data3,
        guid.Data4[0], guid.Data4[1], guid.Data4[2], guid.Data4[3],
        guid.Data4[4], guid.Data4[5], guid.Data4[6], guid.Data4[7]);
}

template<>
inline GUID marshal_as(System::Guid const& guid)
{
    array<System::Byte>^ guidData = const_cast<System::Guid&>(guid).ToByteArray();
    pin_ptr<System::Byte> data = &(guidData[0]);
    return *reinterpret_cast<GUID*>(data);
}

template<typename T>
inline std::vector<T> marshal_as_vector(List<T>^ list)
{
    if (list == nullptr)
        return std::vector<T>();

    std::vector<T> result;
    result.reserve(list->Count);
    for each (T item in list)
        result.push_back(item);
    return result;
}

template<>
inline etk::TraceProviderSpec marshal_as(EventTraceKit::TraceProviderSpec^ const& provider)
{
    etk::TraceProviderSpec native(
        marshal_as<GUID>(provider->Id), provider->Level, provider->MatchAnyKeyword,
        provider->MatchAllKeyword);
    native.IncludeSecurityId = provider->IncludeSecurityId;
    native.IncludeTerminalSessionId = provider->IncludeTerminalSessionId;
    native.IncludeStackTrace = provider->IncludeStackTrace;
    if (provider->Manifest)
        native.SetManifest(marshal_as<std::wstring>(provider->Manifest));
    if (provider->ProviderBinary)
        native.SetProviderBinary(marshal_as<std::wstring>(provider->ProviderBinary));

    native.ProcessIds = marshal_as_vector(provider->ProcessIds);
    native.EventIds = marshal_as_vector(provider->EventIds);

    return native;
}

} // namespace interop
} // namespace msclr

namespace EventTraceKit
{

class EventSink : public etk::IEventSink
{
public:
    EventSink(IList<TraceEvent^>^ events)
        : events(events)
        , providerNameCache(10)
        , eventStringsCache(50)
    {
    }

    virtual void ProcessEvent(etk::FormattedEvent const& event) override
    {
        msclr::lock l(events);
        events->Add(Convert(event));
    }

private:
    String^ MakeString(wchar_t const* str)
    {
        return str ? gcnew String(str) : nullptr;
    }

    TraceEvent^ Convert(etk::FormattedEvent const& event)
    {
        String^ provider = providerNameCache.GetOrCreate(
            event.ProviderId, [&](GUID const&) {
            return gcroot<String^>(MakeString(event.Info.ProviderName()));
        });

        EventInfoStrings& strings = eventStringsCache.GetOrCreate(
            etk::EventInfoKey(event.ProviderId, event.Descriptor.Id),
            [&](etk::EventInfoKey const&) {
            EventInfoStrings s;
            s.ChannelName = MakeString(event.Info.ChannelName());
            s.LevelName = MakeString(event.Info.LevelName());
            s.TaskName = MakeString(event.Info.TaskName());
            s.OpcodeName = MakeString(event.Info.OpcodeName());
            s.Keywords = MakeString(event.Info.KeywordsName());
            return s;
        });

        auto managed = gcnew TraceEvent();

        managed->ProviderId = marshal_as<Guid>(event.ProviderId);
        managed->Id = event.Descriptor.Id;
        managed->Version = event.Descriptor.Version;
        managed->ChannelId = event.Descriptor.Channel;
        managed->LevelId = event.Descriptor.Level;
        managed->OpcodeId = event.Descriptor.Opcode;
        managed->TaskId = event.Descriptor.Task;
        managed->KeywordMask = event.Descriptor.Keyword;

        managed->Provider = provider;
        managed->Channel = strings.ChannelName;
        managed->Level = strings.LevelName;
        managed->Opcode = strings.OpcodeName;
        managed->Task = strings.TaskName;
        managed->Keywords = strings.Keywords;

        managed->Time = marshal_as<DateTime>(event.Time);
        managed->ProcessId = event.ProcessId;
        managed->ThreadId = event.ThreadId;
        managed->ProcessorTime = event.ProcessorTime;
        managed->Message = marshal_as<String^>(event.Message);
        managed->Formatted = !!event.Info;

        return managed;
    }

    struct EventInfoStrings
    {
        gcroot<String^> ChannelName;
        gcroot<String^> LevelName;
        gcroot<String^> TaskName;
        gcroot<String^> OpcodeName;
        gcroot<String^> Keywords;
    };

    etk::LruCache<GUID, gcroot<String^>> providerNameCache;
    etk::LruCache<etk::EventInfoKey, EventInfoStrings, boost::hash<etk::EventInfoKey>> eventStringsCache;
    gcroot<IList<TraceEvent^>^> events;
};

public ref class TraceSession : public System::IDisposable
{
public:
    TraceSession(IList<TraceEvent^>^ events, IEnumerable<TraceProviderSpec^>^ providers);
    ~TraceSession();

    void Start();
    void Stop();
    void Clear();
    TraceStatistics^ Query();

private:
    array<TraceProviderSpec^>^ providers;
    IList<TraceEvent^>^ events;
    std::wstring* loggerName = nullptr;
    EventSink* eventSink = nullptr;
    std::vector<etk::TraceProviderSpec>* nativeProviders = nullptr;
    etk::ITraceSession* session = nullptr;
    etk::ITraceProcessor* processor = nullptr;
};

static std::wstring LoggerNameBase = L"ETK_54644792-9281-48E9-B69D-E82A86F98960";

static std::wstring CreateLoggerName()
{
    int pid = System::Diagnostics::Process::GetCurrentProcess()->Id;
    return LoggerNameBase + L"_" + std::to_wstring(pid);
}

TraceSession::TraceSession(IList<TraceEvent^>^ events,
                           IEnumerable<TraceProviderSpec^>^ providers)
    : events(events)
    , providers(Enumerable::ToArray(providers))
    , loggerName(new std::wstring(CreateLoggerName()))
{
    etk::TraceProperties properties(marshal_as<GUID>(System::Guid::NewGuid()));
    auto session = etk::CreateEtwTraceSession(*loggerName, properties);

    nativeProviders = new std::vector<etk::TraceProviderSpec>();
    for each (TraceProviderSpec^ provider in this->providers) {
        nativeProviders->push_back(marshal_as<etk::TraceProviderSpec>(provider));
    }

    for (auto const& provider : *nativeProviders) {
        session->AddProvider(provider);
        session->EnableProvider(provider.Id);
    }

    eventSink = new EventSink(events);
    this->session = session.release();
}

TraceSession::~TraceSession()
{
    Stop();
    if (session)
        delete session;

    delete eventSink;
    delete loggerName;
    delete nativeProviders;
}

void TraceSession::Start()
{
    session->Start();

    auto processor = etk::CreateEtwTraceProcessor(*loggerName, *nativeProviders);
    processor->SetEventSink(eventSink);

    this->processor = processor.release();
    this->processor->StartProcessing();
}

void TraceSession::Stop()
{
    if (!processor)
        return;

    session->Stop();
    processor->StopProcessing();

    delete processor;
    processor = nullptr;
}

void TraceSession::Clear()
{
    if (!processor)
        return;

    session->Flush();
}

TraceStatistics^ TraceSession::Query()
{
    if (!session)
        return gcnew TraceStatistics();

    etk::TraceStatistics nativeStats;
    session->Query(nativeStats);

    auto stats = gcnew TraceStatistics();
    stats->NumberOfBuffers = nativeStats.NumberOfBuffers;
    stats->FreeBuffers = nativeStats.FreeBuffers;
    stats->EventsLost = nativeStats.EventsLost;
    stats->BuffersWritten = nativeStats.BuffersWritten;
    stats->LogBuffersLost = nativeStats.LogBuffersLost;
    stats->RealTimeBuffersLost = nativeStats.RealTimeBuffersLost;
    stats->LoggerThreadId = nativeStats.LoggerThreadId;

    return stats;
}

} // namespace EventTraceKit
