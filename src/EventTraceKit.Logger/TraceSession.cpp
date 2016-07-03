#include "ADT/Guid.h"
#include "ITraceLog.h"
#include "ITraceProcessor.h"
#include "ITraceSession.h"
#include "WatchDog.h"

#include <msclr/lock.h>
#include <msclr/marshal.h>
#include <msclr/marshal_cppstd.h>
#include <vcclr.h>

#include <memory>
#include <string>

using namespace System;
using namespace System::Linq;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
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

public ref class TraceProviderDescriptor
{
public:
    TraceProviderDescriptor(Guid id)
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

public ref class TraceSessionDescriptor
{
public:
    TraceSessionDescriptor()
    {
        Providers = gcnew List<TraceProviderDescriptor^>();
    }

    property IList<TraceProviderDescriptor^>^ Providers;
};

} // namespace EventTraceKit

namespace msclr
{
namespace interop
{

template<>
inline System::DateTime marshal_as<System::DateTime, ::FILETIME>(::FILETIME const& time)
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
inline etk::TraceProviderDescriptor marshal_as(EventTraceKit::TraceProviderDescriptor^ const& provider)
{
    etk::TraceProviderDescriptor native(
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

class EventSink;

public value struct TraceSessionInfo
{
    property long long StartTime;
    property long long PerfFreq;
    property unsigned PointerSize;
};

public value struct EventInfo
{
    property IntPtr EventRecord;
    property IntPtr TraceEventInfo;
    property IntPtr TraceEventInfoSize;
};

public ref class TraceLog : public System::IDisposable
{
public:
    delegate void EventsChangedDelegate(UIntPtr);

    TraceLog()
    {
        onEventsChangedCallback = gcnew EventsChangedDelegate(this, &TraceLog::OnEventsChanged);

        auto nativeCallback = static_cast<etk::TraceLogEventsChangedCallback*>(
            Marshal::GetFunctionPointerForDelegate(onEventsChangedCallback).ToPointer());
        auto nativeLog = etk::CreateEtwTraceLog(nativeCallback);

        this->nativeLog = nativeLog.release();
    }

    ~TraceLog() { this->!TraceLog(); }
    !TraceLog() { delete nativeLog; }

    event Action<UIntPtr>^ EventsChanged;

    property unsigned EventCount
    {
        unsigned get() { return nativeLog->GetEventCount(); }
    }

    void Clear() { nativeLog->Clear(); }

    EventInfo GetEvent(int index)
    {
        auto eventInfo = nativeLog->GetEvent(index);
        EventInfo info;
        info.EventRecord = IntPtr(eventInfo.Record());
        info.TraceEventInfo = IntPtr(eventInfo.Info());
        info.TraceEventInfoSize = IntPtr((void*)eventInfo.InfoSize());
        return info;
    }

internal:
    etk::ITraceLog* Native() { return nativeLog;  }

private:
    void OnEventsChanged(UIntPtr newCount)
    {
        EventsChanged(newCount);
    }

    EventsChangedDelegate^ onEventsChangedCallback;
    etk::ITraceLog* nativeLog;
};

public ref class TraceSession : public System::IDisposable
{
public:
    TraceSession(TraceSessionDescriptor^ descriptor);
    ~TraceSession() { this->!TraceSession(); }
    !TraceSession();

    void Start(TraceLog^ traceLog);
    void Stop();
    void Flush();
    TraceStatistics^ Query();

    TraceSessionInfo GetInfo()
    {
        if (!this->processor)
            return TraceSessionInfo();

        auto logFileHeader = this->processor->GetLogFileHeader();
        TraceSessionInfo info = {};
        info.StartTime = logFileHeader->StartTime.QuadPart;
        info.PerfFreq = logFileHeader->PerfFreq.QuadPart;
        info.PointerSize = logFileHeader->PointerSize;
        return info;
    }

private:
    TraceSessionDescriptor^ descriptor;
    std::wstring* loggerName = nullptr;
    std::vector<etk::TraceProviderDescriptor>* nativeProviders = nullptr;
    WatchDog^ watchDog;

    etk::ITraceSession* session = nullptr;
    etk::ITraceProcessor* processor = nullptr;
};

static std::wstring LoggerNameBase = L"EventTraceKit_54644792-9281-48E9-B69D-E82A86F98960";

static std::wstring CreateLoggerName()
{
    int pid = System::Diagnostics::Process::GetCurrentProcess()->Id;
    return LoggerNameBase + L"_" + std::to_wstring(pid);
}

TraceSession::TraceSession(TraceSessionDescriptor^ descriptor)
    : descriptor(descriptor)
    , loggerName(new std::wstring(CreateLoggerName()))
{
    watchDog = gcnew WatchDog(marshal_as<String^>(*loggerName));

    etk::TraceProperties properties(marshal_as<GUID>(System::Guid::NewGuid()));
    auto session = etk::CreateEtwTraceSession(*loggerName, properties);

    nativeProviders = new std::vector<etk::TraceProviderDescriptor>();
    for each (TraceProviderDescriptor^ provider in descriptor->Providers)
        nativeProviders->push_back(marshal_as<etk::TraceProviderDescriptor>(provider));

    for (auto const& provider : *nativeProviders) {
        session->AddProvider(provider);
        session->EnableProvider(provider.Id);
    }

    this->session = session.release();
}

TraceSession::!TraceSession()
{
    Stop();
    delete session;
    delete watchDog;
    delete nativeProviders;
    delete loggerName;
}

void TraceSession::Start(TraceLog^ traceLog)
{
    if (!traceLog)
        throw gcnew ArgumentNullException("traceLog");

    watchDog->Start();
    session->Start();

    auto processor = etk::CreateEtwTraceProcessor(*loggerName, *nativeProviders);
    processor->SetEventSink(traceLog->Native());

    this->processor = processor.release();
    this->processor->StartProcessing();
}

void TraceSession::Stop()
{
    if (!processor)
        return;

    processor->StopProcessing();
    session->Stop();
    watchDog->Stop();

    delete processor;
    processor = nullptr;
}

void TraceSession::Flush()
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
